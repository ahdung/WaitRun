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
        static readonly Lazy<TaskFactory> _taskFactoryLazy = new(() => new TaskFactory(TaskScheduler.Default));

        /// <summary>
        /// 本类使用的TaskFactory
        /// </summary>
        static TaskFactory TaskFactory => _taskFactoryLazy.Value;

        static TResult RunTask<TResult>(Func<Task<TResult>> worker) => RunTask(typeof(WaitForm), null, worker);

        static TResult RunTask<TResult>(string message, Func<Task<TResult>> worker) => RunTask(typeof(WaitForm), () => WorkMessage = message, worker);

        static TResult RunTask<TResult>(Type waitFormType, Action stateInitializer, Func<Task<TResult>> worker) => RunTaskCore<TResult>(waitFormType, stateInitializer, worker);

        static void RunTask(Func<Task> worker) => RunTask(typeof(WaitForm), null, worker);

        static void RunTask(string message, Func<Task> worker) => RunTask(typeof(WaitForm), () => WorkMessage = message, worker);

        static void RunTask(Type waitFormType, Action stateInitializer, Func<Task> worker) => RunTaskCore<object>(waitFormType, stateInitializer, worker);

        static TResult RunTaskCore<TResult>(Type waitFormType, Action stateInitializer, Func<Task> worker)
        {
            if (waitFormType == null)
            {
                throw new ArgumentNullException(nameof(waitFormType));
            }

            if (!typeof(IWaitForm).IsAssignableFrom(waitFormType))
            {
                throw new ArgumentException($"{nameof(waitFormType)}必须是实现{nameof(IWaitForm)}的类型！");
            }

            if (worker == null)
            {
                throw new ArgumentNullException(nameof(worker));
            }

            //执行任务前初始化状态
            InitializeStates();

            //执行自定义状态初始化器
            stateInitializer?.Invoke();

            //若是接受取消令牌的任务，令取消控件可见
            if (_cts != null)
            {
                CanBeCanceled = true;
            }

            //执行任务
            //不直接用worker.Invoke()得到Task是考虑worker中可能会有同步阻塞代码，阻塞等候窗体弹出，
            //所以要将整个worker放到线程跑。worker有可能是Func<Task>，也可能是Func<Task<TResult>>，
            //传入StartNew的类型决定Unwrap的返回类型，所以这里必须明确类型给StartNew
            var task = worker is Func<Task<TResult>> copy
                ? TaskFactory.StartNew(copy).Unwrap()
                : TaskFactory.StartNew(worker).Unwrap();

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
                        waitFormType,
                        () => task.ContinueWith(_ => CloseForm(), TaskScheduler.FromCurrentSynchronizationContext()),
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

                if (task is Task<TResult> t)
                {
                    return t.Result;
                }

                return default;
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        #region 执行任务

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run(Func<Task> worker) => RunTask(worker);

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run<T>(Func<T, Task> worker, T arg) => RunTask(() => worker(arg));

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run<T1, T2>(Func<T1, T2, Task> worker, T1 arg1, T2 arg2) => RunTask(() => worker(arg1, arg2));

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run<T1, T2, T3>(Func<T1, T2, T3, Task> worker, T1 arg1, T2 arg2, T3 arg3) => RunTask(() => worker(arg1, arg2, arg3));

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunTask(() => worker(arg1, arg2, arg3, arg4));

        ///// <summary>
        ///// 执行任务并使用默认等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => RunTask(() => worker(arg1, arg2, arg3, arg4, arg5));

        ///// <summary>
        ///// 执行任务并使用默认等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6));

        ///// <summary>
        ///// 执行任务并使用默认等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, arg7));

        ///// <summary>
        ///// 执行任务并使用默认等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));

        #endregion

        #region 执行任务+可设置初始工作消息

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run(string message, Func<Task> worker) => RunTask(message, worker);

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run<T>(string message, Func<T, Task> worker, T arg) => RunTask(message, () => worker(arg));

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run<T1, T2>(string message, Func<T1, T2, Task> worker, T1 arg1, T2 arg2) => RunTask(message, () => worker(arg1, arg2));

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run<T1, T2, T3>(string message, Func<T1, T2, T3, Task> worker, T1 arg1, T2 arg2, T3 arg3) => RunTask(message, () => worker(arg1, arg2, arg3));

        /// <summary>
        /// 执行任务并使用默认等候窗体
        /// </summary>
        public static void Run<T1, T2, T3, T4>(string message, Func<T1, T2, T3, T4, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunTask(message, () => worker(arg1, arg2, arg3, arg4));

        #endregion

        #region 执行任务+可取消

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run(Func<CancellationToken, Task> worker)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => worker(_cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T>(Func<T, CancellationToken, Task> worker, T arg)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => worker(arg, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2>(Func<T1, T2, CancellationToken, Task> worker, T1 arg1, T2 arg2)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => worker(arg1, arg2, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3>(Func<T1, T2, T3, CancellationToken, Task> worker, T1 arg1, T2 arg2, T3 arg3)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => worker(arg1, arg2, arg3, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3, T4>(Func<T1, T2, T3, T4, CancellationToken, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            _cts = new CancellationTokenSource();
            RunTask(() => worker(arg1, arg2, arg3, arg4, _cts.Token));
        }

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, CancellationToken, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        //{
        //    _cts = new CancellationTokenSource();
        //    RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, CancellationToken, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        //{
        //    _cts = new CancellationTokenSource();
        //    RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, CancellationToken, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        //{
        //    _cts = new CancellationTokenSource();
        //    RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, arg7, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, CancellationToken, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        //{
        //    _cts = new CancellationTokenSource();
        //    RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, _cts.Token));
        //}

        #endregion

        #region 执行任务+自定义窗体

        ///// <summary>
        ///// 执行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run(Type waitFormType, Func<Task> worker) => RunTask(waitFormType, worker);

        ///// <summary>
        ///// 执行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T>(Type waitFormType, Func<T, Task> worker, T arg) => RunTask(waitFormType, () => worker(arg));

        ///// <summary>
        ///// 执行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2>(Type waitFormType, Func<T1, T2, Task> worker, T1 arg1, T2 arg2) => RunTask(waitFormType, () => worker(arg1, arg2));

        ///// <summary>
        ///// 执行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3>(Type waitFormType, Func<T1, T2, T3, Task> worker, T1 arg1, T2 arg2, T3 arg3) => RunTask(waitFormType, () => worker(arg1, arg2, arg3));

        ///// <summary>
        ///// 执行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4>(Type waitFormType, Func<T1, T2, T3, T4, Task> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunTask(waitFormType, () => worker(arg1, arg2, arg3, arg4));

        #endregion

        #region 执行任务+有返回

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<TResult>(Func<Task<TResult>> worker) => RunTask(worker);

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T, TResult>(Func<T, Task<TResult>> worker, T arg) => RunTask(() => worker(arg));

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, TResult>(Func<T1, T2, Task<TResult>> worker, T1 arg1, T2 arg2) => RunTask(() => worker(arg1, arg2));

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3) => RunTask(() => worker(arg1, arg2, arg3));

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunTask(() => worker(arg1, arg2, arg3, arg4));

        ///// <summary>
        ///// 执行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => RunTask(() => worker(arg1, arg2, arg3, arg4, arg5));

        ///// <summary>
        ///// 执行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6));

        ///// <summary>
        ///// 执行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, arg7));

        ///// <summary>
        ///// 执行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));

        #endregion

        #region 执行任务+有返回+可设置初始工作消息

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<TResult>(string message, Func<Task<TResult>> worker) => RunTask(message, worker);

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T, TResult>(string message, Func<T, Task<TResult>> worker, T arg) => RunTask(message, () => worker(arg));

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, TResult>(string message, Func<T1, T2, Task<TResult>> worker, T1 arg1, T2 arg2) => RunTask(message, () => worker(arg1, arg2));

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(string message, Func<T1, T2, T3, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3) => RunTask(message, () => worker(arg1, arg2, arg3));

        /// <summary>
        /// 执行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(string message, Func<T1, T2, T3, T4, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunTask(message, () => worker(arg1, arg2, arg3, arg4));

        #endregion

        #region 执行任务+有返回+可取消

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<TResult>(Func<CancellationToken, Task<TResult>> worker)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => worker(_cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T, TResult>(Func<T, CancellationToken, Task<TResult>> worker, T arg)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => worker(arg, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, TResult>(Func<T1, T2, CancellationToken, Task<TResult>> worker, T1 arg1, T2 arg2)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => worker(arg1, arg2, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(Func<T1, T2, T3, CancellationToken, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => worker(arg1, arg2, arg3, _cts.Token));
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, CancellationToken, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            _cts = new CancellationTokenSource();
            return RunTask(() => worker(arg1, arg2, arg3, arg4, _cts.Token));
        }

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, CancellationToken, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        //{
        //    _cts = new CancellationTokenSource();
        //    return RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, CancellationToken, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        //{
        //    _cts = new CancellationTokenSource();
        //    return RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, CancellationToken, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        //{
        //    _cts = new CancellationTokenSource();
        //    return RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, arg7, _cts.Token));
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, CancellationToken, Task<TResult>> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        //{
        //    _cts = new CancellationTokenSource();
        //    return RunTask(() => worker(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, _cts.Token));
        //}

        #endregion
    }
}