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

        //指定调度器避免任务或ContinueWith运行在UI线程
        static readonly Lazy<TaskFactory> _taskFactoryLazy = new Lazy<TaskFactory>(() => new TaskFactory(TaskScheduler.Default));

        /// <summary>
        /// 本类使用的TaskFactory
        /// </summary>
        static TaskFactory TaskFactory => _taskFactoryLazy.Value;

        static void RunTask(Task task) => RunTask(typeof(WaitForm), task);

        static void RunTask(Type typeofWaitForm, Task task)
        {
            if (typeofWaitForm == null)
            {
                throw new ArgumentNullException(nameof(typeofWaitForm));
            }

            if (!typeof(IWaitForm).IsAssignableFrom(typeofWaitForm))
            {
                throw new ArgumentException("typeofWaitForm必须是实现IWaitForm的类型！");
            }

            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            //不可将InitializeStates放在这里，因为与RunAction/RunFunc不同，task在进来前就可能已经得到执行（Factory.StartNew），
            //而task中可能已经修改了UI，此处再InitializeStates的话就会造成覆盖，所以为了确保顺序，
            //InitializeStates必须放置在每个Factory.StartNewq前面

            //先等候任务执行一段时间
            Thread.Sleep(ShowDelay);

            try
            {
                if (!task.IsCompleted)
                {
                    //若任务尚未完成，则创建和显示窗体，一直到显示后或发生异常才会释放锁，
                    //确保在此期间阻塞来自任务线程的ReadUI/UpdateUI
                    //任务完成后的后续操作会在窗体成功显示后才会注册，以此确保 创建 > 显示 > 关闭 的顺序
                    CreateAndShowForm(
                        typeofWaitForm,
                        () => task.ContinueWith(t => CloseForm(), TaskScheduler.FromCurrentSynchronizationContext()),
                        // ReSharper disable once AccessToDisposedClosure
                        () => _cts?.Cancel()
                    );
                }

                if (task.IsCanceled)
                {
                    throw new WorkCanceledException();
                }

                if (task.IsFaulted)
                {
                    throw task.Exception.InnerException;
                }
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        static TResult RunTask<TResult>(Task<TResult> task) => RunTask(typeof(WaitForm), task);

        static TResult RunTask<TResult>(Type typeofWaitForm, Task<TResult> task)
        {
            RunTask(typeofWaitForm, (Task)task);
            return task.Result;
        }

        #region 运行任务

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run(Func<Task> taskFunc)
        {
            ThrowIfArgumentNull(taskFunc);
            InitializeStates();
            RunTask(TaskFactory.StartNew(taskFunc).Unwrap());
        }

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run<T>(Func<T, Task> taskFunc, T arg)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            RunTask(TaskFactory.StartNew(() => taskFunc(arg)).Unwrap());
        }

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2>(Func<T1, T2, Task> taskFunc, T1 arg1, T2 arg2)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2)).Unwrap());
        }

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3>(Func<T1, T2, T3, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3)).Unwrap());
        }

        /// <summary>
        /// 运行任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4)).Unwrap());
        }

        ///// <summary>
        ///// 运行任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)).Unwrap());
        //}

        #endregion

        #region 运行任务+可取消

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run(Func<CancellationToken, Task> taskFunc)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            RunTask(TaskFactory.StartNew(() => taskFunc(_cts.Token)).Unwrap());
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T>(Func<T, CancellationToken, Task> taskFunc, T arg)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            RunTask(TaskFactory.StartNew(() => taskFunc(arg, _cts.Token)).Unwrap());
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2>(Func<T1, T2, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, _cts.Token)).Unwrap());
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3>(Func<T1, T2, T3, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, _cts.Token)).Unwrap());
        }

        /// <summary>
        /// 运行可取消的任务并使用默认等待窗体
        /// </summary>
        public static void Run<T1, T2, T3, T4>(Func<T1, T2, T3, T4, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, _cts.Token)).Unwrap());
        }

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    CanBeCanceled = true;
        //    _cts = new CancellationTokenSource();
        //    RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, _cts.Token)).Unwrap());
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    CanBeCanceled = true;
        //    _cts = new CancellationTokenSource();
        //    RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, _cts.Token)).Unwrap());
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    CanBeCanceled = true;
        //    _cts = new CancellationTokenSource();
        //    RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, _cts.Token)).Unwrap());
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, CancellationToken, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    CanBeCanceled = true;
        //    _cts = new CancellationTokenSource();
        //    RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, _cts.Token)).Unwrap());
        //}

        #endregion

        #region 运行任务+自定义窗体

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run(Type typeofWaitForm, Func<Task> taskFunc)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(typeofWaitForm, TaskFactory.StartNew(taskFunc).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T>(Type typeofWaitForm, Func<T, Task> taskFunc, T arg)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(typeofWaitForm, TaskFactory.StartNew(() => taskFunc(arg)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2>(Type typeofWaitForm, Func<T1, T2, Task> taskFunc, T1 arg1, T2 arg2)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(typeofWaitForm, TaskFactory.StartNew(() => taskFunc(arg1, arg2)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3>(Type typeofWaitForm, Func<T1, T2, T3, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(typeofWaitForm, TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用自定义等待窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4>(Type typeofWaitForm, Func<T1, T2, T3, T4, Task> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    RunTask(typeofWaitForm, TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4)).Unwrap());
        //}

        #endregion

        #region 运行任务+有返回

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<TResult>(Func<Task<TResult>> taskFunc)
        {
            ThrowIfArgumentNull(taskFunc);
            InitializeStates();
            return RunTask(TaskFactory.StartNew(taskFunc).Unwrap());
        }

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T, TResult>(Func<T, Task<TResult>> taskFunc, T arg)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            return RunTask(TaskFactory.StartNew(() => taskFunc(arg)).Unwrap());
        }

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, TResult>(Func<T1, T2, Task<TResult>> taskFunc, T1 arg1, T2 arg2)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2)).Unwrap());
        }

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(Func<T1, T2, T3, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3)).Unwrap());
        }

        /// <summary>
        /// 运行任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4)).Unwrap());
        }

        ///// <summary>
        ///// 运行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7)).Unwrap());
        //}

        ///// <summary>
        ///// 运行任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)).Unwrap());
        //}

        #endregion

        #region 运行任务+有返回+可取消

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<TResult>(Func<CancellationToken, Task<TResult>> taskFunc)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            return RunTask(TaskFactory.StartNew(() => taskFunc(_cts.Token)).Unwrap());
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T, TResult>(Func<T, CancellationToken, Task<TResult>> taskFunc, T arg)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            return RunTask(TaskFactory.StartNew(() => taskFunc(arg, _cts.Token)).Unwrap());
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, TResult>(Func<T1, T2, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, _cts.Token)).Unwrap());
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(Func<T1, T2, T3, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, _cts.Token)).Unwrap());
        }

        /// <summary>
        /// 运行可取消的任务并使用默认窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            ThrowIfArgumentNull(taskFunc, nameof(taskFunc));
            InitializeStates();
            CanBeCanceled = true;
            _cts = new CancellationTokenSource();
            return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, _cts.Token)).Unwrap());
        }

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    CanBeCanceled = true;
        //    _cts = new CancellationTokenSource();
        //    return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, _cts.Token)).Unwrap());
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    CanBeCanceled = true;
        //    _cts = new CancellationTokenSource();
        //    return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, _cts.Token)).Unwrap());
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    CanBeCanceled = true;
        //    _cts = new CancellationTokenSource();
        //    return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, _cts.Token)).Unwrap());
        //}

        ///// <summary>
        ///// 运行可取消的任务并使用默认窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, CancellationToken, Task<TResult>> taskFunc, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        //{
        //    ThrowIfNull(taskFunc, nameof(taskFunc));
        //    InitializeStates();
        //    CanBeCanceled = true;
        //    _cts = new CancellationTokenSource();
        //    return RunTask(TaskFactory.StartNew(() => taskFunc(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, _cts.Token)).Unwrap());
        //}

        #endregion
    }
}
