using System;
using System.Threading;
using System.Windows.Forms;

namespace AhDung.WinForm
{
    public partial class FmTester : Form
    {
        public FmTester()
        {
            InitializeComponent();
        }

        private void btnTestWaitUI_Click(object sender, EventArgs e)
        {
            try
            {
                var result = WaitUI.RunFunc(() =>
                {
                    WaitUI.CancelControlVisible = true;
                    WaitUI.BarStyle = ProgressBarStyle.Blocks;

                    int i;
                    for (i = 0; i < 100; i++)
                    {
                        if (WaitUI.UserCancelling)
                        {
                            WaitUI.Cancelled = true;
                            return 0;//无所谓，只是满足Func必须有返回值
                        }
                        WaitUI.WorkMessage = string.Format("正在XXX，已处理 {0}...", i);
                        WaitUI.BarValue = i;
                        Thread.Sleep(30);
                    }
                    return i;
                });
                MessageBox.Show("任务完成。结果：" + result, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (WorkCancelledException)
            {
                MessageBox.Show("任务已取消", "取消", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        BackgroundWorkerUI _bgwUI;
        private void btnTestBackgroundWorkerUI_Click(object sender, EventArgs e)
        {
            if (_bgwUI == null)
            {
                _bgwUI = new BackgroundWorkerUI { WorkerSupportsCancellation = true };
                _bgwUI.DoWork += _bgwUI_DoWork;
                _bgwUI.RunWorkerCompleted += _bgwUI_RunWorkerCompleted;
            }
            _bgwUI.RunWorkerAsync();
        }

        void _bgwUI_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            _bgwUI.BarStyle = ProgressBarStyle.Blocks;
            int i;
            for (i = 0; i < 100; i++)
            {
                if (_bgwUI.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
                _bgwUI.WorkMessage = string.Format("正在XXX，已处理 {0}...", i);
                _bgwUI.BarValue = i;
                Thread.Sleep(30);
            }
            e.Result = i;
        }

        void _bgwUI_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.ToString(), "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (e.Cancelled)
            {
                MessageBox.Show("任务已取消", "取消", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("任务完成。结果：" + e.Result, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
