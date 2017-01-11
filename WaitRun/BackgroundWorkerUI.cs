using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AhDung.WinForm
{
    /// <summary>
    /// 带等待窗体的BackgroundWorker。报告进度用一组UI操作方法
    /// </summary>
    public class BackgroundWorkerUI : BackgroundWorker
    {
        readonly IWaitForm _waitForm;

        #region 一组操作等候窗体UI的属性/方法

        /// <summary>
        /// 获取或设置进度描述
        /// </summary>
        public string WorkMessage
        {
            set
            {
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
        public bool BarVisible
        {
            set
            {
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
        public ProgressBarStyle BarStyle
        {
            set
            {
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
        public int BarValue
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
                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.BarValue = value));
                    return;
                }
                _waitForm.BarValue = value;
            }
        }

        /// <summary>
        /// 获取或设置进度条步进值
        /// </summary>
        public int BarStep
        {
            set
            {
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
        public void BarPerformStep()
        {
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
        public int BarMaximum
        {
            set
            {
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
        public int BarMinimum
        {
            set
            {
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
        public bool CancelControlVisible
        {
            set
            {
                if (_waitForm.InvokeRequired)
                {
                    _waitForm.BeginInvoke(new Action(() => _waitForm.CancelControlVisible = value));
                    return;
                }
                _waitForm.CancelControlVisible = value;
            }
        }

        #endregion

        /// <summary>
        /// 初始化组件
        /// </summary>
        public BackgroundWorkerUI()
            : this(new WaitForm())
        { }

        /// <summary>
        /// 初始化组件并指定等待窗体
        /// </summary>
        /// <param name="fmWait">等待窗体</param>
        public BackgroundWorkerUI(IWaitForm fmWait)
        {
            if (fmWait == null)
            {
                throw new ArgumentNullException();
            }
            _waitForm = fmWait;
        }

        /// <summary>
        /// 开始执行后台操作
        /// </summary>
        /// <param name="argument">要在DoWork事件处理程序中使用的参数</param>
        /// <remarks>通过可选参数可以同时覆盖基类无参RunWorkerAsync</remarks>
        public new void RunWorkerAsync(object argument = null)
        {
            _waitForm.CancelControlVisible = this.WorkerSupportsCancellation;
            _waitForm.CancelPending = false;//考虑该方法是可能重复进入的

            base.RunWorkerAsync(argument);

            //给异步任务一点时间，如果在此时间内完成，就不弹窗。
            //不能用Sleep，因为异步任务完成是通过Post改IsBusy，
            //Sleep会把Post也卡住，完了还是先走if，失去意义
            DelayRun(50, () =>
            {
                if (IsBusy)
                {
                    //这里有可能出现先把wf关了的情况，所以要吃掉这种异常
                    try { _waitForm.ShowDialog(); }
                    catch (ObjectDisposedException) { }
                }
            });
        }

        /// <summary>
        /// 定时执行任务
        /// </summary>
        private static void DelayRun(int ms, Action method)
        {
            var t = new System.Windows.Forms.Timer { Interval = ms };
            t.Tick += (S, E) =>
            {
                t.Stop();
                GC.KeepAlive(t);
                t.Dispose();
                method();
            };
            t.Start();
        }

        protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            _waitForm.Close();
            base.OnRunWorkerCompleted(e);
        }

        /// <summary>
        /// 指示是否已请求取消任务
        /// </summary>
        public new bool CancellationPending
        {
            get
            {
                return base.CancellationPending
                    || _waitForm.CancelPending;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _waitForm.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
