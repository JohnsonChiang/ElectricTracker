using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ElectTracking
{
    public partial class ETrack : UserControl
    {         
        private GraphicsPath gPath = new GraphicsPath();                //GraphicsPath 物件
        PointF gGraphCenter = new PointF(0F, 0F);                       //畫布中心點的座標
        float gGraphScale = 1;                                          //繪圖的縮放比例 (視窗/畫布)
        SizeF gGraphSize;                                               //畫布尺寸
        RectangleF gGraphRectangle;

        private Font LabelFont = new Font("Arial", 10, FontStyle.Bold);   
        Pen gPen_FingerLine = new Pen(Color.Blue , 2);
        Pen gPen_Dash = new Pen(SystemColors.ButtonShadow , 0);
        Pen gPen_Path = new Pen(Color.Black, 2F);


        PointF gPoint_Last;                 //繪製的最後一點座標    
        float gDistance = 50f;              //格線間距

        Keys gKey_Modifiers;                //鍵盤：輔助鍵(SHIFT、CTRL、ALT..)
        MouseButtons gMouseButton;          //滑鼠：按鍵
        Point gPoint_MouseDown;             //滑鼠：MouseDown 觸發時的座標
        Point gPoint_MouseCurrent;          //滑鼠：MouseMove 觸發時的座標


        public ETrack()
        {
            InitializeComponent();

            gGraphRectangle = new RectangleF(-this.Width / 2, -this.Height / 2, this.Width, this.Height);

            Grid.Type_GridLine = Grid.eNumGridType.Full;
            Grid.Type_GridValue = Grid.eNumTextType.Axis;
            Grid.Interval_GridValue = 1;


            gPen_FingerLine.EndCap = LineCap.ArrowAnchor;
            gPen_Dash.DashStyle = DashStyle.Dash;
        }

        //private void pan(float incrX, float incrY)
        //{
        //    gGraphRectangle.X = gGraphRectangle.X - incrX * Grid.Scale;
        //    gGraphRectangle.Y = gGraphRectangle.Y + incrY * Grid.Scale;

        //    this.Invalidate();
        //}
        //private void scale(RectangleF rect)
        //{
        //    gGraphRectangle = rect;
        //    this.Invalidate();
        //}
        //private void scale(float scale, PointF center)
        //{
        //    gGraphScale = scale;
        //    gGraphCenter = center;
        //    this.Invalidate();
        //}


        //public void CaptureAll()
        //{
        //    buffer.Save("D:\\AAA.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        //}
        public void CaptureScreen()
        {
            Graphics myGraphics = this.CreateGraphics();
            Bitmap memoryImage = new Bitmap(this.ClientSize.Width, this.ClientSize.Height, myGraphics);
            Graphics memoryGraphics = Graphics.FromImage(memoryImage);

            Rectangle R = this.RectangleToScreen(this.ClientRectangle); //(new Rectangle(0, 0, this.Width, this.Height));
            memoryGraphics.CopyFromScreen(R.X, R.Y, 0, 0, this.ClientSize );
            memoryImage.Save("D:\\BBB.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        private bool gHasSinglePt = false;
        public void DrawLine(PointF NewPoint)
        {
            if (gHasSinglePt)
            {
                Graphics gp = this.CreateGraphics();
                gp.Transform = Grid.Matrix_Corrd;
                gp.DrawLine(gPen_Path, gPoint_Last, NewPoint);
                gp.Dispose();

                gPath.AddLine(gPoint_Last, NewPoint);
            }
            else
            {
                gHasSinglePt = true;
            }
            gPoint_Last = NewPoint;
        }
        public void DrawLines(PointF[] Points)
        {
            Graphics gp = this.CreateGraphics();
            gp.Transform = Grid.Matrix_Corrd;
            gp.DrawLines(gPen_Path, Points);           
            gp.Dispose();

            gPath.AddLines(Points);
        }
        public void DrawCircle(float CentX, float CentY, float R)
        {
            Graphics gp = this.CreateGraphics();
            gp.Transform = Grid.Matrix_Corrd;
            gp.DrawEllipse(gPen_Path, new RectangleF(CentX - R, CentY - R, 2 * R, 2 * R));
            gp.Dispose();

            gPath.AddEllipse(new RectangleF(CentX - R, CentY - R, 2 * R, 2 * R));
        }
        public void View_Fit()
        {
            RectangleF rect = gPath.GetBounds();
            gGraphRectangle = rect;
            this.Invalidate();
        }

        private void ETrack_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                switch (gKey_Modifiers)
                {
                    case Keys.Shift:
                    case Keys.Control:
                        gPoint_MouseDown = e.Location;
                        break;
                    default: return;
                }
            }
            gMouseButton = e.Button;
        }        
        private void ETrack_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left && gMouseButton == MouseButtons.Left)
            {
                switch (gKey_Modifiers)
                {
                    case Keys.Shift:
                        gPoint_MouseCurrent = e.Location;
                        this.Invalidate();
                        break;
                    case Keys.Control:
                        if (e.Location.X > gPoint_MouseDown.X)
                        {
                            gPoint_MouseCurrent = e.Location;                            
                            this.Invalidate();
                        }
                        break;
                    default: return;
                }
            }
        }
        private void ETrack_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left && gMouseButton == MouseButtons.Left)
            {
                switch (gKey_Modifiers)
                {
                    case Keys.Shift:
                        gGraphRectangle.X = gGraphRectangle.X - (e.Location.X - gPoint_MouseDown.X) * Grid.Scale;
                        gGraphRectangle.Y = gGraphRectangle.Y + (e.Location.Y - gPoint_MouseDown.Y) * Grid.Scale;
                        this.Invalidate();
                        break;
                    case Keys.Control:
                        RectangleF newRect = new RectangleF();
                        newRect.X = Grid.GraphRect.X + Math.Min(gPoint_MouseDown.X, e.Location.X) * Grid.Scale;
                        newRect.Width = Math.Abs(e.Location.X - gPoint_MouseDown.X) * Grid.Scale;
                        newRect.Height = Math.Abs(e.Location.X - gPoint_MouseDown.X) * this.ClientSize.Height / this.ClientSize.Width * Grid.Scale;
                        newRect.Y = Grid.GraphRect.Bottom - gPoint_MouseDown.Y * Grid.Scale - newRect.Height;
                        if (newRect.Width >= 5) gGraphRectangle = newRect;
                        this.Invalidate();
                        break;
                    default: return;
                }
            }
            gMouseButton = MouseButtons.None;
        }
        private void ETrack_KeyDown(object sender, KeyEventArgs e)
        {
            gKey_Modifiers = e.Modifiers;
        }
        private void ETrack_KeyUp(object sender, KeyEventArgs e)
        {
            gKey_Modifiers = Keys.None;
            this.Invalidate();
        }

        private Font gLabel_Font = new Font("Arial", 12F, FontStyle.Bold);
        private Font gTick_Font = new Font("Arial", 8F, FontStyle.Regular);
        private string gLabel_X = "X-Axis";
        private string gLabel_Y = "Y-Axis";
        private void ETrack_Paint(object sender, PaintEventArgs e)
        {
            this.SuspendLayout();
            Graphics g = e.Graphics;

            Rectangle Rect = this.ClientRectangle;
            SizeF sizeLabel = g.MeasureString(gLabel_X, gLabel_Font, 150);
            SizeF sizeTick = g.MeasureString("-000.000", gTick_Font, 10);

            int space = (int)Math.Ceiling(sizeLabel.Height + sizeTick.Width / 2);
            Rect.Inflate(-space, -space);
            Rect.X += space - 2;
            Rect.Y -= space - 2;

            g.DrawRectangle(Pens.Black, Rect);
            g.TranslateTransform(2 * space - 2, Rect.Height + 2);

            //======================================================================================
            GraphicsContainer axisContainer = e.Graphics.BeginContainer();
            g.DrawString(gLabel_X, gLabel_Font, Brushes.Black, Rect.Width / 2, 0);
            g.RotateTransform(-90);
            g.DrawString(gLabel_Y, gLabel_Font, Brushes.Black, Rect.Height / 2, -space);
            g.EndContainer(axisContainer);
            //======================================================================================

            Grid.DrawGrid(Rect, Rect, 20, g);


            //================= OK ==========================
            #region OK
            //----------------------- 畫布 ---------------------------------------------------------
            //Grid.Draw(gGraphRectangle, this.ClientRectangle, gDistance, e.Graphics);

            //----------------------- 座標物件 ------------------------------------------------------
            //e.Graphics.Transform = Grid.Matrix_Corrd;
            //e.Graphics.DrawPath(gPen_Path, gPath);


            //----------------------- 拖拉 ---------------------------------------------------------
            //if (gMouseButton == MouseButtons.Left)
            //{
            //    e.Graphics.ResetTransform();
            //    switch (gKey_Modifiers)
            //    {
            //        case Keys.Shift:
            //            e.Graphics.DrawLine(gPen_Dash, gPoint_MouseDown, gPoint_MouseCurrent);
            //            break;
            //        case Keys.Control:
            //            float Xmin = Math.Min(gPoint_MouseDown.X, gPoint_MouseCurrent.X);
            //            float Ymin = Math.Min(gPoint_MouseDown.Y, gPoint_MouseCurrent.Y);
            //            float Width = (float)Math.Abs(gPoint_MouseDown.X - gPoint_MouseCurrent.X);
            //            e.Graphics.DrawRectangle(gPen_Dash, Xmin, Ymin, Width, Width * this.ClientSize.Height / this.ClientSize.Width);
            //            break;
            //    }
            //}
            #endregion
            //================= OK ==========================
            //--------------------------------------------------------------------------------------
            g.Dispose();
            this.PerformLayout();
        }
    }
    class Grid
    {

        public enum eNumGridType { Full, Axis, Frame }
        public enum eNumTextType { None, Axis, Frame }

        static private int gInterval_GridValue = 1;
        static private Matrix gMatrix = new Matrix();
        static private float gScale = 1;
        static private RectangleF gGraphRect = new RectangleF();

        /// <summary>
        /// 繪圖座標陣列
        /// </summary>
        static public Matrix Matrix_Corrd
        {
            get { return gMatrix; }
        }
        /// <summary>
        /// 繪製格線類型
        /// </summary>
        static public eNumGridType Type_GridLine { get; set;  }
        /// <summary>
        /// 繪製格線數值位置
        /// </summary>
        static public eNumTextType Type_GridValue { get; set; }
        /// <summary>
        /// 間隔幾格標註尺寸
        /// </summary>
        static public int Interval_GridValue
        {
            get { return gInterval_GridValue; }
            set { gInterval_GridValue = value; }
        }
        /// <summary>
        /// 格線數值的字串格式
        /// </summary>
        static public string Format_GridValue { get; set; }
        /// <summary>
        /// 軸線線型
        /// </summary>
        static public Pen Pen_AxisLine { get; set; }
        /// <summary>
        /// 格線線型
        /// </summary>
        static public Pen Pen_GridLine { get; set; }
        /// <summary>
        /// 邊界線型
        /// </summary>
        static public Pen Pen_FrameLine { get; set; }
        /// <summary>
        /// 文字線型
        /// </summary>
        static public Pen Pen_String { get; set; }
        /// <summary>
        /// 格式數值線型
        /// </summary>
        static public Brush Brush_GridValue { get; set; }
        /// <summary>
        /// 標籤的字型
        /// </summary>
        static public Font Font_Label { get; set; }
        /// <summary>
        /// 文字的字型
        /// </summary>
        static public Font Font_String { get; set; }
        /// <summary>
        /// 格線數值的字型
        /// </summary>
        static public Font Font_GridValue { get; set; }
        /// <summary>
        /// 邊框留白距離
        /// </summary>
        static public int Space_Frame { get; set; }
        static public string X_Label { get; set; }
        static public string Y_Label { get; set; }
        static public float Scale { get { return gScale; } }
        static public RectangleF GraphRect { get { return gGraphRect; } }


        static public void DrawGrid(RectangleF sourRect, RectangleF destRect, float d, Graphics G)
        {
            if (G == null) return;
            if (Pen_AxisLine == null)
            {
                Pen_AxisLine = new Pen(Color.Black);
                Pen_AxisLine.Width = 0;
                Pen_AxisLine.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            }
            if (Pen_GridLine == null)
            {
                Pen_GridLine = new Pen(Color.Silver);
                Pen_GridLine.DashStyle = DashStyle.DashDot;
                Pen_GridLine.Width = 0;
            }

            gScale = Math.Max(sourRect.Width / destRect.Width, sourRect.Height / destRect.Height);
            if (gScale <= 0) return;

            sourRect.Width = destRect.Width * gScale;
            sourRect.Height = destRect.Height * gScale;

            GraphicsContainer gridContainer = G.BeginContainer();
            //---------------------------- 邊界值 --------------------------------------------------
            gGraphRect = sourRect;    
            //======================================================================================
            Matrix myMatrix = new Matrix();
            myMatrix.Scale(1 / gScale, -1 / gScale, MatrixOrder.Append);

            gMatrix.Translate(
                (gGraphRect.Width / 2 - (gGraphRect.Left + gGraphRect.Right) / 2) / gScale, 
                (gGraphRect.Height / 2 + (gGraphRect.Top + gGraphRect.Bottom) / 2) / gScale, MatrixOrder.Append);
            //myMatrix.Translate(10, 10, MatrixOrder.Append);
            G.Transform = myMatrix;

            G.DrawLine(Pens.Black, 5, 5, 50, 50);
            //----------------------------- 軸線 ---------------------------------------------------
            //G.DrawLine(Pen_AxisLine, 0, sourRect.Y, 0, sourRect.Top);         //垂直中線
            //G.DrawLine(Pen_AxisLine, sourRect.X, 0, sourRect.Right, 0);         //水平中線
            //----------------------------- 格線 ---------------------------------------------------
            //switch (Type_GridLine)
            //{
            //    case eNumGridType.Full:     //繪製全長格線            
            //        for (float i = -d; i > gGraphRect.X; i -= d) G.DrawLine(Pen_GridLine, i, gGraphRect.Y, i, gGraphRect.Top);  //垂直線往左
            //        for (float i = d; i < gGraphRect.Right; i += d) G.DrawLine(Pen_GridLine, i, gGraphRect.Y, i, gGraphRect.Top);  //垂直線往右
            //        for (float i = -d; i > gGraphRect.Y; i -= d) G.DrawLine(Pen_GridLine, gGraphRect.X, i, gGraphRect.Right, i);  //水平線往上
            //        for (float i = d; i < gGraphRect.Top; i += d) G.DrawLine(Pen_GridLine, gGraphRect.X, i, gGraphRect.Right, i);  //水平線往下
            //        break;
            //    case eNumGridType.Axis:   //僅繪製軸線上的格線
            //        Pen_GridLine.DashStyle = DashStyle.Solid;
            //        //--------------------- Grid ---------------------------------------------------
            //        for (float i = -d; i > gGraphRect.X; i -= d) G.DrawLine(Pen_GridLine, i, -d / 3, i, d / 3);  //垂直線往左
            //        for (float i = d; i < gGraphRect.Right; i += d) G.DrawLine(Pen_GridLine, i, -d / 3, i, d / 3);  //垂直線往右
            //        for (float i = -d; i > gGraphRect.Y; i -= d) G.DrawLine(Pen_GridLine, -d / 3, i, d / 3, i);  //水平線往上
            //        for (float i = d; i < gGraphRect.Top; i += d) G.DrawLine(Pen_GridLine, -d / 3, i, d / 3, i);  //水平線往下
            //        //------------------------------------------------------------------------------
            //        break;
            //    case eNumGridType.Frame:   //僅繪製邊框上的格線
            //        for (float i = -d; i > gGraphRect.X; i -= d) G.DrawLine(Pen_GridLine, i, gGraphRect.Y - d / 3, i, gGraphRect.Y + d / 3);  //垂直線往左
            //        for (float i = d; i < gGraphRect.Right; i += d) G.DrawLine(Pen_GridLine, i, gGraphRect.Y - d / 3, i, gGraphRect.Y + d / 3);  //垂直線往右
            //        for (float i = -d; i > gGraphRect.Y; i -= d) G.DrawLine(Pen_GridLine, gGraphRect.X - d / 3, i, gGraphRect.X + d / 3, i);  //水平線往上
            //        for (float i = d; i < gGraphRect.Top; i += d) G.DrawLine(Pen_GridLine, gGraphRect.X - d / 3, i, gGraphRect.X + d / 3, i);  //水平線往下
            //        break;
            //}
            //======================================================================================
            G.EndContainer(gridContainer);
        }

        static public void Draw(PointF graphCenter, SizeF graphSize, float graphScale, float d, Graphics G)
        {
            gScale = graphScale;
            //---------------------------------------------------
            if (X_Label == null) X_Label = "X-Axis";
            if (Y_Label == null) Y_Label = "Y-Axis";

            if(Space_Frame==0) Space_Frame=1;
            if (Font_Label == null) Font_Label = new Font("Arial", 12F, FontStyle.Bold);
            if (Font_String == null)Font_String = new Font("Arial", 12F);  
            if (Font_GridValue == null)Font_GridValue = new Font("Arial", 8F);   
            if (Brush_GridValue == null)Brush_GridValue = Brushes.Blue;   

            if (Pen_FrameLine == null)
            {
                Pen_FrameLine = new Pen(Color.Black);
                Pen_FrameLine.Width = 0;
            }
            if (Pen_AxisLine == null)
            {
                Pen_AxisLine = new Pen(Color.Black);
                Pen_AxisLine.Width = 0;
                Pen_AxisLine.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
            }
            if (Pen_GridLine == null)
            {
                Pen_GridLine = new Pen(Color.Silver);
                Pen_GridLine.DashStyle = DashStyle.DashDot;
                Pen_GridLine.Width = 0;
            }

            if (Pen_String == null)
            {
                Pen_String = new Pen(Color.Black);
                Pen_String.Width = 0;
            }     
            //--------------------------------------------------------------------------------------
            G.ResetTransform();

            gMatrix.Reset();
            gMatrix.Scale(1 / graphScale, -1 / graphScale, MatrixOrder.Append);
            gMatrix.Translate((graphSize.Width / 2 - graphCenter.X) / graphScale, (graphSize.Height / 2 + graphCenter.Y) / graphScale, MatrixOrder.Append);
            G.Transform = gMatrix;            
            ////----------------------------測試----------------------------------------------
            //G.DrawLine(Pens.Red, new Point(0, 0), new Point(25, 50));

            //---------------------------- 邊界值 --------------------------------------------------
            float Xmin = graphCenter.X - graphSize.Width / 2;
            float Xmax = graphCenter.X + graphSize.Width / 2;
            float Ymin = graphCenter.Y - graphSize.Height / 2;
            float Ymax = graphCenter.Y + graphSize.Height / 2;
            gGraphRect = new RectangleF(Xmin, Ymin, graphSize.Width , graphSize.Height);
            //----------------------------- 軸線 ---------------------------------------------------
            G.DrawLine(Pen_AxisLine, 0, Ymin, 0, Ymax);         //垂直中線
            G.DrawLine(Pen_AxisLine, Xmin, 0, Xmax, 0);         //水平中線
            //----------------------------- 格線 ---------------------------------------------------
            switch (Type_GridLine)
            {
                case eNumGridType.Full:     //繪製全長格線            
                    for (float i = -d; i > Xmin; i -= d) G.DrawLine(Pen_GridLine, i, Ymin, i, Ymax);  //垂直線往左
                    for (float i = d; i < Xmax; i += d) G.DrawLine(Pen_GridLine, i, Ymin, i, Ymax);  //垂直線往右
                    for (float i = -d; i > Ymin; i -= d) G.DrawLine(Pen_GridLine, Xmin, i, Xmax, i);  //水平線往上
                    for (float i = d; i < Ymax; i += d) G.DrawLine(Pen_GridLine, Xmin, i, Xmax, i);  //水平線往下
                    break;
                case eNumGridType.Axis:   //僅繪製軸線上的格線
                    Pen_GridLine.DashStyle = DashStyle.Solid;
                    //--------------------- Grid ---------------------------------------------------
                    for (float i = -d; i > Xmin; i -= d) G.DrawLine(Pen_GridLine, i, -d / 3, i, d / 3);  //垂直線往左
                    for (float i = d; i < Xmax; i += d) G.DrawLine(Pen_GridLine, i, -d / 3, i, d / 3);  //垂直線往右
                    for (float i = -d; i > Ymin; i -= d) G.DrawLine(Pen_GridLine, -d / 3, i, d / 3, i);  //水平線往上
                    for (float i = d; i < Ymax; i += d) G.DrawLine(Pen_GridLine, -d / 3, i, d / 3, i);  //水平線往下
                    //------------------------------------------------------------------------------
                    break;
                case eNumGridType.Frame:   //僅繪製邊框上的格線
                    for (float i = -d; i > Xmin; i -= d) G.DrawLine(Pen_GridLine, i, Ymin - d / 3, i, Ymin + d / 3);  //垂直線往左
                    for (float i = d; i < Xmax; i += d) G.DrawLine(Pen_GridLine, i, Ymin - d / 3, i, Ymin + d / 3);  //垂直線往右
                    for (float i = -d; i > Ymin; i -= d) G.DrawLine(Pen_GridLine, Xmin - d / 3, i, Xmin + d / 3, i);  //水平線往上
                    for (float i = d; i < Ymax; i += d) G.DrawLine(Pen_GridLine, Xmin - d / 3, i, Xmin + d / 3, i);  //水平線往下
                    break;
            }
            //----------------------------- 格線數值 ---------------------------------------------------
            SizeF sizeStr;
            if (gInterval_GridValue > 0)
            {
                Font font_Scale = new Font(Font_GridValue.FontFamily, Font_GridValue.Size * graphScale);
                int count = 0;
                switch (Type_GridValue)
                {
                    case  eNumTextType.Axis:             //繪製於軸線上
                        G.ScaleTransform(1, -1);
                        count = 0;
                        for (float i = -d; i > Xmin; i -= d)
                        {
                            count++;
                            if (count == gInterval_GridValue)
                            {
                                count = 0;
                                sizeStr = G.MeasureString(i.ToString(Format_GridValue), font_Scale);
                                G.DrawString(i.ToString(Format_GridValue), font_Scale, Brush_GridValue, new PointF(i - sizeStr.Width / 2, 0));
                            }
                        }
                        count = 0;
                        for (float i = d; i < Xmax; i += d)
                        {
                            count++;
                            if (count == gInterval_GridValue)
                            {
                                count = 0;
                                sizeStr = G.MeasureString(i.ToString(Format_GridValue), font_Scale);
                                G.DrawString(i.ToString(Format_GridValue), font_Scale, Brush_GridValue, new PointF(i - sizeStr.Width / 2, 0));
                            }
                        }
                        G.RotateTransform(-90);
                        count = 0;
                        for (float i = -d; i > Ymin; i -= d)
                        {
                            count++;
                            if (count == gInterval_GridValue)
                            {
                                count = 0;
                                sizeStr = G.MeasureString(i.ToString(Format_GridValue), font_Scale);
                                G.DrawString(i.ToString(Format_GridValue), font_Scale, Brush_GridValue, new PointF(i - sizeStr.Width / 2, -sizeStr.Height));
                            }
                        }
                        count = 0;
                        for (float i = d; i < Ymax; i += d)
                        {
                            count++;
                            if (count == gInterval_GridValue)
                            {
                                count = 0;
                                sizeStr = G.MeasureString(i.ToString(Format_GridValue), font_Scale);
                                G.DrawString(i.ToString(Format_GridValue), font_Scale, Brush_GridValue, new PointF(i - sizeStr.Width / 2, -sizeStr.Height));
                            }
                        }
                        //------------------------------------------------------------------------------
                        break;
                    case  eNumTextType.Frame :    //僅繪製邊框上的格線
                        //for (float i = -d; i > Xmin; i -= d) G.DrawLine(Pen_GridLine, i, Ymin - d / 3, i, Ymin + d / 3);  //垂直線往左
                        //for (float i = d; i < Xmax; i += d) G.DrawLine(Pen_GridLine, i, Ymin - d / 3, i, Ymin + d / 3);  //垂直線往右
                        //for (float i = -d; i > Ymin; i -= d) G.DrawLine(Pen_GridLine, Xmin - d / 3, i, Xmin + d / 3, i);  //水平線往上
                        //for (float i = d; i < Ymax; i += d) G.DrawLine(Pen_GridLine, Xmin - d / 3, i, Xmin + d / 3, i);  //水平線往下
                        G.ScaleTransform(1, -1);
                        count = 0;
                        for (float i = -d; i > Xmin; i -= d)
                        {
                            count++;
                            if (count == gInterval_GridValue)
                            {
                                count = 0;
                                sizeStr = G.MeasureString(i.ToString(Format_GridValue), font_Scale);
                                G.DrawString(i.ToString(Format_GridValue), font_Scale, Brush_GridValue, new PointF(i - sizeStr.Width / 2, -Ymin - sizeStr.Height));
                            }
                        }
                        count = 0;
                        for (float i = d; i < Xmax; i += d)
                        {
                            count++;
                            if (count == gInterval_GridValue)
                            {
                                count = 0;
                                sizeStr = G.MeasureString(i.ToString(Format_GridValue), font_Scale);
                                G.DrawString(i.ToString(Format_GridValue), font_Scale, Brush_GridValue, new PointF(i - sizeStr.Width / 2, -Ymin - sizeStr.Height));
                            }
                        }
                        G.RotateTransform(-90);
                        count = 0;
                        for (float i = -d; i > Ymin; i -= d)
                        {
                            count++;
                            if (count == gInterval_GridValue)
                            {
                                count = 0;
                                sizeStr = G.MeasureString(i.ToString(Format_GridValue), font_Scale);
                                G.DrawString(i.ToString(Format_GridValue), font_Scale, Brush_GridValue, new PointF(i - sizeStr.Width / 2, Xmin + sizeStr.Height));
                            }
                        }
                        count = 0;
                        for (float i = d; i < Ymax; i += d)
                        {
                            count++;
                            if (count == gInterval_GridValue)
                            {
                                count = 0;
                                sizeStr = G.MeasureString(i.ToString(Format_GridValue), font_Scale);
                                G.DrawString(i.ToString(Format_GridValue), font_Scale, Brush_GridValue, new PointF(i - sizeStr.Width / 2, Xmin + sizeStr.Height));
                            }
                        }
                        //------------------------------------------------------------------------------
                        break;
                }
            }
            //-------------------------- Frame ----------------------------------------------------- 
            G.ResetTransform();
            RectangleF Rect = new RectangleF(0, 0, graphSize.Width/graphScale , graphSize.Height/graphScale );
            Rect.Inflate(-Space_Frame, -Space_Frame);
            G.DrawRectangle(Pens.Black, Rect.X, Rect.Y, Rect.Width, Rect.Height);
            //------------------------- X Label ----------------------------------------------------
            sizeStr = G.MeasureString(X_Label, Font_Label, 150);
            float X = graphSize.Width/graphScale  / 2 - sizeStr.Width / 2;
            float Y = graphSize.Height/graphScale  - sizeStr.Height;
            G.DrawString(X_Label, Font_Label, Brushes.Black, X, Y);
            //------------------------- Y Label ----------------------------------------------------
            G.RotateTransform(-90);
            sizeStr = G.MeasureString(Y_Label, Font_Label, 150);
            X = -(graphSize.Height/graphScale  / 2 - sizeStr.Width / 2);
            Y = 0;
            G.DrawString(Y_Label, Font_Label, Brushes.Black, X, Y);
            
            //--------------------------- XY -------------------------------------------------------
            G.Transform = gMatrix;
        }

        static public void Draw(RectangleF rectGraph, RectangleF rectScreen, float distGraph, Graphics G)
        {
            float scaleX = rectGraph.Width / rectScreen.Width;
            float scaleY = rectGraph.Height / rectScreen.Height;
            gScale = Math.Max(scaleX, scaleY);
            if (gScale <= 0) return;

            rectGraph.Width = rectScreen.Width * gScale;
            rectGraph.Height = rectScreen.Height * gScale;
            Draw(new PointF((rectGraph.Left + rectGraph.Right) / 2, (rectGraph.Top + rectGraph.Bottom) / 2), rectGraph.Size, gScale, distGraph, G);
        }
    }
}
