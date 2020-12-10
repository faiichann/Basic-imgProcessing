using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hw1_61080500262
{
    public partial class Form1 : Form
    {
        private Bitmap Image, Image2;
        private BitmapData ImageData, ImageData2;
        private byte[] buffer, buffer2;
        private int r, g, b, contrast, r_x, g_x, b_x, r_y, g_y, b_y, grayscale, location, location2;
        private float factor;
        private sbyte weight_x, weight_y;
        private sbyte[,] weights_x;
        private sbyte[,] weights_y;
        private IntPtr pointer, pointer2;

        public Form1()
        {
            InitializeComponent();
            weights_x = new sbyte[,] { { 1, 0, -1 }, { 2, 0, -2 }, { 1, 0, -1 } };
            weights_y = new sbyte[,] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                Image = new Bitmap(open.FileName);
                Image2 = new Bitmap(Image.Width, Image.Height);
            }
            pictureBox1.Image = Image;
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
            if (save.ShowDialog() == DialogResult.OK)
            {
                pictureBox2.Image.Save(save.FileName,ImageFormat.Bmp);
            }
        }

        private void contrastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image = new Bitmap(pictureBox1.Image);
            contrast = (int)updown.Value;
            ImageData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            buffer = new byte[3*Image.Width * Image.Height];
            pointer = ImageData.Scan0;
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            for (int i = 0; i < Image.Height * 3 * Image.Width; i += 3)
            {
                factor = (259 * ((float)contrast + 255)) / (255 * (259 - (float)contrast));
                b = (int)(factor * (buffer[i] - 128) + 128);
                g = (int)(factor * (buffer[i + 1] - 128) + 128);
                r = (int)(factor * (buffer[i + 2] - 128) + 128);
                if (b > 255) b = 255;
                else if (b < 0) b = 0;
                if (g > 255) g = 255;
                else if (g < 0) g = 0;
                if (r > 255) r = 255;
                else if (r < 0) r = 0;
                buffer[i] = (byte)b;
                buffer[i + 1] = (byte)g;
                buffer[i + 2] = (byte)r;
            }
            Marshal.Copy(buffer, 0, pointer, buffer.Length);
            Image.UnlockBits(ImageData);
            pictureBox2.Image = Image;
        }

        private void sobelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image = new Bitmap(pictureBox1.Image);
            Image2 = new Bitmap(Image.Width, Image.Height);
            ImageData = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            ImageData2 = Image2.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            buffer = new byte[ImageData.Stride * Image.Height];
            buffer2 = new byte[ImageData.Stride * Image.Height];
            pointer = ImageData.Scan0;
            pointer2 = ImageData2.Scan0;
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            for (int y = 0; y < Image.Height; y++)
            {
                for (int x = 0; x < Image.Width * 3; x += 3)
                {
                    r_x = g_x = b_x = 0; //reset the gradients in x-direcion values
                    r_y = g_y = b_y = 0; //reset the gradients in y-direction values
                    location = x + y * ImageData.Stride; //to get the location of any pixel >> location = x + y * Stride
                    for (int yy = -(int)Math.Floor(weights_y.GetLength(0) / 2.0d), yyy = 0; yy <= (int)Math.Floor(weights_y.GetLength(0) / 2.0d); yy++, yyy++)
                    {
                        if (y + yy >= 0 && y + yy < Image.Height) //to prevent crossing the bounds of the array
                        {
                            for (int xx = -(int)Math.Floor(weights_x.GetLength(1) / 2.0d) * 3, xxx = 0; xx <= (int)Math.Floor(weights_x.GetLength(1) / 2.0d) * 3; xx += 3, xxx++)
                            {
                                if (x + xx >= 0 && x + xx <= Image.Width * 3 - 3) //to prevent crossing the bounds of the array
                                {
                                    location2 = x + xx + (yy + y) * ImageData.Stride; //to get the location of any pixel >> location = x + y * Stride
                                    weight_x = weights_x[yyy, xxx];
                                    weight_y = weights_y[yyy, xxx];
                                    //applying the same weight to all channels
                                    b_x += buffer[location2] * weight_x;
                                    g_x += buffer[location2 + 1] * weight_x; //G_X
                                    r_x += buffer[location2 + 2] * weight_x;
                                    b_y += buffer[location2] * weight_y;
                                    g_y += buffer[location2 + 1] * weight_y;//G_Y
                                    r_y += buffer[location2 + 2] * weight_y;
                                }
                            }
                        }
                    }
                    //getting the magnitude for each channel
                    b = (int)Math.Sqrt(Math.Pow(b_x, 2) + Math.Pow(b_y, 2));
                    g = (int)Math.Sqrt(Math.Pow(g_x, 2) + Math.Pow(g_y, 2));//G
                    r = (int)Math.Sqrt(Math.Pow(r_x, 2) + Math.Pow(r_y, 2));

                    if (b > 255) b = 255;
                    if (g > 255) g = 255;
                    if (r > 255) r = 255;

                    //getting grayscale value
                    grayscale = (b + g + r) / 3;

                    //thresholding to clean up the background
                    //if (grayscale < 80) grayscale = 0;
                    buffer2[location] = (byte)grayscale;
                    buffer2[location + 1] = (byte)grayscale;
                    buffer2[location + 2] = (byte)grayscale;
                    //thresholding to clean up the background
                    //if (b < 100) b = 0;
                    //if (g < 100) g = 0;
                    //if (r < 100) r = 0;

                    //buffer2[location] = (byte)b;
                    //buffer2[location + 1] = (byte)g;
                    //buffer2[location + 2] = (byte)r;
                }
            }
            Marshal.Copy(buffer2, 0, pointer2, buffer.Length);
            Image.UnlockBits(ImageData);
            Image2.UnlockBits(ImageData2);
            pictureBox2.Image = Image2;
        }
        private void updown_ValueChanged(object sender, EventArgs e)
        {

        }

        private void redToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image = new Bitmap(pictureBox1.Image);
            Image2 = new Bitmap(Image.Width, Image.Height);
            for (int y = 0; y < Image.Height; y++)
            {
                for (int x = 0; x < Image.Width; x++)
                {
                    Color c = Image.GetPixel(x, y);
                    int red = c.R;
                    Color cNew = Color.FromArgb(c.R, 0, 0);
                    Image2.SetPixel(x, y, cNew);
                }
            }
            pictureBox2.Image = Image2;
        }

        private void greenToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Image = new Bitmap(pictureBox1.Image);
            Image2 = new Bitmap(Image.Width, Image.Height);
            for (int y = 0; y < Image.Height; y++)
            {
                for (int x = 0; x < Image.Width; x++)
                {
                    Color c = Image.GetPixel(x, y);
                    int green = c.G;
                    Color cNew = Color.FromArgb(0, c.G, 0);
                    Image2.SetPixel(x, y, cNew);
                }
            }
            pictureBox2.Image = Image2;
        }

        private void blueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image = new Bitmap(pictureBox1.Image);
            Image2 = new Bitmap(Image.Width, Image.Height);
            for (int y = 0; y < Image.Height; y++)
            {
                for (int x = 0; x < Image.Width; x++)
                {
                    Color c = Image.GetPixel(x, y);
                    int blue = c.B;
                    Color cNew = Color.FromArgb(0, 0, c.B);
                    Image2.SetPixel(x, y, cNew);
                }
            }
            pictureBox2.Image = Image2;
        }
    }
}
