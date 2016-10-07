using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        int tIdx = 0;
        private void button9_Click(object sender, EventArgs e)
        {
            tIdx++;
            if (tIdx == 1 || tIdx == 2)
            {
                eTracer1.Visible = true;
                eTracerOnTick1.Visible = false;
                eTracer1.BringToFront();
            }
            else
            {
                eTracer1.Visible = false ;
                eTracerOnTick1.Visible = true ;
                eTracerOnTick1.BringToFront();
            }
            switch (tIdx)
            {
                case 1:
                    eTracer1._Datum = XC.ETracer.eNumDatum.ZX;
                    eTracer1._Draw(@"..\ECIR-S30.DAT", @"..\CIRCULAR-S30.DAT"); break;
                case 2:
                    eTracer1._Datum = XC.ETracer.eNumDatum.ZX;
                    eTracer1._Draw(@"..\ECIR-CIRCLE.DAT", @"..\CIRCULAR-CIRCLE.DAT"); break;
                case 3:
                    eTracerOnTick1._Draw(@"..\CIRCULAR-RIG.DAT"); break;
                //case 4: eTracer1._Draw(@"..\ECIR-CIRCLE.DAT", @"..\CIRCULAR-CIRCLE.DAT", ECDiagnose.eNumDatum.ZX); break;
                default:
                    tIdx = 0;
                    button9_Click(this, EventArgs.Empty);
                    break;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (eTracer1.Visible) 
                eTracer1._SaveImage();
            else 
                eTracerOnTick1._SaveImage();
        }

        private void chk_CheckedChanged(object sender, EventArgs e)
        {
            bool[] check = new bool[] { chkSpeed.Checked, chkError.Checked, chkTorque.Checked };
            eTracerOnTick1._VisibleType = check;
        }

        private void btnFit_Click(object sender, EventArgs e)
        {
            eTracerOnTick1._Fit();
        }
    }
}
