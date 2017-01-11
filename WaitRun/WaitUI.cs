using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace AhDung.WinForm
{
    /// <summary>
    /// 执行任务并显示等候窗体
    /// </summary>
    public static class WaitUI
    {
        //用于排斥WaitUI运行期间的其它调用请求
        static readonly AutoResetEvent _areForWhole = new AutoResetEvent(true);
        static bool _isRunning;   //供外部检测状态用
        static bool _isCompleted; //指示异步任务是否完成

        static SynchronizationContext _syncContext;//记录异步前的线程上下文，供异步任务中调用Post
        static IWaitForm _waitForm;  //等待窗体
        static object _result;       //任务返回结果
        static Exception _exception; //任务执行异常

        static object[] _prmsInput;          //调用者传入的参数
        static ParameterInfo[] _prmsMethod;  //任务所需的参数
        static AsyncCallback _callBackMethod;//回调方法

        /// <summary>
        /// 供用户自用的属性。每次Run后会置为null
        /// </summary>
        public static object Tag { get; set; }

        /// <summary>
        /// 指示用户是否已请求取消任务
        /// </summary>
        public static bool UserCancelling
        {
            get { return _waitForm != null && _waitForm.CancelPending; }
        }

        /// <summary>
        /// 指示任务是否已取消
        /// </summary>
        public static bool Cancelled
        {
            private get;
            set;
        }

        /// <summary>
        /// 指示WaitUI是否正在使用中
        /// </summary>
        public static bool IsBusy
        {
            get { return _isRunning; }
        }

        /// <summary>
        /// 回调方法委托（免得每次都new）
        /// </summary>
        private static AsyncCallback CallBackMethod
        {
            get { return _callBackMethod ?? (_callBackMethod = Callback); }
        }

        #region 一组操作等候窗体UI的属性/方法

        /// <summary>
        /// 获取或设置进度描述
        /// </summary>
        public static string WorkMessage
        {
            set
            {
                if (_waitForm == null) { return; }

                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.WorkMessage = value));
                    return;
                }
                _waitForm.WorkMessage = value;
            }
        }

        /// <summary>
        /// 获取或设置进度条可见性
        /// </summary>
        public static bool BarVisible
        {
            set
            {
                if (_waitForm == null) { return; }

                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.BarVisible = value));
                    return;
                }
                _waitForm.BarVisible = value;
            }
        }

        /// <summary>
        /// 获取或设置进度条动画样式
        /// </summary>
        public static ProgressBarStyle BarStyle
        {
            set
            {
                if (_waitForm == null) { return; }

                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.BarStyle = value));
                    return;
                }
                _waitForm.BarStyle = value;
            }
        }

        /// <summary>
        /// 获取或设置进度值
        /// </summary>
        public static int BarValue
        {
            get
            {
                if (_waitForm.InvokeRequired)
                {
                    return Convert.ToInt32(_waitForm.Invoke(new Func<int>(() => _waitForm.BarValue)));
                }
                return _waitForm.BarValue;
            }
            set
            {
                if (_waitForm == null) { return; }

                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.BarValue = value));
                }
                else { _waitForm.BarValue = value; }
            }
        }

        /// <summary>
        /// 获取或设置进度条步进值
        /// </summary>
        public static int BarStep
        {
            set
            {
                if (_waitForm == null) { return; }

                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.BarStep = value));
                    return;
                }
                _waitForm.BarStep = value;
            }
        }

        /// <summary>
        /// 使进度条步进
        /// </summary>
        public static void BarPerformStep()
        {
            if (_waitForm == null) { return; }

            if (_waitForm.InvokeRequired)
            {
                _waitForm.BeginInvoke(new Action(() => _waitForm.BarPerformStep()));
                return;
            }
            _waitForm.BarPerformStep();
        }

        /// <summary>
        /// 获取或设置进度条上限值
        /// </summary>
        public static int BarMaximum
        {
            set
            {
                if (_waitForm == null) { return; }

                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.BarMaximum = value));
                    return;
                }
                _waitForm.BarMaximum = value;
            }
        }

        /// <summary>
        /// 获取或设置进度条下限值
        /// </summary>
        public static int BarMinimum
        {
            set
            {
                if (_waitForm == null) { return; }

                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.BarMinimum = value));
                    return;
                }
                _waitForm.BarMinimum = value;
            }
        }

        /// <summary>
        /// 获取或设置取消任务的控件的可见性
        /// </summary>
        public static bool CancelControlVisible
        {
            set
            {
                if (_waitForm == null) { return; }

                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.CancelControlVisible = value));
                    return;
                }
                _waitForm.CancelControlVisible = value;
            }
        }

        #endregion

        #region 公共方法：无返回值+默认窗体

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction(Action method)
        {
            RunDelegate(method);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction<T>(Action<T> method, T arg)
        {
            RunDelegate(method, arg);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction<T1, T2>(Action<T1, T2> method, T1 arg1, T2 arg2)
        {
            RunDelegate(method, arg1, arg2);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3>(Action<T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3)
        {
            RunDelegate(method, arg1, arg2, arg3);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4>(Action<T1, T2, T3, T4> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            RunDelegate(method, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            RunDelegate(method, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            RunDelegate(method, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            RunDelegate(method, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            RunDelegate(method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        #endregion

        #region 公共方法：无返回值+自定义窗体

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction(IWaitForm fmWait, Action method)
        {
            RunDelegate(fmWait, method);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction<T>(IWaitForm fmWait, Action<T> method, T arg)
        {
            RunDelegate(fmWait, method, arg);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction<T1, T2>(IWaitForm fmWait, Action<T1, T2> method, T1 arg1, T2 arg2)
        {
            RunDelegate(fmWait, method, arg1, arg2);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3>(IWaitForm fmWait, Action<T1, T2, T3> method, T1 arg1, T2 arg2, T3 arg3)
        {
            RunDelegate(fmWait, method, arg1, arg2, arg3);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4>(IWaitForm fmWait, Action<T1, T2, T3, T4> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            RunDelegate(fmWait, method, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4, T5>(IWaitForm fmWait, Action<T1, T2, T3, T4, T5> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            RunDelegate(fmWait, method, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4, T5, T6>(IWaitForm fmWait, Action<T1, T2, T3, T4, T5, T6> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            RunDelegate(fmWait, method, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4, T5, T6, T7>(IWaitForm fmWait, Action<T1, T2, T3, T4, T5, T6, T7> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            RunDelegate(fmWait, method, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static void RunAction<T1, T2, T3, T4, T5, T6, T7, T8>(IWaitForm fmWait, Action<T1, T2, T3, T4, T5, T6, T7, T8> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            RunDelegate(fmWait, method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        #endregion

        #region 公共方法：有返回值+默认窗体

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<TResult>(Func<TResult> method)
        {
            return (TResult)RunDelegate(method);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<T, TResult>(Func<T, TResult> method, T arg)
        {
            return (TResult)RunDelegate(method, arg);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, TResult>(Func<T1, T2, TResult> method, T1 arg1, T2 arg2)
        {
            return (TResult)RunDelegate(method, arg1, arg2);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> method, T1 arg1, T2 arg2, T3 arg3)
        {
            return (TResult)RunDelegate(method, arg1, arg2, arg3);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return (TResult)RunDelegate(method, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return (TResult)RunDelegate(method, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return (TResult)RunDelegate(method, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            return (TResult)RunDelegate(method, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 执行方法并显示默认等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            return (TResult)RunDelegate(method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        #endregion

        #region 公共方法：有返回值+自定义窗体

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<TResult>(IWaitForm fmWait, Func<TResult> method)
        {
            return (TResult)RunDelegate(fmWait, method);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<T, TResult>(IWaitForm fmWait, Func<T, TResult> method, T arg)
        {
            return (TResult)RunDelegate(fmWait, method, arg);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, TResult>(IWaitForm fmWait, Func<T1, T2, TResult> method, T1 arg1, T2 arg2)
        {
            return (TResult)RunDelegate(fmWait, method, arg1, arg2);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, TResult>(IWaitForm fmWait, Func<T1, T2, T3, TResult> method, T1 arg1, T2 arg2, T3 arg3)
        {
            return (TResult)RunDelegate(fmWait, method, arg1, arg2, arg3);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, TResult>(IWaitForm fmWait, Func<T1, T2, T3, T4, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return (TResult)RunDelegate(fmWait, method, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, T5, TResult>(IWaitForm fmWait, Func<T1, T2, T3, T4, T5, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return (TResult)RunDelegate(fmWait, method, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, T5, T6, TResult>(IWaitForm fmWait, Func<T1, T2, T3, T4, T5, T6, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return (TResult)RunDelegate(fmWait, method, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, T5, T6, T7, TResult>(IWaitForm fmWait, Func<T1, T2, T3, T4, T5, T6, T7, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            return (TResult)RunDelegate(fmWait, method, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// 执行方法并显示自定义等候窗体
        /// </summary>
        public static TResult RunFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(IWaitForm fmWait, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> method, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            return (TResult)RunDelegate(fmWait, method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        #endregion

        /// <summary>
        /// 在原同步上下文中执行方法。若原上下文为null，则由线程池执行
        /// </summary>
        public static void Post<T>(Action<T> action, T arg)
        {
            if (_syncContext == null)
            {
                ThreadPool.QueueUserWorkItem(obj => action((T)obj), arg);
            }
            else
            {
                _syncContext.Post(obj => action((T)obj), arg);
            }
        }

        /// <summary>
        /// 执行委托并显示默认等候窗体
        /// </summary>
        public static object RunDelegate(Delegate del, params object[] args)
        {
            return RunDelegate(new WaitForm(), del, args);
        }

        /// <summary>
        /// 执行委托并显示自定义等候窗体
        /// </summary>
        public static object RunDelegate(IWaitForm fmWait, Delegate del, params object[] args)
        {
            //利用AutoResetEvent.WaitOne的原子性，防止重入
            if (!_areForWhole.WaitOne(0)) { throw new WorkIsBusyException(); }
            _isRunning = true;

            try
            {
                if (fmWait == null) { throw new ArgumentNullException("fmWait"); }
                if (del == null || del.GetInvocationList().Length != 1)
                {
                    throw new ApplicationException("委托不能为空，且只能绑定1个方法！");
                }
                if (args == null) { throw new ArgumentNullException("args"); }

                Reset();
                _waitForm = fmWait;//需在开始异步任务前赋值，因为异步中可能用到
                //更新上下文为UI线程上下文，因为如果在Main中Run，先前拿到的上下文也许时null，
                //会导致在异步完成时用Post关闭窗体抛异常
                _waitForm.Shown += (S, E) => _syncContext = SynchronizationContext.Current;

                StartAsync(del, args);

                Thread.Sleep(50); //给异步任务一点时间，如果在此时间内完成，就不弹窗
                if (!_isCompleted)
                {
                    //这里有可能出现异步先把wf关了的情况，所以要吃掉这种异常
                    try { _waitForm.ShowDialog(); }
                    catch (ObjectDisposedException) { }
                }

                //返回
                if (Cancelled) { throw new WorkCancelledException(); }
                if (_exception != null) { throw _exception; }
                return _result;
            }
            finally
            {
                Release();
                _areForWhole.Set();
                _isRunning = false;
            }
        }

        /// <summary>
        /// 开始异步任务
        /// </summary>
        private static void StartAsync(Delegate del, object[] args)
        {
            MethodInfo beginInvoke = del.GetType().GetMethod("BeginInvoke");
            object[] parmsBeginInvoke = new object[beginInvoke.GetParameters().Length];
            if (args.Length > parmsBeginInvoke.Length - 2)
            {
                throw new ArgumentException("提供的参数超过了方法所需的参数！");
            }

            _prmsMethod = del.Method.GetParameters();//假定GetParameters总是返回按参数Position排序的数组，如果将来有问题，要查验这个假设
            _prmsInput = args;

            //赋值BeginInvoke参数
            _prmsInput.CopyTo(parmsBeginInvoke, 0); //塞入传入的参数
            for (int i = _prmsInput.Length; i < _prmsMethod.Length; i++) //对未传入的参数赋予默认值
            {
                ParameterInfo p = _prmsMethod[i];
                object pVal;

                if ((pVal = p.DefaultValue) == DBNull.Value) //若参数不具有默认值则抛异常
                { throw new ArgumentException(string.Format("方法所需的参数{0}没有定义默认值，必须传入！", p.Name)); }

                parmsBeginInvoke[i] = pVal;
            }
            parmsBeginInvoke[parmsBeginInvoke.Length - 2] = CallBackMethod;//倒数第2个参数
            parmsBeginInvoke[parmsBeginInvoke.Length - 1] = del;           //倒数第1个参数

            beginInvoke.Invoke(del, parmsBeginInvoke);
        }

        /// <summary>
        /// 回调方法
        /// </summary>
        private static void Callback(IAsyncResult ar)
        {
            try
            {
                if (Cancelled) { return; } //若任务取消就不必EndInvoke了

                MethodInfo endInvoke = ar.AsyncState.GetType().GetMethod("EndInvoke");
                object[] parmsEndInvoke = new object[endInvoke.GetParameters().Length];

                if (parmsEndInvoke.Length != 1)//若方法存在ref或out参数，赋值给endInvoke参数
                {
                    int i = 0;
                    foreach (ParameterInfo p in _prmsMethod)
                    {
                        if (p.ParameterType.IsByRef) { parmsEndInvoke[i++] = _prmsInput[p.Position]; }
                    }
                }
                parmsEndInvoke[parmsEndInvoke.Length - 1] = ar;

                _result = endInvoke.Invoke(ar.AsyncState, parmsEndInvoke);

                if (parmsEndInvoke.Length != 1)//从endInvoke参数取出值返给输入参数
                {
                    int i = 0;
                    foreach (ParameterInfo p in _prmsMethod)
                    {
                        if (p.ParameterType.IsByRef) { _prmsInput[p.Position] = parmsEndInvoke[i++]; }
                    }
                }
            }
            catch (TargetInvocationException ex)
            {
                _exception = ex.InnerException;
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
            finally
            {
                _isCompleted = true;
                Thread.Sleep(300);//既然wf已显示，就让它正常显示一下，避免快闪
                Post(arg => { if (_waitForm != null) { _waitForm.Close(); } }, (object)null);
            }
        }

        /// <summary>
        /// 重置状态。在执行异步任务前调用
        /// </summary>
        private static void Reset()
        {
            Cancelled = false;
            _exception = null;
            _isCompleted = false;
            _syncContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        private static void Release()
        {
            Tag = null;
            _prmsInput = null;//这里不会影响调用者传入的object[]实例，因为不是ref进来的
            _prmsMethod = null;

            //先置null再慢慢dispose，因为方案中多处地方依赖null判断，
            //就怕判断不为空时正在销毁
            var fm = _waitForm;
            _waitForm = null;
            fm.Dispose();
        }
    }

    /// <summary>
    /// 任务正在执行
    /// </summary>
    public class WorkIsBusyException : InvalidOperationException
    {
        public WorkIsBusyException() : base("任务正在执行！") { }
    }

    /// <summary>
    /// 任务已被取消
    /// </summary>
    public class WorkCancelledException : ApplicationException
    {
        public WorkCancelledException() : base("任务已被取消！") { }
    }
}
