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

            XYChart.PathData[] gPath = new XYChart.PathData[2];

            gPath[0] = new XYChart.PathData();
            gPath[0].PenStyle.PenColor = Color.Black;
            gPath[0].PenStyle.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
            gPath[0].PenStyle.Width = 1f;

            gPath[1] = new XYChart.PathData();
            gPath[1].PenStyle.PenColor = Color.Red;
            gPath[1].PenStyle.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            gPath[1].PenStyle.Width = 1f;

            xyChart1.PathList_Init(gPath);
        }

        private void button1_Click(object sender, EventArgs e)
        {            
            eTrack1.CaptureScreen();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            eTrack1.DrawCircle(-50, -50, 150);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            eTrack1.DrawLines(new PointF[] { 
                new PointF(20, 20),
                new PointF(20, 100) ,
                new PointF(100, 100),
                new PointF(200, 200) ,
                new PointF(1, 200),
                new PointF(300, 300) 
            });
        }

        private void button4_Click(object sender, EventArgs e)
        {
            float x = 0f, y=0f;
            Single.TryParse(textBox1.Text, out x);
            Single.TryParse(textBox2.Text, out y);
            eTrack1.DrawLine(new PointF(x, y));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            eTrack1.View_Fit();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //xyChart1.PathList[0]
            xyChart1.ChartStyle.Format_Tick = "F3";
            xyChart1.ChartStyle.Rect_ChartAreaInUserDefine = new RectangleF(0, 5.45f, 1, 0.1f);
            //xyChart1.Path_AddPoint(0, new PointF(0, 0));
            xyChart1.Path_AddPt(0, new PointF(0, 5.5f));
            xyChart1.Path_AddPt(0, new PointF(5f, 5.5f));
            xyChart1.Path_AddPt(0, new PointF(5f, 5.501f));  
            xyChart1.Path_AddPt(0, new PointF(0f, 5.501f));



            //xyChart1.ChartStyle.Rect_ChartAreaInUserDefine = new RectangleF(-30, -30, 100, 100);
            xyChart1.ChartStyle.Tick_X = 0.005f;
            xyChart1.ChartStyle.Tick_Y = 0.005f;
            
        }

        private void button7_Click(object sender, EventArgs e)
        {
            xyChart1.ChartStyle.Tick_Auto = true;
            xyChart1.Fit();
            xyChart1.Path_Name(0,"TEST");            
        }

        private void button8_Click(object sender, EventArgs e)
        {
            xyChart1.Path_AddPt(0, new PointF(-12, -2));
            xyChart1.Path_AddPt(0, new PointF(-3, -3));
            xyChart1.Path_AddPt(0, new PointF(-1, -1));
            xyChart1.Path_AddPt(0, new PointF(-12, -2));
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //eTracer1._DrawESquare(ECDiagnose.eNumDatum.ZX, Convert.ToInt32(textBox3.Text));
            //eTracer1._DrawECircle(ECDiagnose.eNumDatum.ZX, Convert.ToInt32(textBox3.Text));
            eTracer1._Draw(@"..\ECIR-S30.DAT", @"..\CIRCULAR-S30.DAT", ECDiagnose.eNumDatum.ZX);
            //eTracer1._Draw(@"..\ECIR-CIRCLE.DAT", @"..\CIRCULAR-CIRCLE.DAT", ECDiagnose.eNumDatum.ZX);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            eTracer1._SaveImage();
        }
    }
}
