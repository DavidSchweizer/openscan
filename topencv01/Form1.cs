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


namespace topencv01
{
    public partial class Form1 : Form
    {

        Image<Bgr, Byte> img1 = new Image<Bgr, Byte>("c:/temp/test1.jpg");
        UMat uimage = new UMat();

        public Form1()
        {
            InitializeComponent();
            pictureBox1.Image = img1.ToBitmap();
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

        private void button2_Click(object sender, EventArgs e)
        {
            button1_Click(this, e);

            // Canny and edge detection
            double cannyThreshold = 180.0;
            double cannyThresholdLinking = 120.0;

            Double.TryParse(textBox1.Text, out cannyThreshold);
            Double.TryParse(textBox2.Text, out cannyThresholdLinking);
            UMat cannyEdges = new UMat();
            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);
            int threshold = 20;
            Int32.TryParse(textBox3.Text, out threshold);
            double minLineWidth = 30;
            Double.TryParse(textBox4.Text, out minLineWidth);
            double gap = 10;
            Double.TryParse(textBox5.Text, out gap);

            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, //Angle resolution measured in radians.
               threshold, //threshold
               minLineWidth, //min Line width
               gap); //gap between lines
            OCVGridData gridData = new OCVGridData(lines);
            OCVGrid grid = new OCVGrid(gridData);

            using (StreamWriter sw = new StreamWriter("lines.dmp"))
            {
                foreach (LineSegment2D line in lines)
                {
                    sw.WriteLine("line: (direction) {0} (P1) {1}  (P2) {2}   len: {3}", xyS(line.Direction.X, line.Direction.Y), xyS(line.P1.X, line.P1.Y), xyS(line.P2.X, line.P2.Y), line.Length);
                }
                sw.WriteLine("HorizontalLines: ");
                foreach(OCVLineData line in gridData.HorizontalLines)
                {
                    sw.WriteLine("line: Y = {0,1}, len = {1,1}", line.GetY(), line.Length);
                }
                sw.WriteLine("Vertical Lines: ");
                foreach (OCVLineData line in gridData.VerticalLines)
                {
                    sw.WriteLine("line: X = {0,1}, len = {1,1}", line.GetX(), line.Length);
                }
                grid.Dump(sw);
            }
            Image<Bgr, Byte> lineImage = img1.CopyBlank();
            foreach (LineSegment2D line in lines)
                lineImage.Draw(line, new Bgr(Color.Green), 2);
            foreach (OCVCombinedLinesData line in grid.HorizontalLines)
            {
                OCVLineData summ = line.GetSummaryLine();
                if (summ.Length > 35)
                    lineImage.Draw(summ.Line, new Bgr(Color.White), 2);
            }
            foreach (OCVCombinedLinesData line in grid.VerticalLines)
            {
                OCVLineData summ = line.GetSummaryLine();
                if (summ.Length > 35)
                    lineImage.Draw(summ.Line, new Bgr(Color.LightPink), 2);
            }
            pictureBox2.Image = lineImage.Bitmap;

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
                                                  new Point(gridDef.ColLocation(c), gridDef.TopLeft.Y +  + gridDef.Height)),
                                                  new Bgr(Color.MediumTurquoise), 2);
            }

            lineImage2.Draw(new Rectangle(gridDef.TopLeft.X, gridDef.TopLeft.Y, gridDef.Width, gridDef.Height), new Bgr(Color.White), 2);

            pictureBox3.Image = lineImage2.Bitmap;

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
