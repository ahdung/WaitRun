using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace AhDung.WinForm
{
    /// <summary>
    /// 带等待窗体的BackgroundWorker
    /// </summary>
    public class BackgroundWorkerUI : BackgroundWorker
    {
        const int ShowDelay = 100; //延迟启动等待窗体的时间（毫秒），也就是说如果任务能在这个时间内跑完，就不劳驾窗体出面了

        readonly Type ThisType;
        readonly Type _typeofWaitForm;
        Action _actionOnCompleted;
        IWaitForm _waitForm;
        bool _isCompleted;

        //供this.OnRunWorkerCompleted中BeginInvoke基类完成事件用
        static Control _marshalingControl;
        /// <summary>
        /// 消息控件
        /// </summary>
        static Control MarshalingControl
        {
            get
            {
                if (_marshalingControl == null)
                {
                    var type = Assembly.GetAssembly(typeof(Application)).GetType("System.Windows.Forms.Application+MarshalingControl");
                    _marshalingControl = (Control)Activator.CreateInstance(type, true);
                }

                return _marshalingControl;
            }
        }

        #region 一组操作等候窗体UI的属性/方法

        /// <summary>
        /// 更新UI
        /// </summary>
        void UpdateUI<T>(Action<T> action, T arg = default(T))
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
        T ReadUI<T>(Func<T> func)
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

        int _defaultBarMaximum;
        /// <summary>
        /// 获取或设置进度条上限值
        /// </summary>
        public int BarMaximum
        {
            get => ReadUI<int?>(() => _waitForm.BarMaximum) ?? _defaultBarMaximum;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarMaximum = value;
                UpdateUI(v => _waitForm.BarMaximum = v, value);
            }
        }

        int _defaultBarMinimum;
        /// <summary>
        /// 获取或设置进度条下限值
        /// </summary>
        public int BarMinimum
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
        public void BarPerformStep() => UpdateUI<object>(_ => _waitForm.BarPerformStep());

        int _defaultBarStep;
        /// <summary>
        /// 获取或设置进度条步进值
        /// </summary>
        public int BarStep
        {
            get => _defaultBarStep;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarStep = value;
                UpdateUI(v => _waitForm.BarStep = v, value);
            }
        }

        /// <summary>
        /// 获取或设置能否报告进度
        /// </summary>
        public new bool WorkerReportsProgress
        {
            get => base.WorkerReportsProgress;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                base.WorkerReportsProgress = value;
                UpdateUI(v => _waitForm.BarStyle = v ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee, value);
            }
        }

        int _defaultBarValue;
        /// <summary>
        /// 获取或设置进度值
        /// </summary>
        public int BarValue
        {
            get => ReadUI<int?>(() => _waitForm.BarValue) ?? _defaultBarValue;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarValue = value;
                UpdateUI(v => _waitForm.BarValue = v, value);
            }
        }

        bool _defaultBarVisible;
        /// <summary>
        /// 获取或设置进度条可见性
        /// </summary>
        public bool BarVisible
        {
            get => _defaultBarVisible;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                _defaultBarVisible = value;
                UpdateUI(v => _waitForm.BarVisible = v, value);
            }
        }

        /// <summary>
        /// 获取或设置是否支持取消
        /// </summary>
        public new bool WorkerSupportsCancellation
        {
            get => base.WorkerSupportsCancellation;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                base.WorkerSupportsCancellation = value;
                UpdateUI(v => _waitForm.CancelControlVisible = v, value);
            }
        }

        string _defaultWorkMessage;
        /// <summary>
        /// 获取或设置进度描述
        /// </summary>
        public string WorkMessage
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

        /// <summary>
        /// 初始化组件
        /// </summary>
        public BackgroundWorkerUI()
            : this(typeof(WaitForm))
        {
        }

        /// <summary>
        /// 初始化组件并指定等待窗体
        /// </summary>
        /// <param name="typeofWaitForm">等待窗体类型</param>
        public BackgroundWorkerUI(Type typeofWaitForm)
        {
            if (typeofWaitForm == null)
            {
                throw new ArgumentNullException();
            }

            if (!typeof(IWaitForm).IsAssignableFrom(typeofWaitForm))
            {
                throw new ArgumentException("typeofWaitForm必须是实现IWaitForm的类型！");
            }

            _typeofWaitForm = typeofWaitForm;
            ThisType = GetType();
        }

        /// <summary>
        /// 开始执行后台操作
        /// </summary>
        /// <param name="argument">要在DoWork事件处理程序中使用的参数</param>
        /// <remarks>通过可选参数可以同时覆盖基类无参RunWorkerAsync</remarks>
        public new void RunWorkerAsync(object argument = null)
        {
            //执行任务前初始化状态
            InitializeStates();

            base.RunWorkerAsync(argument);

            //先等候任务执行一段时间
            Thread.Sleep(ShowDelay);

            if (IsBusy)
            {
                CreateAndShowForm(_typeofWaitForm, () =>
                {
                    //这里的逻辑是，如果任务已跑完就立即关闭等待窗体，否则注册委托，this.OnRunWorkerCompleted中负责调用
                    //这里不用上锁，因为修改_isCompleted的this.OnRunWorkerCompleted也是在UI线程执行
                    if (_isCompleted)
                    {
                        CloseForm();
                    }
                    else
                    {
                        _actionOnCompleted = CloseForm;
                    }
                }, CancelAsync);
            }
        }

        /// <summary>
        /// 创建并显示窗体
        /// </summary>
        /// <param name="typeofWaitForm">等待窗体类型</param>
        /// <param name="actionOnShown">当窗体显示后的动作</param>
        /// <param name="actionOnCancellationRequested">当用户请求取消时的动作</param>
        void CreateAndShowForm(Type typeofWaitForm, Action actionOnShown, Action actionOnCancellationRequested)
        {
            Monitor.Enter(ThisType);
            IWaitForm form = null;
            try
            {
                form = (IWaitForm)Activator.CreateInstance(typeofWaitForm);
                form.BarValue = BarValue; //Value放前面很重要，因为任务可能先确定Max/Min，Value放后面可能触发超出范围异常
                form.BarMaximum = BarMaximum;
                form.BarMinimum = BarMinimum;
                form.BarStep = BarStep;
                form.BarStyle = WorkerReportsProgress ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee;
                form.BarVisible = BarVisible;
                form.CancelControlVisible = WorkerSupportsCancellation;
                form.WorkMessage = WorkMessage;

                form.CancellationRequested += (_, __) => actionOnCancellationRequested?.Invoke();
                form.Shown += (_, __) =>
                {
                    Monitor.Exit(ThisType);
                    Application.DoEvents();
                    actionOnShown?.Invoke();
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
                form?.Dispose();
            }
        }

        /// <summary>
        /// 关闭窗体
        /// </summary>
        void CloseForm() => _waitForm.Close();

        /// <summary>
        /// 初始化若干状态字段。需在任务开始前调用
        /// </summary>
        void InitializeStates()
        {
            _actionOnCompleted = null;
            _isCompleted = false;

            _defaultBarMaximum = 100;
            _defaultBarMinimum = 0;
            _defaultBarStep = 1;
            _defaultBarValue = 0;
            _defaultBarVisible = true;
            _defaultWorkMessage = "正在处理，请稍候...";
        }

        protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            _isCompleted = true;
            _actionOnCompleted?.Invoke();

            //这里必须用有句柄的控件的BeginInvoke，来跑完成事件，才能：
            // 1、避免等待窗体受完成事件中的阻塞代码影响。比如完成事件中会弹出模式窗体（如消息框）的话，那等待窗体就会等模式窗体关闭后才会关闭
            // 2、完成事件中产生的异常不会传染到等待窗体的Shown事件

            //用UI线程的同步上下文Post是不行的，这里有个很蹊跷的事情，Post内部用的也是MarshalingControl，准确来说是用的
            //Application+ThreadContext.FromCurrent().MarshalingControl，getter也是直接new MarshalingControl()，按道理跟用反射
            //实例化一个MarshalingControl并无不同，但偏偏前者就是存在1的问题，后者则不会。
            MarshalingControl.BeginInvoke(new Action<RunWorkerCompletedEventArgs>(base.OnRunWorkerCompleted), e);
        }
    }
}
