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
using Emgu.CV.OCR;
using Emgu.CV.Util;

namespace topencv01
{
    class TekBorderAnalyzer
    {
        public bool[,] BottomAreaBorders;
        public bool[,] RightAreaBorders;
        public int Rows { get { return BottomAreaBorders.GetLength(0); } }
        public int Cols { get { return BottomAreaBorders.GetLength(1); } }
        public TekBorderAnalyzer(UMat matGray, OCVGridDefinition gridDef)
        {
            int testWidth = 10;
            int[,] RightBorderValues;
            int[,] BottomBorderValues;

            Matrix<Byte> matrix = new Matrix<Byte>(matGray.Rows, matGray.Cols, matGray.NumberOfChannels);
            matGray.CopyTo(matrix);
            int threshold = FindThresholdAndWidth(matrix, gridDef, out testWidth);
            RightBorderValues = FindRowValues(matrix, gridDef, testWidth, threshold);
            BottomBorderValues = FindColValues(matrix, gridDef, testWidth, threshold);
            RightAreaBorders = AnalyzeBorderValues(RightBorderValues);
            BottomAreaBorders = AnalyzeBorderValues(BottomBorderValues);
        }

        private int FindThresholdAndWidth(Matrix<byte> matrix, OCVGridDefinition gridDef, out int testWidth)
        {
            const int MAXBIN = 10;
            double[] minValues = new double[1];
            double[] maxValues = new double[1];
            Point[] minLocations = new Point[1];
            Point[] maxLocations = new Point[1];
            testWidth = (int)(gridDef.ColSize * 0.1);
            int[] binLimits;
            int[] frequencyDistribution = FrequencyDistribution(matrix, MAXBIN, out binLimits);
            int i = MAXBIN;
            int maxi = i;
            while (--i >= 0)
            {
                if (frequencyDistribution[i] > frequencyDistribution[maxi])
                    maxi = i;
            }
            if (maxi > 0)
                return binLimits[maxi - 1];
            else
                return binLimits[0];
        }

        private int[] FrequencyDistribution(Matrix<byte> matrix, int nBins, out int[] binLimits)
        {
            int[] result = new int[nBins + 1];
            binLimits = new int[nBins + 1];
            for (int i = 0; i < nBins; i++)
                binLimits[i] = (i + 1) * (255 / nBins);
            binLimits[nBins] = 255;
            for (int r = 0; r < matrix.Rows; r++)
            { 
                for (int c = 0; c < matrix.Cols; c++)
                {
                    int i = 0;
                    while (i <= nBins)
                        if (matrix.Data[r, c] <= binLimits[i])
                        {
                            result[i]++;
                            break;
                        }
                        else i++;
                }
            }
            return result;
        }

        private int[,] FindRowValues(Matrix<byte> matrix, OCVGridDefinition gridDef, int testWidth, int threshold)
        {
            int[,] result = new int[gridDef.Rows, gridDef.Cols];
            for (int r = 0; r < gridDef.Rows; r++)
            {
                int rowLoc = (int)(gridDef.RowLocation(r) + gridDef.RowSize * 0.5);
                for (int c = 0; c < gridDef.Cols; c++)
                {
                    int loc = gridDef.ColLocation(c);
                    int nBelow = 0;
                    for (int col = loc - testWidth; col < loc + testWidth; col++)
                    {
                        if (col < 0 || col >= matrix.Cols)
                            continue;
                        byte value = matrix.Data[rowLoc, col];
                        if (value < threshold)
                            nBelow++;
                    }
                    result[r, c] = (int)(100 * (nBelow / (2.0 * testWidth)));
                }
            }
            return result;
        }
        private int[,] FindColValues(Matrix<byte> matrix, OCVGridDefinition gridDef, int testWidth, int threshold)
        {
            int[,] result = new int[gridDef.Rows, gridDef.Cols];

            for (int c = 0; c < gridDef.Cols; c++)
            {
                int colLoc = (int)(gridDef.ColLocation(c) + gridDef.ColSize * 0.5);
                for (int r = 0; r < gridDef.Rows; r++)
                {
                    int loc = gridDef.RowLocation(r);
                    int nBelow = 0;
                    for (int row = loc - testWidth; row < loc + testWidth; row++)
                    {
                        if (row < 0 || row >= matrix.Rows)
                            continue;
                        byte value = matrix.Data[row, colLoc];
                        if (value < threshold)
                            nBelow++;
                    }
                    result[r, c] = (int)(100 * (nBelow / (2.0 * testWidth)));
                }
            }
            return result;
        }

        private int GetThreshold(int[,] Values)
        {
            int rows = Values.GetLength(0);
            int cols = Values.GetLength(1);
            List<int> AllValues = new List<int>();
            foreach (int value in Values)
                if (!AllValues.Contains(value))
                    AllValues.Add(value);
            AllValues.Sort();
            int result = AllValues.Min();
            int maxGap = 0;
            for (int i = 1; i < AllValues.Count; i++)
            {
                int gap = AllValues[i] - AllValues[i - 1];
                if (gap > maxGap)
                {
                    maxGap = gap;
                    result = AllValues[i - 1];
                }
            }
            return result;
        }
        private bool[,] AnalyzeBorderValues(int[,] BorderValues)
        {
            int rows = BorderValues.GetLength(0);
            int cols = BorderValues.GetLength(1);
            bool[,] result = new bool[rows, cols];
            int threshold = GetThreshold(BorderValues);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    result[r, c] = (BorderValues[r, c] > threshold);
            return result;
        }
        public void Dump(StreamWriter sw)
        {
            for (int r = 0; r < Rows; r++)
            {
                if (r == 0)
                {
                    sw.Write("  \u2506");
                    for (int c = 0; c < Cols; c++)
                        sw.Write("{0,2}", c);
                    sw.WriteLine();
                }
                for (int c = 0; c < Cols; c++)
                {
                    if (c == 0)
                        sw.Write("{0,2}:", r);
                    sw.Write(" ");
                    if (RightAreaBorders[r, c])
                        sw.Write("|");
                    else
                        sw.Write(" ");
                }
                sw.WriteLine();
                for (int c = 0; c < Cols; c++)
                {
                    if (c == 0)
                        sw.Write("   ");
                    if (BottomAreaBorders[r, c])
                        sw.Write("__");
                    else
                        sw.Write("  ");
                }
                sw.WriteLine();
            }
        }

    }
}
