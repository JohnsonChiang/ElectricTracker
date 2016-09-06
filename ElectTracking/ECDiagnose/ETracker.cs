﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ECDiagnose
{
    public partial class ETracer : PictureBox
    {
        Font gFontAxis = new Font("Arial", 14F, FontStyle.Regular);
        Font gFontComment = new Font("Arial", 8F, FontStyle.Regular);
        Pen gPenAxis = new Pen(Color.BlueViolet, 1);
        Pen gPenStandard = new Pen(Color.Green, 1);
        Pen gPenError = new Pen(Color.Red, 1);

        Matrix myMatrix;
        int wSpace = 20;                        //兩側留白距離
        eNumMethod gMethod = eNumMethod.Circle;

        public enum eNumDatum { XY, ZX, YZ }
        public enum eNumMethod { None, Circle, Rectangle, Diamond, RigidTapping }

        public ETracer()
        {
            InitializeComponent();
        }

        public int Space
        {
            get { return wSpace; }
            set { wSpace = value; }
        }
        public float H
        {
            get { return this.Width - wSpace * 2; }
        }
        public float W
        {
            get { return this.Width - wSpace * 2; }
        }

        public bool _DrawECircle(eNumDatum Datum, int UnitDiv)
        {
            this.Refresh();

            Graphics G = this.CreateGraphics();
            float[] ECIR = new float[5];        //型態(0:圓 1:方 2:菱 3:剛攻) 、 圓心X 、 圓心Y 、 圓心Z 、 標準圓半徑   (um)  ==> (新)
            float scale_Std2Pixel = 0; ;                           //將標準圓放在第三格上時的倍率
            float scale_Div2Std = 0;                              //每格單位比照設定時的單位縮放比率
            List<float[]> lstPts = new List<float[]>();

            #region 標準循跡繪圖
            if (File.Exists(@"D:\L2100_1\MACHINE\ECIR.DAT") == false) return false;
            using (StreamReader sr = new StreamReader(@"D:\L2100_1\MACHINE\ECIR.DAT"))
            {
                string sline = sr.ReadLine();
                if (sline == null) return false;

                string[] s = sline.Split(' ');                                                  //1(圓),X,Y,Z,R(半徑)
                if (sline.Length < 5)
                {
                    MessageBox.Show("String Format Error in ECIR.DAT");
                    return false;
                }
                for (int i = 0; i < 5; i++)                                                     //2(方),X,Y,Z,L(邊長)
                {                                                                               //3(菱),X,Y,Z,L(邊長)
                    ECIR[i] = Convert.ToSingle(s[i]);                                           //4(剛攻),0,0,0,0
                }
                scale_Div2Std = ECIR[4] / (UnitDiv * 8);      //    um(標準圓單位) / um(偏差格單位) (單位格比設定時的單位縮放比率)      
                scale_Std2Pixel = (H * 0.4F) / ECIR[4];       //   pixel/um (將標準圓放在第八格上時的倍率(um-->graph))

                if (ECIR[0] >= Enum.GetNames(typeof(eNumMethod)).Length)
                {
                    MessageBox.Show("Machine Type Error in ECIR.DAT");
                    return false;
                }

                gMethod = (eNumMethod)ECIR[0];

                drawComment_Std(G, ECIR);
                drawPattern_Std(G, ECIR);                       //繪製於圖上
            }
            #endregion

            #region 誤差循跡取值
            if (File.Exists(@"D:\L2100_1\NCFILES\CIRCULAR.DAT") == false) return false;

            int iCheckCount = 4;
            //if (gMethod == eNumMethod.RigidTapping) iCheckCount = 5;        //變數個數：剛攻5個，其他4個 (suspended)
            using (StreamReader sr = new StreamReader(@"D:\L2100_1\NCFILES\CIRCULAR.DAT"))
            {
                float maxErr_Positve = 0, maxErr_Negative = 0;
                float totalErr = 0;
                int totalCount = 0;
                string sline;
                string[] stmp;

                while (!sr.EndOfStream)
                {
                    sline = sr.ReadLine();
                    if (sline == null) continue;    //允許檔案中間有空行

                    stmp = sline.Split(' ');

                    // 1(圓): X、Y、Z、dR
                    // 2(方): X、Y、Z、0
                    // 3(菱): X、Y、Z、0
                    // 4(剛攻): 命令轉速(RPM)、實際轉速(RPM)、軸向誤差(um)、主軸扭力比、軸向扭力比
                    if (stmp.Length < iCheckCount)
                    {
                        MessageBox.Show("String Format Error in CIRCULAR.DAT");
                        return false;
                    }

                    float[] inf = new float[iCheckCount];
                    for (int i = 0; i < inf.Length; i++)                                
                    {                                                                   
                        inf[i] = Convert.ToSingle(stmp[i]);                             
                    }                                                                   
                    lstPts.Add(inf);

                    //---------------------- 找出最大誤差、最小誤差 ---------------------------------
                    if (inf[3] > maxErr_Positve) maxErr_Positve = inf[3];
                    else if (inf[3] < maxErr_Negative) maxErr_Negative = inf[3];
                    totalErr += inf[3];
                    totalCount++;
                }
                drawComment_Err(G, maxErr_Positve, maxErr_Negative, totalErr / totalCount);
            }
            #endregion

            #region 誤差循跡轉換(書宇)
            float[][] Pts_Real = lstPts.ToArray();
            float[][] Pts_Magnification = lstPts.ToArray();
            for (int i = 0; i < Pts_Real.GetLength(0); i++)
            {
                //------------------------ 加大誤差值偏差度(放大位率) ----------------------------------------------
                Pts_Magnification[i][0] = ((Pts_Real[i][0] - ECIR[1]) * (Pts_Real[i][3] * scale_Div2Std + ECIR[4])) / (Pts_Real[i][3] + ECIR[4]);
                Pts_Magnification[i][1] = ((Pts_Real[i][1] - ECIR[2]) * (Pts_Real[i][3] * scale_Div2Std + ECIR[4])) / (Pts_Real[i][3] + ECIR[4]);
                Pts_Magnification[i][2] = ((Pts_Real[i][2] - ECIR[3]) * (Pts_Real[i][3] * scale_Div2Std + ECIR[4])) / (Pts_Real[i][3] + ECIR[4]);
            }
            #endregion

            #region 誤差循跡繪圖
            PointF[] ptArray = new PointF[Pts_Magnification.GetLength(0)];
            for (int i = 0; i < ptArray.GetLength(0); i++)
            {
                ptArray[i] = new PointF();
                switch (Datum)
                {
                    case eNumDatum.XY: //XY
                        ptArray[i].X = Pts_Magnification[i][0] * scale_Std2Pixel;
                        ptArray[i].Y = Pts_Magnification[i][1] * scale_Std2Pixel;
                        break;
                    case eNumDatum.ZX: //ZX
                        ptArray[i].X = Pts_Magnification[i][2] * scale_Std2Pixel;
                        ptArray[i].Y = Pts_Magnification[i][0] * scale_Std2Pixel;
                        break;
                    case eNumDatum.YZ: //YZ
                        ptArray[i].X = Pts_Magnification[i][1] * scale_Std2Pixel;
                        ptArray[i].Y = Pts_Magnification[i][2] * scale_Std2Pixel;
                        break;
                }
            }
            drawPattern_Err(G, ptArray);                //誤差循跡繪圖
            #endregion

            if(gMethod!= eNumMethod.RigidTapping)           
                drawAxis(G, Datum, UnitDiv);                    //標準圖及標示繪圖
            else
                drawAxis_rigidTapping(G, Datum, UnitDiv);       //標準圖及標示繪圖
            return true;
        }
        public bool _DrawESquare(eNumDatum Datum, int UnitDiv)
        {
            Method_Square methSqu = new Method_Square();

            this.Refresh();

            Graphics G = this.CreateGraphics();
            float[] ECIR = new float[5];        //型態(0:圓 1:方 2:菱 3:剛攻) 、 圓心X 、 圓心Y 、 圓心Z 、 標準圓半徑   (um)  ==> (新)
            float scale_Std2Pixel = 0; ;                           //將標準圓放在第三格上時的倍率
            float scale_Div2Std = 0;                              //每格單位比照設定時的單位縮放比率
            List<float[]> lstPts = new List<float[]>();

            #region 標準循跡繪圖
            if (File.Exists(@"D:\L2100_1\MACHINE\ECIR.DAT") == false) return false;
            using (StreamReader sr = new StreamReader(@"D:\L2100_1\MACHINE\ECIR.DAT"))
            {
                string sline = sr.ReadLine();
                if (sline == null) return false;

                string[] s = sline.Split(' ');                                                  //1(圓),X,Y,Z,R(半徑)
                if (sline.Length < 5)
                {
                    MessageBox.Show("String Format Error in ECIR.DAT");
                    return false;
                }
                for (int i = 0; i < 5; i++)                                                     //2(方),X,Y,Z,L(邊長)
                {                                                                               //3(菱),X,Y,Z,L(邊長)
                    ECIR[i] = Convert.ToSingle(s[i]);                                           //4(剛攻),0,0,0,0
                }

                //scale_Div2Std = ECIR[4] / (UnitDiv * 8);      //    um(標準圓單位) / um(偏差格單位) (單位格比設定時的單位縮放比率)      
                //scale_Std2Pixel = (H * 0.4F) / ECIR[4];       //   pixel/um (將標準圓放在第八格上時的倍率(um-->graph))

                if (ECIR[0] >= Enum.GetNames(typeof(eNumMethod)).Length)
                {
                    MessageBox.Show("Machine Type Error in ECIR.DAT");
                    return false;
                }
                methSqu.setECIR(H, Convert.ToInt32(Datum), ECIR);

                gMethod = (eNumMethod)ECIR[0];
                drawComment_Std(G, ECIR);
                drawPattern_Std(G, ECIR);                       //繪製於圖上
            }
            #endregion

            #region 誤差循跡取值
            if (File.Exists(@"D:\L2100_1\NCFILES\CIRCULAR.DAT") == false) return false;

            int iCheckCount = 4;
            if (gMethod == eNumMethod.RigidTapping) iCheckCount = 5;        //變數個數：剛攻5個，其他4個 (suspended)
            using (StreamReader sr = new StreamReader(@"D:\L2100_1\NCFILES\CIRCULAR.DAT"))
            {
                string sline;
                string[] stmp;

                while (!sr.EndOfStream)
                {
                    sline = sr.ReadLine();
                    if (sline == null) continue;    //允許檔案中間有空行

                    stmp = sline.Split(' ');

                    // 1(圓): X、Y、Z、dR
                    // 2(方): X、Y、Z、0
                    // 3(菱): X、Y、Z、0
                    // 4(剛攻): 命令轉速(RPM)、實際轉速(RPM)、軸向誤差(um)、主軸扭力比、軸向扭力比
                    if (stmp.Length < iCheckCount)
                    {
                        MessageBox.Show("String Format Error in CIRCULAR.DAT");
                        return false;
                    }

                    float[] inf = new float[iCheckCount];
                    for (int i = 0; i < inf.Length; i++)
                    {
                        inf[i] = Convert.ToSingle(stmp[i]);
                    }
                    switch (Datum)
                    {
                        case eNumDatum.XY:
                            lstPts.Add(new float[] { inf[0], inf[1] });
                            break;
                        case eNumDatum.ZX:
                            lstPts.Add(new float[] { inf[2], inf[0] });
                            break;
                        case eNumDatum.YZ:
                            lstPts.Add(new float[] { inf[1], inf[2] });
                            break;
                    }                    

                //    //---------------------- 找出最大誤差、最小誤差 ---------------------------------
                //    if (inf[3] > maxErr_Positve) maxErr_Positve = inf[3];
                //    else if (inf[3] < maxErr_Negative) maxErr_Negative = inf[3];
                //    totalErr += inf[3];
                //    totalCount++;
                }
                //drawComment_Err(G, maxErr_Positve, maxErr_Negative, totalErr / totalCount);
            }
            #endregion

            #region 誤差循跡轉換(書宇)
            float[][] Pts_Real = lstPts.ToArray();
            //float[][] Pts_Magnification = lstPts.ToArray();
            //for (int i = 0; i < Pts_Real.Length; i++)
            //{
            //    //------------------------ 加大誤差值偏差度(放大位率) ----------------------------------------------
            //    Pts_Magnification[i][0] = ((Pts_Real[i][0] - ECIR[1]) * (Pts_Real[i][3] * scale_Div2Std + ECIR[4])) / (Pts_Real[i][3] + ECIR[4]);
            //    Pts_Magnification[i][1] = ((Pts_Real[i][1] - ECIR[2]) * (Pts_Real[i][3] * scale_Div2Std + ECIR[4])) / (Pts_Real[i][3] + ECIR[4]);
            //    Pts_Magnification[i][2] = ((Pts_Real[i][2] - ECIR[3]) * (Pts_Real[i][3] * scale_Div2Std + ECIR[4])) / (Pts_Real[i][3] + ECIR[4]);
            //}

            float[] fRet = methSqu.setCircular(Pts_Real);
            UnitDiv = (int)fRet[3];
            drawComment_Err(G, fRet[0], fRet[1], fRet[2]);
            #endregion

            #region 誤差循跡繪圖
            //PointF[] ptArray = new PointF[Pts_Magnification.Length];
            //for (int i = 0; i < ptArray.Length; i++)
            //{
            //    ptArray[i] = new PointF();
            //    switch (Datum)
            //    {
            //        case eNumDatum.XY: //XY
            //            ptArray[i].X = Pts_Magnification[i][0] * scale_Std2Pixel;
            //            ptArray[i].Y = Pts_Magnification[i][1] * scale_Std2Pixel;
            //            break;
            //        case eNumDatum.ZX: //ZX
            //            ptArray[i].X = Pts_Magnification[i][2] * scale_Std2Pixel;
            //            ptArray[i].Y = Pts_Magnification[i][0] * scale_Std2Pixel;
            //            break;
            //        case eNumDatum.YZ: //YZ
            //            ptArray[i].X = Pts_Magnification[i][1] * scale_Std2Pixel;
            //            ptArray[i].Y = Pts_Magnification[i][2] * scale_Std2Pixel;
            //            break;
            //    }
            //}
            float[][] ptXY = methSqu.getXY();
            PointF[] ptArray = new PointF[ptXY.GetLength(0)];
            for (int i = 0; i < ptArray.Length; i++)
            {
                ptArray[i] = new PointF(ptXY[i][0], ptXY[i][1]);
            }
            drawPattern_Err(G, ptArray);                //誤差循跡繪圖
            #endregion

            if (gMethod != eNumMethod.RigidTapping)
                drawAxis(G, Datum, UnitDiv);                    //標準圖及標示繪圖
            else
                drawAxis_rigidTapping(G, Datum, UnitDiv);       //標準圖及標示繪圖
            return true;
        }
        private void drawAxis(Graphics G, eNumDatum Datum, int UnitDiv)         //標準座標系統  + 軸名 + 軸線 + 刻度
        {
            myMatrix = new Matrix(1, 0, 0, 1, this.Width / 2, this.Height / 2);     //畫布零點偏移
            G.Transform = myMatrix;

            string sAxis0 = "Axis0", sAxis1 = "Axis1";                  //sAxis0(水平軸)，sAxis1(垂直軸)
            switch (Datum)
            {
                case eNumDatum.XY: sAxis0 = "X"; sAxis1 = "Y"; break;
                case eNumDatum.ZX: sAxis0 = "Z"; sAxis1 = "X"; break;
                case eNumDatum.YZ: sAxis0 = "Y"; sAxis1 = "Z"; break;
            }
            G.DrawString(sAxis0, gFontAxis, Brushes.Blue, W / 2, 0);                //水平軸名 
            G.DrawString(sAxis1, gFontAxis, Brushes.Blue, 0, -H / 2 - 12 - 4);      //垂直軸名
            G.DrawString(string.Format("+{0} um", UnitDiv), gFontComment, Brushes.Blue, 7, -H / 20 * 9.3f);              //單位標示

            myMatrix = new Matrix(1, 0, 0, -1, this.Width / 2, this.Height / 2);    //畫布座標系上下傾倒
            G.Transform = myMatrix;


            int wArrow = 5;                         //箭號距離  
            int startIdx = -4;                      //以標準線為準的刻痕起始點
            int endIdx = 2;                         //以標準線為準的刻痕結束點
            
            float div = H / 20;                     //每格的像素值
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
                float locStart = lengthStd +startIdx * div;
                float locEnd = lengthStd + endIdx * div;
                G.DrawLine(gPenAxis, new PointF(locStart, 0), new PointF(locEnd, 0));
                G.DrawLine(gPenAxis, new PointF(-locStart, 0), new PointF(-locEnd, 0));
                G.DrawLine(gPenAxis, new PointF(0, locStart), new PointF(0, locEnd));
                G.DrawLine(gPenAxis, new PointF(0, -locStart), new PointF(0, -locEnd));

                if (n == 0)
                {
                    //畫中線
                    G.DrawLine(gPenAxis, new PointF(locEnd, 0), new PointF(W / 2, 0));                //軸線(水平)
                    G.DrawLine(gPenAxis, new PointF(W / 2 + wArrow, 0), new PointF(W / 2, wArrow));   //水平箭號(上撇)
                    G.DrawLine(gPenAxis, new PointF(W / 2 + wArrow, 0), new PointF(W / 2, -wArrow));  //水平箭號(下撇)

                    G.DrawLine(gPenAxis, new PointF(0, locEnd), new PointF(0, H / 2));                //軸線(垂直)
                    G.DrawLine(gPenAxis, new PointF(0, H / 2 + wArrow), new PointF(-wArrow, H / 2));  //垂直箭號(左撇)
                    G.DrawLine(gPenAxis, new PointF(0, H / 2 + wArrow), new PointF(wArrow, H / 2));   //垂直箭號(右撇)
                }
                G.RotateTransform(45);
            }
        }
        private void drawAxis_rigidTapping(Graphics G, eNumDatum Datum, int UnitDiv)         //標準座標系統  + 軸名 + 軸線 + 刻度
        {
            myMatrix = new Matrix(1, 0, 0, 1, this.Width / 2, this.Height / 2);     //畫布零點偏移
            G.Transform = myMatrix;

            string sAxis0 = "Axis0", sAxis1 = "Axis1";                  //sAxis0(水平軸)，sAxis1(垂直軸)
            switch (Datum)
            {
                case eNumDatum.XY: sAxis0 = "X"; sAxis1 = "Y"; break;
                case eNumDatum.ZX: sAxis0 = "Z"; sAxis1 = "X"; break;
                case eNumDatum.YZ: sAxis0 = "Y"; sAxis1 = "Z"; break;
            }
            G.DrawString(sAxis0, gFontAxis, Brushes.Blue, W / 2, 0);                //水平軸名 
            G.DrawString(sAxis1, gFontAxis, Brushes.Blue, 0, -H / 2 - 12 - 4);      //垂直軸名
            G.DrawString(string.Format("+{0} um", UnitDiv), gFontComment, Brushes.Blue, 7, -H / 20 * 9.3f);              //單位標示

            myMatrix = new Matrix(1, 0, 0, -1, this.Width / 2, this.Height / 2);    //畫布座標系上下傾倒
            G.Transform = myMatrix;

            int startTick = 4;                      //起始刻號index
            int wArrow = 5;                         //箭號距離  

            float div = 0f;
            float tmp = 0f;
            for (int n = 0; n < 2; n++)
            {
                //水平刻度線
                div = W / 20;
                for (int i = startTick; i < 11; i++)
                {
                    tmp = div * i;
                    G.DrawLine(gPenAxis, new PointF(tmp, -5), new PointF(tmp, 5));
                    G.DrawLine(gPenAxis, new PointF(-tmp, -5), new PointF(-tmp, 5));
                }
                G.DrawLine(gPenAxis, new PointF(div * startTick, 0), new PointF(div * 10, 0));
                G.DrawLine(gPenAxis, new PointF(-div * startTick, 0), new PointF(-div * 10, 0));


                //垂直刻度線
                div = H / 20;
                for (int i = startTick; i < 11; i++)
                {
                    tmp = div * i;
                    G.DrawLine(gPenAxis, new PointF(-5, tmp), new PointF(5, tmp));
                    G.DrawLine(gPenAxis, new PointF(-5, -tmp), new PointF(5, -tmp));
                }
                G.DrawLine(gPenAxis, new PointF(0, div * startTick), new PointF(0, div * 10));
                G.DrawLine(gPenAxis, new PointF(0, -div * startTick), new PointF(0, -div * 10));

                if (n == 0)
                {
                    //畫中線
                    G.DrawLine(gPenAxis, new PointF(div * 10, 0), new PointF(W / 2, 0));                //軸線(水平)
                    G.DrawLine(gPenAxis, new PointF(W / 2 + wArrow, 0), new PointF(W / 2, wArrow));   //水平箭號(上撇)
                    G.DrawLine(gPenAxis, new PointF(W / 2 + wArrow, 0), new PointF(W / 2, -wArrow));  //水平箭號(下撇)

                    G.DrawLine(gPenAxis, new PointF(0, div * 10), new PointF(0, H / 2));                //軸線(垂直)
                    G.DrawLine(gPenAxis, new PointF(0, H / 2 + wArrow), new PointF(-wArrow, H / 2));  //垂直箭號(左撇)
                    G.DrawLine(gPenAxis, new PointF(0, H / 2 + wArrow), new PointF(wArrow, H / 2));   //垂直箭號(右撇)
                }
                G.RotateTransform(45);
            }
            //picGraphics.ResetTransform();
            //picGraphics.Transform = myMatrix;
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
                case eNumMethod.RigidTapping:
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
            float h = H / 1.414f;
            switch (gMethod)
            {
                case eNumMethod.Circle:
                    G.DrawEllipse(gPenStandard, -0.4F * H, -0.4F * H, 0.8F * H, 0.8F * H);      //繪製標準圓    
                    break;
                case eNumMethod.Rectangle:
                    G.DrawRectangle(gPenStandard, -0.4F * H, -0.4F * H, 0.8F * H, 0.8F * H);    //繪製標準方 
                    break;
                case eNumMethod.Diamond:
                    G.RotateTransform(45);
                    G.DrawRectangle(gPenStandard, -0.4F * h, -0.4F * h, 0.8F * h, 0.8F * h);    //繪製標準方 
                    break;
                case eNumMethod.RigidTapping:
                    break;
                default:
                    return;
            }
        }
        private void drawPattern_Err(Graphics G, PointF[] ptArray)
        {
            myMatrix = new Matrix(1, 0, 0, -1, this.Width / 2, this.Height / 2);                //畫布零點偏移
            G.Transform = myMatrix;
            G.DrawLines(gPenError, ptArray);
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
    }

    public class Method_Square
    {
        int gDatum = 0;
        float[] standard_point = new float[5];              //Ecir.dat  type,Xc,Yc,Zc,L
        float[][] data_point1;         //Circular.dat
        float[][] data_point2;         //給底層做資料處理的點位
        float[][] data_point3;         //處理完的誤差點位  X,Y.angle
        float[][] data_point4;         //人機要畫的圖點
        PointF[] point4;

        float delta_angle = 1.0f;                         //特殊狀況時判斷的角度範圍
        float multiple = 0;
        float unitdiv = 0, scale = 0, grid_in = 8, grid_out = 4;

        //直接給定變數值做測試
        //float L = 10f;
        float L = 0;
        private float H;

        public Method_Square()
        {
            //for (int i = 0; i < data_point1.GetLength(0); i++)
            //{
            //    data_point1[i] = new float[4];
            //    data_point2[i] = new float[4];
            //    data_point3[i] = new float[4];
            //    data_point4[i] = new float[4];
            //}
        }

        public void setECIR(float GraphicsHeight, int datum, float[] Value)
        {
            if (Value.Length == 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    standard_point[i] = Value[i];
                }
                L = Value[4];
                gDatum = datum;
                this.H = GraphicsHeight;
            }
        }
        public float[] setCircular(float[][] Value)
        {
            data_point1 = new float[Value.GetLength(0)][];

            for (int i = 0; i < Value.GetLength(0); i++)
            {
                data_point1[i] = new float[Value[i].Length];
                for (int j = 0; j < Value[i].Length; j++)
                {
                    data_point1[i][j] = Value[i][j];
                }
            }
            offset_datapts();//將圖形偏移到原點
            //rotate_diamond_to_square();//把菱形旋轉成方形
            calculate_square();
            return square_point();            
        }
        public float[][] getXY()
        {
            //rotate_square_to_diamond();//把方形旋轉成菱形
            return data_point4;
        }
        void offset_datapts()//將圖形偏移到原點
        {
            data_point2 = new float[data_point1.GetLength(0)][];
            data_point3 = new float[data_point1.GetLength(0)][];
            data_point4 = new float[data_point1.GetLength(0)][];
            int i = 0;
            for (i = 0; i < data_point1.GetLength(0); i++)
            {
                data_point2[i] = new float[data_point1[i].Length];
                data_point3[i] = new float[data_point1[i].Length + 1];
                data_point4[i] = new float[data_point1[i].Length];
                switch (gDatum)
                {
                    case 0:     //XY
                        data_point2[i][0] = data_point1[i][0] - standard_point[1]; //x
                        data_point2[i][1] = data_point1[i][1] - standard_point[2]; //y
                        break;
                    case 1:     //ZX
                        data_point2[i][0] = data_point1[i][0] - standard_point[3]; //z
                        data_point2[i][1] = data_point1[i][1] - standard_point[1]; //x
                        break;
                    case 2:     //YZ
                        data_point2[i][0] = data_point1[i][0] - standard_point[2]; //y
                        data_point2[i][1] = data_point1[i][1] - standard_point[3]; //z
                        break;
                }
            }
        }

        void rotate_diamond_to_square()//把菱形旋轉成方形
        { 
            //x'=x*cos-y*sin        y'=y*cos+x*sin
            double theta1=-45*2*Math.PI/360;//順轉45度
            int i=0;
            for (i = 0; i < data_point1.GetLength(0); i++)
            {
                data_point2[i][0] = (float)(data_point2[i][0] * Math.Cos(theta1) - data_point2[i][1] * Math.Sin(theta1));
                data_point2[i][1] = (float)(data_point2[i][1] * Math.Cos(theta1) + data_point2[i][0] * Math.Sin(theta1));
            }
        }
        void rotate_square_to_diamond()//把方形旋轉回菱形
        {
            //x'=x*cos-y*sin        y'=y*cos+x*sin
            double theta1 = 45 * 2 * Math.PI / 360;//順轉45度
            int i = 0;
            for (i = 0; i < data_point1.GetLength(0); i++)
            {
                data_point4[i][0] = (float)(data_point4[i][0] * Math.Cos(theta1) - data_point4[i][1] * Math.Sin(theta1));
                data_point4[i][1] = (float)(data_point4[i][1] * Math.Cos(theta1) + data_point4[i][0] * Math.Sin(theta1));
            } 
        }

        void calculate_square()//算方形的點位
        {
            int i = 0;            
            float param = 0, angle = 0;
            //利用點位的角度估算目前所在位置，狀態機作為區域切換
            //計算各點位誤差，分成9種CASE討論
            //1:往上    //2:轉彎    //3:往左    //4:轉彎    //5:往下    //6:轉彎    //7:往右    //8:轉彎
            for (i = 0; i < data_point3.GetLength(0); i++)
            {
                if (data_point2[i][0] > 0)//右半邊
                {
                    if (data_point2[i][0] != 0)//先判斷角度
                    {
                        param = data_point2[i][1] / data_point2[i][0];
                        angle = Convert.ToSingle(Math.Atan(param) * 180 / Math.PI);//判斷範圍為-90~90
                    }
                    else
                        angle = 0;
                }
                else //左半邊
                {
                    if (data_point2[i][0] != 0)//先判斷角度
                    {
                        param = data_point2[i][1] / data_point2[i][0];
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
                    data_point3[i][0] = data_point2[i][0] - L / 2;
                }
                else if ((angle >= (45 - delta_angle)) && (angle <= 45))//2
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point3[i][0] = data_point2[i][0] - L / 2;
                        data_point3[i][1] = data_point2[i][1] - L / 2;
                    }
                    else
                    {
                        data_point3[i][0] = data_point2[i][0] - L / 2;
                    }
                }
                else if ((angle > 45) && (angle <= (45 + delta_angle)))//2
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point3[i][0] = data_point2[i][0] - L / 2;
                        data_point3[i][1] = data_point2[i][1] - L / 2;
                    }
                    else
                    {
                        data_point3[i][1] = data_point2[i][1] - L / 2;
                    }
                }
                else if ((angle > (45 + delta_angle)) && (angle < (135 - delta_angle)))//3
                {
                    data_point3[i][1] = data_point2[i][1] - L / 2;
                }                
                else if ((angle >= (135 - delta_angle)) && (angle <= 135))//4
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point3[i][0] = data_point2[i][0] + L / 2;
                        data_point3[i][1] = data_point2[i][1] - L / 2;
                    }
                    else
                    {
                        data_point3[i][1] = data_point2[i][1] - L / 2;
                    }
                }
                else if ((angle > 135) && (angle <= (135 + delta_angle)))//4
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point3[i][0] = data_point2[i][0] + L / 2;
                        data_point3[i][1] = data_point2[i][1] - L / 2;
                    }
                    else
                    {
                        data_point3[i][0] = data_point2[i][0] + L / 2;
                    }
                }
                else if ((angle > (135 + delta_angle)) && (angle < (225 - delta_angle)))//5
                {
                    data_point3[i][0] = data_point2[i][0] + L / 2;
                }
                else if ((angle >= (225 - delta_angle)) && (angle <= 225))//6
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point3[i][0] = data_point2[i][0] + L / 2;
                        data_point3[i][1] = data_point2[i][1] + L / 2;
                    }
                    else
                    {
                        data_point3[i][0] = data_point2[i][0] + L / 2;
                    }
                }
                else if ((angle > 225) && (angle <= (225 + delta_angle)))//6
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point3[i][0] = data_point2[i][0] + L / 2;
                        data_point3[i][1] = data_point2[i][1] + L / 2;
                    }
                    else
                    {
                        data_point3[i][1] = data_point2[i][1] + L / 2;
                    }
                }
                else if (((angle > (225 + delta_angle)) && (angle <= 270)) || ((angle >= -90) && (angle < (-45 - delta_angle))))//7
                {
                    data_point3[i][1] = data_point2[i][1] + L / 2;
                }                
                else if ((angle >= (-45 - delta_angle)) && (angle <= -45))//8
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point3[i][0] = data_point2[i][0] - L / 2;
                        data_point3[i][1] = data_point2[i][1] + L / 2;
                    }
                    else
                    {
                        data_point3[i][1] = data_point2[i][1] + L / 2;
                    }
                }
                else if ((angle > -45) && (angle <= (-45 + delta_angle)))//8
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point3[i][0] = data_point2[i][0] - L / 2;
                        data_point3[i][1] = data_point2[i][1] + L / 2;
                    }
                    else
                    {
                        data_point3[i][0] = data_point2[i][0] - L / 2;
                    }
                }
                data_point3[i][2] = angle;//紀錄該點的角度  之後才有辦法重新描繪data_point4
            }
        }
        float[] square_point()//計算圖紙上的點
        {
            int i = 0;
            float max_error = 0, min_error = 99999;
            float temp = 0, avg_error = 0 , sum = 0 ;
            float angle = 0;

            //算最大誤差 最小誤差 平均誤差
            for (i = 0; i < data_point2.GetLength(0); i++)
            {                
                temp=(float)(Math.Sqrt(data_point3[i][0] * data_point3[i][0] + data_point3[i][1] * data_point3[i][1]));
                if (max_error < temp)
                    max_error = temp;

                if (min_error > temp)
                    min_error = temp;

                sum = sum + temp;
            }
            avg_error = sum / data_point2.GetLength(0);
            
            //算一些必要的圖紙資訊
            unitdiv = max_error / (grid_in / 2);//內部可視格數只有一半  所以要/2
            unitdiv = Convert.ToSingle(Math.Ceiling(unitdiv));
            scale = (standard_point[4] / 2) / (unitdiv * grid_in);//邊長/2
            multiple = (float)(H * 0.4) / (L / 2);

            //計算圖紙上的點&點誤差
            for (i = 0; i < data_point3.GetLength(0); i++)
            {
                //計算圖紙上的點誤差
                data_point3[i][0] = data_point3[i][0] * scale;
                data_point3[i][1] = data_point3[i][1] * scale;

                angle = data_point3[i][2];
                //計算圖紙上的點                
                if ((angle > (-45 + delta_angle)) && (angle < (45 - delta_angle)))//1
                {
                    data_point4[i][0] = data_point3[i][0] + L / 2;  //誤差量+邊長
                    data_point4[i][1] = data_point2[i][1];          //原本的數值
                }                
                else if ((angle >= (45 - delta_angle)) && (angle <= 45))//2
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point4[i][0] = data_point3[i][0] + L / 2;
                        data_point4[i][1] = data_point3[i][1] + L / 2;
                    }
                    else
                    {
                        data_point4[i][0] = data_point3[i][0] + L / 2;
                        data_point4[i][1] = data_point2[i][1];
                    }
                }
                else if ((angle >= 45) && (angle <= (45 + delta_angle)))//2
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point4[i][0] = data_point3[i][0] + L / 2;
                        data_point4[i][1] = data_point3[i][1] + L / 2;
                    }
                    else
                    {
                        data_point4[i][0] = data_point2[i][0];
                        data_point4[i][1] = data_point3[i][1] + L / 2;
                    }
                }
                else if ((angle > (45 + delta_angle)) && (angle < (135 - delta_angle)))//3
                {
                    data_point4[i][0] = data_point2[i][0];
                    data_point4[i][1] = data_point3[i][1] + L / 2;
                }                
                else if ((angle >= (135 - delta_angle)) && (angle <= 135))//4
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point4[i][0] = data_point3[i][0] - L / 2;
                        data_point4[i][1] = data_point3[i][1] + L / 2;
                    }
                    else
                    {
                        data_point4[i][0] = data_point2[i][0] ;
                        data_point4[i][1] = data_point3[i][1] + L / 2;
                    }
                }
                else if ((angle >= 135) && (angle <= (135 + delta_angle)))//4
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point4[i][0] = data_point3[i][0] - L / 2;
                        data_point4[i][1] = data_point3[i][1] + L / 2;
                    }
                    else
                    {
                        data_point4[i][0] = data_point3[i][0] - L / 2;
                        data_point4[i][1] = data_point2[i][1] ;
                    }
                }
                else if ((angle > (135 + delta_angle)) && (angle < (225 - delta_angle)))//5
                {
                    data_point4[i][0] = data_point3[i][0] - L / 2;
                    data_point4[i][1] = data_point2[i][1];
                }               
                else if ((angle >= (225 - delta_angle)) && (angle <= 225))//6
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point4[i][0] = data_point3[i][0] - L / 2;
                        data_point4[i][1] = data_point3[i][1] - L / 2;
                    }
                    else
                    {
                        data_point4[i][0] = data_point3[i][0] - L / 2;
                        data_point4[i][1] = data_point2[i][1] ;
                    }
                }
                else if ((angle >= 225) && (angle <= (225 + delta_angle)))//6
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point4[i][0] = data_point3[i][0] - L / 2;
                        data_point4[i][1] = data_point3[i][1] - L / 2;
                    }
                    else
                    {
                        data_point4[i][0] = data_point2[i][0] ;
                        data_point4[i][1] = data_point3[i][1] - L / 2;
                    }
                }
                else if (((angle > (225 + delta_angle)) && (angle <= 270)) || ((angle >= -90) && (angle < (-45 - delta_angle))))//7
                {
                    data_point4[i][0] = data_point2[i][0];
                    data_point4[i][1] = data_point3[i][1] - L / 2;
                }                
                else if ((angle >= (-45 - delta_angle)) && (angle <= -45))//8
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point4[i][0] = data_point3[i][0] + L / 2;
                        data_point4[i][1] = data_point3[i][1] - L / 2;
                    }
                    else
                    {
                        data_point4[i][0] = data_point2[i][0];
                        data_point4[i][1] = data_point3[i][1] - L / 2;                        
                    }
                }
                else if ((angle >= -45) && (angle <= (-45 + delta_angle)))//8
                {
                    if (Math.Abs(data_point2[i][0]) > L / 2 && Math.Abs(data_point2[i][1]) > L / 2)
                    {
                        data_point4[i][0] = data_point3[i][0] + L / 2;
                        data_point4[i][1] = data_point3[i][1] - L / 2;
                    }
                    else
                    {                        
                        data_point4[i][0] = data_point3[i][0] + L / 2;
                        data_point4[i][1] = data_point2[i][1];
                    }
                }
                data_point4[i][0] = data_point4[i][0] * multiple;
                data_point4[i][1] = data_point4[i][1] * multiple;
            }
            return new float[] { max_error, min_error, avg_error, unitdiv };
        }
    }
}