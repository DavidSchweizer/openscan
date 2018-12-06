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
using Emgu.CV.OCR;
using Emgu.CV.Util;


namespace topencv01
{
    public partial class Form1 : Form
    {

        Image<Bgr, Byte> imgOriginal;
        //Image<Bgr, Byte> img1 = new Image<Bgr, Byte>("c:/temp/test1.jpg");
        UMat matGrayScaleImage;
        Image<Bgr, Byte> lineImage;
        Image<Bgr, Byte> lineImage2;

        public Form1()
        {
            InitializeComponent();
        }


        private void GrayScaleImage()
        {
            //Convert the image to grayscale and filter out the noise
            matGrayScaleImage = new UMat();
            CvInvoke.CvtColor(imgOriginal, matGrayScaleImage, ColorConversion.Bgr2Gray);
            //use image pyr to remove noise
            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(matGrayScaleImage, pyrDown);
            CvInvoke.PyrUp(pyrDown, matGrayScaleImage);
            UMat mat2 = matGrayScaleImage.Clone();
            //CvInvoke.Threshold(mat2, matGrayScaleImage, 200, 255, ThresholdType.Binary);
            pbGray.Image = matGrayScaleImage.Bitmap;
        }

        private void LoadFile(string filename)
        {
            imgOriginal = new Image<Bgr, Byte>(filename);
            pbOriginal.Image = imgOriginal.ToBitmap();
            GrayScaleImage();
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
            CvInvoke.Canny(matGrayScaleImage, cannyEdges, cannyThreshold, cannyThresholdLinking);
            pbEdges.Image = cannyEdges.Bitmap;
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

        private void DrawGrid(Image<Bgr, Byte> image, OCVGridDefinition gridDef, Bgr color)
        {
            for (int r = 0; r <= gridDef.Rows; r++)
            {
                image.Draw(new LineSegment2D(new Point(gridDef.TopLeft.X, gridDef.RowLocation(r)),
                                             new Point(gridDef.TopLeft.X + gridDef.Width, gridDef.RowLocation(r))),
                                                  color, 2);
            }
            for (int c = 0; c <= gridDef.Cols; c++)
            {
                image.Draw(new LineSegment2D(new Point(gridDef.ColLocation(c), gridDef.TopLeft.Y),
                                                  new Point(gridDef.ColLocation(c), gridDef.TopLeft.Y + gridDef.Height)),
                                                  color, 2);
            }

            image.Draw(new Rectangle(gridDef.TopLeft.X, gridDef.TopLeft.Y, gridDef.Width, gridDef.Height), color, 2);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            UMat edges = FindEdges();
            LineSegment2D[] lines = FindLines(edges);

            lineImage = imgOriginal.CopyBlank();
            foreach (LineSegment2D line in lines)
                lineImage.Draw(line, new Bgr(Color.LightGreen), 2);
            pbLines.Image = lineImage.Bitmap;

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
            pbGridLines.Image = image3.Bitmap;

            Image<Bgr, Byte> lineImage2 = imgOriginal.CopyBlank();
            OCVGridDefinition gridDef = grid.Analyze();


//            MessageBox.Show(String.Format("{0} x {1} [delta: {2}]", gridDef.Rows, gridDef.Cols, grid.Delta(gridData, gridDef)));

            using (GridSizeForm gridForm = new GridSizeForm())
            {
                gridForm.LoadImage(imgOriginal);
                gridForm.SetGridDef(gridDef);
                if (gridForm.ShowDialog() == DialogResult.OK)
                {
                    gridDef = gridForm.GetGridDef();                                   
                }                    
            }
            DrawGrid(lineImage2, gridDef, new Bgr(Color.White));
            pbGrid.Image = lineImage2.Bitmap;

            Image<Bgr, Byte> lineImage4 = imgOriginal.Copy();
            DrawGrid(lineImage4, gridDef, new Bgr(Color.Purple));
            pbCombi.Image = lineImage4.Bitmap;


            tabControl1.SelectedTab = tbCombi;
            testingpixels(gridDef);
            CharacterTest(gridDef);
        }

        private void testingpixels(OCVGridDefinition gridDef)
        {
            int threshold = 250;

            Matrix<Byte> matrix = new Matrix<Byte>(matGrayScaleImage.Rows, matGrayScaleImage.Cols, matGrayScaleImage.NumberOfChannels);
            matGrayScaleImage.CopyTo(matrix);
            using (StreamWriter sw = new StreamWriter("pixels.dmp"))
            {
                sw.WriteLine("------------------ROWS-------------------");
                for (int r = 0; r < gridDef.Rows; r++)
                {
                    int rowLoc = (int)(gridDef.RowLocation(r) + gridDef.RowSize * 0.5);
                    sw.WriteLine("*** row: {0}    (loc: {1})", r, rowLoc);
                    for (int c = 1; c < gridDef.Cols; c++)
                    {
                        int loc = gridDef.ColLocation(c);
                        sw.WriteLine("col {0}  location {1}", c, loc);
                        int nBelow = 0;
                        for (int col = loc - 10; col < loc + 10; col++)
                        {
                            if (col < 0 || col >= matrix.Cols)
                                continue;
                            byte value = matrix.Data[rowLoc, col];
                            if (value < threshold)
                                nBelow++;
                            //sw.WriteLine("row: {0}  col: {1}  : {2}", row, col, value);
                        }
                        sw.WriteLine("col {0}  pct: {1} %", c, (nBelow / 20.0) * 100);
                    }
                    sw.WriteLine("***");
                }
                sw.WriteLine("------------------COLUMNS-------------------");
                for (int c = 0; c < gridDef.Cols; c++)
                {
                    int colLoc = (int)(gridDef.ColLocation(c) + gridDef.ColSize * 0.5);
                    sw.WriteLine("*** col: {0}    (loc: {1})", c, colLoc);
                    for (int r = 1; r < gridDef.Rows; r++)
                    {
                        int loc = gridDef.RowLocation(r);
                        sw.WriteLine("row {0}  location {1}", r, loc);
                        int nBelow = 0;
                        for (int row = loc - 10; row < loc + 10; row++)
                        {
                            if (row < 0 || row >= matrix.Cols)
                                continue;
                            byte value = matrix.Data[row, colLoc];
                            if (value < threshold)
                                nBelow++;
                            //sw.WriteLine("row: {0}  col: {1}  : {2}", row, col, value);
                        }
                        sw.WriteLine("row {0}  pct: {1} %", r, (nBelow / 20.0) * 100);
                    }
                    sw.WriteLine("***");
                }
            }

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                LoadFile(ofd1.FileName);
                button2.Enabled = true;
                tabControl1.SelectedTab = tbControls;
            }
            else
                button2.Enabled = false;
        }

