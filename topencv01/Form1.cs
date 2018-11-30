using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;


namespace topencv01
{
    public partial class Form1 : Form
    {

        Image<Bgr, Byte> img1 = new Image<Bgr, Byte>("c:/temp/t13x3.jpg");
        //Image<Bgr, Byte> img1 = new Image<Bgr, Byte>("c:/temp/test1.jpg");
        UMat uimage = new UMat();
        Image<Bgr, Byte> lineImage;

        public Form1()
        {
            InitializeComponent();
            pictureBox1.Image = img1.ToBitmap();
            lineImage = img1.CopyBlank();
        }
        static bool grayed = false;

        private void button1_Click(object sender, EventArgs e)
        {
            if (grayed)
                return;
            //Convert the image to grayscale and filter out the noise

            CvInvoke.CvtColor(img1, uimage, ColorConversion.Bgr2Gray);
            //use image pyr to remove noise
            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(uimage, pyrDown);
            CvInvoke.PyrUp(pyrDown, uimage);
            //pictureBox2.Image = uimage.Bitmap;
            grayed = true;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
        private string xyS(float x, float y)
        {
            return String.Format("({0};{1})", Math.Round(x, 1), Math.Round(y, 1));
        }

        UMat FindEdges()
        {
            // Canny and edge detection
            double cannyThreshold = 180.0;
            double cannyThresholdLinking = 120.0;
            Double.TryParse(textBox1.Text, out cannyThreshold);
            Double.TryParse(textBox2.Text, out cannyThresholdLinking);
            UMat cannyEdges = new UMat();
            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);
            return cannyEdges;
        }

        LineSegment2D[] FindLines(UMat cannyEdges)
        {
            int threshold = 20;
            Int32.TryParse(textBox3.Text, out threshold);
            double minLineWidth = 30;
            Double.TryParse(textBox4.Text, out minLineWidth);
            double gap = 10;
            Double.TryParse(textBox5.Text, out gap);
            //UMat cannyEdgesCopy = cannyEdges.Clone();
            //VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            //CvInvoke.FindContours(cannyEdgesCopy, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            //CvInvoke.DrawContours(lineImage, contours, -1, new MCvScalar(Color.Yellow.B, Color.Yellow.G, Color.Yellow.R), 1);
            //pictureBox3.Image = lineImage.Bitmap;
            return CvInvoke.HoughLinesP(
               cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, //Angle resolution measured in radians.
               threshold, //threshold
               minLineWidth, //min Line width
               gap); //gap between lines
        }


        private void button2_Click(object sender, EventArgs e)
        {
            button1_Click(this, e);
            UMat edges = FindEdges();
            LineSegment2D[] lines = FindLines(edges);

            foreach (LineSegment2D line in lines)
                lineImage.Draw(line, new Bgr(Color.LightGreen), 2);
            pictureBox2.Image = lineImage.Bitmap;

            using (StreamWriter sw = new StreamWriter("lines0.dmp"))
            {
                foreach (LineSegment2D line in lines)
                {
                    if (line.Length > 10)
                        sw.WriteLine("line: (direction) {0} (P1) {1}  (P2) {2}   len: {3}", xyS(line.Direction.X, line.Direction.Y), xyS(line.P1.X, line.P1.Y), xyS(line.P2.X, line.P2.Y), line.Length);
                    else if (line.Length > 0)
                        sw.WriteLine("lijntje: (direction) {0} (P1) {1}  (P2) {2}   len: {3}", xyS(line.Direction.X, line.Direction.Y), xyS(line.P1.X, line.P1.Y), xyS(line.P2.X, line.P2.Y), line.Length);
                    else
                        sw.WriteLine("line: (point) {0}", xyS(line.P1.X, line.P1.Y));
                }
            }
            OCVGridData gridData = new OCVGridData(lines);
            using (StreamWriter sw = new StreamWriter("lines.dmp"))
            {
                sw.WriteLine("------------------------------------------------");
                sw.WriteLine("griddata (HorizontalLines): ");
                foreach (OCVLineData line in gridData.HorizontalLines)
                {
                    sw.WriteLine("line: Y = {0}, X1 = {1} X2 = {2} len = {3}", line.GetY(), line.GetMinValue(), line.GetMaxValue(), line.Length);
                }
                sw.WriteLine("------------------------------------------------");
                sw.WriteLine("griddata (Vertical Lines: ");
                foreach (OCVLineData line in gridData.VerticalLines)
                {
                    sw.WriteLine("line: X = {0}, Y1 = {1} Y2 = {2} len = {3}", line.GetX(), line.GetMinValue(), line.GetMaxValue(), line.Length);
                }
                sw.WriteLine("------------------------------------------------");
            }
            OCVGrid grid = new OCVGrid(gridData);
            using (StreamWriter sw = new StreamWriter("grid.dmp"))
            {
                grid.Dump(sw);
            }

            Image<Bgr, Byte> image3 = lineImage.CopyBlank();

            foreach (OCVCombinedLinesData line in grid.HorizontalLines)
            {
                OCVLineData summ = line.GetSummaryLine();
                image3.Draw(summ.Line, new Bgr(Color.Azure), 2);
            }
            foreach (OCVCombinedLinesData line in grid.VerticalLines)
            {
                OCVLineData summ = line.GetSummaryLine();
                if (summ.Length > 35)
                    image3.Draw(summ.Line, new Bgr(Color.LightPink), 2);
            }
            pictureBox2.Image = image3.Bitmap;

            Image<Bgr, Byte> lineImage2 = img1.CopyBlank();
            OCVGridDefinition gridDef = grid.Analyze();
            for (int r = 0; r <= gridDef.Rows; r++)
            {
                lineImage2.Draw(new LineSegment2D(new Point(gridDef.TopLeft.X, gridDef.RowLocation(r)),
                                                  new Point(gridDef.TopLeft.X + gridDef.Width, gridDef.RowLocation(r))),
                                                  new Bgr(Color.PeachPuff), 2);
            }
            for (int c = 0; c <= gridDef.Cols; c++)
            {
                lineImage2.Draw(new LineSegment2D(new Point(gridDef.ColLocation(c), gridDef.TopLeft.Y),
                                                  new Point(gridDef.ColLocation(c), gridDef.TopLeft.Y + +gridDef.Height)),
                                                  new Bgr(Color.MediumTurquoise), 2);
            }

            lineImage2.Draw(new Rectangle(gridDef.TopLeft.X, gridDef.TopLeft.Y, gridDef.Width, gridDef.Height), new Bgr(Color.White), 2);

            pictureBox3.Image = lineImage2.Bitmap;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }
    }

