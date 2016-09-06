namespace ElectTracking
{
    partial class ETrack
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置 Managed 資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器
        /// 修改這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(577, 263);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "label1";
            // 
            // ETrack
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "ETrack";
            this.Size = new System.Drawing.Size(800, 300);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ETrack_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ETrack_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ETrack_KeyUp);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ETrack_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ETrack_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ETrack_MouseUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;

    }
}
