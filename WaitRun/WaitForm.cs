using System;
using System.Windows.Forms;

namespace AhDung.WinForm
{
    /// <summary>
    /// 等待窗体
    /// </summary>
    ///<remarks>IWaitForm的默认实现</remarks>
    public class WaitForm : Form, IWaitForm
    {
        #region Windows Form Designer generated code

        // ReSharper disable RedundantNameQualifier
        // ReSharper disable ArrangeThisQualifier

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private readonly System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lbMsg = new System.Windows.Forms.Label();
            this.bar = new System.Windows.Forms.ProgressBar();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbMsg
            // 
            this.lbMsg.Location = new System.Drawing.Point(10, 20);
            this.lbMsg.Name = "lbMsg";
            this.lbMsg.Size = new System.Drawing.Size(386, 55);
            this.lbMsg.TabIndex = 0;
            this.lbMsg.Text = "正在处理，请稍候...";
            // 
            // bar
            // 
            this.bar.Location = new System.Drawing.Point(12, 78);
            this.bar.Name = "bar";
            this.bar.Step = 1;
            this.bar.Size = new System.Drawing.Size(384, 16);
            this.bar.TabIndex = 1;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(321, 109);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // WaitForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(408, 155);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.bar);
            this.Controls.Add(this.lbMsg);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "WaitForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.Text = "请稍候...";
            this.ResumeLayout(false);
        }

        System.Windows.Forms.Label lbMsg;
        System.Windows.Forms.Button btnCancel;
        System.Windows.Forms.ProgressBar bar;

        // ReSharper restore ArrangeThisQualifier
        // ReSharper restore RedundantNameQualifier

        #endregion

        public WaitForm()
        {
            InitializeComponent();
            btnCancel.Click += (_, __) => CancellationRequested?.Invoke(this, EventArgs.Empty);
        }

        //屏蔽窗体关闭按钮
        protected override CreateParams CreateParams
        {
            get
            {
                var prms = base.CreateParams;
                prms.ClassStyle |= 0x200;
                return prms;
            }
        }

        #region 实现接口

        public event EventHandler CancellationRequested;

        public int BarMaximum
        {
            get => bar.Maximum;
            set => bar.Maximum = value;
        }

        public int BarMinimum
        {
            get => bar.Minimum;
            set => bar.Minimum = value;
        }

        public void BarPerformStep() => bar.PerformStep();

        public int BarStep
        {
            get => bar.Step;
            set => bar.Step = value;
        }

        public ProgressBarStyle BarStyle
        {
            get => bar.Style;
            set => bar.Style = value;
        }

        public int BarValue
        {
            get => bar.Value;
            set => bar.Value = value;
        }

        public bool BarVisible
        {
            get => bar.Visible;
            set => bar.Visible = value;
        }

        public bool CancelControlVisible
        {
            get => btnCancel.Visible;
            set => btnCancel.Visible = value;
        }

        public string WorkMessage
        {
            get => lbMsg.Text;
            set => lbMsg.Text = value;
        }

        #endregion
    }

    /// <summary>
    /// 等待窗体规范
    /// </summary>
    public interface IWaitForm : IDisposable
    {
        #region 用于操作等待窗体UI表现的属性和方法，实现时不用操心线程问题，让客户端（任务执行器）去操心

        /// <summary>
        /// 获取或设置进度条的值上限
        /// </summary>
        /// <remarks>建议默认值为100</remarks>
        int BarMaximum { get; set; }

        /// <summary>
        /// 获取或设置进度条的值下限
        /// </summary>
        /// <remarks>建议默认值为0</remarks>
        int BarMinimum { get; set; }

        /// <summary>
        /// 使进度条步进
        /// </summary>
        void BarPerformStep();

        /// <summary>
        /// 获取或设置进度条的步进幅度
        /// </summary>
        int BarStep { get; set; }

        /// <summary>
        /// 获取或设置进度条的动画样式
        /// </summary>
        /// <remarks>建议默认值为Marquee</remarks>
        ProgressBarStyle BarStyle { get; set; }

        /// <summary>
        /// 获取或设置进度条的值
        /// </summary>
        /// <remarks>建议默认值为0</remarks>
        int BarValue { get; set; }

        /// <summary>
        /// 获取或设置进度条的可见性
        /// </summary>
        /// <remarks>建议默认值为true</remarks>
        bool BarVisible { get; set; }

        /// <summary>
        /// 获取或设置取消任务的控件的可见性
        /// </summary>
        /// <remarks>建议默认值为false</remarks>
        bool CancelControlVisible { get; set; }

        /// <summary>
        /// 获取或设置进度描述
        /// </summary>
        /// <remarks>建议默认值为“请稍候...”之类的字眼</remarks>
        string WorkMessage { get; set; }

        #endregion

        #region Invoke相关，供客户端在跨线程操作窗体UI

        /// <summary>
        /// 指示是否需要使用Invoke操作窗体控件
        /// </summary>
        /// <remarks>建议使用Form类的默认实现</remarks>
        bool InvokeRequired { get; }

        /// <summary>
        /// 窗体Invoke方法
        /// </summary>
        /// <remarks>建议使用Form类的默认实现</remarks>
        object Invoke(Delegate method, params object[] args);

        #endregion

        /// <summary>
        /// 显示模式等待窗体
        /// </summary>
        /// <remarks>建议使用Form类的默认实现</remarks>
        DialogResult ShowDialog();

        /// <summary>
        /// 当窗体首次显示后
        /// </summary>
        event EventHandler Shown;

        /// <summary>
        /// 当用户请求取消
        /// </summary>
        event EventHandler CancellationRequested;

        // 建议使用Form类的默认实现
        // 不建议换成Hide/Visible，或直接Dispose，因为只有Close才能正确处理焦点切换
        /// <summary>
        /// 关闭窗体
        /// </summary>
        void Close();
    }
}
