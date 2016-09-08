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
            switch (tIdx)
            {
                case 1: eTracer1._Draw(@"..\ECIR-S30.DAT", @"..\CIRCULAR-S30.DAT", XC.eNumDatum.ZX); break;
                case 2: eTracer1._Draw(@"..\ECIR-CIRCLE.DAT", @"..\CIRCULAR-CIRCLE.DAT", XC.eNumDatum.ZX); break;
                //case 3: eTracer1._Draw(@"..\ECIR-CIRCLE.DAT", @"..\CIRCULAR-CIRCLE.DAT", ECDiagnose.eNumDatum.ZX); break;
                //case 4: eTracer1._Draw(@"..\ECIR-CIRCLE.DAT", @"..\CIRCULAR-CIRCLE.DAT", ECDiagnose.eNumDatum.ZX); break;
                default:
                    tIdx = 0;
                    button9_Click(this, EventArgs.Empty);
                    break;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            eTracer1._SaveImage();
        }
    }
}
