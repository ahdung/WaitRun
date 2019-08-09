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
            this.btnTestAsync = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnTestWaitUIEx = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnTestWaitUI
            // 
            this.btnTestWaitUI.Location = new System.Drawing.Point(12, 12);
            this.btnTestWaitUI.Name = "btnTestWaitUI";
            this.btnTestWaitUI.Size = new System.Drawing.Size(141, 39);
            this.btnTestWaitUI.TabIndex = 0;
            this.btnTestWaitUI.Text = "WaitUI";
            this.btnTestWaitUI.UseVisualStyleBackColor = true;
            this.btnTestWaitUI.Click += new System.EventHandler(this.btnTestWaitUI_Click);
            // 
            // btnTestBackgroundWorkerUI
            // 
            this.btnTestBackgroundWorkerUI.Location = new System.Drawing.Point(362, 12);
            this.btnTestBackgroundWorkerUI.Name = "btnTestBackgroundWorkerUI";
            this.btnTestBackgroundWorkerUI.Size = new System.Drawing.Size(175, 39);
            this.btnTestBackgroundWorkerUI.TabIndex = 1;
            this.btnTestBackgroundWorkerUI.Text = "BackgroundWorkerUI";
            this.btnTestBackgroundWorkerUI.UseVisualStyleBackColor = true;
            this.btnTestBackgroundWorkerUI.Click += new System.EventHandler(this.btnTestBackgroundWorkerUI_Click);
            // 
            // btnTestAsync
            // 
            this.btnTestAsync.Location = new System.Drawing.Point(12, 240);
            this.btnTestAsync.Name = "btnTestAsync";
            this.btnTestAsync.Size = new System.Drawing.Size(120, 39);
            this.btnTestAsync.TabIndex = 2;
            this.btnTestAsync.Tag = "23";
            this.btnTestAsync.Text = "Async 23";
            this.btnTestAsync.UseVisualStyleBackColor = true;
            this.btnTestAsync.Click += new System.EventHandler(this.btnTestAsync_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(147, 240);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(120, 39);
            this.button1.TabIndex = 2;
            this.button1.Tag = "35";
            this.button1.Text = "Async 35";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.btnTestAsync_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(282, 240);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(120, 39);
            this.button2.TabIndex = 2;
            this.button2.Tag = "73";
            this.button2.Text = "Async 73";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.btnTestAsync_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(417, 240);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(120, 39);
            this.button3.TabIndex = 2;
            this.button3.Tag = "5";
            this.button3.Text = "Async 5";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.btnTestAsync_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(280, 216);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "Force";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(415, 216);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "Force";
            // 
            // btnTestWaitUIEx
            // 
            this.btnTestWaitUIEx.Location = new System.Drawing.Point(159, 12);
            this.btnTestWaitUIEx.Name = "btnTestWaitUIEx";
            this.btnTestWaitUIEx.Size = new System.Drawing.Size(141, 39);
            this.btnTestWaitUIEx.TabIndex = 0;
            this.btnTestWaitUIEx.Text = "WaitUI Ex";
            this.btnTestWaitUIEx.UseVisualStyleBackColor = true;
            this.btnTestWaitUIEx.Click += new System.EventHandler(this.btnTestWaitUIEx_Click);
            // 
            // FmTester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(549, 308);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnTestAsync);
            this.Controls.Add(this.btnTestBackgroundWorkerUI);
            this.Controls.Add(this.btnTestWaitUIEx);
            this.Controls.Add(this.btnTestWaitUI);
            this.Name = "FmTester";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Tester";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnTestWaitUI;
        private System.Windows.Forms.Button btnTestBackgroundWorkerUI;
        private System.Windows.Forms.Button btnTestAsync;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnTestWaitUIEx;
    }
}

