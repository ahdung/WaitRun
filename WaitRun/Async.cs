using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace AhDung
{
    /// <summary>
    /// 异步运行类。适合不用阻塞后续代码，但同时又涉及UI更新的独立任务
    /// <para>- <see langword="before"/>、<see langword="after"/>分别是前置、后置动作，均运行在当前同步上下文</para>
    /// <para>- 任务会在前置动作跑完后才开始执行</para>
    /// <para>- <see langword="after"/>的入参分别代表任务是否取消、异常、结果（Action类任务没有）</para>
    /// <para>- 默认情况下，如果一个方法正在执行，再次执行相同方法会被忽略（不同方法不受影响），只有执行完才能再次执行，可用<see langword="force"/>参数强制执行</para>
    /// </summary>
    public static class Async
    {
        static readonly Type ThisType = typeof(Async);

        /// <summary>
        /// 用于存储正在执行的方法。key=方法，value=该方法的正在运行个数
        /// </summary>
        static readonly Dictionary<MethodInfo, int> _inRunningMethods = new Dictionary<MethodInfo, int>();

        /// <summary>
        /// 异步执行无参方法
        /// </summary>
        public static void Run(Action action, Action before = null, Action<bool, Exception> after = null, bool force = false)
        {
            RunDelegate(action, null, before, after == null ? null : new Action<bool, Exception, object>((c, ex, r) => after(c, ex)), force);
        }

        /// <summary>
        /// 异步执行带参方法
        /// </summary>
        public static void Run<T>(Action<T> action, T arg, Action before = null, Action<bool, Exception> after = null, bool force = false)
        {
            RunDelegate(action, new object[] { arg }, before, after == null ? null : new Action<bool, Exception, object>((c, ex, r) => after(c, ex)), force);
        }

        /// <summary>
        /// 异步执行无参方法
        /// </summary>
        public static void Run<TResult>(Func<TResult> func, Action before = null, Action<bool, Exception, TResult> after = null, bool force = false)
        {
            RunDelegate(func, null, before, after == null ? null : new Action<bool, Exception, object>((c, ex, r) => after(c, ex, (TResult)r)), force);
        }

        /// <summary>
        /// 异步执行带参方法
        /// </summary>
        public static void Run<T, TResult>(Func<T, TResult> func, T arg, Action before = null, Action<bool, Exception, TResult> after = null, bool force = false)
        {
            RunDelegate(func, new object[] { arg }, before, after == null ? null : new Action<bool, Exception, object>((c, ex, r) => after(c, ex, (TResult)r)), force);
        }

        /// <summary>
        /// 异步执行委托
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public static void RunDelegate(Delegate del, object[] args = null, Action before = null, Action<bool, Exception, object> after = null, bool force = false)
        {
            if (del == null)
            {
                throw new ArgumentNullException(nameof(del));
            }

            if (del.GetInvocationList().Length != 1)
            {
                throw new ArgumentException("委托须仅绑定 1 个方法！");
            }

            //方法重入处理。逻辑是利用容器记录正在运行的方法及其运行个数，若相同的方法正在运行，
            //则根据force决定是否继续，若继续，个数+1；方法运行完后，个数-1，若为0则从容器删除方法。
            //采用计数而不是跑完就删的原因是，由于存在强制执行的渠道，所以相同方法可能有多个正在运行，
            //先跑完的那一个如果把方法从容器删除，那么后面进来的方法会认为没有同类在跑，从而就算它是force=false进来的，
            //也能得到执行，所以要通过计数的方式来避免这种问题
            var method = del.Method;

            if (!force)
            {
                lock (ThisType)
                {
                    if (_inRunningMethods.ContainsKey(method))
                    {
                        return;
                    }
                }
            }

            lock (ThisType)
            {
                _inRunningMethods.TryGetValue(method, out var count);
                _inRunningMethods[method] = count + 1;
            }

            //跑前置
            before?.Invoke();
            var ctx = SynchronizationContext.Current;

            //跑任务
            ThreadPool.QueueUserWorkItem(_ =>
            {
                bool isCanceled = false;
                Exception exception = null;
                object result = null;
                try
                {
                    result = del.DynamicInvoke(args);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException)
                {
                    isCanceled = true;
                }
                catch (Exception ex)
                {
                    exception = (ex as TargetInvocationException)?.InnerException ?? ex;
                }
                finally
                {
                    lock (ThisType)
                    {
                        _inRunningMethods[method]--;
                        if (_inRunningMethods[method] <= 0)
                        {
                            _inRunningMethods.Remove(method);
                        }
                    }

                    //跑后置
                    if (after != null)
                    {
                        if (ctx == null)
                        {
                            after.Invoke(isCanceled, exception, result);
                        }
                        else
                        {
                            ctx.Post(__ => after.Invoke(isCanceled, exception, result), null);
                        }
                    }
                }
            });
        }
    }
}