        Tesseract _ocr;

        Rectangle gridRect(OCVGridDefinition gridDef, int row, int col)
        {
            return new Rectangle((int)gridDef.ColLocation(col), (int)gridDef.RowLocation(row), (int)gridDef.ColSize, (int)gridDef.RowSize);
        }
        string TestRowCol(Image<Gray,Byte> image, OCVGridDefinition gridDef, int row, int col, StreamWriter sw)
        {
            const int MARGIN = 7;
            string charFound;
            Rectangle Rect = gridRect(gridDef, row, col);
            Rect.X += MARGIN;
            Rect.Y += MARGIN;
            Rect.Width -= 2 * MARGIN;
            Rect.Height -= 2 * MARGIN;
            Image<Gray, Byte> hokje = image.Copy(Rect);
            Image<Gray, Byte> hokje2 = hokje.CopyBlank();
            CvInvoke.Threshold(hokje, hokje2, 100, 255, ThresholdType.Binary);
            image.Draw(Rect, new Gray(25));

             _ocr.SetImage(hokje2);
            
            if (_ocr.Recognize() != 0)
                throw new Exception("Failed to recognize zie image");
            Tesseract.Character[] characters = _ocr.GetCharacters();
            sw.WriteLine("{0} characters recognized", characters.Length);
            charFound = "";
            foreach (Tesseract.Character ch in characters)
                if (ch.Text != " ")
                {
                    charFound = ch.Text;
                    sw.WriteLine("{0} ({1})", ch.Text, ch.Cost);
                }
            if (charFound != "")
            {
                CvInvoke.PutText(image, charFound, new Point(Rect.Left, Rect.Top + 24), FontFace.HersheyPlain, 2, new MCvScalar(100));

                //Matrix<Byte> matrix = new Matrix<Byte>(hokje.Rows, hokje.Cols, hokje.NumberOfChannels);
                //hokje.CopyTo(matrix);

                //for (int r =0; r < matrix.Width; r++)
                //{
                //    sw.Write("r {0}:", r);
                //    for (int c = 0; c < matrix.Width; c++)
                //        sw.Write("{0} ", matrix.Data[r, c]);
                //    sw.WriteLine();
                //}


                return charFound;
            }
            return "";
        }
        void CharacterTest(OCVGridDefinition gridDef)
        {
            const string dataPath = @"C:\Program Files (x86)\Tesseract-OCR\tessdata";

            //create OCR engine
            _ocr = new Tesseract(dataPath, "eng", OcrEngineMode.TesseractLstmCombined);
            _ocr.SetVariable("tessedit_char_whitelist", "12345");


            Image<Gray,Byte > image23 = matGrayScaleImage.ToImage<Gray, Byte>().Copy();
            pbOCR.Image = image23.Bitmap;

            using (StreamWriter sw = new StreamWriter("ocr.txt"))
            {
                for (int row = 0; row < gridDef.Rows; row++)
                    for (int col = 0; col < gridDef.Cols; col++)
                       
                    {
                        sw.Write("({0},{1}):", row, col);
                        string charFound = TestRowCol(image23, gridDef, row, col, sw);
                        if (charFound.Trim() != "")
                            sw.WriteLine(charFound);
                        else
                            sw.WriteLine();

                    }
            }


        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            button2_Click(sender, e);
        }

        private void tbOriginal_Click(object sender, EventArgs e)
        {

        }
    }

    
}
