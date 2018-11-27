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

        private void button1_Click(object sender, EventArgs e)
        {

            //Convert the image to grayscale and filter out the noise

            CvInvoke.CvtColor(img1, uimage, ColorConversion.Bgr2Gray);
            //use image pyr to remove noise
            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(uimage, pyrDown);
            CvInvoke.PyrUp(pyrDown, uimage);
            pictureBox2.Image = uimage.Bitmap;
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
            CVGridData gridData = new CVGridData(lines);
            using (StreamWriter sw = new StreamWriter("lines.dmp"))
            {
                foreach (LineSegment2D line in lines)
                {
                    sw.WriteLine("line: (direction) {0} (P1) {1}  (P2) {2}   len: {3}", xyS(line.Direction.X, line.Direction.Y), xyS(line.P1.X, line.P1.Y), xyS(line.P2.X, line.P2.Y), line.Length);
                }
                sw.WriteLine("HorizontalLines: ");
                foreach(CVLineData line in gridData.HorizontalLines)
                {
                    sw.WriteLine("line: Y = {0,1}, len = {1,1}", line.GetY(), line.Length);
                }
                sw.WriteLine("Vertical Lines: ");
                foreach (CVLineData line in gridData.VerticalLines)
                {
                    sw.WriteLine("line: X = {0,1}, len = {1,1}", line.GetX(), line.Length);
                }
            }
            Image<Bgr, Byte> lineImage = img1.CopyBlank();
            foreach (LineSegment2D line in lines)
                lineImage.Draw(line, new Bgr(Color.Green), 2);
            pictureBox3.Image = lineImage.Bitmap;


        }
    }

    class CVLineDataSorter : IComparer<CVLineData>
    {
        public int Compare(CVLineData x, CVLineData y)
        {
            if (x.IsVertical() && y.IsVertical())
            {
                if (x.GetX() < y.GetX())
                    return -1;
                else if (x.GetX() > y.GetX())
                    return 1;
                else
                    return 0;
            }
            else if (x.IsHorizontal() && y.IsHorizontal())
            {
                if (x.GetY() < y.GetY())
                    return -1;
                else if (x.GetY() > y.GetY())
                    return 1;
                else
                    return 0;
            }
            return 0;
        }
    }
    public class CVLineData
    {
        LineSegment2D Line { get; }
        double margin = 0.1;
        public double Length { get { return Line.Length; } }

        public CVLineData(LineSegment2D line)
        {
            Line = line;
        }

        private bool IsEqualValue(double value, double target)
        {
            return Math.Abs(value - target) < margin;
        }
        public bool IsHorizontal()
        {
            return IsEqualValue(Math.Round(Line.Direction.X,1), -1) && 
                   IsEqualValue(Math.Round(Line.Direction.Y,1), 0);
        }
        public bool IsVertical()
        {
            return IsEqualValue(Math.Round(Line.Direction.X, 1), 0) &&
                   IsEqualValue(Math.Round(Line.Direction.Y, 1), 1);
        }
        public double GetX()
        {
            return 0.5 * (Math.Round((double)Line.P1.X + Math.Round((double)Line.P2.X)));
        }
        public double GetY()
        {
            return 0.5 * (Math.Round((double)Line.P1.Y + Math.Round((double)Line.P2.Y)));
        }
        public double HorizontalDistance(CVLineData line2)
        {
            if (this.IsVertical() && line2.IsVertical())
                return Math.Abs(GetX() - line2.GetX());
            else
                return -1;
        }
        public double VerticalDistance(CVLineData line2)
        {
            if (this.IsHorizontal() && line2.IsHorizontal())
                return Math.Abs(GetY() - line2.GetY());
            else
                return -1;
        }
    }
    public class CVGridData
    {
        public List<CVLineData> HorizontalLines;
        public List<CVLineData> VerticalLines;

        public CVGridData(LineSegment2D[] lines)
        {
            HorizontalLines = new List<CVLineData>();
            VerticalLines = new List<CVLineData>();
            foreach(LineSegment2D line in lines)
            {
                CVLineData newData = new CVLineData(line);
                if (newData.IsHorizontal())
                    HorizontalLines.Add(newData);
                else if (newData.IsVertical())
                    VerticalLines.Add(newData);
            }
            HorizontalLines.Sort(new CVLineDataSorter());
            VerticalLines.Sort(new CVLineDataSorter());            
        }
    }
}
