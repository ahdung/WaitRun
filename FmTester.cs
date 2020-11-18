using System;
using System.Threading;
using System.Threading.Tasks;
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
                var result = WaitUI.Run(() =>
                {
                    WaitUI.CanBeCanceled = true;
                    WaitUI.BarStyle = ProgressBarStyle.Blocks;
                    //Thread.Sleep(120);
                    int i = 0;
                    for (i = 0; i < 100; i++)
                    {
                        WaitUI.ThrowIfCancellationRequested();

                        WaitUI.WorkMessage = $"正在XXX，已处理 {i}...";
                        WaitUI.BarValue = i;

                        //测试异常
                        //if (i == 30)
                        //{
                        //    throw new FormatException("test");
                        //}

                        Thread.Sleep(30);
                    }

                    return i;
                });

                MessageBox.Show("任务完成。结果：" + result, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
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
                _bgwUI = new BackgroundWorkerUI
                {
                    WorkerSupportsCancellation = true,
                    WorkerReportsProgress = true
                };

                _bgwUI.DoWork += _bgwUI_DoWork;
                _bgwUI.ProgressChanged += _bgwUI_ProgressChanged;
                _bgwUI.RunWorkerCompleted += _bgwUI_RunWorkerCompleted;
            }

            _bgwUI.RunWorkerAsync();
        }

        void _bgwUI_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            int i;

            for (i = 0; i < 100; i++)
            {
                if (_bgwUI.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                _bgwUI.ReportProgress(i + 1);

                //或者直接这样也可以
                //_bgwUI.WorkMessage = $"正在XXX，已处理 {i + 1}...";
                //_bgwUI.BarValue = i + 1;

                Thread.Sleep(30);
            }

            e.Result = i;
        }

        private void _bgwUI_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            _bgwUI.WorkMessage = $"正在XXX，已处理 {e.ProgressPercentage}...";
            _bgwUI.BarValue = e.ProgressPercentage;
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

        private void btnTestAsync_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var tag = Convert.ToInt32(btn.Tag);
            var force = btn == button2 || btn == button3;

            Async.Run(arg =>
                {
                    Thread.Sleep(1000);
                    return arg;
                }, tag,
                () =>
                {
                    btn.Enabled = false;
                    btn.Text = "Processing...";
                },
                (c, ex, r) =>
                {
                    btn.Enabled = true;
                    btn.Text = $"Result：{r}";
                }
                , force);
        }

        private void btnTestWaitUIAsync_Click(object sender, EventArgs e)
        {
            try
            {
                var result = WaitUI.Run(async token =>
                {
                    WaitUI.BarStyle = ProgressBarStyle.Blocks;

                    int i;
                    for (i = 0; i < 100; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        //也可以这样取消
                        //WaitUI.ThrowIfCancellationRequested();

                        WaitUI.WorkMessage = $"正在XXX，已处理 {i}...";
                        WaitUI.BarValue = i;

                        //测试异常
                        //if (i == 30)
                        //{
                        //    throw new FormatException("test");
                        //}

                        await TaskEx.Delay(30, token);
                    }

                    return i;
                });

                MessageBox.Show("任务完成。结果：" + result, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("任务已取消", "取消", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