    //class OCVLineDataSorter : IComparer<OCVLineData>
    //{
    //    public int Compare(OCVLineData x, OCVLineData y)
    //    {
    //        if (x.IsVertical() && y.IsVertical())
    //        {
    //            if (x.GetX() < y.GetX())
    //                return -1;
    //            else if (x.GetX() > y.GetX())
    //                return 1;
    //            else
    //                return 0;
    //        }
    //        else if (x.IsHorizontal() && y.IsHorizontal())
    //        {
    //            if (x.GetY() < y.GetY())
    //                return -1;
    //            else if (x.GetY() > y.GetY())
    //                return 1;
    //            else
    //                return 0;
    //        }
    //        return 0;
    //    }
    //}
    //public class OCVLineLevelData
    //{
    //    private bool _isHorizontal;
    //    static double margin = 10;
    //    private double minVal, maxVal;
    //    private double averageVal;
    //    public bool IsHorizontal { get { return _isHorizontal; } }
    //    public List<OCVLineData> Lines;

    //    public OCVLineLevelData()
    //    {
    //        Lines = new List<OCVLineData>();
    //    }

    //    public bool AddLine(OCVLineData line)
    //    {
    //        if (!isInLevel(line))
    //            return false;
    //        Lines.Add(line);
    //        if (Lines.Count == 1)
    //        {
    //            _isHorizontal = line.IsHorizontal();
    //            if (IsHorizontal)
    //            {
    //                minVal = line.GetY() - margin / 2;
    //            }
    //            else
    //            {
    //                minVal = line.GetX() - margin / 2;
    //            }
    //            maxVal = minVal + margin;
    //        }
    //        else
    //        {
    //            double value = 0;
    //            foreach(OCVLineData l in Lines)
    //            {
    //                if (IsHorizontal)
    //                    value += l.GetY();
    //                else
    //                    value += l.GetX();
    //            }
    //            averageVal = value / Lines.Count;
    //        }
    //        return true;
    //    }

    //    private bool isInLevel(OCVLineData line)
    //    {
    //        if (Lines.Count == 0)
    //            return line.IsHorizontal() || line.IsVertical();
    //        else if (IsHorizontal)
    //        {
    //            return line.IsHorizontal() && line.GetY() <= maxVal && line.GetY() >= minVal;
    //        }
    //        else
    //        {
    //            return line.IsVertical() && line.GetX() <= maxVal && line.GetX() >= minVal;
    //        }            
    //    }

    //    OCVLineData GetSummaryLine()
    //    {
    //        if (isHorizontal)
    //        {
    //            OCVLineData line = new OCVLineData(new Point ();
    //        }
    //    }
    //}

    
}
