﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace topencv01
{
    public partial class GridSizeForm : Form
    {
        public OCVGridDefinition GridDef;
        private Image<Bgr, Byte> imageCopy;
        private Image<Bgr, Byte> imageOriginal;

        public GridSizeForm()
        {
            InitializeComponent();
        }

        public void LoadImage(Image<Bgr, Byte> image)
        {
            imageCopy = image.Clone();
            imageOriginal = image;
            pbGrid.Image = imageOriginal.Bitmap;
        }

        public void SetGridDef(OCVGridDefinition gridDef)
        {
            GridDef = new OCVGridDefinition(gridDef);
            nudRows.Value = GridDef.Rows;
            nudCols.Value = GridDef.Cols;
            DrawGrid();
        }

        private void DrawGrid()
        {
            DrawGrid(imageCopy, GridDef, new Bgr(Color.Purple));
            pbGrid.Image = imageCopy.Bitmap;
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

        private void cbShowGrid_CheckedChanged(object sender, EventArgs e)
        {
            if (cbShowGrid.Checked)
            {
                DrawGrid();
                cbShowGrid.Text = "Hide grid";
            }
            else
            {
                pbGrid.Image = imageOriginal.Bitmap;
                cbShowGrid.Text = "Show grid";
            }
        }
        public OCVGridDefinition GetGridDef()
        {
            return new OCVGridDefinition(GridDef);
        }

        private void Redraw()
        {
            if (cbShowGrid.Checked)
            {
                imageCopy = imageOriginal.Clone();
                DrawGrid();
            }

        }
        private void nudRows_ValueChanged(object sender, EventArgs e)
        {
            GridDef.Rows = (int)nudRows.Value;
            Redraw();
        }

        private void nudCols_ValueChanged(object sender, EventArgs e)
        {
            GridDef.Cols = (int)nudCols.Value;
            Redraw();
        }
    }
}
