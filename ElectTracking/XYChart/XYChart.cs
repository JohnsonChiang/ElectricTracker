using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XYChart
{
    public partial class XYChart : UserControl
    {
        private ArrayList gPathList;
        private ChartStyle gChartStyle;
        private LegendStyle gLegendStyle;

        private eNumElecTrace gStandardType = eNumElecTrace.Circle;
        private double gStandardLength = 10;
        private double gStandardRadius = 0;

        private PathData[] gPath;

        public XYChart()
        {
            InitializeComponent();       

            gPathList = new ArrayList();
            gChartStyle = new ChartStyle();
            gLegendStyle = new LegendStyle();
        }
        public ChartStyle ChartStyle
        {
            get { return gChartStyle; }
            set { gChartStyle = value; }
        }


        public void Path_Name(int index, string Name)
        {
            if (gPath == null) return;
            if (index >= gPath.Length) return;

            gPath[index].Name = Name;
            this.Invalidate();
        }
        public void Path_AddPattern(eNumElecTrace type, double Length, double Radius)       //unit:mm
        {
            if (Length <= 0) return;
            gStandardType = type;
            gStandardLength = Length;
            gStandardRadius = Radius;
        }
        public void Path_AddPt(int index, PointF pt)
        {
            if (gPath == null) return;
            if (index == 0) return;                 //0:為標準圓/標準方形/標準菱形
            if (index >= gPath.Length) return; 
            gPath[index].AddPoint(pt);
            this.Invalidate();
        }     

        public void PathList_Init(PathData[] path)
        {
            if (path == null) return;
            gPathList.Clear();
            for (int i = 0; i < path.Length; i++)
            {
                gPathList.Add(path[i]);
                if (path[i].Name == "Default Name") path[i].Name = string.Format("DataSeries{0}", i);
            }
            gPath = path;
        }       
        public void PathList_Remove()
        {
            if (gPathList == null) return;
            gPathList.Clear();
            this.Invalidate();
        }
        public void Fit()
        {
            RectangleF newRect = new RectangleF();
            RectangleF tmp = new RectangleF();
            for (int i = 0; i < gPath.Length; i++)
            {
                tmp = gPath[i].GetRectangle();
                if (tmp.Width == 0 || tmp.Height == 0) continue;

                if (i > 0)
                {
                    if (tmp.X < newRect.X) newRect.X = tmp.X;
                    if (tmp.Y < newRect.Y) newRect.Y = tmp.Y;
                    if (tmp.Right > newRect.Right) newRect.Width = tmp.Right - newRect.X;
                    if (tmp.Bottom > newRect.Bottom) newRect.Height = tmp.Bottom - newRect.Y;
                }
                else
                {
                    newRect = tmp;
                }
            }
            gChartStyle.Rect_ChartAreaInUserDefine = newRect;
            this.Invalidate();
        }

        protected override void OnLoad(EventArgs e)
        {
            Rectangle A = this.ClientRectangle;
            //A.Inflate(-20, -20);

            gChartStyle.BackColor_Chart = this.BackColor;
            gChartStyle.BorderColor_ChartArea = this.BackColor;
            gChartStyle.Rect_ChartAreaInScreen = this.ClientRectangle;
            gChartStyle.Rect_PlotAreaInScreen = A;
            gChartStyle.Rect_ChartAreaInUserDefine = new RectangleF(-10f, -10f, 20, 20);

            gLegendStyle.IsVisible_Legend = true;
            gLegendStyle.Position_Legend = LegendStyle.eNumLegendPos.TopRight;
            //lg.IsVisible_Legend = false;

            //gPath = new PathData[2];    
            //gPath[0] = new PathData();
            //gPath[0].PenStyle.PenColor = Color.Black;
            //gPath[0].PenStyle.DashStyle = DashStyle.Solid;
            //gPath[0].PenStyle.Width = 1f;

            //PathList_Init(gPath);

            base.OnLoad(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            setPlotArea(g);
            gChartStyle.DrawChart(g);
            
            g.Clip = new Region(gChartStyle.Rect_PlotAreaInScreen);     // limit the scope of the drawing            
            drawPaths(g);                                               // Draw all points     
      
            gLegendStyle.DrawLegend(g, gPathList, gChartStyle);         // Draw Legend

            g.Dispose();
            //base.OnPaint(e);
        }
        private void drawPaths(Graphics g)
        {
            // Plot Standard Pattern:
            if (gPath == null) return;
            if (gPath[0].PenStyle.IsVisible == true)
            {
                Pen aPen = new Pen(gPath[0].PenStyle.PenColor, gPath[0].PenStyle.Width);
                aPen.DashStyle = gPath[0].PenStyle.DashStyle;

                float halfL = (float)gStandardLength / 2;

                switch (gStandardType)
                {
                    case eNumElecTrace.Circle:
                        g.DrawEllipse(aPen, -halfL, -halfL, halfL * 2, halfL * 2);
                        //g.DrawEllipse(aPen,new RectangleF(gChartStyle.ConvertPoint(new PointF(0,0)),);
                        break;
                    case eNumElecTrace.Square:
                        //g.DrawRectangle(aPen, -halfL, -halfL, halfL * 2, halfL * 2);
                        break;
                    case eNumElecTrace.Diamond:
                        break;
                }
                aPen.Dispose();
            }
            // Plot lines:
            foreach (PathData pd in gPathList)
            {                
                if (pd.PenStyle.IsVisible == true)
                {
                    Pen aPen = new Pen(pd.PenStyle.PenColor, pd.PenStyle.Width);
                    aPen.DashStyle = pd.PenStyle.DashStyle;
                    for (int i = 1; i < pd.PointList.Count; i++)
                    {
                        g.DrawLine(aPen,
                            gChartStyle.ConvertPoint((PointF)pd.PointList[i - 1]),
                            gChartStyle.ConvertPoint((PointF)pd.PointList[i]));
                    }
                    aPen.Dispose();
                }
            }
        }
        private void setPlotArea(Graphics g)
        {
            // Set PlotArea:
            int xOffset = gChartStyle.Rect_ChartAreaInScreen.Width / 10;
            int yOffset = gChartStyle.Rect_ChartAreaInScreen.Height / 10;
            // Define the plot area:
            int plotX = gChartStyle.Rect_ChartAreaInScreen.X + xOffset;
            int plotY = gChartStyle.Rect_ChartAreaInScreen.Y + yOffset;
            int plotWidth = gChartStyle.Rect_ChartAreaInScreen.Width - 2 * xOffset;
            int plotHeight = gChartStyle.Rect_ChartAreaInScreen.Height - 2 * yOffset;
            gChartStyle.Rect_PlotAreaInScreen = new Rectangle(plotX, plotY, plotWidth, plotHeight);
        }
        

        public enum eNumElecTrace
        {
            Circle,Square,Diamond
        }
    }
    public class PathData
    {
        public PathData()
        {
            Name = "Default Name";
            PenStyle = new PenStyle();
            PointList = new ArrayList();
        }

        public string Name
        {
            get;
            set;
        }
        public PenStyle PenStyle
        {
            get;
            set;
        }
        public ArrayList PointList
        {
            get;
            set;
        }
        public RectangleF GetRectangle()
        {
            if (PointList.Count > 0)
            {
                var x = PointList.OfType<PointF>().Min(point => point.X);
                var y = PointList.OfType<PointF>().Min(point => point.Y);
                var x_max = PointList.OfType<PointF>().Max(point => point.X);
                var y_max = PointList.OfType<PointF>().Max(point => point.Y);
                return new RectangleF(x, y, x_max - x, y_max - y);
            }
            else return new RectangleF(0, 0, 0, 0);
        }
        public void AddPoint(PointF pt)
        {
            PointList.Add(pt);            
        }
    }
    public class PenStyle
    {
        public PenStyle()
        {
            IsVisible = true;
            Width = 1.0f;
            PenColor = Color.Black;
        }

        public bool IsVisible
        {
            get;
            set;
        }
        public DashStyle DashStyle
        {
            get;
            set;
        }
        public float Width
        {
            get;
            set;
        }
        public Color PenColor
        {
            get;
            set;
        }
    }
    public class ChartStyle
    { 
        public ChartStyle()
        {
            Rect_ChartAreaInScreen = new Rectangle(0, 0, 10, 10);
            Rect_PlotAreaInScreen = new Rectangle(0, 0, 10, 10);
            Rect_ChartAreaInUserDefine = new RectangleF(-10f, -10f, 10f, 10f);

            BackColor_Chart = SystemColors.Control;
            BorderColor_ChartArea = Color.Black;
            BackColor_PlotArea = Color.White;
            BorderColor_PlotArea = Color.Black;
            Color_AxisLine = Color.SlateGray;
            Color_GridLine = Color.LightGray;

            FontColor_Title = Color.Black;
            FontColor_Label = Color.Black;
            FontColor_Tick = Color.Black;
            Font_Title = new Font("Arial", 12, FontStyle.Regular);
            Font_Label = new Font("Arial", 10, FontStyle.Regular);
            Font_Tick = new Font("Arial", 8, FontStyle.Regular);

            Format_Tick = "0.000";

            Text_Title = "Title";
            Text_LabelX = "X Axis";
            Text_LabelY = "Y Axis";

            IsVisible_GridX = true;
            IsVisible_GridY = true;
            Tick_X = 1f;
            Tick_Y = 1f;
            
            DashStyle_GridLine = DashStyle.Solid;
        }

        public string Format_Tick
        {
            get;
            set;
        }
        public Font Font_Title
        {
            get;
            set;
        }          
        public Font Font_Label
        {
            get;
            set;
        }    
        public Font Font_Tick
        {
            get;
            set;
        }
        public Color FontColor_Title
        {
            get;
            set;
        }
        public Color FontColor_Label
        {
            get;
            set;
        }
        public Color FontColor_Tick
        {
            get;
            set;
        }
        public Color BackColor_Chart
        {
            get;
            set;
        }
        public Color BackColor_PlotArea
        {
            get;
            set;
        }
        public Color BorderColor_ChartArea
        {
            get;
            set;
        }
        public Color BorderColor_PlotArea
        {
            get;
            set;
        }      
        public Color Color_GridLine
        {
            get;
            set;
        }
        public Color Color_AxisLine
        {
            get;
            set;
        }
        public Rectangle Rect_PlotAreaInScreen
        {
            get;
            set;
        }
        public Rectangle Rect_ChartAreaInScreen
        {
            get;
            set;
        }
        public RectangleF Rect_ChartAreaInUserDefine
        {
            get;
            set;
        }
        public DashStyle DashStyle_GridLine
        {
            get;
            set;
        }
        public bool IsVisible_GridX
        {
            get;
            set;
        }
        public bool IsVisible_GridY
        {
            get;
            set;
        }
        public string Text_Title
        {
            get;
            set;
        }
        public string Text_LabelX
        {
            get;
            set;
        }
        public string Text_LabelY
        {
            get;
            set;
        }
        public bool Tick_Auto
        {
            get;
            set;
        }
        public float Tick_X
        {
            get;
            set;
        }
        public float Tick_Y
        {
            get;
            set;
        }

        public void DrawChart(Graphics g)
        {
            //========================== Draw ChartArea:============================================
            Pen aPen = new Pen(BorderColor_ChartArea, 1f);
            SolidBrush aBrush = new SolidBrush(BackColor_Chart);
            g.FillRectangle(aBrush, Rect_ChartAreaInScreen);
            g.DrawRectangle(aPen, Rect_ChartAreaInScreen);

            //========================== Draw PlotArea:=============================================
            aPen = new Pen(BorderColor_PlotArea, 1f);
            aBrush = new SolidBrush(BackColor_PlotArea);
            g.FillRectangle(aBrush, Rect_PlotAreaInScreen);
            g.DrawRectangle(aPen, Rect_PlotAreaInScreen);

            aPen = new Pen(Color_AxisLine, 2f);
            aPen.DashStyle = DashStyle.Solid;
            g.DrawLine(aPen, 
                ConvertPoint(new PointF(0f, Rect_ChartAreaInUserDefine.Bottom)), 
                ConvertPoint(new PointF(0f, Rect_ChartAreaInUserDefine.Top)));
            g.DrawLine(aPen,
                ConvertPoint(new PointF(Rect_ChartAreaInUserDefine.Left, 0f)), 
                ConvertPoint(new PointF(Rect_ChartAreaInUserDefine.Right, 0f)));
            //========================== Create vertical gridlines:=================================
            float fX, fY;
            SizeF tickFontSize = g.MeasureString("A", Font_Tick);

            if (Tick_Auto == true)
            {
                Tick_X = Rect_ChartAreaInUserDefine.Width / 10;
                Tick_Y = Rect_ChartAreaInUserDefine.Height / 10;
            }

            if (IsVisible_GridY == true)
            {
                aPen = new Pen(Color_GridLine, 1f);
                aPen.DashStyle = DashStyle_GridLine;

                for (fX = Rect_ChartAreaInUserDefine.Left + Tick_X; fX < Rect_ChartAreaInUserDefine.Right; fX += Tick_X)
                {
                    g.DrawLine(aPen, 
                        ConvertPoint(new PointF(fX, Rect_ChartAreaInUserDefine.Bottom)), 
                        ConvertPoint(new PointF(fX, Rect_ChartAreaInUserDefine.Top)));
                }
            }

            //========================= Create horizontal gridlines:================================
            if (IsVisible_GridX == true)
            {
                aPen = new Pen(Color_GridLine, 1f);
                aPen.DashStyle = DashStyle_GridLine;
                for (fY = Rect_ChartAreaInUserDefine.Top + Tick_Y; fY < Rect_ChartAreaInUserDefine.Bottom; fY += Tick_Y)
                {
                    g.DrawLine(aPen, 
                        ConvertPoint(new PointF(Rect_ChartAreaInUserDefine.Left, fY)), 
                        ConvertPoint(new PointF(Rect_ChartAreaInUserDefine.Right, fY)));
                }
            }


            //========================= Create the x-axis tick marks:===============================
            aBrush = new SolidBrush(FontColor_Tick);
            for (fX = Rect_ChartAreaInUserDefine.Left; fX <= Rect_ChartAreaInUserDefine.Right; fX += Tick_X)
            {
                PointF yAxisPoint = ConvertPoint(new PointF(fX, Rect_ChartAreaInUserDefine.Top));
                g.DrawLine(Pens.Black, yAxisPoint, new PointF(yAxisPoint.X, yAxisPoint.Y - 5f));

                StringFormat sFormat = new StringFormat();
                sFormat.Alignment = StringAlignment.Far;
                SizeF sizeXTick = g.MeasureString(fX.ToString(), Font_Tick);
                g.DrawString(fX.ToString(Format_Tick), Font_Tick, aBrush,
                    new PointF(yAxisPoint.X + sizeXTick.Width / 2, yAxisPoint.Y + 4f), sFormat);
            }

            //========================= Create the y-axis tick marks:===============================
            for (fY = Rect_ChartAreaInUserDefine.Top; fY <= Rect_ChartAreaInUserDefine.Bottom; fY += Tick_Y)
            {
                PointF xAxisPoint = ConvertPoint(new PointF(Rect_ChartAreaInUserDefine.Left, fY));
                g.DrawLine(Pens.Black, xAxisPoint, new PointF(xAxisPoint.X + 5f, xAxisPoint.Y));

                StringFormat sFormat = new StringFormat();
                sFormat.Alignment = StringAlignment.Far;
                g.DrawString(fY.ToString(Format_Tick), Font_Tick, aBrush, 
                    new PointF(xAxisPoint.X - 3f, xAxisPoint.Y - tickFontSize.Height / 2), sFormat);
            }
            aPen.Dispose();
            aBrush.Dispose();

            //========================= Add label's & tiles's string: =============================================
            drawLabels(g);
        }
        public PointF ConvertPoint(PointF pt)
        {
            PointF aPoint = new PointF();
            // out of the plot area
            //if (pt.X < gDisplayRange.Left || pt.X > gDisplayRange.Right || pt.Y < gDisplayRange.Bottom || pt.Y > gDisplayRange.Top)
            //{
            //    pt.X = Single.NaN;
            //    pt.Y = Single.NaN;
            //}            
            aPoint.X = Rect_PlotAreaInScreen.X 
                + (pt.X - Rect_ChartAreaInUserDefine.Left) * Rect_PlotAreaInScreen.Width / (Rect_ChartAreaInUserDefine.Right - Rect_ChartAreaInUserDefine.Left);
            aPoint.Y = Rect_PlotAreaInScreen.Bottom 
                - (pt.Y - Rect_ChartAreaInUserDefine.Top) * Rect_PlotAreaInScreen.Height / (Rect_ChartAreaInUserDefine.Bottom - Rect_ChartAreaInUserDefine.Top);
            return aPoint;
        }

        private void drawLabels(Graphics g)
        {
            float xOffset = Rect_ChartAreaInScreen.Width / 30.0f;
            float yOffset = Rect_ChartAreaInScreen.Height / 30.0f;

            SizeF sizeLabelFont = g.MeasureString("A", Font_Label);
            SizeF sizeTitleFont = g.MeasureString("A", Font_Title);
            SizeF sizeString = g.MeasureString(Text_LabelX, Font_Label);

            //======================= Add x-axis label:=============================================
            SolidBrush aBrush = new SolidBrush(FontColor_Label);
            g.DrawString(Text_LabelX, Font_Label, aBrush, new Point(Rect_PlotAreaInScreen.Left + Rect_PlotAreaInScreen.Width / 2 - (int)sizeString.Width / 2, Rect_ChartAreaInScreen.Bottom - (int)yOffset - (int)sizeLabelFont.Height));

            //======================= Add y-axis label:=============================================
            StringFormat sFormat = new StringFormat();
            sFormat.Alignment = StringAlignment.Center;
            sizeString = g.MeasureString(Text_LabelY, Font_Label);

            // Save the state of the current Graphics object
            GraphicsState gState = g.Save();
            g.TranslateTransform(xOffset, yOffset + sizeTitleFont.Height + yOffset / 3 + Rect_PlotAreaInScreen.Height / 2);
            g.RotateTransform(-90);
            g.DrawString(Text_LabelY, Font_Label, aBrush, 0, 0, sFormat);

            // Restore it:
            g.Restore(gState);

            //======================= Add title:====================================================
            aBrush = new SolidBrush(FontColor_Title);
            sizeString = g.MeasureString(Text_Title, Font_Title);
            if (Text_Title.ToUpper() != "NO TITLE")
            {
                g.DrawString(Text_Title, Font_Title, aBrush, new Point(Rect_PlotAreaInScreen.Left + Rect_PlotAreaInScreen.Width / 2 - (int)sizeString.Width / 2, Rect_ChartAreaInScreen.Top + (int)yOffset));
            }
            aBrush.Dispose();
        }
    }
    public class LegendStyle
    {
        public enum eNumLegendPos
        {
            Top,
            TopLeft,
            Left,
            BottomLeft,
            Bottom,
            BottomRight,
            Right,
            TopRight
        }

        public LegendStyle()
        {
            Position_Legend = eNumLegendPos.TopRight;
            Color_Text = Color.Black;
            IsVisible_Legend = false;
            IsVisible_Border = true;
            BackColor_Legend = Color.White;
            BorderColor_Legend = Color.Black;
            Font_Legend = new Font("Arial", 8, FontStyle.Regular);
        }

        public eNumLegendPos Position_Legend
        {
            get;
            set;
        }
        public Font Font_Legend
        {
            get;
            set;
        }
        public Color BackColor_Legend
        {
            get;
            set;
        }
        public Color BorderColor_Legend
        {
            get;
            set;
        }
        public Color Color_Text
        {
            get;
            set;
        }
        public bool IsVisible_Legend
        {
            get;
            set;
        }
        public bool IsVisible_Border
        {
            get;
            set;
        }

        public void DrawLegend(Graphics g, ArrayList pathList, ChartStyle cs)
        {
            if (pathList.Count < 1) return;
            if (!IsVisible_Legend) return;

            string[] legendLabels = new string[pathList.Count];
            int n = 0;
            foreach (PathData ds in pathList)
            {
                legendLabels[n] = ds.Name;
                n++;
            }

            // find the max. width of path's name string
            SizeF size = g.MeasureString(legendLabels[0], Font_Legend);
            float legendWidth = size.Width;
            for (int i = 0; i < legendLabels.Length; i++)
            {
                size = g.MeasureString(legendLabels[i], Font_Legend);
                float tempWidth = size.Width;
                if (legendWidth < tempWidth) legendWidth = tempWidth;
            }

            // define the legend region
            legendWidth = legendWidth + 50.0f;

            float offSet = 10;
            float xc = 0f;
            float yc = 0f;
            float hWidth = legendWidth / 2;
            float legendHeight = 18.0f * pathList.Count;
            float hHeight = legendHeight / 2;
            switch (Position_Legend)
            {
                case eNumLegendPos.Right:
                    xc = cs.Rect_PlotAreaInScreen.X + cs.Rect_PlotAreaInScreen.Width - offSet - hWidth;
                    yc = cs.Rect_PlotAreaInScreen.Y + cs.Rect_PlotAreaInScreen.Height / 2;
                    break;
                case eNumLegendPos.Top:
                    xc = cs.Rect_PlotAreaInScreen.X + cs.Rect_PlotAreaInScreen.Width / 2;
                    yc = cs.Rect_PlotAreaInScreen.Y + offSet + hHeight;
                    break;
                case eNumLegendPos.TopRight:
                    xc = cs.Rect_PlotAreaInScreen.X + cs.Rect_PlotAreaInScreen.Width - offSet - hWidth;
                    yc = cs.Rect_PlotAreaInScreen.Y + offSet + hHeight;
                    break;
                case eNumLegendPos.TopLeft:
                    xc = cs.Rect_PlotAreaInScreen.X + offSet + hWidth;
                    yc = cs.Rect_PlotAreaInScreen.Y + offSet + hHeight;
                    break;
                case eNumLegendPos.Bottom:
                    xc = cs.Rect_PlotAreaInScreen.X + cs.Rect_PlotAreaInScreen.Width / 2;
                    yc = cs.Rect_PlotAreaInScreen.Y + cs.Rect_PlotAreaInScreen.Height - offSet - hHeight;
                    break;
                case eNumLegendPos.BottomRight:
                    xc = cs.Rect_PlotAreaInScreen.X + cs.Rect_PlotAreaInScreen.Width - offSet - hWidth;
                    yc = cs.Rect_PlotAreaInScreen.Y + cs.Rect_PlotAreaInScreen.Height - offSet - hHeight;
                    break;
                case eNumLegendPos.BottomLeft:
                    xc = cs.Rect_PlotAreaInScreen.X + offSet + hWidth;
                    yc = cs.Rect_PlotAreaInScreen.Y + cs.Rect_PlotAreaInScreen.Height - offSet - hHeight;
                    break;
                case eNumLegendPos.Left:
                    xc = cs.Rect_PlotAreaInScreen.X + offSet + hWidth;
                    yc = cs.Rect_PlotAreaInScreen.Y + cs.Rect_PlotAreaInScreen.Height / 2;
                    break;
            }
            draw(g, xc, yc, hWidth, hHeight, pathList, cs);
        }

        private void draw(Graphics g, float xCenter, float yCenter, float hWidth, float hHeight, ArrayList pathList, ChartStyle cs)
        {
            float spacing = 8.0f;
            float textHeight = 8.0f;
            float htextHeight = textHeight / 2.0f;
            float lineLength = 30.0f;
            float hlineLength = lineLength / 2.0f;

            Rectangle legendRectangle;
            Pen aPen = new Pen(BorderColor_Legend, 1f);
            SolidBrush aBrush = new SolidBrush(BackColor_Legend);

            if (IsVisible_Legend)
            {
                legendRectangle = new Rectangle((int)xCenter - (int)hWidth, (int)yCenter - (int)hHeight, (int)(2.0f * hWidth), (int)(2.0f * hHeight));
                g.FillRectangle(aBrush, legendRectangle);

                if (IsVisible_Border) g.DrawRectangle(aPen, legendRectangle);

                int n = 1;
                foreach (PathData p in pathList)
                {
                    aPen = new Pen(p.PenStyle.PenColor, p.PenStyle.Width);
                    aPen.DashStyle = p.PenStyle.DashStyle;            
                    
                    // Draw lines and symbols:
                    float xSymbol = legendRectangle.X + spacing + hlineLength;
                    float xText = legendRectangle.X + 2 * spacing + lineLength;
                    float yText = legendRectangle.Y + n * spacing + (2 * n - 1) * htextHeight;  
                    
                    //float hsize = 5f;
                    //PointF[] pts = new PointF[4];
                    //pts[0] = new PointF(xSymbol - hsize,                    yText - hsize);
                    //pts[1] = new PointF(xSymbol + hsize,                    yText - hsize);
                    //pts[2] = new PointF(xSymbol + hsize,                    yText + hsize);
                    //pts[3] = new PointF(xSymbol - hsize,                    yText + hsize);
                    //g.FillPolygon(aBrush, pts);
                    //g.DrawPolygon(aPen, pts);

                    PointF ptStart = new PointF(legendRectangle.X + spacing, yText);
                    PointF ptEnd = new PointF(legendRectangle.X + spacing + lineLength, yText);
                    g.DrawLine(aPen, ptStart, ptEnd);

              

                    // Draw text:
                    StringFormat sFormat = new StringFormat();
                    sFormat.Alignment = StringAlignment.Near;
                    g.DrawString(p.Name, Font_Legend, new SolidBrush(Color_Text), new PointF(xText, yText - 8), sFormat);

                    n++;
                }
            }
            aPen.Dispose();
            aBrush.Dispose();
        }
    }
}


