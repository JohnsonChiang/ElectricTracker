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
    public enum eNumDatum { XY, ZX, YZ } 
    public enum eNumMethod { None, Circle, Rectangle, Diamond, RigidTapping }

    public partial class ETracer : PictureBox
    {
        Font gFontAxis = new Font("Arial", 14F, FontStyle.Regular);
        Font gFontComment = new Font("Arial", 8F, FontStyle.Regular);
        Pen gPenAxis = new Pen(Color.BlueViolet, 1);
        Pen gPenStandard = new Pen(Color.Green, 1);
        Pen gPenError = new Pen(Color.Red, 1);

        Matrix myMatrix;
        int gPadding_Chart = 20;                        //兩側留白距離

        eNumMethod gMethod = eNumMethod.Circle;
        TrackConvert gTrack = new TrackConvert();

        public ETracer()
        {
            InitializeComponent();
        }

        public int _Padding_Chart
        {
            get { return gPadding_Chart; }
            set { gPadding_Chart = value; }
        }
        public float _Width_Chart
        {
            get { return this.Width - gPadding_Chart * 2; }
        }
        public bool _Draw(string fileInit, string fileTrack, eNumDatum Datum)
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
            gTrack.Initial(_Width_Chart, ECIR);
            // 計算：並取得 [最大誤差]、[最小誤差]、[平均誤差]、[每格誤差量] 
            float[] fRet = gTrack.Calculate(Datum, listPts.ToArray());  

            // 誤差繪圖
            drawPattern_Err(G, gTrack.GetMappingXY());              //繪圖：誤差循跡
            drawComment_Err(G, fRet[0], fRet[1], fRet[2]);          //繪圖：誤差註解

            if (gMethod != eNumMethod.RigidTapping)
                drawAxis(G, Datum, (int)fRet[3]);                    //繪圖：標準軸線
            else
                drawAxis_rigidTapping(G, Datum, (int)fRet[3]);       //繪圖：標準軸線
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
            G.DrawString(sAxis0, gFontAxis, Brushes.Blue, _Width_Chart / 2, 0);                //水平軸名 
            G.DrawString(sAxis1, gFontAxis, Brushes.Blue, 0, -_Width_Chart / 2 - 12 - 4);      //垂直軸名
            G.DrawString(string.Format("+{0} um", UnitDiv), gFontComment, Brushes.Blue, 7, -_Width_Chart / 20 * 9.3f);              //單位標示

            myMatrix = new Matrix(1, 0, 0, -1, this.Width / 2, this.Height / 2);    //畫布座標系上下傾倒
            G.Transform = myMatrix;


            int wArrow = 5;                         //箭號距離  
            int startIdx = -4;                      //以標準線為準的刻痕起始點
            int endIdx = 2;                         //以標準線為準的刻痕結束點

            float div = _Width_Chart / 20;                     //每格的像素值
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
                    G.DrawLine(gPenAxis, new PointF(locEnd, 0), new PointF(_Width_Chart / 2, 0));                //軸線(水平)
                    G.DrawLine(gPenAxis, new PointF(_Width_Chart / 2 + wArrow, 0), new PointF(_Width_Chart / 2, wArrow));   //水平箭號(上撇)
                    G.DrawLine(gPenAxis, new PointF(_Width_Chart / 2 + wArrow, 0), new PointF(_Width_Chart / 2, -wArrow));  //水平箭號(下撇)

                    G.DrawLine(gPenAxis, new PointF(0, locEnd), new PointF(0, _Width_Chart / 2));                //軸線(垂直)
                    G.DrawLine(gPenAxis, new PointF(0, _Width_Chart / 2 + wArrow), new PointF(-wArrow, _Width_Chart / 2));  //垂直箭號(左撇)
                    G.DrawLine(gPenAxis, new PointF(0, _Width_Chart / 2 + wArrow), new PointF(wArrow, _Width_Chart / 2));   //垂直箭號(右撇)
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
            G.DrawString(sAxis0, gFontAxis, Brushes.Blue, _Width_Chart / 2, 0);                //水平軸名 
            G.DrawString(sAxis1, gFontAxis, Brushes.Blue, 0, -_Width_Chart / 2 - 12 - 4);      //垂直軸名
            G.DrawString(string.Format("+{0} um", UnitDiv), gFontComment, Brushes.Blue, 7, -_Width_Chart / 20 * 9.3f);              //單位標示

            myMatrix = new Matrix(1, 0, 0, -1, this.Width / 2, this.Height / 2);    //畫布座標系上下傾倒
            G.Transform = myMatrix;

            int startTick = 4;                      //起始刻號index
            int wArrow = 5;                         //箭號距離  

            float div = 0f;
            float tmp = 0f;
            for (int n = 0; n < 2; n++)
            {
                //水平刻度線
                div = _Width_Chart / 20;
                for (int i = startTick; i < 11; i++)
                {
                    tmp = div * i;
                    G.DrawLine(gPenAxis, new PointF(tmp, -5), new PointF(tmp, 5));
                    G.DrawLine(gPenAxis, new PointF(-tmp, -5), new PointF(-tmp, 5));
                }
                G.DrawLine(gPenAxis, new PointF(div * startTick, 0), new PointF(div * 10, 0));
                G.DrawLine(gPenAxis, new PointF(-div * startTick, 0), new PointF(-div * 10, 0));


                //垂直刻度線
                div = _Width_Chart / 20;
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
                    G.DrawLine(gPenAxis, new PointF(div * 10, 0), new PointF(_Width_Chart / 2, 0));                //軸線(水平)
                    G.DrawLine(gPenAxis, new PointF(_Width_Chart / 2 + wArrow, 0), new PointF(_Width_Chart / 2, wArrow));   //水平箭號(上撇)
                    G.DrawLine(gPenAxis, new PointF(_Width_Chart / 2 + wArrow, 0), new PointF(_Width_Chart / 2, -wArrow));  //水平箭號(下撇)

                    G.DrawLine(gPenAxis, new PointF(0, div * 10), new PointF(0, _Width_Chart / 2));                //軸線(垂直)
                    G.DrawLine(gPenAxis, new PointF(0, _Width_Chart / 2 + wArrow), new PointF(-wArrow, _Width_Chart / 2));  //垂直箭號(左撇)
                    G.DrawLine(gPenAxis, new PointF(0, _Width_Chart / 2 + wArrow), new PointF(wArrow, _Width_Chart / 2));   //垂直箭號(右撇)
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
            float h = _Width_Chart / 1.414f;
            switch (gMethod)
            {
                case eNumMethod.Circle:
                    G.DrawEllipse(gPenStandard, -0.4F * _Width_Chart, -0.4F * _Width_Chart, 0.8F * _Width_Chart, 0.8F * _Width_Chart);      //繪製標準圓形    
                    break;
                case eNumMethod.Rectangle:
                    G.DrawRectangle(gPenStandard, -0.4F * _Width_Chart, -0.4F * _Width_Chart, 0.8F * _Width_Chart, 0.8F * _Width_Chart);    //繪製標準方形 
                    break;
                case eNumMethod.Diamond:
                    G.RotateTransform(45);
                    G.DrawRectangle(gPenStandard, -0.4F * h, -0.4F * h, 0.8F * h, 0.8F * h);    //繪製標準菱形 
                    break;
                case eNumMethod.RigidTapping:
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
    }

    public class TrackConvert
    {
        eNumDatum gDatum = eNumDatum.XY;

        float[] gDataStd = new float[5];        //Ecir.dat      (type,Xc,Yc,Zc,R/L/0/0)
        float[,] gDataReal_Ori;                //把Circular.dat資料移至原點
        float[,] gDataReal_Err;                //處理完的誤差點位              X,Y.angle
        float[,] gDataReal_Gdi;                //人機要畫的圖點

        float delta_angle = 1.0f;               //特殊狀況時判斷的角度範圍
        float multiple = 0;
        float unitdiv = 0, scale = 0, grid_in = 8;
        float H = 0;                            //圖紙寬度(方形圖紙)

        public TrackConvert()
        {
            //for (int i = 0; i < data_point1.GetLength(0); i++)
            //{
            //    data_point1[i] = new float[4];
            //    data_point2[i] = new float[4];
            //    data_point3[i] = new float[4];
            //    data_point4[i] = new float[4];
            //}
        }

        public void Initial(float GraphicsHeight, float[] ValStd)
        {
            if (ValStd.Length == 5)
            {
                for (int i = 0; i < 5; i++) gDataStd[i] = ValStd[i];
                this.H = GraphicsHeight;
            }
        }
        public float[] Calculate(eNumDatum Datum, float[][] ValReal)
        {
            gDatum = Datum;
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
            //=====================================================================
            switch ((eNumMethod)gDataStd[0])
            {
                case eNumMethod.Circle:
                    calError_circle(ValReal);
                    return getPlot_circle(gDataStd[4]);

                case eNumMethod.Rectangle:
                    calError_square(gDataStd[4]);
                    return getPlot_square(gDataStd[4]);

                case eNumMethod.Diamond:
                    return null;

                case eNumMethod.RigidTapping:
                    return null;

                default:
                    return null;
            }
        }
        public float[,] GetMappingXY()
        {
            return gDataReal_Gdi;
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
            multiple = (H * 0.4F) / R;

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
                    if (Math.Abs(gDataReal_Ori[i, 0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
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
                temp = (float)(Math.Sqrt(gDataReal_Err[i,0] * gDataReal_Err[i,0] + gDataReal_Err[i,1] * gDataReal_Err[i,1]));
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
            multiple = (float)(H * 0.4) / (L / 2);

            //計算圖紙上的點&點誤差
            gDataReal_Gdi = new float[gDataReal_Err.GetLength(0), 2];
            for (i = 0; i < gDataReal_Err.GetLength(0); i++)
            {
                //計算圖紙上的點誤差
                gDataReal_Err[i,0] = gDataReal_Err[i,0] * scale;
                gDataReal_Err[i,1] = gDataReal_Err[i,1] * scale;

                angle = gDataReal_Err[i,2];
                //計算圖紙上的點                
                if ((angle > (-45 + delta_angle)) && (angle < (45 - delta_angle)))//1
                {
                    gDataReal_Gdi[i,0] = gDataReal_Err[i,0] + L / 2;  //誤差量+邊長
                    gDataReal_Gdi[i,1] = gDataReal_Ori[i,1];       //原本的數值
                }
                else if ((angle >= (45 - delta_angle)) && (angle <= 45))//2
                {
                    if (Math.Abs(gDataReal_Ori[i,0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] + L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] + L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Ori[i,1];
                    }
                }
                else if ((angle >= 45) && (angle <= (45 + delta_angle)))//2
                {
                    if (Math.Abs(gDataReal_Ori[i,0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] + L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Ori[i,0];
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] + L / 2;
                    }
                }
                else if ((angle > (45 + delta_angle)) && (angle < (135 - delta_angle)))//3
                {
                    gDataReal_Gdi[i,0] = gDataReal_Ori[i,0];
                    gDataReal_Gdi[i,1] = gDataReal_Err[i,1] + L / 2;
                }
                else if ((angle >= (135 - delta_angle)) && (angle <= 135))//4
                {
                    if (Math.Abs(gDataReal_Ori[i,0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] - L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Ori[i,0];
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] + L / 2;
                    }
                }
                else if ((angle >= 135) && (angle <= (135 + delta_angle)))//4
                {
                    if (Math.Abs(gDataReal_Ori[i,0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] - L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] + L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] - L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Ori[i,1];
                    }
                }
                else if ((angle > (135 + delta_angle)) && (angle < (225 - delta_angle)))//5
                {
                    gDataReal_Gdi[i,0] = gDataReal_Err[i,0] - L / 2;
                    gDataReal_Gdi[i,1] = gDataReal_Ori[i,1];
                }
                else if ((angle >= (225 - delta_angle)) && (angle <= 225))//6
                {
                    if (Math.Abs(gDataReal_Ori[i,0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] - L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] - L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Ori[i,1];
                    }
                }
                else if ((angle >= 225) && (angle <= (225 + delta_angle)))//6
                {
                    if (Math.Abs(gDataReal_Ori[i,0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] - L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Ori[i,0];
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] - L / 2;
                    }
                }
                else if (((angle > (225 + delta_angle)) && (angle <= 270)) || ((angle >= -90) && (angle < (-45 - delta_angle))))//7
                {
                    gDataReal_Gdi[i,0] = gDataReal_Ori[i,0];
                    gDataReal_Gdi[i,1] = gDataReal_Err[i,1] - L / 2;
                }
                else if ((angle >= (-45 - delta_angle)) && (angle <= -45))//8
                {
                    if (Math.Abs(gDataReal_Ori[i,0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] + L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Ori[i,0];
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] - L / 2;
                    }
                }
                else if ((angle >= -45) && (angle <= (-45 + delta_angle)))//8
                {
                    if (Math.Abs(gDataReal_Ori[i,0]) > L / 2 && Math.Abs(gDataReal_Ori[i,1]) > L / 2)
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] + L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Err[i,1] - L / 2;
                    }
                    else
                    {
                        gDataReal_Gdi[i,0] = gDataReal_Err[i,0] + L / 2;
                        gDataReal_Gdi[i,1] = gDataReal_Ori[i,1];
                    }
                }
                gDataReal_Gdi[i,0] = gDataReal_Gdi[i,0] * multiple;
                gDataReal_Gdi[i,1] = gDataReal_Gdi[i,1] * multiple;
            }
            return new float[] { max_error, min_error, avg_error, unitdiv };
        }



        /* 未使用
        private void rotate_diamond_to_square() //把菱形旋轉成方形
        {
            //x'=x*cos-y*sin        y'=y*cos+x*sin
            double theta1 = -45 * 2 * Math.PI / 360;//順轉45度
            int i = 0;
            for (i = 0; i < gDataReal.GetLength(0); i++)
            {
                gDataReal_Origin[i][0] = (float)(gDataReal_Origin[i][0] * Math.Cos(theta1) - gDataReal_Origin[i][1] * Math.Sin(theta1));
                gDataReal_Origin[i][1] = (float)(gDataReal_Origin[i][1] * Math.Cos(theta1) + gDataReal_Origin[i][0] * Math.Sin(theta1));
            }
        }
        private void rotate_square_to_diamond() //把方形旋轉回菱形
        {
            //x'=x*cos-y*sin        y'=y*cos+x*sin
            double theta1 = 45 * 2 * Math.PI / 360;//順轉45度
            int i = 0;
            for (i = 0; i < gDataReal.GetLength(0); i++)
            {
                gDataReal_Gdi[i][0] = (float)(gDataReal_Gdi[i][0] * Math.Cos(theta1) - gDataReal_Gdi[i][1] * Math.Sin(theta1));
                gDataReal_Gdi[i][1] = (float)(gDataReal_Gdi[i][1] * Math.Cos(theta1) + gDataReal_Gdi[i][0] * Math.Sin(theta1));
            }
        }
        */
    }
}
