using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;


namespace topencv01
{
    public class OCVGridDefinition
    {
        public int Rows, Cols;
        public Point TopLeft;
        public Point BottomRight;
        public int RowSize, ColSize;
        public int Width { get { return BottomRight.X - TopLeft.X; } }
        public int Height { get { return BottomRight.Y - TopLeft.Y; } }

    }

    class OCVGrid
    {
        public List<OCVLineRangeData> HorizontalLines;
        public List<OCVLineRangeData> VerticalLines;
        public OCVGrid()
        {
            HorizontalLines = new List<OCVLineRangeData>();
            VerticalLines = new List<OCVLineRangeData>();
        }
        public OCVGrid(OCVGridData data) : this()
        {
            CopyLines(data.HorizontalLines, HorizontalLines, getNewHorizontalRange);
            CopyLines(data.VerticalLines, VerticalLines, getNewVerticalRange);
        }
        public OCVGridDefinition Analyze()
        {
            OCVGridDefinition result = new OCVGridDefinition();
            double left = OCVConst.REALLYLARGE, top = OCVConst.REALLYLARGE, right = -OCVConst.REALLYLARGE, bottom = -OCVConst.REALLYLARGE;
        // analyze horizontal lines
            result.Rows = 0;
            foreach (OCVLineRangeData range in HorizontalLines)
            {
                if (range.Length() > 100)
                {
                    result.Rows++;
                    OCVLineData rangeLine = range.GetSummaryLine();
                    if (rangeLine.GetMinValue() < top)
                        top = rangeLine.GetMinValue();
                    if (rangeLine.GetMaxValue() > bottom)
                         bottom = rangeLine.GetMaxValue();
                }
            }
            
            // analyze vertical lines
            result.Cols = 0;
            foreach (OCVLineRangeData range in VerticalLines)
            {
                if (range.Length() > 100)
                {
                    result.Cols++;
                    OCVLineData rangeLine = range.GetSummaryLine();
                    if (rangeLine.GetMinValue() < left)
                        left = rangeLine.GetMinValue();
                    if (rangeLine.GetMaxValue() > right)
                        right = rangeLine.GetMaxValue();
                }
            }
            result.TopLeft = new Point((int) left, (int)top);
            result.BottomRight = new Point((int) right, (int)bottom);

            result.ColSize = (result.BottomRight.X - result.TopLeft.X) / result.Cols;
            result.RowSize = Math.Abs(result.BottomRight.Y - result.TopLeft.Y) / result.Cols;
            return result;
        }

        private delegate OCVLineRangeData getNewRange();
        private OCVLineRangeData getNewHorizontalRange()
        {
            return new OCVHorizontalLineRangeData();
        }
        private OCVLineRangeData getNewVerticalRange()
        {
            return new OCVVerticalLineRangeData();
        }
        private void CopyLines(List<OCVLineData> lines, List<OCVLineRangeData> ranges, getNewRange GetNewRange)
        {
            foreach (OCVLineData line in lines)            
            {
                OCVLineRangeData range = null;
                for (int i = 0; i < ranges.Count && range == null; i++)
                    if (ranges[i].IsInRange(line))
                    {
                        range = ranges[i];
                    }
                if (range == null)
                {
                    ranges.Add(range = GetNewRange());
                }
                range.AddLine(line);
            }
        }
        public void Dump(StreamWriter sw)
        {
            sw.WriteLine("horizontal lines:");
            foreach (OCVLineRangeData range in HorizontalLines)
                range.Dump(sw);
            sw.WriteLine("vertical lines:");
            foreach (OCVLineRangeData range in VerticalLines)
                range.Dump(sw);
        }
    }

    public class OCVConst
    {
        static public double REALLYLARGE = Math.Pow(10, 20);
        static public double RangeMargin = 10;
        static private double PointMargin = 0.1;
        static public bool IsEqualValue(double value, double target)
        {
            return Math.Abs(value - target) < PointMargin;
        }
    }

    public class OCVLineData
    {
        public LineSegment2D Line { get; }
        public double Length { get { return Line.Length; } }

        public OCVLineData(Point p1, Point p2)
        {
            Line = new LineSegment2D(p1, p2);
        }
        private string xyS(float x, float y)
        {
            return String.Format("({0};{1})", Math.Round(x, 1), Math.Round(y, 1));
        }
        public void Dump(StreamWriter sw)
        {
            sw.WriteLine("line: (direction) {0} (P1) {1}  (P2) {2}   len: {3}", xyS(Line.Direction.X, Line.Direction.Y), xyS(Line.P1.X, Line.P1.Y), xyS(Line.P2.X, Line.P2.Y), Line.Length);
        }
        public OCVLineData(LineSegment2D line)
        {
            Line = line;
        }

