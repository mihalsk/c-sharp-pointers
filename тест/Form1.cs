using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace тест
{
    public partial class Form1 : Form
    {
        Bitmap newImage;
        private readonly object lockObj = new object();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "Image files (*.bmp;*.jpg)|*.bmp;*.jpg";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    newImage = new Bitmap(dlg.FileName);
                    pictureBox1.Image = new Bitmap(dlg.FileName);
                    progressBar1.Maximum = newImage.Width * newImage.Height;
                    progressBar1.Value = 0;
                    progressBar1.Minimum = 0;
                }
            }
        }

        private async void fastSobelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var progress = new Progress<int>(value => { progressBar1.Value = value; });
            pictureBox1.Image = await Task.Run(() => FastSobel(progress));
        }

        private void fastContrastingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 newChild = new Form2(this);
            newChild.BringToFront();
            newChild.Show();
        }

        /// <summary>контрастирование изображения.</summary>
        /// <param name="progress">параметр для обновления прогрессбара на главной форме.</param>
        /// <param name="value">значение контраста.</param>
        /// <value>обработанное изображение</value>
        public Bitmap FastContrasting(IProgress<int> progress, float value)
        {
            value = (100.0f + value) / 100.0f;
            value *= value;
            Bitmap NewBitmap;
            lock (lockObj)
            {
                NewBitmap = (Bitmap)newImage.Clone();
                BitmapData bd = NewBitmap.LockBits(
                    new Rectangle(0, 0, NewBitmap.Width, NewBitmap.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format24bppRgb);
                int x = 0, y = 0;
                unsafe
                {
                    for (y = 0; y < NewBitmap.Height; y++)
                    {
                        byte* row = (byte*)bd.Scan0 + (y * bd.Stride);
                        int columnOffset = 0;
                        for (x = 0; x < NewBitmap.Width; x++)
                        {
                            byte B = *(row + columnOffset);
                            byte G = *(row + columnOffset + 1);
                            byte R = *(row + columnOffset + 2);

                            float Red = R / 255.0f;
                            float Green = G / 255.0f;
                            float Blue = B / 255.0f;
                            Red = (((Red - 0.5f) * value) + 0.5f) * 255.0f;
                            Green = (((Green - 0.5f) * value) + 0.5f) * 255.0f;
                            Blue = (((Blue - 0.5f) * value) + 0.5f) * 255.0f;

                            int newR = clamp((int)Red, 0, 255);
                            int newG = clamp((int)Green, 0, 255);
                            int newB = clamp((int)Blue, 0, 255);

                            *(row + columnOffset) = (byte)newB;
                            *(row + columnOffset + 1) = (byte)newG;
                            *(row + columnOffset + 2) = (byte)newR;

                            columnOffset += 3;
                        }
                        progress?.Report(x * y);
                    }
                    progress?.Report(x * y);
                }

                NewBitmap.UnlockBits(bd);
            }
            

            return NewBitmap;
        }

        /// <summary>градиент по собелю.</summary>
        /// <param name="progress">параметр для обновления прогрессбара на главной форме.</param>
        /// <value>обработанное изображение</value>
        private Bitmap FastSobel(IProgress<int> progress)
        {
            Bitmap image = (Bitmap)newImage.Clone();
            Bitmap outputBitmap = new Bitmap(image.Width, image.Height);
            int[][] sobelx = {new int[] {-1, 0, 1},
                          new int[] {-2, 0, 2},
                          new int[] {-1, 0, 1}};

            int[][] sobely = {new int[] {-1, -2, -1},
                          new int[] { 0, 0, 0},
                          new int[] { 1, 2, 1}};
            int x = 1, y = 1;

            BitmapData bd = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb); 
            BitmapData outputBd = outputBitmap.LockBits(new Rectangle(0, 0, outputBitmap.Width, outputBitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb); 
            try
            {
                unsafe
                {
                    byte* curpos;
                    byte* curposOutputBd;
                    for (y = 1; y < image.Height - 1; y++)
                    {
                        curpos = ((byte*)bd.Scan0) + y * bd.Stride + 2 + 3; // +3 - пропускаем первый пиксель по х, +2 - смещение красной компоненты(bgr)
                        curposOutputBd = ((byte*)outputBd.Scan0) + y * outputBd.Stride;
                        for (x = 1; x < image.Width - 1; x++)
                        {
                            int dx = *(curpos - bd.Stride - 3) * sobelx[0][0]  //        int dx = bmp.GetPixel(x - 1, y - 1).R * sobelx[0][0]
                                   + *(curpos - bd.Stride) * sobelx[0][1]      //               + bmp.GetPixel(x, y - 1).R * sobelx[0][1]
                                   + *(curpos - bd.Stride + 3) * sobelx[0][2]  //               + bmp.GetPixel(x + 1, y - 1).R * sobelx[0][2]
                                   + *(curpos - 3) * sobelx[1][0]              //               + bmp.GetPixel(x - 1, y).R * sobelx[1][0]
                                   + *(curpos) * sobelx[1][1]                  //               + bmp.GetPixel(x, y).R * sobelx[1][1]
                                   + *(curpos + 3) * sobelx[1][2]              //               + bmp.GetPixel(x + 1, y).R * sobelx[1][2]
                                   + *(curpos + bd.Stride - 3) * sobelx[2][0]  //               + bmp.GetPixel(x - 1, y + 1).R * sobelx[2][0]
                                   + *(curpos + bd.Stride) * sobelx[2][1]      //               + bmp.GetPixel(x, y + 1).R * sobelx[2][1]
                                   + *(curpos + bd.Stride + 3) * sobelx[2][2]; //               + bmp.GetPixel(x + 1, y + 1).R * sobelx[2][2];

                            int dy = *(curpos - bd.Stride - 3) * sobely[0][0]
                                   + *(curpos - bd.Stride) * sobely[0][1]
                                   + *(curpos - bd.Stride + 3) * sobely[0][2]
                                   + *(curpos - 3) * sobely[1][0]
                                   + *(curpos) * sobely[1][1]
                                   + *(curpos + 3) * sobely[1][2]
                                   + *(curpos + bd.Stride - 3) * sobely[2][0]
                                   + *(curpos + bd.Stride) * sobely[2][1]
                                   + *(curpos + bd.Stride + 3) * sobely[2][2];
                            curpos += 3;
                            
                            double derivative = Math.Sqrt((dx * dx) + (dy * dy));

                            if (derivative > 255)
                            {
                                //outputBitmap.SetPixel(x, y, Color.White);
                                *(curposOutputBd++) = Color.White.B;
                                *(curposOutputBd++) = Color.White.G;
                                *(curposOutputBd++) = Color.White.R;
                            }
                            else
                            {
                                //outputBitmap.SetPixel(x, y, Color.FromArgb(255, (int)derivative, (int)derivative, (int)derivative));
                                Color outputColor = Color.FromArgb(255, (int)derivative, (int)derivative, (int)derivative);
                                *(curposOutputBd++) = outputColor.B;
                                *(curposOutputBd++) = outputColor.G;
                                *(curposOutputBd++) = outputColor.R;
                            }
                        }
                        progress?.Report(x * y);
                    }
                }
                progress?.Report(x * y);
            }
            finally
            {
                image.UnlockBits(bd);
                outputBitmap.UnlockBits(outputBd);
            }
            return outputBitmap;
        }

        /// <summary>"Обрезка" значений по min и max.</summary>
        /// <param name="val">.</param>
        /// <param name="min">Минимальное значение.</param>
        /// <param name="max">Максимальное значение.</param>
        /// <value>"обрезанное" значение</value>
        int clamp(int val, int min, int max)
        {
            return val < min ? min : val > max ? max : val;
        }
    }
}
