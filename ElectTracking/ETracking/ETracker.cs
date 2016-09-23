using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace XC
{
    public partial class ETracer : PictureBox
    {
        public enum eNumDatum { XY, ZX, YZ }
        enum eNumMethod { Circle, Rectangle, Diamond }     
        
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        Font gFontAxis = new Font("Arial", 14F, FontStyle.Regular);
        Font gFontComment = new Font("Arial", 8F, FontStyle.Regular);

        Pen gPenAxis = new Pen(Color.BlueViolet, 1);
        Pen gPenStandard = new Pen(Color.Green, 1);
        Pen gPenError = new Pen(Color.Red, 1);

        eNumDatum gDatum = eNumDatum.XY;
        eNumMethod gMethod = eNumMethod.Circle;

        Matrix myMatrix;
        int gPadding_Chart = 20;                        //兩側留白距離

        public ETracer()
        {
            this.SuspendLayout();
            this.Name = "ETracer";
            this.BackColor = Color.White;
            this.Size = new System.Drawing.Size(450, 450);
            this.ResumeLayout(false);
        }

        public eNumDatum _Datum
        {
            get { return gDatum; }
            set { gDatum = value; }
        }
        public int _Padding_Chart
        {
            get { return gPadding_Chart; }
            set { gPadding_Chart = value; }
        }

        public bool _Draw(string fileInit, string fileTrack)
        {
            this.Refresh();
            if (File.Exists(fileInit) != true) { MessageBox.Show(string.Format("{0} isn't Exist!", fileInit)); return false; }
            if (File.Exists(fileTrack) != true) { MessageBox.Show(string.Format("{0}  isn't Exist!", fileTrack)); return false; }

            Graphics G = this.CreateGraphics();

            //型態(0:圓 1:方 2:菱 3:剛攻) 、 圓心X 、 圓心Y 、 圓心Z 、 標準圓半徑   (um)  ==> (新)
            float[] ECIR = new float[5];
            List<float[]> listPts = new List<float[]>();

            #region  取值 (ECIR.DAT , CIRCULAR.DAT)
            #region 標準循跡繪圖
            using (StreamReader sr = new StreamReader(fileInit))
            {
                string sline = sr.ReadLine();
                if (sline == null) return false;

                string[] s = sline.Split(' ');
                if (sline.Length < 5)
                {
                    MessageBox.Show(string.Format("Format Error in {0}", fileInit));
                    return false;
                }

                for (int i = 0; i < 5; i++)                     //1(圓),X,Y,Z,R(半徑)                                
                {                                               //2(方),X,Y,Z,L(邊長)                                
                    ECIR[i] = Convert.ToSingle(s[i]);           //3(菱),X,Y,Z,L(邊長)                                
                }                                               //4(剛攻),0,0,0,0                                
                if (ECIR[0] >= Enum.GetNames(typeof(eNumMethod)).Length)
                {
                    MessageBox.Show(string.Format("Machine Type Error in {0}", fileInit));
                    return false;
                }
                gMethod = (eNumMethod)ECIR[0];
            }
            #endregion
            #region 誤差循跡取值
            using (StreamReader sr = new StreamReader(fileTrack))
            {
                string sline;
                string[] stmp;

                while (!sr.EndOfStream)
                {
                    sline = sr.ReadLine();
                    if (sline == null) continue;                //允許檔案中間有空行

                    stmp = sline.Split(' ');
                    if (stmp.Length < 5) { MessageBox.Show(string.Format("Format Error in {0}", fileTrack)); return false; }

                    float[] inf = new float[5];                 // 1(圓): X、Y、Z、dR、(x)
                    for (int i = 0; i < inf.Length; i++)        // 2(方): X、Y、Z、0、(x)
                    {                                           // 3(菱): X、Y、Z、0、(x)
                        inf[i] = Convert.ToSingle(stmp[i]);     // 4(剛攻): 命令轉速(RPM)、實際轉速(RPM)、軸向誤差(um)、主軸扭力比、軸向扭力比
                    }
                    listPts.Add(new float[] { inf[0], inf[1], inf[2], inf[3], inf[4] });
                }
            }
            #endregion
            #endregion

            // 標準圖元
            drawComment_Std(G, ECIR);                               //繪圖：標準註解
            drawPattern_Std(G, ECIR);                               //繪圖：標準圖元

            // 計算：初始設定      
            if (ECIR.Length == 5)
            {
                for (int i = 0; i < 5; i++) gDataStd[i] = ECIR[i];
                gWd = this.Width - gPadding_Chart * 2;

                if (gDataStd[0] < 0 || gDataStd[0] >= Enum.GetNames(typeof(eNumMethod)).Length) return false;
                gMethod = (eNumMethod)(int)gDataStd[0];
            }

            // 計算：並取得 [最大誤差]、[最小誤差]、[平均誤差]、[每格誤差量]          
            float[] fRet = calculate(listPts.ToArray());
            drawPattern_Err(G, gDataReal_Gdi);              //繪圖：誤差循跡
            drawComment_Err(G, fRet[0], fRet[1], fRet[2]);          //繪圖：誤差註解          
            drawAxis(G, (int)fRet[3]);                       //繪圖：標準軸線
            return true;
        }
        public void _SaveImage()
        {
            Image bmp = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            Rectangle rect = this.RectangleToScreen(this.ClientRectangle);

            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size);
            string strDateTime = string.Format("{0:yyyy/MM/dd HH:mm:ss}", DateTime.Now);
            SizeF sizeDateTime = g.MeasureString(strDateTime, gFontComment);

            g.DrawString(strDateTime, gFontComment, Brushes.Blue, this.ClientRectangle.Width - sizeDateTime.Width, this.ClientRectangle.Height - sizeDateTime.Height);


            using (SaveFileDialog file = new SaveFileDialog())
            {
                file.Filter = "Image Files (*.Png)|*.png";
                if (file.ShowDialog() == DialogResult.OK)
                {
                    bmp.Save(file.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private void drawAxis(Graphics G, int UnitDiv)         //標準座標系統  + 軸名 + 軸線 + 刻度
        {
            myMatrix = new Matrix(1, 0, 0, 1, this.Width / 2, this.Height / 2);     //畫布零點偏移
            G.Transform = myMatrix;

            string sAxis0 = "Axis0", sAxis1 = "Axis1";                  //sAxis0(水平軸)，sAxis1(垂直軸)
            switch (gDatum)
            {
                case eNumDatum.XY: sAxis0 = "X"; sAxis1 = "Y"; break;
                case eNumDatum.ZX: sAxis0 = "Z"; sAxis1 = "X"; break;
                case eNumDatum.YZ: sAxis0 = "Y"; sAxis1 = "Z"; break;
            }
            G.DrawString(sAxis0, gFontAxis, Brushes.Blue, gWd / 2, 0);                //水平軸名 
            G.DrawString(sAxis1, gFontAxis, Brushes.Blue, 0, -gWd / 2 - 12 - 4);      //垂直軸名
            G.DrawString(string.Format("+{0} um", UnitDiv), gFontComment, Brushes.Blue, 7, -gWd / 20 * 9.3f);              //單位標示

            myMatrix = new Matrix(1, 0, 0, -1, this.Width / 2, this.Height / 2);    //畫布座標系上下傾倒
            G.Transform = myMatrix;


            int wArrow = 5;                         //箭號距離  
            int startIdx = -4;                      //以標準線為準的刻痕起始點
            int endIdx = 2;                         //以標準線為準的刻痕結束點

            float div = gWd / 20;                     //每格的像素值
            float lengthStd = div * 8;              //標準線上的位置點


            float tmp = 0f;
            for (int n = 0; n < 2; n++)
            {
                if (n != 0)
                {
                    if (gMethod == eNumMethod.Rectangle) lengthStd = lengthStd * Convert.ToSingle(Math.Sqrt(2));
                    else if (gMethod == eNumMethod.Diamond) lengthStd = lengthStd / Convert.ToSingle(Math.Sqrt(2));
                }
                //刻度線 
                G.DrawLine(gPenAxis, new PointF(lengthStd, -5), new PointF(lengthStd, 5));
                for (int i = startIdx; i <= endIdx; i++)
                {
                    tmp = div * i + lengthStd;
                    G.DrawLine(gPenAxis, new PointF(tmp, -5), new PointF(tmp, 5));
                    G.DrawLine(gPenAxis, new PointF(-tmp, -5), new PointF(-tmp, 5));
                    G.DrawLine(gPenAxis, new PointF(-5, tmp), new PointF(5, tmp));
                    G.DrawLine(gPenAxis, new PointF(-5, -tmp), new PointF(5, -tmp));
                }
                //軸線
                float locStart = lengthStd + startIdx * div;
                float locEnd = lengthStd + endIdx * div;
                G.DrawLine(gPenAxis, new PointF(locStart, 0), new PointF(locEnd, 0));
                G.DrawLine(gPenAxis, new PointF(-locStart, 0), new PointF(-locEnd, 0));
                G.DrawLine(gPenAxis, new PointF(0, locStart), new PointF(0, locEnd));
                G.DrawLine(gPenAxis, new PointF(0, -locStart), new PointF(0, -locEnd));

                if (n == 0)
                {
                    //畫中線
                    G.DrawLine(gPenAxis, new PointF(locEnd, 0), new PointF(gWd / 2, 0));                //軸線(水平)
                    G.DrawLine(gPenAxis, new PointF(gWd / 2 + wArrow, 0), new PointF(gWd / 2, wArrow));   //水平箭號(上撇)
                    G.DrawLine(gPenAxis, new PointF(gWd / 2 + wArrow, 0), new PointF(gWd / 2, -wArrow));  //水平箭號(下撇)

                    G.DrawLine(gPenAxis, new PointF(0, locEnd), new PointF(0, gWd / 2));                //軸線(垂直)
                    G.DrawLine(gPenAxis, new PointF(0, gWd / 2 + wArrow), new PointF(-wArrow, gWd / 2));  //垂直箭號(左撇)
                    G.DrawLine(gPenAxis, new PointF(0, gWd / 2 + wArrow), new PointF(wArrow, gWd / 2));   //垂直箭號(右撇)
                }
                G.RotateTransform(45);
            }
        }
        private void drawComment_Std(Graphics G, float[] ECIR)
        {
            G.ResetTransform();
            string strECIR = "";
            switch (gMethod)
            {
                case eNumMethod.Circle:
                    strECIR = string.Format("Standard:\n X :  {0:F3}\n Y :  {1:F3}\n Z : {2:F3}\n R : {3:F3}", ECIR[1], ECIR[2], ECIR[3], ECIR[4]);
                    break;
                case eNumMethod.Rectangle:
                case eNumMethod.Diamond:
                    strECIR = string.Format("Standard:\n X :  {0:F3}\n Y :  {1:F3}\n Z : {2:F3}\n L : {3:F3}", ECIR[1], ECIR[2], ECIR[3], ECIR[4]);
                    break;
                default:
                    return;
            }
            SizeF sizeECIR = G.MeasureString(strECIR, gFontComment);
            G.DrawString(strECIR
                , gFontComment, SystemBrushes.ControlText, 0, this.ClientRectangle.Height - sizeECIR.Height);
        }
        private void drawComment_Err(Graphics G, float maxErr_Positve, float maxErr_Negative, float avgErr)
        {
            G.ResetTransform();
            G.DrawString(string.Format("Deviation:\nmax:  {0:F3}\nmin:  {1:F3}\navg: {2:F3}"
                , maxErr_Positve, maxErr_Negative, avgErr)
                , gFontComment, SystemBrushes.ControlText, 0, 0);
        }
        private void drawPattern_Std(Graphics G, float[] ECIR)
        {
            myMatrix = new Matrix(1, 0, 0, -1, this.Width / 2, this.Height / 2);                //畫布零點偏移
            G.Transform = myMatrix;
            float h = gWd / 1.414f;
            switch (gMethod)
            {
                case eNumMethod.Circle:
                    G.DrawEllipse(gPenStandard, -0.4F * gWd, -0.4F * gWd, 0.8F * gWd, 0.8F * gWd);      //繪製標準圓形    
                    break;
                case eNumMethod.Rectangle:
                    G.DrawRectangle(gPenStandard, -0.4F * gWd, -0.4F * gWd, 0.8F * gWd, 0.8F * gWd);    //繪製標準方形 
                    break;
                case eNumMethod.Diamond:
                    G.RotateTransform(45);
                    G.DrawRectangle(gPenStandard, -0.4F * h, -0.4F * h, 0.8F * h, 0.8F * h);    //繪製標準菱形 
                    break;
                default:
                    return;
            }
        }
        private void drawPattern_Err(Graphics G, float[,] ptXY)
        {
            PointF[] ptArray = new PointF[ptXY.GetLength(0)];
            for (int i = 0; i < ptArray.Length; i++)
            {
                ptArray[i] = new PointF(ptXY[i, 0], ptXY[i, 1]);
            }

            myMatrix = new Matrix(1, 0, 0, -1, this.Width / 2, this.Height / 2);                //畫布零點偏移
            G.Transform = myMatrix;
            G.DrawLines(gPenError, ptArray);
        }
        //===============================================================================================================
        float[] gDataStd = new float[5];        //Ecir.dat      (type,Xc,Yc,Zc,R/L/0/0)
        float[,] gDataReal_Ori;                 //把Circular.dat資料移至原點
        float[,] gDataReal_Err;                 //處理完的誤差點位              X,Y.angle
        float[,] gDataReal_Gdi;                 //人機要畫的圖點

        float delta_angle = 1.0f;               //特殊狀況時判斷的角度範圍
        float multiple = 0;
        float unitdiv = 0, scale = 0, grid_in = 8;
        float gWd = 0;                          //圖紙寬度(方形圖紙)


        private float[] calculate(float[][] ValReal)
        {
            if (gMethod == eNumMethod.Circle ||
                gMethod == eNumMethod.Rectangle ||
                gMethod == eNumMethod.Diamond)
            {
                //============= 將圖形偏移到原點 ================================
                gDataReal_Ori = new float[ValReal.GetLength(0), 2];
                for (int i = 0; i < ValReal.GetLength(0); i++)
                {
                    switch (gDatum)
                    {
                        case eNumDatum.XY:
                            gDataReal_Ori[i, 0] = ValReal[i][0] - gDataStd[1]; //x
                            gDataReal_Ori[i, 1] = ValReal[i][1] - gDataStd[2]; //y
                            break;
                        case eNumDatum.ZX:
                            gDataReal_Ori[i, 0] = ValReal[i][2] - gDataStd[3]; //z
                            gDataReal_Ori[i, 1] = ValReal[i][0] - gDataStd[1]; //x
                            break;
                        case eNumDatum.YZ:
                            gDataReal_Ori[i, 0] = ValReal[i][1] - gDataStd[2]; //y
                            gDataReal_Ori[i, 1] = ValReal[i][2] - gDataStd[3]; //z
                            break;
                    }
                }
            }
            //=====================================================================
            switch (gMethod)
            {
                case eNumMethod.Circle:
                    calError_circle(ValReal);
                    return getPlot_circle(gDataStd[4]);

                case eNumMethod.Rectangle:
                    calError_square(gDataStd[4]);
                    return getPlot_square(gDataStd[4]);

                case eNumMethod.Diamond:
                    return null;

                default:
                    return null;
            }
        }

        //==== 循圓 ====
        private void calError_circle(float[][] TrackVal)       //算圓形的點位
        {
            gDataReal_Err = new float[gDataReal_Ori.GetLength(0), 1];
            for (int i = 0; i < TrackVal.GetLength(0); i++)
            {
                gDataReal_Err[i, 0] = TrackVal[i][3];           //gDataReal_Err[i][0] = delta R
            }
        }
        private float[] getPlot_circle(float R)     //計算圖紙上的點
        {
            int i = 0;
            float max_error = 0, min_error = 99999;
            float temp = 0, avg_error = 0, sum = 0;
            float angle = 0;

            //算最大誤差 最小誤差 平均誤差
            for (i = 0; i < gDataReal_Err.GetLength(0); i++)
            {
                temp = gDataReal_Err[i, 0];
                if (max_error < temp) max_error = temp;
                if (min_error > temp) min_error = temp;
                sum = sum + temp;
            }
            avg_error = sum / gDataReal_Err.GetLength(0);

            //算一些必要的圖紙資訊
            unitdiv = max_error / (grid_in / 2);//內部可視格數只有一半  所以要/2
            unitdiv = Convert.ToSingle(Math.Ceiling(unitdiv / 10)) * 10;
            scale = R / (unitdiv * grid_in);//邊長/2
            multiple = (gWd * 0.4F) / R;

            //scale = 1f;
            //multiple = 1f;
            //unitdiv = 20;
            //scale = R / (unitdiv * 8);      //    um(標準圓單位) / um(偏差格單位) (單位格比設定時的單位縮放比率)      
            //multiple = (H * 0.4F) / R;       //   pixel/um (將標準圓放在第八格上時的倍率(um-->graph))

            //計算圖紙上的點&點誤差         
            gDataReal_Gdi = new float[gDataReal_Err.GetLength(0), 2];
            for (i = 0; i < gDataReal_Err.GetLength(0); i++)
            {
                //計算圖紙上的點誤差
                gDataReal_Gdi[i, 0] = gDataReal_Ori[i, 0] * (gDataReal_Err[i, 0] * scale + R) / (gDataReal_Err[i, 0] + R);
                gDataReal_Gdi[i, 1] = gDataReal_Ori[i, 1] * (gDataReal_Err[i, 0] * scale + R) / (gDataReal_Err[i, 0] + R); ;

                gDataReal_Gdi[i, 0] = gDataReal_Gdi[i, 0] * multiple;
                gDataReal_Gdi[i, 1] = gDataReal_Gdi[i, 1] * multiple;
            }
            return new float[] { max_error, min_error, avg_error, unitdiv };
        }

        //==== 循方 ====
        private void calError_square(float L)       //算方形的點位
        {
            float param = 0, angle = 0;
            gDataReal_Err = new float[gDataReal_Ori.GetLength(0), 3];    //Xerr,Yerr,Angle
            //利用點位的角度估算目前所在位置，狀態機作為區域切換
            //計算各點位誤差，分成9種CASE討論
            //1:往上    //2:轉彎    //3:往左    //4:轉彎    //5:往下    //6:轉彎    //7:往右    //8:轉彎
            for (int i = 0; i < gDataReal_Err.GetLength(0); i++)
            {
                if (gDataReal_Ori[i, 0] > 0)//右半邊
                {
                    if (gDataReal_Ori[i, 0] != 0)//先判斷角度
                    {
                        param = gDataReal_Ori[i, 1] / gDataReal_Ori[i, 0];
                        angle = Convert.ToSingle(Math.Atan(param) * 180 / Math.PI);//判斷範圍為-90~90
                    }
                    else
                        angle = 0;
                }
                else //左半邊
                {
                    if (gDataReal_Ori[i, 0] != 0)//先判斷角度
                    {
                        param = gDataReal_Ori[i, 1] / gDataReal_Ori[i, 0];
                        angle = Convert.ToSingle(180 + Math.Atan(param) * 180 / Math.PI);//判斷範圍為90~270
                    }
                    else
                        angle = 270;
                }
                //判斷各點的誤差量
                //在往上跟往左運行的路徑，直接把該點扣掉一半的邊長，就是誤差量  case 1,case 3
                //在往下跟往右運行的路徑，因為有負號的問題，需要把該點加回一半的邊長，才是誤差量  case 5,case 7
                //在角點的時候就要同時算兩個方向的誤差量 case 2,case 4,case 6,case 8

                if ((angle > (-45 + delta_angle)) && (angle < (45 - delta_angle)))//1
                {
                    gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] - L / 2;
                }
                else if ((angle >= (45 - delta_angle)) && (angle <= 45))//2
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] - L / 2;
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] - L / 2;
                    }
                }
                else if ((angle > 45) && (angle <= (45 + delta_angle)))//2
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] - L / 2;
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] - L / 2;
                    }
                }
                else if ((angle > (45 + delta_angle)) && (angle < (135 - delta_angle)))//3
                {
                    gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] - L / 2;
                }
                else if ((angle >= (135 - delta_angle)) && (angle <= 135))//4
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] + L / 2;
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] - L / 2;
                    }
                }
                else if ((angle > 135) && (angle <= (135 + delta_angle)))//4
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] + L / 2;
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] + L / 2;
                    }
                }
                else if ((angle > (135 + delta_angle)) && (angle < (225 - delta_angle)))//5
                {
                    gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] + L / 2;
                }
                else if ((angle >= (225 - delta_angle)) && (angle <= 225))//6
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] + L / 2;
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] + L / 2;
                    }
                }
                else if ((angle > 225) && (angle <= (225 + delta_angle)))//6
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] + L / 2;
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] + L / 2;
                    }
                }
                else if (((angle > (225 + delta_angle)) && (angle <= 270)) || ((angle >= -90) && (angle < (-45 - delta_angle))))//7
                {
                    gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] + L / 2;
                }
                else if ((angle >= (-45 - delta_angle)) && (angle <= -45))//8
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] - L / 2;
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] + L / 2;
                    }
                }
                else if ((angle > -45) && (angle <= (-45 + delta_angle)))//8
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] - L / 2;
                        gDataReal_Err[i, 1] = gDataReal_Ori[i, 1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Err[i, 0] = gDataReal_Ori[i, 0] - L / 2;
                    }
                }
                gDataReal_Err[i, 2] = angle;//紀錄該點的角度  之後才有辦法重新描繪data_point4
            }
        }
        private float[] getPlot_square(float L)     //計算圖紙上的點
        {
            int i = 0;
            float max_error = 0, min_error = 99999;
            float temp = 0, avg_error = 0, sum = 0;
            float angle = 0;

            //算最大誤差 最小誤差 平均誤差
            for (i = 0; i < gDataReal_Err.GetLength(0); i++)
            {
                temp = (float)(Math.Sqrt(gDataReal_Err[i, 0] * gDataReal_Err[i, 0] + gDataReal_Err[i, 1] * gDataReal_Err[i, 1]));
                if (max_error < temp)
                    max_error = temp;

                if (min_error > temp)
                    min_error = temp;

                sum = sum + temp;
            }
            avg_error = sum / gDataReal_Err.GetLength(0);

            //算一些必要的圖紙資訊
            unitdiv = max_error / (grid_in / 2);//內部可視格數只有一半  所以要/2
            unitdiv = Convert.ToSingle(Math.Ceiling(unitdiv / 10)) * 10;
            scale = (gDataStd[4] / 2) / (unitdiv * grid_in);//邊長/2
            multiple = (float)(gWd * 0.4) / (L / 2);

            //計算圖紙上的點&點誤差
            gDataReal_Gdi = new float[gDataReal_Err.GetLength(0), 2];
            for (i = 0; i < gDataReal_Err.GetLength(0); i++)
            {
                //計算圖紙上的點誤差
                gDataReal_Err[i, 0] = gDataReal_Err[i, 0] * scale;
                gDataReal_Err[i, 1] = gDataReal_Err[i, 1] * scale;

                angle = gDataReal_Err[i, 2];
                //計算圖紙上的點                
                if ((angle > (-45 + delta_angle)) && (angle < (45 - delta_angle)))//1
                {
                    gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] + L / 2;  //誤差量+邊長
                    gDataReal_Gdi[i, 1] = gDataReal_Ori[i, 1];       //原本的數值
                }
                else if ((angle >= (45 - delta_angle)) && (angle <= 45))//2
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] + L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] + L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Ori[i, 1];
                    }
                }
                else if ((angle >= 45) && (angle <= (45 + delta_angle)))//2
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] + L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Ori[i, 0];
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] + L / 2;
                    }
                }
                else if ((angle > (45 + delta_angle)) && (angle < (135 - delta_angle)))//3
                {
                    gDataReal_Gdi[i, 0] = gDataReal_Ori[i, 0];
                    gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] + L / 2;
                }
                else if ((angle >= (135 - delta_angle)) && (angle <= 135))//4
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] - L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Ori[i, 0];
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] + L / 2;
                    }
                }
                else if ((angle >= 135) && (angle <= (135 + delta_angle)))//4
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] - L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] - L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Ori[i, 1];
                    }
                }
                else if ((angle > (135 + delta_angle)) && (angle < (225 - delta_angle)))//5
                {
                    gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] - L / 2;
                    gDataReal_Gdi[i, 1] = gDataReal_Ori[i, 1];
                }
                else if ((angle >= (225 - delta_angle)) && (angle <= 225))//6
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] - L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] - L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Ori[i, 1];
                    }
                }
                else if ((angle >= 225) && (angle <= (225 + delta_angle)))//6
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] - L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Ori[i, 0];
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] - L / 2;
                    }
                }
                else if (((angle > (225 + delta_angle)) && (angle <= 270)) || ((angle >= -90) && (angle < (-45 - delta_angle))))//7
                {
                    gDataReal_Gdi[i, 0] = gDataReal_Ori[i, 0];
                    gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] - L / 2;
                }
                else if ((angle >= (-45 - delta_angle)) && (angle <= -45))//8
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] + L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Ori[i, 0];
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] - L / 2;
                    }
                }
                else if ((angle >= -45) && (angle <= (-45 + delta_angle)))//8
                {
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i, 1]) > L / 2)
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] + L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Err[i, 1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i, 0] = gDataReal_Err[i, 0] + L / 2;
                        gDataReal_Gdi[i, 1] = gDataReal_Ori[i, 1];
                    }
                }
                gDataReal_Gdi[i, 0] = gDataReal_Gdi[i, 0] * multiple;
                gDataReal_Gdi[i, 1] = gDataReal_Gdi[i, 1] * multiple;
            }
            return new float[] { max_error, min_error, avg_error, unitdiv };
        }
    }
    public partial class ETracerOnTick : PictureBox
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        Font gFontStr = new Font("Arial", 7F, FontStyle.Regular);
        Pen gPen_AxisLine = new Pen(Color.Gray, 1);
        Pen gPen_GridLine = new Pen(Color.Gray, 1);
        Pen[] gPens = new Pen[]{
            new Pen(Color.Black, 1), 
            new Pen(Color.Magenta, 1),
            new Pen(Color.Red, 1), 
            new Pen(Color.Blue  , 1),
            new Pen(Color.Green  , 1)};

        float[,] gData;                 //從檔案取得資料
        float[] gLimit;                 //轉速、誤差、扭力比的最大限(在80%處)
        RectangleF gRectPlot;           //畫布範圍(GDI+)
        PointF gPtOrigin;               //原點的座標位置(GDI+)
        Graphics G;
        bool[] gVisible = new bool[] { true, true, true };

        public ETracerOnTick()
        {
            this.SuspendLayout();
            this.Name = "ETracerOnTick";
            this.BackColor = Color.White;
            this.Size = new System.Drawing.Size(900, 250);
            this.ResumeLayout(false);
            gPen_GridLine.DashStyle = DashStyle.Dot;
            gPen_GridLine.DashPattern = new float[] { 2, 4 };
        }
        public bool _Draw(string fileTrack)
        {
            this.Refresh();
            if (File.Exists(fileTrack) != true) { MessageBox.Show(string.Format("{0}  isn't Exist!", fileTrack)); return false; }

            G = this.CreateGraphics();
            List<float[]> listPts = new List<float[]>();

            #region  取值 (CIRCULAR.DAT)
            using (StreamReader sr = new StreamReader(fileTrack))
            {
                string sline;
                string[] stmp;

                while (!sr.EndOfStream)
                {
                    sline = sr.ReadLine();
                    if (sline == null) continue;                //允許檔案中間有空行

                    stmp = sline.Split(' ');
                    if (stmp.Length < 5) { MessageBox.Show(string.Format("Format Error in {0}", fileTrack)); return false; }

                    float[] inf = new float[5];
                    for (int i = 0; i < inf.Length; i++)
                    {
                        inf[i] = Convert.ToSingle(stmp[i]);     // 命令轉速(RPM)、實際轉速(RPM)、軸向誤差(um)、主軸扭力比、軸向扭力比
                    }
                    listPts.Add(new float[] { inf[0], inf[1], inf[2], inf[3], inf[4] });
                }
            }
            #endregion
            float[][] ValReal = listPts.ToArray();          //Circular.dat資料               
            gData = new float[ValReal.GetLength(0), 5];
            for (int i = 0; i < ValReal.GetLength(0); i++)
            {
                gData[i, 0] = ValReal[i][0];        //命令轉速(RPM)
                gData[i, 1] = ValReal[i][1];        //實際轉速(RPM)
                gData[i, 2] = ValReal[i][2];        //軸向誤差(um)
                gData[i, 3] = ValReal[i][3];        //主軸扭力比
                gData[i, 4] = ValReal[i][4];        //軸向扭力比
            }

            // 計算：初始設定                 
            getLimit();
            drawAxis();                 //繪圖：標準軸線
            drawData();
            return true;
        }
        public void _SaveImage()
        {
            Image bmp = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            Rectangle rect = this.RectangleToScreen(this.ClientRectangle);

            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size);
            string strDateTime = string.Format("{0:yyyy/MM/dd HH:mm:ss}", DateTime.Now);
            SizeF sizeDateTime = g.MeasureString(strDateTime, gFontStr);

            g.DrawString(strDateTime, gFontStr, Brushes.Blue, this.ClientRectangle.Width - sizeDateTime.Width, this.ClientRectangle.Height - sizeDateTime.Height);


            using (SaveFileDialog file = new SaveFileDialog())
            {
                file.Filter = "Image Files (*.Png)|*.png";
                if (file.ShowDialog() == DialogResult.OK)
                {
                    bmp.Save(file.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }
        public bool[] _VisibleType
        {
            get { return gVisible; }
            set
            {
                if (G == null) return;
                if (value.Length != gVisible.Length) return;
                for (int i = 0; i < gVisible.Length; i++)
                {
                    if (gVisible[i] != value[i])
                    {
                        gVisible = value;
                        drawAxis();                 //繪圖：標準軸線
                        drawData();
                        break;
                    }
                }
            }
        }

        private void getLimit()     //取得各數數的最大值、最小值
        {
            float[,] limit = new float[3, 2];           //row:轉速,誤差,扭力比   col:min,max
            gLimit = new float[3];              //取絕對值後的最大值 (0~2:轉速、誤差、扭力比)

            //最小值      //最大值
            limit[0, 0] = limit[0, 1] = gData[0, 0];    //初始轉速  (index: 0,1)
            limit[1, 0] = limit[1, 1] = gData[0, 2];    //初始誤差  (index: 2)
            limit[2, 0] = limit[2, 1] = gData[0, 3];    //初始扭力比(index: 3,4)            
            for (int i = 0; i < gData.GetLength(0); i++)
            {
                //轉速
                if (gData[i, 0] < limit[0, 0]) limit[0, 0] = gData[i, 0];   //最小轉速
                if (gData[i, 1] < limit[0, 0]) limit[0, 0] = gData[i, 1];
                if (gData[i, 0] > limit[0, 1]) limit[0, 1] = gData[i, 0];   //最大轉速
                if (gData[i, 1] > limit[0, 1]) limit[0, 1] = gData[i, 1];

                //軸向誤差
                if (gData[i, 2] < limit[1, 0]) limit[1, 0] = gData[i, 2];   //最小誤差
                if (gData[i, 2] > limit[1, 1]) limit[1, 1] = gData[i, 2];   //最大誤差

                //扭力比
                if (gData[i, 3] < limit[2, 0]) limit[2, 0] = gData[i, 3];   //最小扭力比
                if (gData[i, 4] < limit[2, 0]) limit[2, 0] = gData[i, 4];
                if (gData[i, 3] > limit[2, 1]) limit[2, 1] = gData[i, 3];   //最大扭力比
                if (gData[i, 4] > limit[2, 1]) limit[2, 1] = gData[i, 4];
            }
            for (int i = 0; i < 3; i++)
            {
                gLimit[i] = Math.Max(Math.Abs(limit[i, 0]), Math.Abs(limit[i, 1]));
                //========== 最大值在80% ===
                gLimit[i] = gLimit[i] * 10 / 8;
                //========== 去除個位數 ===
                gLimit[i] = (float)(Math.Ceiling(gLimit[i] / 10) * 10);
            }
        }
        private void drawAxis()         //標準座標系統  + 軸名 + 軸線 + 刻度
        {
            G.Clear(this.BackColor);
            G.ResetTransform();

            string[] strTitle = new string[] { "Act.Speed(RPM)", "Cmd.Speed(RPM)", "Error(um)", "S1.Torgue(%)", "S2.Torgue(%)" };
            SizeF[] sizTitle = new SizeF[5];
            for (int i = 0; i < 5; i++) sizTitle[i] = G.MeasureString(strTitle[i], gFontStr);

            float fWid = this.Width;
            for (int i = 0; i < 5; i++) fWid -= sizTitle[i].Width;
            fWid = fWid / 4;                    //標題間的平均間距

            float tmp = 0;
            G.DrawString(strTitle[0], gFontStr, new SolidBrush(gPens[0].Color), 0, 0);              //軸名:Act.Speed  
            for (int i = 1; i < 5; i++)
            {
                tmp += sizTitle[i - 1].Width + fWid;
                G.DrawString(strTitle[i], gFontStr, new SolidBrush(gPens[i].Color), tmp, 0);
            }
            //=====================================================================================================================
            SizeF sizLabel1 = G.MeasureString(string.Format(" {0} ", (int)gLimit[0]), gFontStr);    //標籤尺寸：左
            SizeF sizLabel2 = G.MeasureString(string.Format(" {0} ", (int)gLimit[1]), gFontStr);    //標籤尺寸：右1
            SizeF sizLabel3 = G.MeasureString(string.Format(" {0} ", (int)gLimit[2]), gFontStr);    //標籤尺寸：右2
            gRectPlot = new RectangleF(                                                           //圖形範圍
                new PointF(sizLabel1.Width, 2 * sizLabel1.Height),
                new SizeF(this.Width - (sizLabel1.Width + sizLabel2.Width + sizLabel3.Width), this.Height - 4 * sizLabel1.Height)
                );
            gPtOrigin = new PointF(gRectPlot.Left, (gRectPlot.Top + gRectPlot.Bottom) / 2);                         //原點位置
            SizeF sizeStr = G.MeasureString("tick", gFontStr);
            G.DrawString("tick", gFontStr, new SolidBrush(gPen_GridLine.Color), (this.Width - sizeStr.Width) / 2, gRectPlot.Bottom + sizeStr.Height);
            //=====================================================================================================================
            G.DrawRectangle(gPen_AxisLine, gRectPlot.Left, gRectPlot.Top, gRectPlot.Width, gRectPlot.Height);              //外框線  (畫布)
            G.DrawLine(gPen_AxisLine, gRectPlot.Left, gPtOrigin.Y, gRectPlot.Right, gPtOrigin.Y);                        //水平軸線(畫布)

            tmp = gRectPlot.Height / 10;                                                              //垂直間距(畫布)
            for (int i = 1; i < 6; i++)
            {
                if (i < 5)
                {
                    G.DrawLine(gPen_GridLine, gRectPlot.Left, gPtOrigin.Y - tmp * i, gRectPlot.Right, gPtOrigin.Y - tmp * i);     //格線(軸線向上)
                    G.DrawLine(gPen_GridLine, gRectPlot.Left, gPtOrigin.Y + tmp * i, gRectPlot.Right, gPtOrigin.Y + tmp * i);     //格線(軸線向下)
                }
                else
                {   //標籤文字：0  
                    G.DrawString(string.Format(" {0}", 0), gFontStr, new SolidBrush(gPens[0].Color), 0, gPtOrigin.Y - sizeStr.Height / 2);
                    G.DrawString(string.Format(" {0}", 0), gFontStr, new SolidBrush(gPens[2].Color), gRectPlot.Right, gPtOrigin.Y - sizeStr.Height / 2);
                    G.DrawString(string.Format(" {0}", 0), gFontStr, new SolidBrush(gPens[3].Color), gRectPlot.Right + sizLabel2.Width, gPtOrigin.Y - sizeStr.Height / 2);
                }
                //標籤文字：Label-1
                G.DrawString(string.Format(" {0}", (int)(gLimit[0] / 5 * i)), gFontStr, new SolidBrush(gPens[0].Color), 0, gPtOrigin.Y - sizeStr.Height / 2 - tmp * i);
                G.DrawString(string.Format("-{0}", (int)(gLimit[0] / 5 * i)), gFontStr, new SolidBrush(gPens[0].Color), 0, gPtOrigin.Y - sizeStr.Height / 2 + tmp * i);

                //標籤文字：Label-2
                G.DrawString(string.Format(" {0}", (int)(gLimit[1] / 5 * i)), gFontStr, new SolidBrush(gPens[2].Color), gRectPlot.Right, gPtOrigin.Y - sizeStr.Height / 2 - tmp * i);
                G.DrawString(string.Format("-{0}", (int)(gLimit[1] / 5 * i)), gFontStr, new SolidBrush(gPens[2].Color), gRectPlot.Right, gPtOrigin.Y - sizeStr.Height / 2 + tmp * i);

                //標籤文字：Label-3
                G.DrawString(string.Format(" {0}", (int)(gLimit[2] / 5 * i)), gFontStr, new SolidBrush(gPens[3].Color), gRectPlot.Right + sizLabel2.Width, gPtOrigin.Y - sizeStr.Height / 2 - tmp * i);
                G.DrawString(string.Format("-{0}", (int)(gLimit[2] / 5 * i)), gFontStr, new SolidBrush(gPens[3].Color), gRectPlot.Right + sizLabel2.Width, gPtOrigin.Y - sizeStr.Height / 2 + tmp * i);
            }

            int Count = gData.GetLength(0);
            int iDiv = Count / 10;                                                                      //格線：水平方向刻劃間隔tick數
            tmp = gRectPlot.Width / Count;                                                                   //水平間距(畫布)
            string strTick = "";
            SizeF sizTick;
            for (int i = 0; i < Count; i += iDiv)
            {
                G.DrawLine(gPen_GridLine, gRectPlot.Left + tmp * i, gRectPlot.Top, gRectPlot.Left + tmp * i, gRectPlot.Bottom);     //格線(原點向右)

                strTick = string.Format("{0}", i);
                sizTick = G.MeasureString(strTick, gFontStr);
                G.DrawString(string.Format("{0}", strTick), gFontStr, new SolidBrush(gPen_GridLine.Color), gRectPlot.Left + tmp * i - sizTick.Width / 2, gRectPlot.Bottom);
            }
        }
        private void drawData()
        {
            int Count = gData.GetLength(0);
            PointF[] ptArray = new PointF[Count];
            int idx = 0;
            Matrix myMatrix;
            for (int n = 0; n < 5; n++)
            {
                if (n == 0 || n == 1) idx = 0;
                else if (n == 2) idx = 1;
                else if (n == 3 || n == 4) idx = 2;

                if (gVisible[idx] == false) continue;

                for (int i = 0; i < Count; i++) ptArray[i] = new PointF(i, gData[i, n]);
                myMatrix = new Matrix(gRectPlot.Width / Count, 0, 0, -(gRectPlot.Height / gLimit[idx] / 2), gPtOrigin.X, gPtOrigin.Y);
                G.Transform = myMatrix;
                G.DrawLines(gPens[n], ptArray);
            }
        }
    }
}