        public bool IsHorizontal()
        {
            return OCVConst.IsEqualValue(Math.Round(Line.Direction.X, 1), -1) &&
                   OCVConst.IsEqualValue(Math.Round(Line.Direction.Y, 1), 0);
        }
        public bool IsVertical()
        {
            return OCVConst.IsEqualValue(Math.Round(Line.Direction.X, 1), 0) &&
                   OCVConst.IsEqualValue(Math.Round(Line.Direction.Y, 1), 1);
        }
        public double GetX()
        {
            return 0.5 * (Math.Round((double)Line.P1.X + Math.Round((double)Line.P2.X)));
        }
        public double GetY()
        {
            return 0.5 * (Math.Round((double)Line.P1.Y + Math.Round((double)Line.P2.Y)));
        }
        public double HorizontalDistance(OCVLineData line2)
        {
            if (this.IsVertical() && line2.IsVertical())
                return Math.Abs(GetX() - line2.GetX());
            else
                return -1;
        }
        public double VerticalDistance(OCVLineData line2)
        {
            if (this.IsHorizontal() && line2.IsHorizontal())
                return Math.Abs(GetY() - line2.GetY());
            else
                return -1;
        }
        public double GetMinValue()
        {
            if (IsHorizontal())
                return Math.Min(Line.P1.X, Line.P2.X);
            else
                return Math.Min(Line.P1.Y, Line.P2.Y);
        }
        public double GetMaxValue()
        {
            if (IsHorizontal())
                return Math.Max(Line.P1.X, Line.P2.X);
            else
                return Math.Max(Line.P1.Y, Line.P2.Y);
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

    public abstract class OCVLineRangeData
    {
        private double minRange, maxRange;
        public double midRange;
        public double minVal, maxVal;
        protected PointF Direction;
        protected IComparer<OCVLineData> sorter;
        public List<OCVLineData> Lines;

        public OCVLineRangeData(PointF direction)
        {
            Lines = new List<OCVLineData>();
            Direction = direction;
            minVal = OCVConst.REALLYLARGE;
            maxVal = -OCVConst.REALLYLARGE;
            maxRange = OCVConst.REALLYLARGE;
            minRange = -OCVConst.REALLYLARGE;
        }

        protected abstract double GetLocation(OCVLineData line);

        public double Length()
        {
            return maxVal - minVal;
        }
        public bool IsInRange(OCVLineData line)
        {
            
            if (!OCVConst.IsEqualValue(line.Line.Direction.X, Direction.X) && OCVConst.IsEqualValue(line.Line.Direction.Y, Direction.Y))
                return false;
            else 
                return (GetLocation(line) <= maxRange && GetLocation(line) >= minRange);
        }

        public bool AddLine(OCVLineData line)
        {
            if (!IsInRange(line))
                return false;
            Lines.Add(line);
            if (Lines.Count == 1)
            {
                minRange = GetLocation(line) - OCVConst.RangeMargin / 2;
                midRange = GetLocation(line);
            }
            else
            {
                double value = 0;
                foreach (OCVLineData l in Lines)
                {
                    value += GetLocation(l);
                }
                midRange = value / Lines.Count;
                minRange = midRange - OCVConst.RangeMargin / 2;
                // what if one of the lines now is outside of margin?
            }
            maxRange = minRange + OCVConst.RangeMargin;
            if (line.GetMinValue() < minVal)
                minVal = line.GetMinValue();
            if (line.GetMaxValue() > maxVal)
                maxVal = line.GetMaxValue();
            Sort();
            return true;
        }
        public abstract OCVLineData GetSummaryLine();

        public void Sort()
        {
            if (sorter != null) Lines.Sort(sorter);
        }
        private string xyS(float x, float y)
        {
            return String.Format("({0};{1})", Math.Round(x, 1), Math.Round(y, 1));
        }
        public void Dump(StreamWriter sw)
        {
            sw.WriteLine(" --- Direction: {0}, range {1} - ({2}) - {3}   value {4} - {5}", xyS(Direction.X, Direction.Y), minRange, midRange, maxRange, minVal, maxVal);
            foreach (OCVLineData line in Lines)
            {
                line.Dump(sw);
            }
        }
    }

    class OCVHorizontalLineDataSorter : IComparer<OCVLineData>
    {
        public int Compare(OCVLineData x, OCVLineData y)
        {
            if (x.GetY() < y.GetY())
                return -1;
            else if (x.GetY() > y.GetY())
                return 1;
            else
                return 0;
        }
    }
    class OCVVerticalLineDataSorter : IComparer<OCVLineData>
    {
        public int Compare(OCVLineData x, OCVLineData y)
        {
            if (x.GetX() < y.GetX())
                return -1;
            else if (x.GetX() > y.GetX())
                return 1;
            else
                return 0;
        }
    }

    public class OCVHorizontalLineRangeData : OCVLineRangeData
    {
        public OCVHorizontalLineRangeData() : base(new PointF(-1,0))
        {
            sorter = new OCVHorizontalLineDataSorter();
        }
        protected override double GetLocation(OCVLineData line)
        {
            return line.GetY();
        }
        public override OCVLineData GetSummaryLine()
        {
            return new OCVLineData(new Point((int)minVal, (int)midRange), new Point((int)maxVal, (int)midRange));
        }
    }
    public class OCVVerticalLineRangeData : OCVLineRangeData
    {
        public OCVVerticalLineRangeData() : base(new PointF(0,1))
        {
            sorter = new OCVVerticalLineDataSorter();
        }
        protected override double GetLocation(OCVLineData line)
        {
            return line.GetX();
        }
        public override OCVLineData GetSummaryLine()
        {
            return new OCVLineData(new Point((int)midRange, (int)minVal), new Point((int)midRange, (int)maxVal));
        }
    }

    public class OCVGridData
    {
        public List<OCVLineData> HorizontalLines;
        public List<OCVLineData> VerticalLines;

        public OCVGridData(LineSegment2D[] lines)
        {
            HorizontalLines = new List<OCVLineData>();
            VerticalLines = new List<OCVLineData>();
            foreach (LineSegment2D line in lines)
            {
                OCVLineData newData = new OCVLineData(line);
                if (newData.IsHorizontal())
                    HorizontalLines.Add(newData);
                else if (newData.IsVertical())
                    VerticalLines.Add(newData);
            }
            HorizontalLines.Sort(new OCVHorizontalLineDataSorter());
            VerticalLines.Sort(new OCVVerticalLineDataSorter());
        }
    }
}
