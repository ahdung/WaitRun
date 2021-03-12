using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace AhDung.WinForm
{
    /*
     * 该部分用于net20及公共部分，若要引入WaitEx.cs，该文件也需引入。
     * 基于节省程序集大小的考虑，部分不太会用到的入口方法RunXXX已被注释，需要用到的话按需取消注释即可
     */

    /// <summary>
    /// 执行任务并显示等候窗体。取消任务统一抛出 <see cref="OperationCanceledException"/>，而不是TaskCanceledException
    /// </summary>
    /// <remarks>
    /// 说明：<br/>
    /// - Run方法组可执行Action和Func，有传入自定义等候窗体(waitFormType)，和初始化工作消息(message)的重载<br/>
    /// - RunDelegate方法组是更底层的实现，当Run无法满足时（如参数数量超出、想初始化更多状态）可选用
    /// </remarks>
    public static partial class WaitUI
    {
        const int ShowDelay = 100; //延迟启动等待窗体的时间（毫秒），也就是说如果任务能在这个时间内跑完，就不劳驾窗体出面了
        static readonly Type ThisType = typeof(WaitUI); //本类Type，供同步锁定用
        static IWaitForm _waitForm; //等待窗体

        /// <summary>
        /// 指示用户是否已请求取消任务
        /// </summary>
        public static bool IsCancellationRequested { get; private set; }

        /// <summary>
        /// 若已请求取消就抛出任务取消异常
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        public static void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }

        #region 一组操作等候窗体UI的属性/方法

        /*
         * - 该组属性使用backing field的原因是，任务一开始访问setter时，窗体也许还未开始创
         *   建，所以要用字段把值存起，供窗体初始化用
         *
         * - 在正常使用的情况下，任务中的UI读写应该只会在同一个线程进行，唯一可能会争用的情
         *   况是任务一开始就进行UI读写，此时可能正好遇上窗体正在创建（CreateAndShowForm），
         *   创建过程中会读初始值，所以setter和创建过程需要双双上锁，保证无论哪一个先进行，
         *   另一个都不会脏读/写
         *
         * - BarValue/BarMaximum/BarMinimum这几个属性会联动，所以需要从UI取值
         *
         * - 其余属性都只会由其setter改变，所以getter只取字段就行，且不用上锁
         */

        /// <summary>
        /// 更新UI
        /// </summary>
        static void UpdateUI<T>(Action<T> action, T arg = default(T))
        {
            if (action == null || _waitForm == null)
            {
                return;
            }

            if (_waitForm.InvokeRequired)
            {
                _waitForm.Invoke(new Action<T>(v =>
                {
                    Application.DoEvents();
                    action(v);
                }), arg);

                return;
            }

            Application.DoEvents();
            action(arg);
        }

        /// <summary>
        /// 读取UI值
        /// </summary>
        static T ReadUI<T>(Func<T> func)
        {
            if (func == null || _waitForm == null)
            {
                return default(T);
            }

            if (_waitForm.InvokeRequired)
            {
                return (T)_waitForm.Invoke(func);
            }

            return func();
        }

        static int _defaultBarMaximum;

        /// <summary>
        /// 获取或设置进度条上限值
        /// </summary>
        public static int BarMaximum
        {
            get => ReadUI<int?>(() => _waitForm.BarMaximum) ?? _defaultBarMaximum;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarMaximum = value;
                UpdateUI(v => _waitForm.BarMaximum = v, value);
            }
        }

        static int _defaultBarMinimum;

        /// <summary>
        /// 获取或设置进度条下限值
        /// </summary>
        public static int BarMinimum
        {
            get => ReadUI<int?>(() => _waitForm.BarMinimum) ?? _defaultBarMinimum;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarMinimum = value;
                UpdateUI(v => _waitForm.BarMinimum = v, value);
            }
        }

        /// <summary>
        /// 使进度条步进
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void BarPerformStep() => UpdateUI<object>(_ => _waitForm.BarPerformStep());

        static int _defaultBarStep;

        /// <summary>
        /// 获取或设置进度条步进值
        /// </summary>
        public static int BarStep
        {
            get => _defaultBarStep;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarStep = value;
                UpdateUI(v => _waitForm.BarStep = v, value);
            }
        }

        static ProgressBarStyle _defaultBarStyle;

        /// <summary>
        /// 获取或设置进度条动画样式
        /// </summary>
        public static ProgressBarStyle BarStyle
        {
            get => _defaultBarStyle;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarStyle = value;
                UpdateUI(v => _waitForm.BarStyle = v, value);
            }
        }

        static int _defaultBarValue;

        /// <summary>
        /// 获取或设置进度值
        /// </summary>
        public static int BarValue
        {
            get => ReadUI<int?>(() => _waitForm.BarValue) ?? _defaultBarValue;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarValue = value;
                UpdateUI(v => _waitForm.BarValue = v, value);
            }
        }

        static bool _defaultBarVisible;

        /// <summary>
        /// 获取或设置进度条可见性
        /// </summary>
        public static bool BarVisible
        {
            get => _defaultBarVisible;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarVisible = value;
                UpdateUI(v => _waitForm.BarVisible = v, value);
            }
        }

        static bool _defaultCanBeCanceled;

        /// <summary>
        /// 获取或设置是否可取消任务
        /// </summary>
        public static bool CanBeCanceled
        {
            get => _defaultCanBeCanceled;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultCanBeCanceled = value;
                UpdateUI(v => _waitForm.CancelControlVisible = v, value);
            }
        }

        static string _defaultWorkMessage;

        /// <summary>
        /// 获取或设置进度描述   
        /// </summary>
        public static string WorkMessage
        {
            get => _defaultWorkMessage;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultWorkMessage = value;
                UpdateUI(v => _waitForm.WorkMessage = v, value);
            }
        }

        #endregion

        #region 执行任务

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static void Run(Action worker) => RunDelegate(worker);

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static void Run<T>(Action<T> worker, T arg) => RunDelegate(worker, arg);

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static void Run<T1, T2>(Action<T1, T2> worker, T1 arg1, T2 arg2) => RunDelegate(worker, arg1, arg2);

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static void Run<T1, T2, T3>(Action<T1, T2, T3> worker, T1 arg1, T2 arg2, T3 arg3) => RunDelegate(worker, arg1, arg2, arg3);

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static void Run<T1, T2, T3, T4>(Action<T1, T2, T3, T4> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunDelegate(worker, arg1, arg2, arg3, arg4);

        ///// <summary>
        ///// 执行任务并显示默认等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => RunDelegate(worker, arg1, arg2, arg3, arg4, arg5);

        ///// <summary>
        ///// 执行任务并显示默认等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => RunDelegate(worker, arg1, arg2, arg3, arg4, arg5, arg6);

        ///// <summary>
        ///// 执行任务并显示默认等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => RunDelegate(worker, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        ///// <summary>
        ///// 执行任务并显示默认等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => RunDelegate(worker, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        #endregion

        #region 执行任务+自定义窗体

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run(Type waitFormType, Action worker) => RunDelegate(waitFormType, worker);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run<T>(Type waitFormType, Action<T> worker, T arg) => RunDelegate(waitFormType, worker, arg);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run<T1, T2>(Type waitFormType, Action<T1, T2> worker, T1 arg1, T2 arg2) => RunDelegate(waitFormType, worker, arg1, arg2);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3>(Type waitFormType, Action<T1, T2, T3> worker, T1 arg1, T2 arg2, T3 arg3) => RunDelegate(waitFormType, worker, arg1, arg2, arg3);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4>(Type waitFormType, Action<T1, T2, T3, T4> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5>(Type waitFormType, Action<T1, T2, T3, T4, T5> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4, arg5);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6>(Type waitFormType, Action<T1, T2, T3, T4, T5, T6> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4, arg5, arg6);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7>(Type waitFormType, Action<T1, T2, T3, T4, T5, T6, T7> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static void Run<T1, T2, T3, T4, T5, T6, T7, T8>(Type waitFormType, Action<T1, T2, T3, T4, T5, T6, T7, T8> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        #endregion

        #region 执行任务+有返回+可设置初始工作消息

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static TResult Run<TResult>(string message, Func<TResult> worker) => (TResult)RunDelegate(() => WorkMessage = message, worker);

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static TResult Run<T, TResult>(string message, Func<T, TResult> worker, T arg) => (TResult)RunDelegate(() => WorkMessage = message, worker, arg);

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static TResult Run<T1, T2, TResult>(string message, Func<T1, T2, TResult> worker, T1 arg1, T2 arg2) => (TResult)RunDelegate(() => WorkMessage = message, worker, arg1, arg2);

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(string message, Func<T1, T2, T3, TResult> worker, T1 arg1, T2 arg2, T3 arg3) => (TResult)RunDelegate(() => WorkMessage = message, worker, arg1, arg2, arg3);

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(string message, Func<T1, T2, T3, T4, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => (TResult)RunDelegate(() => WorkMessage = message, worker, arg1, arg2, arg3, arg4);

        #endregion

        #region 执行任务+可设置初始工作消息

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static void Run(string message, Action worker) => RunDelegate(() => WorkMessage = message, worker);

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static void Run<T>(string message, Action<T> worker, T arg) => RunDelegate(() => WorkMessage = message, worker, arg);

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static void Run<T1, T2>(string message, Action<T1, T2> worker, T1 arg1, T2 arg2) => RunDelegate(() => WorkMessage = message, worker, arg1, arg2);

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static void Run<T1, T2, T3>(string message, Action<T1, T2, T3> worker, T1 arg1, T2 arg2, T3 arg3) => RunDelegate(() => WorkMessage = message, worker, arg1, arg2, arg3);

        /// <summary>
        /// 执行任务并显示默认等候窗体。可设置初始工作消息
        /// </summary>
        public static void Run<T1, T2, T3, T4>(string message, Action<T1, T2, T3, T4> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => RunDelegate(() => WorkMessage = message, worker, arg1, arg2, arg3, arg4);

        #endregion

        #region 执行任务+有返回

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static TResult Run<TResult>(Func<TResult> worker) => (TResult)RunDelegate(worker);

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static TResult Run<T, TResult>(Func<T, TResult> worker, T arg) => (TResult)RunDelegate(worker, arg);

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static TResult Run<T1, T2, TResult>(Func<T1, T2, TResult> worker, T1 arg1, T2 arg2) => (TResult)RunDelegate(worker, arg1, arg2);

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> worker, T1 arg1, T2 arg2, T3 arg3) => (TResult)RunDelegate(worker, arg1, arg2, arg3);

        /// <summary>
        /// 执行任务并显示默认等候窗体
        /// </summary>
        public static TResult Run<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => (TResult)RunDelegate(worker, arg1, arg2, arg3, arg4);

        ///// <summary>
        ///// 执行任务并显示默认等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => (TResult)RunDelegate(worker, arg1, arg2, arg3, arg4, arg5);

        ///// <summary>
        ///// 执行任务并显示默认等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => (TResult)RunDelegate(worker, arg1, arg2, arg3, arg4, arg5, arg6);

        ///// <summary>
        ///// 执行任务并显示默认等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => (TResult)RunDelegate(worker, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        ///// <summary>
        ///// 执行任务并显示默认等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => (TResult)RunDelegate(worker, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        #endregion

        #region 执行任务+有返回+自定义窗体

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<TResult>(Type waitFormType, Func<TResult> worker) => (TResult)RunDelegate(waitFormType, worker);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<T, TResult>(Type waitFormType, Func<T, TResult> worker, T arg) => (TResult)RunDelegate(waitFormType, worker, arg);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, TResult>(Type waitFormType, Func<T1, T2, TResult> worker, T1 arg1, T2 arg2) => (TResult)RunDelegate(waitFormType, worker, arg1, arg2);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, TResult>(Type waitFormType, Func<T1, T2, T3, TResult> worker, T1 arg1, T2 arg2, T3 arg3) => (TResult)RunDelegate(waitFormType, worker, arg1, arg2, arg3);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, TResult>(Type waitFormType, Func<T1, T2, T3, T4, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => (TResult)RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, TResult>(Type waitFormType, Func<T1, T2, T3, T4, T5, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => (TResult)RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4, arg5);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, TResult>(Type waitFormType, Func<T1, T2, T3, T4, T5, T6, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => (TResult)RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4, arg5, arg6);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, TResult>(Type waitFormType, Func<T1, T2, T3, T4, T5, T6, T7, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => (TResult)RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

        ///// <summary>
        ///// 执行任务并显示自定义等候窗体
        ///// </summary>
        //public static TResult Run<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Type waitFormType, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> worker, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => (TResult)RunDelegate(waitFormType, worker, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

        #endregion

        /// <summary>
        /// 执行委托并显示默认等候窗体
        /// </summary>
        public static object RunDelegate(Delegate worker, params object[] args) => RunDelegate(typeof(WaitForm), worker, args);

        /// <summary>
        /// 执行委托并显示自定义等候窗体
        /// </summary>
        public static object RunDelegate(Type waitFormType, Delegate worker, params object[] args) => RunDelegate(waitFormType, null, worker, args);

        /// <summary>
        /// 执行委托并显示默认等候窗体。可设置初始状态
        /// </summary>
        public static object RunDelegate(Action initializer, Delegate worker, params object[] args) => RunDelegate(typeof(WaitForm), initializer, worker, args);

        /// <summary>
        /// 执行委托并显示自定义等候窗体
        /// </summary>
        /// <param name="waitFormType">等候窗体类型。不可为null</param>
        /// <param name="stateInitializer">自定义状态初始化器。用于初始化等候窗体控件的值</param>
        /// <param name="worker">要执行的任务</param>
        /// <param name="args">任务方法的参数</param>
        public static object RunDelegate(Type waitFormType, Action stateInitializer, Delegate worker, params object[] args)
        {
            if (waitFormType == null)
            {
                throw new ArgumentNullException(nameof(waitFormType));
            }

            if (!typeof(IWaitForm).IsAssignableFrom(waitFormType))
            {
                throw new ArgumentException($"{nameof(waitFormType)}必须是实现{nameof(IWaitForm)}的类型！");
            }

            if (worker?.GetInvocationList().Length is not 1)
            {
                throw new ArgumentException("委托不能为空，且只能绑定1个方法！");
            }

            //执行任务前初始化状态
            InitializeStates();

            //执行自定义状态初始化器
            stateInitializer?.Invoke();

            //执行任务
            var task = SimpleTask.Run(worker, args);

            //先等候任务执行一段时间
            Thread.Sleep(ShowDelay);

            if (!task.IsCompleted)
            {
                //若任务尚未完成，则创建和显示窗体，一直到显示后或发生异常才会释放锁，
                //确保在此期间阻塞来自任务线程的UpdateUI，
                //任务完成后的后续操作会在窗体成功显示后才会注册，以此确保 创建 > 显示 > 关闭 的顺序
                CreateAndShowForm(
                    waitFormType,
                    () => task.ContinueWith(_ => CloseForm(), SynchronizationContext.Current),
                    () => IsCancellationRequested = true
                );
            }

            //获取结果时会反馈取消或出错
            return task.Result;
        }

        /// <summary>
        /// 创建并显示窗体
        /// </summary>
        /// <param name="waitFormType">等待窗体类型</param>
        /// <param name="onShown">当窗体显示后的动作</param>
        /// <param name="onCancellationRequested">当用户请求取消时的动作</param>
        static void CreateAndShowForm(Type waitFormType, Action onShown, Action onCancellationRequested)
        {
            Monitor.Enter(ThisType);
            IWaitForm form = null;
            try
            {
                form                      = (IWaitForm)Activator.CreateInstance(waitFormType);
                form.BarValue             = BarValue; //Value放前面很重要，因为任务可能先确定Max/Min，Value放后面可能触发超出范围异常
                form.BarMaximum           = BarMaximum;
                form.BarMinimum           = BarMinimum;
                form.BarStep              = BarStep;
                form.BarStyle             = BarStyle;
                form.BarVisible           = BarVisible;
                form.CancelControlVisible = CanBeCanceled;
                form.WorkMessage          = WorkMessage;

                form.CancellationRequested += (_, _) => onCancellationRequested?.Invoke();
                form.Shown += (_, _) =>
                {
                    Monitor.Exit(ThisType);
                    Application.DoEvents();
                    onShown?.Invoke();
                };

                (_waitForm = form).ShowDialog();
            }
            catch
            {
                Monitor.Exit(ThisType);
                throw;
            }
            finally
            {
                _waitForm = null;

                //必须释放，因为form是以模式打开的，Close只会隐藏。而且释放只能在这种地方，
                //放到CloseForm中的话，是由SynchronizationContext.Post跑，存在焦点问题
                form?.Dispose();
            }
        }

        //执行到该方法时，首先是说明已经Show过，然后是任务已经跑完，不会再有ReadUI/UpdateUI，可以直接调用Close，不用做多余的处理
        /// <summary>
        /// 关闭窗体
        /// </summary>
        static void CloseForm() => _waitForm.Close();

        /// <summary>
        /// 初始化若干状态字段。需在任务开始前调用
        /// </summary>
        static void InitializeStates()
        {
            IsCancellationRequested = false;

            _defaultBarMaximum    = 100;
            _defaultBarMinimum    = 0;
            _defaultBarStep       = 1;
            _defaultBarStyle      = ProgressBarStyle.Marquee;
            _defaultBarValue      = 0;
            _defaultBarVisible    = true;
            _defaultCanBeCanceled = false;
            _defaultWorkMessage   = "正在处理，请稍候...";
        }

        /// <summary>
        /// 仿TPL的简易任务类
        /// </summary>
        private class SimpleTask
        {
            readonly Delegate _worker;
            readonly object[] _args;

            SimpleTask _continuationTask;
            SynchronizationContext _continuationContext;

            bool _isStarted;
            object _result;

            /// <summary>
            /// 指示任务是否被取消
            /// </summary>
            public bool IsCanceled { get; private set; }

            /// <summary>
            /// 获取任务异常。若无异常为<see langword="null"/>
            /// </summary>
            public Exception Exception { get; private set; }

            /// <summary>
            /// 指示任务是否发生错误。为true时可从 <see cref="Exception"/> 获取异常
            /// </summary>
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public bool IsFaulted { get; private set; }

            /// <summary>
            /// 指示任务是否执行完成。取消、发生错误、正常跑完都视为完成
            /// </summary>
            public bool IsCompleted { get; private set; }

            /// <summary>
            /// 获取任务结果。若任务被取消或出错，获取结果时会抛异常
            /// </summary>
            /// <exception cref="OperationCanceledException"/>
            /// <exception cref="System.Exception"/>
            public object Result
            {
                get
                {
                    if (IsCanceled)
                    {
                        throw new OperationCanceledException();
                    }

                    if (Exception != null)
                    {
                        throw Exception;
                    }

                    return _result;
                }

                private set => _result = value;
            }

            private SimpleTask(Delegate del, object[] args = null)
            {
                _worker = del ?? throw new ArgumentNullException(nameof(del));
                _args   = args;
            }

            /// <summary>
            /// 启动任务
            /// </summary>
            public void Start(SynchronizationContext context = null)
            {
                if (_isStarted)
                {
                    throw new InvalidOperationException("任务已经启动过！");
                }

                _isStarted = true;

                if (context == null)
                {
                    ThreadPool.QueueUserWorkItem(Work);
                }
                else
                {
                    context.Post(Work, null);
                }

                void Work(object arg)
                {
                    try
                    {
                        Result = _worker.DynamicInvoke(_args);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException)
                    {
                        IsCanceled = true;
                    }
                    catch (Exception ex)
                    {
                        Exception = (ex as TargetInvocationException)?.InnerException ?? ex;
                        IsFaulted = true;
                    }
                    finally
                    {
                        lock (this)
                        {
                            IsCompleted = true;
                        }

                        _continuationTask?.Start(_continuationContext);
                    }
                }
            }

            /// <summary>
            /// 当任务完成后执行
            /// </summary>
            /// <param name="continuationAction">动作</param>
            /// <param name="context">指定同步上下文执行动作。<see langword="null"/> 则使用线程池</param>
            // ReSharper disable once UnusedMethodReturnValue.Local
            public SimpleTask ContinueWith(Action<SimpleTask> continuationAction, SynchronizationContext context = null)
            {
                if (continuationAction == null)
                {
                    throw new ArgumentNullException(nameof(continuationAction));
                }

                //如果任务已跑完，立即运行后续动作，否则把后续动作包装成一个新任务，本任务跑完时负责运行
                Monitor.Enter(this);
                if (IsCompleted)
                {
                    Monitor.Exit(this);
                    return Run(continuationAction, new object[] { this }, context);
                }

                _continuationTask    = new SimpleTask(continuationAction, new object[] { this });
                _continuationContext = context;
                Monitor.Exit(this);
                return _continuationTask;
            }

            /// <summary>
            /// 启动并返回任务
            /// </summary>
            public static SimpleTask Run(Delegate del, object[] args = null, SynchronizationContext context = null)
            {
                var task = new SimpleTask(del, args);
                task.Start(context);
                return task;
            }
        }
    }
}