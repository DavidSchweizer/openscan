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
    public class OCVConst
    {
        static public double REALLYLARGE = Math.Pow(10, 20);
        static public double RangeMargin = 20;
        static private double PointMargin = 0.2;
        static public double MinimalLineLength = 10;
        static public bool IsEqualValue(double value, double target)
        {
            return Math.Abs(value - target) < PointMargin;
        }
    }

    public class OCVGridDefinition
    {
        public int Rows, Cols;
        public Point TopLeft;
        public Point BottomRight;
        public double RowSize, ColSize;
        public int Width { get { return BottomRight.X - TopLeft.X; } }
        public int Height { get { return BottomRight.Y - TopLeft.Y; } }

        public OCVGridDefinition(Point topLeft, Point bottomRight, int rows, int cols)
        {
            TopLeft = topLeft;
            BottomRight = bottomRight;
            Rows = rows;
            Cols = cols;
            ColSize = (1.0 * (BottomRight.X - TopLeft.X) / Cols);
            RowSize = (1.0 * (BottomRight.Y - TopLeft.Y) / Rows);
        }

        public int RowLocation(int row)
        {
            return (int)(TopLeft.Y + row * RowSize);
        }
        public int ColLocation(int col)
        {
            return (int)(TopLeft.X + col * ColSize);
        }
    }

    class OCVGrid
    {
        public List<OCVCombinedLinesData> HorizontalLines;
        public List<OCVCombinedLinesData> VerticalLines;
        public OCVGrid()
        {
            HorizontalLines = new List<OCVCombinedLinesData>();
            VerticalLines = new List<OCVCombinedLinesData>();
        }
        public OCVGrid(OCVGridData data) : this()
        {
            CopyLines(data.HorizontalLines, HorizontalLines, getNewHorizontalMember);
            CopyLines(data.VerticalLines, VerticalLines, getNewVerticalMember);
        }
        private void RemoveSmallLines(double threshold)
        {
            int i = 0;
            while (i < HorizontalLines.Count)
                if (HorizontalLines[i].Length() < threshold)
                    HorizontalLines.RemoveAt(i);
                else i++;
            i = 0;
            while (i < VerticalLines.Count)
                if (VerticalLines[i].Length() < threshold)
                    VerticalLines.RemoveAt(i);
                else i++;
        }

        private void RemoveInconsistentLines(List<OCVCombinedLinesData> lines, double blocksize, double threshold)
        {
            int i = 1;
            while (i < lines.Count - 1)
            {
                double diff1 = lines[i].midRange - lines[i - 1].midRange;
                double diff2 = lines[i].midRange - lines[i + 1].midRange;
                if (Math.Abs(diff1 / blocksize) < (1 - threshold) || Math.Abs(diff2 / blocksize) < (1 - threshold))
                    lines.RemoveAt(i);
                else
                    i++;
            }
        }

        private OCVGridDefinition Analyze(double threshold)
        {
            double left = OCVConst.REALLYLARGE, top = OCVConst.REALLYLARGE, right = -OCVConst.REALLYLARGE, bottom = -OCVConst.REALLYLARGE;
            // remove lines that are too small
            RemoveSmallLines(threshold);
            // analyze horizontal lines
            int rows = -1;
            foreach (OCVCombinedLinesData linesGroup in HorizontalLines)
            {
                rows++;
                OCVLineData summaryLine = linesGroup.GetSummaryLine();
                if (summaryLine.GetMinValue() < left)
                    left = summaryLine.GetMinValue();
                if (summaryLine.GetMaxValue() > right)
                    right = summaryLine.GetMaxValue();
                if (summaryLine.GetY() > top)
                    top = summaryLine.GetY();
                if (summaryLine.GetY() < bottom)
                    bottom = summaryLine.GetY();
            }

            // analyze vertical lines
            int cols = -1;
            foreach (OCVCombinedLinesData linesGroup in VerticalLines)
            {
                cols++;
                OCVLineData summaryLine = linesGroup.GetSummaryLine();
                if (summaryLine.GetX() < left)
                    left = summaryLine.GetX();
                if (summaryLine.GetX() > right)
                    right = summaryLine.GetX();
                if (summaryLine.GetMinValue() < top)
                    top = summaryLine.GetMinValue();
                if (summaryLine.GetMaxValue() > bottom)
                    bottom = summaryLine.GetMaxValue();
            }
            OCVGridDefinition result = new OCVGridDefinition(new Point((int)left, (int)top),
                                                            new Point((int)right, (int)bottom),
                                                            rows, cols
                                                            );
            return result;
        }
        public OCVGridDefinition Analyze()
        {
            OCVGridDefinition tempResult = Analyze(20);
            if (tempResult.Rows > 0 && tempResult.Cols > 0)
            {
                double rowsize = tempResult.Width / (tempResult.Rows + 1);
                double colsize = tempResult.Width / (tempResult.Cols + 1);
                RemoveInconsistentLines(HorizontalLines, colsize, 0.45);
                RemoveInconsistentLines(VerticalLines, rowsize, 0.45);
                return Analyze(((rowsize > colsize) ? rowsize : colsize) - 5);
            }
            return null;
        }
        
        private delegate OCVCombinedLinesData getNewMember();
        private OCVCombinedLinesData getNewHorizontalMember()
        {
            return new OCVCombinedHorizontalLinesData();
        }
        private OCVCombinedLinesData getNewVerticalMember()
        {
            return new OCVCombinedVerticalLinesData();
        }
        private void CopyLines(List<OCVLineData> lines, List<OCVCombinedLinesData> combinedLines, getNewMember GetNewMember)
        {
            foreach (OCVLineData line in lines)            
            {
                OCVCombinedLinesData combinedLine = null;
                for (int i = 0; i < combinedLines.Count && combinedLine == null; i++)
                    if (combinedLines[i].IsInRange(line))
                    {
                        combinedLine = combinedLines[i];
                    }
                if (combinedLine == null)
                {
                    combinedLines.Add(combinedLine = GetNewMember());
                }
                combinedLine.AddLine(line);
            }
        }
        public void Dump(StreamWriter sw)
        {
            sw.WriteLine("horizontal lines:");
            foreach (OCVCombinedLinesData combinedLine in HorizontalLines)
                combinedLine.Dump(sw);
            sw.WriteLine("vertical lines:");
            foreach (OCVCombinedLinesData combinedLine in VerticalLines)
                combinedLine.Dump(sw);
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

    public abstract class OCVCombinedLinesData
    {
        private double minRange, maxRange;
        public double midRange;
        public double minVal, maxVal;
        public double minLength, maxLength;
        protected PointF Direction;
        protected IComparer<OCVLineData> sorter;
        public List<OCVLineData> Lines;

        public OCVCombinedLinesData(PointF direction)
        {
            Lines = new List<OCVLineData>();
            Direction = direction;
            minRange = -OCVConst.REALLYLARGE; // note: first line is in range
            maxRange = OCVConst.REALLYLARGE;
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

        private void ReCompute()
        {
            minVal = OCVConst.REALLYLARGE;
            maxVal = -OCVConst.REALLYLARGE;
            minRange = OCVConst.REALLYLARGE;
            maxRange = -OCVConst.REALLYLARGE;
            minLength = OCVConst.REALLYLARGE;
            maxLength = -OCVConst.REALLYLARGE;
            double TotalValue = 0;
            double TotalLen = 0;
            foreach (OCVLineData line in Lines)
            {
                TotalValue += line.Length * GetLocation(line); // compute weighted average
                TotalLen += line.Length;
                if (line.GetMinValue() < minVal)
                    minVal = line.GetMinValue();
                if (line.GetMaxValue() > maxVal)
                    maxVal = line.GetMaxValue();
                if (line.Length < minLength)
                    minLength = line.Length;
                if (line.Length > maxLength)
                    maxLength = line.Length;
            }
            midRange = TotalValue / TotalLen;
            minRange = midRange - OCVConst.RangeMargin / 2;
            maxRange = minRange + OCVConst.RangeMargin;
            // what if one of the lines now is outside of margin?
        }

        private void RemoveLine(OCVLineData line)
        {
            Lines.Remove(line);
            ReCompute();
        }
        public bool AddLine(OCVLineData line)
        {
            if (!IsInRange(line))
                return false;
            Lines.Add(line);
            ReCompute();
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
        //public void RemoveSmallLines(double threshold)
        //{
        //    int i = 0;
        //    while (i < Lines.Count)
        //        if (Lines[i].Length > threshold)
        //            i++;
        //        else RemoveLine(Lines[i]);
        //}
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

    public class OCVCombinedHorizontalLinesData : OCVCombinedLinesData
    {
        public OCVCombinedHorizontalLinesData() : base(new PointF(-1,0))
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
    public class OCVCombinedVerticalLinesData : OCVCombinedLinesData
    {
        public OCVCombinedVerticalLinesData() : base(new PointF(0,1))
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
                if (line.Length < OCVConst.MinimalLineLength)
                    continue;
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
