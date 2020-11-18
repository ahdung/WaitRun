using System;
using System.Threading;
using System.Threading.Tasks;

namespace AhDung.WinForm
{
    /*
     * 本分部文件供net40及以上目标框架用。是基于TPL实现，提供一组接受返回Task的委托的接口，
     * 换句话可以执行async方法或lambda，如：Run(async () => { })
     * 若是net20项目则不要引入该文件
     */

    public static partial class WaitUI
    {
        static CancellationTokenSource _cts;

        //指定调度器避免任务运行在UI线程
        static readonly Lazy<TaskFactory> _taskFactoryLazy = new Lazy<TaskFactory>(() => new TaskFactory(TaskScheduler.Default));
        /// <summary>
        /// 本类使用的TaskFactory
        /// </summary>
        static TaskFactory TaskFactory => _taskFactoryLazy.Value;

        static TResult RunTask<TResult>(Func<Task<TResult>> taskFunc) => RunTask(typeof(WaitForm), taskFunc);

        static TResult RunTask<TResult>(Type typeofWaitForm, Func<Task<TResult>> taskFunc) => RunTaskCore<TResult>(typeofWaitForm, taskFunc);

        static void RunTask(Func<Task> taskFunc) => RunTask(typeof(WaitForm), taskFunc);

        static void RunTask(Type typeofWaitForm, Func<Task> taskFunc) => RunTaskCore<object>(typeofWaitForm, taskFunc);

        static TResult RunTaskCore<TResult>(Type typeofWaitForm, Func<Task> taskFunc)
        {
            if (typeofWaitForm == null)
            {
                throw new ArgumentNullException(nameof(typeofWaitForm));
            }

            if (!typeof(IWaitForm).IsAssignableFrom(typeofWaitForm))
            {
                throw new ArgumentException("typeofWaitForm必须是实现IWaitForm的类型！");
            }

            if (taskFunc == null)
            {
                throw new ArgumentNullException(nameof(taskFunc));
            }

            //执行任务前初始化状态
            InitializeStates();

            //若是接受取消令牌的任务，令取消控件可见
            if (_cts != null)
            {
                CanBeCanceled = true;
            }

            //执行任务
            //taskFun有可能是Func<Task>，也可能是Func<Task<TResult>>
            var isTaskT = taskFunc.GetType().GetGenericArguments()[0].IsGenericType;
            var task = isTaskT
                ? TaskFactory.StartNew((Func<Task<TResult>>)taskFunc).Unwrap()
                : TaskFactory.StartNew(taskFunc).Unwrap();

            //先等候任务执行一段时间
            Thread.Sleep(ShowDelay);

            try
            {
                if (!task.IsCompleted)
                {
                    //若任务尚未完成，则创建和显示窗体，一直到显示后或发生异常才会释放锁，
                    //确保在此期间阻塞来自任务线程的UpdateUI
                    //任务完成后的后续操作会在窗体成功显示后才会注册，以此确保 创建 > 显示 > 关闭 的顺序
                    CreateAndShowForm(
                        typeofWaitForm,
                        () => task.ContinueWith(t => CloseForm(), TaskScheduler.FromCurrentSynchronizationContext()),
                        // ReSharper disable once AccessToDisposedClosure
                        () =>
                        {
                            //允许使用token.ThrowIfCancellationRequested和WaitUI.ThrowIfCancellationRequested
                            //两种方式中止任务，基本上前者适合Run其它类库的接受token的异步方法，如Run(token=>ExecuteNonQueryAsync(token))，
                            //后者用于Run即席lambda，如Run(async ()=>{WaitUI.ThrowIfCancellationRequested();xxx})
                            _cts?.Cancel();
                            IsCancellationRequested = true;
                        });
                }

                if (task.IsCanceled)
                {
                    throw new OperationCanceledException();
                }

                if (task.IsFaulted)
                {
                    throw task.Exception.InnerException;
                }

                if (isTaskT)
                {
                    return ((Task<TResult>)task).Result;
                }

                return default;
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        #region 运行任务

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run(Func<Task> taskFunc) => RunTask(taskFunc);

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run<T>(Func<T, Task> taskFunc, T arg) => RunTask(() => taskFunc(arg));

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2>(Func<T1, T2, Task> taskFunc, T1 arg1, T2 arg2) => RunTask(() => taskFunc(arg1, arg2));

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3>(Func<T1, T2, T3, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3) => RunTask(() => taskFunc(arg1, arg2, arg3));

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4));

