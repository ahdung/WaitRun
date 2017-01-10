namespace AhDung.WinForm
{
    partial class FmTester
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnTestWaitUI = new System.Windows.Forms.Button();
            this.btnTestBackgroundWorkerUI = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnTestWaitUI
            // 
            this.btnTestWaitUI.Location = new System.Drawing.Point(114, 52);
            this.btnTestWaitUI.Name = "btnTestWaitUI";
            this.btnTestWaitUI.Size = new System.Drawing.Size(175, 39);
            this.btnTestWaitUI.TabIndex = 0;
            this.btnTestWaitUI.Text = "WaitUI";
            this.btnTestWaitUI.UseVisualStyleBackColor = true;
            this.btnTestWaitUI.Click += new System.EventHandler(this.btnTestWaitUI_Click);
            // 
            // btnTestBackgroundWorkerUI
            // 
            this.btnTestBackgroundWorkerUI.Location = new System.Drawing.Point(354, 52);
            this.btnTestBackgroundWorkerUI.Name = "btnTestBackgroundWorkerUI";
            this.btnTestBackgroundWorkerUI.Size = new System.Drawing.Size(175, 39);
            this.btnTestBackgroundWorkerUI.TabIndex = 1;
            this.btnTestBackgroundWorkerUI.Text = "BackgroundWorkerUI";
            this.btnTestBackgroundWorkerUI.UseVisualStyleBackColor = true;
            this.btnTestBackgroundWorkerUI.Click += new System.EventHandler(this.btnTestBackgroundWorkerUI_Click);
            // 
            // FmTester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(642, 430);
            this.Controls.Add(this.btnTestBackgroundWorkerUI);
            this.Controls.Add(this.btnTestWaitUI);
            this.Name = "FmTester";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tester";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnTestWaitUI;
        private System.Windows.Forms.Button btnTestBackgroundWorkerUI;
    }
}

