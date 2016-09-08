namespace Test
{
    partial class Form1
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

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器
        /// 修改這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.button9 = new System.Windows.Forms.Button();
            this.button10 = new System.Windows.Forms.Button();
            this.eTracer1 = new XC.ETracer();
            ((System.ComponentModel.ISupportInitialize)(this.eTracer1)).BeginInit();
            this.SuspendLayout();
            // 
            // button9
            // 
            this.button9.Location = new System.Drawing.Point(12, 470);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(96, 55);
            this.button9.TabIndex = 12;
            this.button9.Text = "Read";
            this.button9.UseVisualStyleBackColor = true;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // button10
            // 
            this.button10.Location = new System.Drawing.Point(360, 470);
            this.button10.Name = "button10";
            this.button10.Size = new System.Drawing.Size(102, 55);
            this.button10.TabIndex = 15;
            this.button10.Text = "SavePic";
            this.button10.UseVisualStyleBackColor = true;
            this.button10.Click += new System.EventHandler(this.button10_Click);
            // 
            // eTracer1
            // 
            this.eTracer1.BackColor = System.Drawing.Color.White;
            this.eTracer1.Location = new System.Drawing.Point(12, 12);
            this.eTracer1.Name = "eTracer1";
            this.eTracer1.Size = new System.Drawing.Size(450, 450);
            this.eTracer1._Padding_Chart = 20;
            this.eTracer1.TabIndex = 11;
            this.eTracer1.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(475, 537);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.eTracer1);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.eTracer1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private XC.ETracer eTracer1;
        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
    }
}