        ///// <summary>
        ///// 运行任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5));

        ///// <summary>
        ///// 运行任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6));

        ///// <summary>
        ///// 运行任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7));

        ///// <summary>
        ///// 运行任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));

        #endregion

        #region 运行任务+可取消

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run(Func<CancellationToken, Task> taskFunc)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => taskFunc(_cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T>(Func<T, CancellationToken, Task> taskFunc, T arg)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => taskFunc(arg, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2>(Func<T1, T2, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => taskFunc(arg1, arg2, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3>(Func<T1, T2, T3, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => taskFunc(arg1, arg2, arg3, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3, T4>(Func<T1, T2, T3, T4, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => taskFunc(arg1, arg2, arg3, arg4, _cts.Token));
        }

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        //{
        //    _cts = new CancellationTokenSource();
        //    RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        //{
        //    _cts = new CancellationTokenSource();
        //    RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        //{
        //    _cts = new CancellationTokenSource();
        //    RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        //{
        //    _cts = new CancellationTokenSource();
        //    RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, _cts.Token));
        //}

        #endregion

        #region 运行任务+自定义窗体

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run(Type typeofWaitForm, Func<Task> taskFunc) => RunTask(typeofWaitForm, taskFunc);

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T>(Type typeofWaitForm, Func<T, Task> taskFunc, T arg) => RunTask(typeofWaitForm, () => taskFunc(arg));

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2>(Type typeofWaitForm, Func<T1, T2, Task> taskFunc, T1 arg1, T2 arg2) => RunTask(typeofWaitForm, () => taskFunc(arg1, arg2));

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3>(Type typeofWaitForm, Func<T1, T2, T3, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3) => RunTask(typeofWaitForm, () => taskFunc(arg1, arg2, arg3));

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4>(Type typeofWaitForm, Func<T1, T2, T3, T4, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunTask(typeofWaitForm, () => taskFunc(arg1, arg2, arg3, arg4));

        #endregion

        #region 运行任务+有返回

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<TResult>(Func<Task<TResult>> taskFunc) => RunTask(taskFunc);

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T, TResult>(Func<T, Task<TResult>> taskFunc, T arg) => RunTask(() => taskFunc(arg));

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, TResult>(Func<T1, T2, Task<TResult>> taskFunc, T1 arg1, T2 arg2) => RunTask(() => taskFunc(arg1, arg2));

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3) => RunTask(() => taskFunc(arg1, arg2, arg3));

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4));

        ///// <summary>
        ///// 运行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5));

        ///// <summary>
        ///// 运行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6));

        ///// <summary>
        ///// 运行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7));

        ///// <summary>
        ///// 运行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));

        #endregion

        #region 运行任务+有返回+可取消

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<TResult>(Func<CancellationToken, Task<TResult>> taskFunc)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => taskFunc(_cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T, TResult>(Func<T, CancellationToken, Task<TResult>> taskFunc, T arg)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => taskFunc(arg, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, TResult>(Func<T1, T2, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => taskFunc(arg1, arg2, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(Func<T1, T2, T3, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => taskFunc(arg1, arg2, arg3, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => taskFunc(arg1, arg2, arg3, arg4, _cts.Token));
        }

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        //{
        //    _cts = new CancellationTokenSource();
        //    return RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        //{
        //    _cts = new CancellationTokenSource();
        //    return RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        //{
        //    _cts = new CancellationTokenSource();
        //    return RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        //{
        //    _cts = new CancellationTokenSource();
        //    return RunTask(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, _cts.Token));
        //}

        #endregion
    }
}
