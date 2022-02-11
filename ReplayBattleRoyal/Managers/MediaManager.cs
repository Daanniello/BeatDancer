using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ReplayBattleRoyal.Managers
{
    public class MediaManager
    {
        public static BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        public static Bitmap GrayScaleFilter(Bitmap image)
        {
            Bitmap grayScale = new Bitmap(image.Width, image.Height);

            for (Int32 y = 0; y < grayScale.Height; y++)
                for (Int32 x = 0; x < grayScale.Width; x++)
                {
                    System.Drawing.Color c = image.GetPixel(x, y);

                    Int32 gs = (Int32)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);

                    grayScale.SetPixel(x, y, System.Drawing.Color.FromArgb(gs, gs, gs));
                }
            return grayScale;
        }

        public static async Task ShowIntro(MainWindow mainWindow, string[] textList)
        {

            mainWindow.TransitionScreen.Visibility = Visibility.Visible;

            await Task.Delay(1000);

            foreach (var text in textList)
            {
                mainWindow.TransitionLabel.Text = text;
                await Task.Delay(4000);
            }

            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(10);
                mainWindow.TransitionScreen.Opacity -= 0.01;
                mainWindow.TransitionLabel.Opacity -= 0.01;
            }

            mainWindow.TransitionScreen.Visibility = Visibility.Hidden;
        }

        public static async Task ShowOutro(MainWindow mainWindow, string[] textList = null)
        {
            if (textList == null) mainWindow.TransitionLabel.Visibility = Visibility.Hidden;
            mainWindow.TransitionScreen.Visibility = Visibility.Visible;

            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(10);
                mainWindow.TransitionScreen.Opacity += 0.01;
                mainWindow.TransitionLabel.Opacity += 0.01;
            }

           if(textList != null)
            {
                foreach (var text in textList)
                {
                    mainWindow.TransitionLabel.Text = text;
                    await Task.Delay(4000);
                }
            }
        }
    }
}
