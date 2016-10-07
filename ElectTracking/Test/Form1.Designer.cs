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
            this.chkSpeed = new System.Windows.Forms.CheckBox();
            this.chkError = new System.Windows.Forms.CheckBox();
            this.chkTorque = new System.Windows.Forms.CheckBox();
            this.eTracer1 = new XC.ETracer();
            this.eTracerOnTick1 = new XC.ETracerOnTick();
            this.btnFit = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.eTracer1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.eTracerOnTick1)).BeginInit();
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
            // chkSpeed
            // 
            this.chkSpeed.AutoSize = true;
            this.chkSpeed.Checked = true;
            this.chkSpeed.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSpeed.Location = new System.Drawing.Point(56, 294);
            this.chkSpeed.Name = "chkSpeed";
            this.chkSpeed.Size = new System.Drawing.Size(52, 16);
            this.chkSpeed.TabIndex = 18;
            this.chkSpeed.Text = "Speed";
            this.chkSpeed.UseVisualStyleBackColor = true;
            this.chkSpeed.CheckedChanged += new System.EventHandler(this.chk_CheckedChanged);
            // 
            // chkError
            // 
            this.chkError.AutoSize = true;
            this.chkError.Checked = true;
            this.chkError.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkError.Location = new System.Drawing.Point(56, 316);
            this.chkError.Name = "chkError";
            this.chkError.Size = new System.Drawing.Size(49, 16);
            this.chkError.TabIndex = 18;
            this.chkError.Text = "Error";
            this.chkError.UseVisualStyleBackColor = true;
            this.chkError.CheckedChanged += new System.EventHandler(this.chk_CheckedChanged);
            // 
            // chkTorque
            // 
            this.chkTorque.AutoSize = true;
            this.chkTorque.Checked = true;
            this.chkTorque.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTorque.Location = new System.Drawing.Point(56, 338);
            this.chkTorque.Name = "chkTorque";
            this.chkTorque.Size = new System.Drawing.Size(58, 16);
            this.chkTorque.TabIndex = 18;
            this.chkTorque.Text = "Torque";
            this.chkTorque.UseVisualStyleBackColor = true;
            this.chkTorque.CheckedChanged += new System.EventHandler(this.chk_CheckedChanged);
            // 
            // eTracer1
            // 
            this.eTracer1._Datum = XC.ETracer.eNumDatum.XY;
            this.eTracer1._Padding_Chart = 20;
            this.eTracer1.BackColor = System.Drawing.Color.White;
            this.eTracer1.Location = new System.Drawing.Point(13, 13);
            this.eTracer1.Name = "eTracer1";
            this.eTracer1.Size = new System.Drawing.Size(450, 450);
            this.eTracer1.TabIndex = 16;
            this.eTracer1.TabStop = false;
            // 
            // eTracerOnTick1
            // 
            this.eTracerOnTick1._VisibleType = new bool[] {
        true,
        true,
        true};
            this.eTracerOnTick1.BackColor = System.Drawing.Color.White;
            this.eTracerOnTick1.Location = new System.Drawing.Point(13, 13);
            this.eTracerOnTick1.Name = "eTracerOnTick1";
            this.eTracerOnTick1.Size = new System.Drawing.Size(900, 250);
            this.eTracerOnTick1.TabIndex = 17;
            this.eTracerOnTick1.TabStop = false;
            this.eTracerOnTick1.Visible = false;
            // 
            // btnFit
            // 
            this.btnFit.Location = new System.Drawing.Point(788, 270);
            this.btnFit.Name = "btnFit";
            this.btnFit.Size = new System.Drawing.Size(94, 54);
            this.btnFit.TabIndex = 19;
            this.btnFit.Text = "Fit";
            this.btnFit.UseVisualStyleBackColor = true;
            this.btnFit.Click += new System.EventHandler(this.btnFit_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(930, 537);
            this.Controls.Add(this.btnFit);
            this.Controls.Add(this.eTracer1);
            this.Controls.Add(this.eTracerOnTick1);
            this.Controls.Add(this.button10);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.chkTorque);
            this.Controls.Add(this.chkError);
            this.Controls.Add(this.chkSpeed);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.eTracer1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.eTracerOnTick1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button9;
        private System.Windows.Forms.Button button10;
        private XC.ETracer eTracer1;
        private XC.ETracerOnTick eTracerOnTick1;
        private System.Windows.Forms.CheckBox chkSpeed;
        private System.Windows.Forms.CheckBox chkError;
        private System.Windows.Forms.CheckBox chkTorque;
        private System.Windows.Forms.Button btnFit;
    }
}

