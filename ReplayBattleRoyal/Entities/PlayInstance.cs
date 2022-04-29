using FFMpegCore;
using FFMpegCore.Enums;
using Newtonsoft.Json;
using ReplayBattleRoyal.GameModes;
using ReplayBattleRoyal.Managers;
using ReplayBattleRoyal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReplayBattleRoyal.Entities
{
    public class PlayInstance
    {
        private MainWindow mainWindow;
        private static MediaPlayer mediaPlayer = new MediaPlayer();

        public MapDetailsModel mapDetails;
        public List<Grid> lightEventBackgrounds = new List<Grid>();
        private Bitmap originalImage;
        private ImageBrush backgroundBrush;
        private Bitmap backgroundImageBitmap;
        private ColorManager.HSVColor[,] originalPixels;
        private MapInfoModel mapInfoModel;
        private Random random = new Random();

        public System.Windows.Media.Brush NoteColorLeft { get; set; }
        public System.Windows.Media.Brush NoteColorRight { get; set; }
        public System.Windows.Media.Brush NoteColorLeftArrow { get; set; }
        public System.Windows.Media.Brush NoteColorRightArrow { get; set; }


        public PlayInstance(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            NoteColorLeft = System.Windows.Media.Brushes.Red;
            NoteColorRight = System.Windows.Media.Brushes.Blue;
            NoteColorLeftArrow = System.Windows.Media.Brushes.White;
            NoteColorRightArrow = System.Windows.Media.Brushes.White;
        }

        public void EliminateLastPlayer()
        {
            mainWindow.Dispatcher.Invoke(() =>
            {
                RemovePlayer(mainWindow.leaderboard.GetLastPlayer());
            });
        }

        public void RemovePlayer(Player player)
        {
            mainWindow.Dispatcher.Invoke(() =>
            {
                if (player == null) return;
                var playerToRemove = mainWindow.leaderboard.GetPlayer(player.Name);
                if (playerToRemove == null) return;

                //Perfect acc Mode
                if (mainWindow.gameMode.SelectedGamemode == Gamemode.GameModes.PerfectAcc || mainWindow.gameMode.SelectedGamemode == Gamemode.GameModes.ComboDropSafe)
                {
                    var opacity = 0.2;
                    playerToRemove.Opacity = opacity;
                    player.LeftHand.Opacity = opacity;
                    player.RightHand.Opacity = opacity;
                    player.LeftHandTip.Opacity = opacity;
                    player.RightHandTip.Opacity = opacity;
                    player.Head.Opacity = opacity;
                    foreach (var trail in player.TrailListLeft) trail.Opacity = opacity;
                    foreach (var trail in player.TrailListRight) trail.Opacity = opacity;

                    mainWindow.leaderboard.RefreshLeaderboard();
                    return;
                }

                mainWindow.leaderboard.RemovePlayer(playerToRemove);
                if (!player.hasLead) mainWindow.Players.Remove(player);
                mainWindow.CanvasSpace.Children.Remove(player.LeftHand);
                mainWindow.CanvasSpace.Children.Remove(player.RightHand);
                mainWindow.CanvasSpace.Children.Remove(player.LeftHandTip);
                mainWindow.CanvasSpace.Children.Remove(player.RightHandTip);
                mainWindow.CanvasSpace.Children.Remove(player.Head);
                foreach (var trail in player.TrailListLeft) mainWindow.CanvasSpace.Children.Remove(trail);
                foreach (var trail in player.TrailListRight) mainWindow.CanvasSpace.Children.Remove(trail);

                mainWindow.leaderboard.RefreshLeaderboard();
            });
        }

        public async void AddNote(string noteInfo, double delay = 0)
        {
            var xNotePlacement = Convert.ToInt32(noteInfo.Substring(0, 1));
            var yNotePlacement = Convert.ToInt32(noteInfo.Substring(1, 1));
            var noteDirection = Convert.ToInt32(noteInfo.Substring(2, 1));
            var noteType = Convert.ToInt32(noteInfo.Substring(3, 1));

            mainWindow.Dispatcher.Invoke(async () =>
            {
                var noteRectangle = new System.Windows.Shapes.Rectangle() { Stroke = System.Windows.Media.Brushes.White, Width = 140, Height = 140, Fill = noteType == 0 ? NoteColorLeft : NoteColorRight, Opacity = 1, RadiusX = 10, RadiusY = 10 };
                var noteDot = new Ellipse() { Fill = System.Windows.Media.Brushes.White, Width = 50, Height = 50 };
                var converter = TypeDescriptor.GetConverter(typeof(Geometry));
                string pathData = "M50,50 L150,90 L250,50 L250,30 L50,30 Z";
                var noteArrow = new System.Windows.Shapes.Path() { Data = (Geometry)converter.ConvertFrom(pathData), Fill = noteType == 0 ? NoteColorLeftArrow : NoteColorRightArrow, Stroke = System.Windows.Media.Brushes.White, StrokeThickness = 2, Width = 100, Height = 30, Stretch = Stretch.Fill };

                mainWindow.CanvasSpace.Children.Add(noteRectangle);

                var actualXNotePosition = mainWindow.CanvasSpace.Width / 4 * xNotePlacement + 110;
                var actualYNotePosition = mainWindow.CanvasSpace.Height / 3 * yNotePlacement + 100;
                var posxdir = actualXNotePosition + 15;
                var posydir = actualYNotePosition + 65;

                Canvas.SetLeft(noteRectangle, actualXNotePosition);
                Canvas.SetBottom(noteRectangle, actualYNotePosition);

                //Dot note
                if (noteDirection == 8)
                {
                    mainWindow.CanvasSpace.Children.Add(noteDot);
                    Canvas.SetLeft(noteDot, actualXNotePosition + 40);
                    Canvas.SetBottom(noteDot, actualYNotePosition + 40);
                }
                else
                {
                    mainWindow.CanvasSpace.Children.Add(noteArrow);
                    Canvas.SetLeft(noteArrow, posxdir);
                    Canvas.SetBottom(noteArrow, posydir);
                }
                                                                                        //x up = right // y up = down
                if (noteDirection == 0) noteArrow.RenderTransform = new RotateTransform(180, 53, 30);//up
                if (noteDirection == 1) noteArrow.RenderTransform = new RotateTransform(0, -45, -20); //down 
                if (noteDirection == 2) noteArrow.RenderTransform = new RotateTransform(90, 50, 28); //left 
                if (noteDirection == 3) noteArrow.RenderTransform = new RotateTransform(270, 48, 33);//right

                if (noteDirection == 4)//upleft
                {
                    noteRectangle.RenderTransform = new RotateTransform(315, noteRectangle.Width / 2, noteRectangle.Height / 2);
                    noteArrow.RenderTransform = new RotateTransform(135, noteRectangle.Width / 2 - 15, noteRectangle.Height / 2 - 35);
                }
                if (noteDirection == 5)//upright
                {
                    noteRectangle.RenderTransform = new RotateTransform(45, noteRectangle.Width / 2, noteRectangle.Height / 2);
                    noteArrow.RenderTransform = new RotateTransform(225, noteRectangle.Width / 2 - 20, noteRectangle.Height / 2 - 35);
                }
                if (noteDirection == 6)//downleft
                {
                    noteRectangle.RenderTransform = new RotateTransform(45, noteRectangle.Width / 2, noteRectangle.Height / 2);
                    noteArrow.RenderTransform = new RotateTransform(45, noteRectangle.Width / 2 - 5, noteRectangle.Height / 2 - 25);
                }
                if (noteDirection == 7)//downright
                {
                    noteRectangle.RenderTransform = new RotateTransform(315, noteRectangle.Width / 2, noteRectangle.Height / 2);
                    noteArrow.RenderTransform = new RotateTransform(315, noteRectangle.Width / 2 - 35, noteRectangle.Height / 2 - 30);
                }


                //Add Note animation
                //TODO: make note animation
                noteRectangle.Opacity = 0.15;
                //for (var i = 0; i < 3; i++)
                //{
                //    await Task.Delay(50 / 3);
                //    noteRectangle.Width += 10;
                //    noteRectangle.Height += 10;
                //    Canvas.SetLeft(noteRectangle, actualXNotePosition - 5 * i);
                //    Canvas.SetBottom(noteRectangle, actualYNotePosition - 5 * i);
                //    Canvas.SetLeft(noteArrow, posxdir - 5 * i);
                //    Canvas.SetBottom(noteArrow, posydir - 5 * i);
                //}

                //70
                var soundDelay = 0.070;
                if (delay - soundDelay > 0) await Task.Delay(TimeSpan.FromSeconds(delay - soundDelay));
                //AudioManager.PlayHitSound();
                noteRectangle.Opacity = 1;
                await Task.Delay(TimeSpan.FromSeconds(soundDelay));

                //Remove all note parts
                mainWindow.CanvasSpace.Children.Remove(noteRectangle);
                mainWindow.CanvasSpace.Children.Remove(noteArrow);
                mainWindow.CanvasSpace.Children.Remove(noteDot);
            });
        }

        public async Task PlaySong()
        {
            mediaPlayer.Open(new Uri(AppContext.BaseDirectory + $@"Audio\{mainWindow.songID}.mp3"));
            mediaPlayer.Play();

            //var noteTimings = new List<double>();
            //foreach (var time in Players.First().ReplayModel.NoteTime) noteTimings.Add(time);
            //AudioManager.PlayHitSounds(noteTimings);

            var sw = new Stopwatch();
            sw.Start();

            while (true)
            {
                mainWindow.songTime = sw.ElapsedMilliseconds / 1000.000;
                mainWindow.Dispatcher.Invoke(() => { mainWindow.SongTimeLabel.Content = mainWindow.songTime; });
                await Task.Delay(100);
            }
        }

        public async Task PrepareSongFile(string hash, bool useBackgroundVideo = false)
        {
            if (!File.Exists(AppContext.BaseDirectory + @$"Audio\ZipTemp/Download/{hash}.zip"))
            {
                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
                    webClient.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
                    webClient.DownloadFileAsync(new Uri($"https://eu.cdn.beatsaver.com/{hash.ToLower()}.zip"), $@"Audio\ZipTemp/Download/{hash}.zip");
                    do
                    {
                        await Task.Delay(100);
                    } while (webClient.IsBusy);
                    webClient.Dispose();
                }
            }

            using (ZipArchive archive = ZipFile.OpenRead(AppContext.BaseDirectory + $@"Audio/ZipTemp/Download/{hash}.zip"))
            {

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.Replace("Standard", "").Contains($"{mainWindow.leaderboard.leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "").Replace("Standard", "")}.dat"))
                    {
                        if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.leaderboard.leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "").Replace("Standard", "")}.dat")) File.Delete(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.leaderboard.leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "").Replace("Standard", "")}.dat");
                        entry.ExtractToFile(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.leaderboard.leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "").Replace("Standard", "")}.dat");
                    }
                    if (entry.FullName.ToLower().Contains($"info.dat"))
                    {
                        if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/info_{mainWindow.songID}.dat")) File.Delete(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/info_{mainWindow.songID}.dat");
                        entry.ExtractToFile(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/info_{mainWindow.songID}.dat");
                    }
                    if (entry.FullName.Contains(".egg"))
                    {
                        if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.egg")) File.Delete(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.egg");
                        entry.ExtractToFile(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.egg");
                    }
                    if (entry.FullName.Contains(".jpg") || entry.FullName.Contains(".png") && !File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.jpg"))
                    {
                        if (entry.FullName.Contains("cover"))
                        {
                            if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.jpg")) File.Delete(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.jpg");
                            entry.ExtractToFile(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.jpg");
                        }
                    }
                }
            }

            FFMpegCore.GlobalFFOptions.Configure(new FFOptions { BinaryFolder = AppContext.BaseDirectory + @"Audio/ffmpeg" });
            FFMpegCore.FFMpegArguments.FromFileInput(AppContext.BaseDirectory + $@"Audio\ZipTemp/Extract/{mainWindow.songID}.egg")
                .OutputToFile(AppContext.BaseDirectory + $@"Audio\{mainWindow.songID}.mp3", true, options =>
                options.WithAudioCodec(audioCodec: AudioCodec.LibMp3Lame))
                .ProcessSynchronously();

            //Get the NoteJumpSpeedBeatOffset
            var mapInfoJson = File.ReadAllText(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/info_{mainWindow.songID}.dat");
            mapInfoModel = JsonConvert.DeserializeObject<MapInfoModel>(mapInfoJson);

            if (useBackgroundVideo)
            {
                if (File.Exists(GlobalConfig.BasePath + "/Resources/Vids/" + $"{mainWindow.songID}.mp4"))
                {
                    mainWindow.BackgroundVideo.Source = new Uri(GlobalConfig.BasePath + "/Resources/Vids/" + $"{mainWindow.songID}.mp4");
                    mainWindow.BackgroundVideo.Stretch = Stretch.Fill;
                    mainWindow.BackgroundVideo.Opacity = 0.22;
                    mainWindow.BackgroundVideo.Volume = 0;
                    mainWindow.BackgroundVideo.Play();
                    await Task.Delay(10000);
                    mainWindow.BackgroundVideo.Position = TimeSpan.FromSeconds(0);
                    mainWindow.BackgroundVideo.Pause();
                }
            }
            else
            {
                if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.jpg"))
                {
                    backgroundBrush = new ImageBrush();
                    var bitmap = new Bitmap(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{mainWindow.songID}.jpg");
                    originalImage = bitmap;
                    backgroundImageBitmap = MediaManager.GrayScaleFilter(bitmap);


                    //Set main background
                    var imageSource = MediaManager.BitmapToImageSource(backgroundImageBitmap);
                    backgroundBrush.ImageSource = imageSource;
                    //0.04
                    backgroundBrush.Opacity = 0.06;
                    mainWindow.MainGrid.Background = backgroundBrush;

                    //Set original pixels
                    originalPixels = new ColorManager.HSVColor[originalImage.Width, originalImage.Height];
                    for (var y = 0; y < originalImage.Height; y++)
                    {
                        for (var x = 0; x < originalImage.Width; x++)
                        {
                            var color = originalImage.GetPixel(x, y);
                            //Testing-------------
                            //var color2 = System.Drawing.Color.FromArgb(0, 0, 0);
                            //var color3 = System.Drawing.Color.FromArgb(0, 0, 0);
                            //if (x - 1 > 0 && y - 1 > 0) color2 = originalImage.GetPixel(x - 1, y - 1);
                            //if (x - 2 > 0 && y - 2 > 0) color3 = originalImage.GetPixel(x - 2, y - 2);

                            //var newColor = System.Drawing.Color.FromArgb(
                            //    color.R / 3 + color2.R / 3 + color3.R / 3,
                            //    color.G / 3 + color2.G / 3 + color3.G / 3,
                            //    color.B / 3 + color2.B / 3 + color3.B / 3
                            //    );

                            //-----------------------
                            var hsv = ColorManager.ColorToHSV(color);
                            originalPixels[x, y] = hsv;
                        }
                    }

                    //Add different types of background images to lightshow list
                    var amount = 14;
                    var offset = 360 / amount;
                    for (var i = 0; i < amount; i++)
                    {
                        double minSat = 0.05;
                        double minVal = 0.05;
                        double maxSat = 1;
                        double maxVal = 1;

                        var piece = 360 / amount;
                        var share = 360 / amount * i;

                        var bmGrayScale = (Bitmap)backgroundImageBitmap.Clone();
                        var randomRange = random.Next(0, 360);

                        var coloredPixelCount = 0;
                        for (var y = 0; y < bmGrayScale.Height; y++)
                        {
                            for (var x = 0; x < bmGrayScale.Width; x++)
                            {
                                var hue = originalPixels[x, y].Hue;
                                var sat = originalPixels[x, y].Saturation;
                                var val = originalPixels[x, y].Value;
                                if (hue > share - (piece / 2) - offset && hue < share + (piece / 2) + offset && sat > minSat && val > minVal && val < maxVal && sat < maxSat) coloredPixelCount++;
                            }
                        }

                        maxSat = 1 - ((coloredPixelCount * (maxSat * 1)) / (bmGrayScale.Height * bmGrayScale.Width));
                        maxVal = 1 - ((coloredPixelCount * (maxVal * 1)) / (bmGrayScale.Height * bmGrayScale.Width));

                        for (var y = 0; y < bmGrayScale.Height; y++)
                        {
                            for (var x = 0; x < bmGrayScale.Width; x++)
                            {
                                var hue = originalPixels[x, y].Hue;
                                var sat = originalPixels[x, y].Saturation;
                                var val = originalPixels[x, y].Value;

                                if (hue > share - (piece / 2) - offset && hue < share + (piece / 2) + offset && sat > minSat && val > minVal && val < maxVal && sat < maxSat)
                                {
                                    var mediaColor = ColorManager.ColorFromHSV(share, 1, 1);
                                    bmGrayScale.SetPixel(x, y, System.Drawing.Color.FromArgb(mediaColor.R, mediaColor.G, mediaColor.B));
                                }
                                else
                                {
                                    var pix = bmGrayScale.GetPixel(x, y);
                                    bmGrayScale.SetPixel(x, y, System.Drawing.Color.Transparent);
                                }
                            }
                        }
                        var background = new ImageBrush() { ImageSource = MediaManager.ToBitmapImage(bmGrayScale) };
                        var grid = new Grid() { Background = background, Visibility = Visibility.Hidden, Opacity = 0.1 };
                        mainWindow.LightshowOverlayGrid.Children.Add(grid);
                        lightEventBackgrounds.Add(grid);
                    }
                }
                else
                {
                    mainWindow.CanvasSpace.Background = System.Windows.Media.Brushes.Black;
                }
            }

            mapDetails = GetMapDetailsModel();


            await Task.Delay(2000);
            File.Delete(AppContext.BaseDirectory + $@"Audio\ZipTemp/Extract/{mainWindow.songID}.egg");
        }

        public MapDetailsModel GetMapDetailsModel()
        {
            var json = File.ReadAllText(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{ mainWindow.leaderboard.leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "").Replace("Standard", "")}.dat");
            var mapDetails = JsonConvert.DeserializeObject<MapDetailsModel>(json);
            return mapDetails;
        }
    }
}
