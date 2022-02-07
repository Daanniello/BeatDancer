using FFMpegCore;
using FFMpegCore.Enums;
using Newtonsoft.Json;
using ScoreSaberLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ScoreSaberLib.Models.LeaderboardScoresModel;

namespace ReplayBattleRoyal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int songID;
        public string hashCode;
        public List<Player> Players = new List<Player>();
        private static MediaPlayer mediaPlayer = new MediaPlayer();
        private double songTime = 0;
        private ScoreSaberClient _scoresaberClient;
        private List<ListViewItem> _items = new List<ListViewItem>();
        private double _speedFactor = 6.5;

        public MainWindow()
        {
            InitializeComponent();
            _scoresaberClient = new ScoreSaberClient();
            Start(407936, 10, country: null, streamMode: false);
        }

        public async void Start(int songID, int playerAmount = 1, string country = null, bool streamMode = false, bool battleRoyal = false)
        {
            if (streamMode)
            {
                SongTimeLabel.Visibility = Visibility.Hidden;
                SpeedLabel.Visibility = Visibility.Hidden;
                TimeLabel.Visibility = Visibility.Hidden;
                TimeLabelLead.Visibility = Visibility.Hidden;

                LeadLabelText.Visibility = Visibility.Hidden;
                SpeedLabelText.Visibility = Visibility.Hidden;
                SubsLabelText.Visibility = Visibility.Hidden;
                TimeLabelText.Visibility = Visibility.Hidden;
                SongNameLabel.Visibility = Visibility.Hidden;
            }

            this.songID = songID;
            LoadingLabel.Content = $"Loading... Getting song info";
            var leaderboardInfo = await _scoresaberClient.Api.Leaderboards.GetLeaderboardInfoByID(songID);
            this.hashCode = leaderboardInfo.SongHash.ToLower();
            var leaderboardScores = new List<Score>();

            //Load leaderboard players 
            for (var i = 0; i <= Math.Round(playerAmount / 10.0); i++) leaderboardScores.AddRange(await _scoresaberClient.Api.Leaderboards.GetLeaderboardScoresByID(songID, country, page: i + 1));

            SongNameLabel.Content = leaderboardInfo.SongName;

            var gridView = ListViewPlayers.View as GridView;
            gridView.Columns.First().Width = 280;

            LoadingLabel.Content = $"Loading... preparing song file";
            await PrepareSongFile(hashCode);
            LoadingLabel.Content = $"Loading... start loading players";

            var playerCount = playerAmount;
            var playersLoaded = 0;
            for (var i = 0; i < playerAmount; i++)
            {
                if (await LoadInPlayer($"{leaderboardScores[i].LeaderboardPlayerInfo.Id}")) playersLoaded++;
                else playerCount--;
                LoadingLabel.Content = $"Loading {playersLoaded}/{playerCount}...";
            }
            LoadingLabel.Visibility = Visibility.Hidden;
            StartPlay();
        }

        public async void StartPlay()
        {
            ListViewPlayers.ItemsSource = _items;

            var width = CanvasSpace.Width;
            var height = CanvasSpace.Height;

            var tasks = new List<Task>();
            foreach (var player in Players)
            {
                var task = new Task(() =>
                {
                    if (player == Players.FirstOrDefault(x => (x.ReplayModel.Frames[100].A - x.ReplayModel.Frames[0].A) < 2)) Play(player, width, height, true);
                    else Play(player, width, height, false);
                });
                task.Start();
                tasks.Add(task);

            }
            PlaySong(1000);
            Task.WaitAll(tasks.ToArray());
        }

        public async void Play(Player player, double width, double height, bool hasLead)
        {
            var zoomx = width / 1.4;
            var zoomy = height / 1.8;
            var offsetHeight = 0 - (height / 1.35);

            //Remove start loading frames 
            player.ReplayModel.Frames.RemoveAll(x => x.A == 0);
            //player.ReplayModel.Frames.RemoveAll(x => x.A < player.ReplayModel.NoteTime.First());

            //await Task.Delay(TimeSpan.FromSeconds(player.ReplayModel.NoteTime.First()));
            await Task.Delay(1000);


            var avgFps = player.ReplayModel.Frames.Average(x => x.I);

            var lowestSpeedFactor = _speedFactor - 0.1;
            var highestSpeedFactor = _speedFactor + 0.1;
            var startSpeedFactor = _speedFactor;

            var storedNoteTimes = new List<double>();
            storedNoteTimes.AddRange(player.ReplayModel.NoteTime.ToArray());

            //Add trails for every player 
            var trailListLeft = new List<Line>();
            var trailListRight = new List<Line>();
            var trailMax = 5;
            for (var i = 0; i < trailMax; i++)
            {
                Dispatcher.Invoke(() =>
                {
                    var left = new Line()
                    {
                        Stroke = player.LeftHand.Stroke,
                        Fill = player.LeftHand.Stroke,
                        StrokeThickness = 5,
                        StrokeStartLineCap = PenLineCap.Flat,
                        StrokeEndLineCap = PenLineCap.Flat,
                        Opacity = 0.3
                    };
                    var right = new Line()
                    {
                        Stroke = player.LeftHand.Stroke,
                        Fill = player.LeftHand.Stroke,
                        StrokeThickness = 5,
                        StrokeStartLineCap = PenLineCap.Flat,
                        StrokeEndLineCap = PenLineCap.Flat,
                        Opacity = 0.3
                    };

                    trailListLeft.Add(left);
                    trailListRight.Add(right);
                    CanvasSpace.Children.Add(left);
                    CanvasSpace.Children.Add(right);
                });
            }

            double positionxoldleft = 0;
            double positionyoldleft = 0;
            double positionxoldright = 0;
            double positionyoldright = 0;

            var trailIndex = 0;
            var count = 0;
            foreach (var frame in player.ReplayModel.Frames)
            {
                //Stop if its the last frame
                if (frame == player.ReplayModel.Frames[player.ReplayModel.Frames.Count() - 2]) break;
                //Skip paused frames 
                if (player.ReplayModel.Frames[player.ReplayModel.Frames.IndexOf(frame) + 1].A - frame.A == 0) continue;

                //Add note if the time has come
                if (player.ReplayModel.NoteTime.Count != 0)
                {
                    var noteTime = player.ReplayModel.NoteTime.First();
                    if (noteTime < frame.A + 0.05)
                    {

                        var note = player.ReplayModel.NoteInfos.First();
                        if (hasLead) AddNote(note);
                        player.ReplayModel.NoteInfos.Remove(note);
                        player.ReplayModel.NoteTime.Remove(noteTime);

                        player.ReplayModel.Combos.Remove(player.ReplayModel.Combos.First());
                    }
                }

                //Add misses
                if (player.ReplayModel.NoteTime.FirstOrDefault() != 0)
                {
                    var noteIndex = storedNoteTimes.IndexOf(player.ReplayModel.NoteTime.First());
                    if (noteIndex > 0)
                    {
                        if (player.ReplayModel.Scores[noteIndex] == -3 || player.ReplayModel.Scores[noteIndex] == -2)
                        {
                            Dispatcher.Invoke(async () => {
                                var item = _items.FirstOrDefault(x => x.Background == player.LeftHand.Stroke);
                                item.BorderBrush = System.Windows.Media.Brushes.Red;
                                item.BorderThickness = new Thickness(2);
                                await Task.Delay(1000);
                                item.BorderBrush = null;
                            });                            
                        }
                    }
                }


                //Force non-leads to keep up with the lead
                bool tooSlow = false;
                bool tooFast = false;
                Dispatcher.Invoke(() =>
                {
                    if (!hasLead && frame.A < Convert.ToDouble(TimeLabelLead.Content) - 0.01) tooSlow = true;
                    if (!hasLead && frame.A > Convert.ToDouble(TimeLabelLead.Content) + 0.01) tooFast = true;
                });
                if (tooSlow) continue;
                if (tooFast) await Task.Delay(10);

                //Get the time to wait till the next frame
                var frameTimeNext = player.ReplayModel.Frames[player.ReplayModel.Frames.IndexOf(frame) + 1].A;
                var frameTimeNow = frame.A;
                var timeTillNextFrame = frameTimeNext - frameTimeNow;
                if (timeTillNextFrame < 0) continue;

                //Force the lead to get as fast as the song that is playing
                if (hasLead)
                {
                    if (frame.A < songTime - 0.01) _speedFactor += (songTime - frame.A) * 0.1;
                    else if (frame.A > songTime + 0.01) _speedFactor -= (frame.A - songTime) * 0.1;
                    else _speedFactor = (lowestSpeedFactor + highestSpeedFactor) / 2;

                    if (_speedFactor > startSpeedFactor + 1) _speedFactor = startSpeedFactor + 1;
                    if (_speedFactor < startSpeedFactor - 1) _speedFactor = startSpeedFactor - 1;

                    if (_speedFactor < lowestSpeedFactor) lowestSpeedFactor = _speedFactor;
                    if (_speedFactor > highestSpeedFactor) highestSpeedFactor = _speedFactor;
                }

                //Show the speedfactor of the lead
                if (hasLead)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SpeedLabel.Content = _speedFactor;
                    });
                }

                //Wait till next frame 
                await Task.Delay(TimeSpan.FromSeconds(timeTillNextFrame) / _speedFactor);

                //Add points to the player and change leaderboard
                if (count % 50 == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (frame.A > storedNoteTimes.First())
                        {
                            var lastNoteTimePassed = storedNoteTimes.Where(x => x < frame.A).Last();
                            var totalScore = player.ReplayModel.Scores.GetRange(0, storedNoteTimes.IndexOf(lastNoteTimePassed)).Sum();
                            var item = _items.FirstOrDefault(x => x.Background == player.LeftHand.Stroke);
                            var name = item.Content.ToString().Split("|")[0].Trim();
                            for (var i = 0; i < 50 - name.Length; i++) name += " ";
                            if (player.ReplayModel.Combos.Count() != 0) item.Content = name + " | " + totalScore + " | " + player.ReplayModel.Combos.First();

                            ListViewPlayers.ItemsSource = _items.OrderByDescending(x => x.Content.ToString().Split("|")[1].Trim());
                            ListViewPlayers.Items.Refresh();
                        }
                    });
                }

                var centerWidth = width / 2;
                var centerHeight = height / 2;

                Dispatcher.Invoke(() =>
                {
                    ////TODO: Create saber tip swings
                    //    var saberLength = 0.2;

                    //    //Left Hand
                    //    var leulerAngle = ToEulerAngles(new Quaternion { x = frame.L.R.X, y = frame.L.R.Y, z = frame.L.R.Z, w = (double)frame.L.R.W });
                    //    //Create pitch degree from eulerAngle
                    //    var lpitchDegree = leulerAngle.pitch * 45;
                    //    //calculate y from pitch 
                    //    var ly = Math.Sin(lpitchDegree) * (saberLength * leulerAngle.pitch);
                    //    //Create yaw degree from eulerAngle
                    //    var lyawDegree = leulerAngle.yaw * 45;
                    //    //calculate x from yaw 
                    //    var lx = Math.Sin(lyawDegree) * (saberLength * leulerAngle.yaw);

                    //    Canvas.SetLeft(player.LeftHandTip, centerWidth + (lx + frame.L.P.X) * zoomx);
                    //    Canvas.SetBottom(player.LeftHandTip, centerHeight + offsetHeight + (ly + frame.L.P.Y) * zoomy);

                    //    //Right Hand
                    //    var reulerAngle = ToEulerAngles(new Quaternion { x = frame.R.R.X, y = frame.R.R.Y, z = frame.R.R.Z, w = (double)frame.R.R.W });

                    //    //Create pitch degree from eulerAngle
                    //    var rpitchDegree = reulerAngle.pitch * 45;
                    //    //calculate y from pitch 
                    //    var ry = Math.Sin(rpitchDegree) * (saberLength * reulerAngle.pitch);
                    //    //Create yaw degree from eulerAngle
                    //    var ryawDegree = reulerAngle.yaw * 45;
                    //    //calculate x from yaw 
                    //    var rx = Math.Sin(ryawDegree) * (saberLength * reulerAngle.yaw);


                    //    Canvas.SetLeft(player.RightHandTip, centerWidth + (rx + frame.R.P.X) * zoomx);
                    //    Canvas.SetBottom(player.RightHandTip, centerHeight + offsetHeight + (ry + frame.R.P.Y) * zoomy);

                    //Set Hand Positions
                    Canvas.SetLeft(player.LeftHand, centerWidth + frame.L.P.X * zoomx);
                    Canvas.SetBottom(player.LeftHand, centerHeight + offsetHeight + (frame.L.P.Y + (1.7 - player.ReplayModel.Info.Height))/*Removes height differences*/ * zoomy);

                    Canvas.SetLeft(player.RightHand, centerWidth + frame.R.P.X * zoomx);
                    Canvas.SetBottom(player.RightHand, centerHeight + offsetHeight + (frame.R.P.Y + (1.7 - player.ReplayModel.Info.Height))/*Removes height differences*/ * zoomy);
                    if (hasLead) TimeLabelLead.Content = frame.A;
                    else TimeLabel.Content = frame.A;


                    //Give positions to each trail
                    if (player.ReplayModel.Frames.IndexOf(frame) > 1)
                    {
                        if (trailIndex == trailMax) trailIndex = 0;
                        DrawTrail(player, positionxoldleft, positionyoldleft, positionxoldright, positionyoldright, trailListLeft, trailListRight, trailIndex);

                        positionxoldleft = Canvas.GetLeft(player.LeftHand);
                        positionyoldleft = Canvas.GetBottom(player.LeftHand);
                        positionxoldright = Canvas.GetLeft(player.RightHand);
                        positionyoldright = Canvas.GetBottom(player.RightHand);

                        trailIndex++;
                    }

                });

                count++;
            }
        }

        public async void DrawTrail(Player player, double positionxoldleft, double positionyoldleft, double positionxoldright, double positionyoldright, List<Line> trailListLeft, List<Line> trailListRight, int trailIndex)
        {
            trailListLeft[trailIndex].X1 = positionxoldleft + player.LeftHand.Width / 2;
            trailListLeft[trailIndex].Y1 = CanvasSpace.Height - player.LeftHand.Height / 2 - positionyoldleft;
            trailListLeft[trailIndex].X2 = Canvas.GetLeft(player.LeftHand) + player.LeftHand.Width / 2;
            trailListLeft[trailIndex].Y2 = CanvasSpace.Height - player.LeftHand.Height / 2 - Canvas.GetBottom(player.LeftHand);

            trailListRight[trailIndex].X1 = positionxoldright + player.RightHand.Width / 2;
            trailListRight[trailIndex].Y1 = CanvasSpace.Height - player.RightHand.Height / 2 - positionyoldright;
            trailListRight[trailIndex].X2 = Canvas.GetLeft(player.RightHand) + player.RightHand.Width / 2;
            trailListRight[trailIndex].Y2 = CanvasSpace.Height - player.RightHand.Height / 2 - Canvas.GetBottom(player.RightHand);
        }

        public async Task<bool> LoadInPlayer(string playerID)
        {
            var playerInfo = await _scoresaberClient.Api.Players.GetPlayer(Convert.ToInt64(playerID));

            var replayModel = await GetReplayModel($"https://sspreviewdecode.azurewebsites.net/?playerID={playerID}&songID={songID}");
            if (replayModel == null) return false;
            if (replayModel.Frames == null || replayModel.Info.LeftHanded == true) return false;
            var rand = new Random();
            var stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)rand.Next(100, 256), (byte)rand.Next(100, 256), (byte)rand.Next(100, 256)));
            var leftHand = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 15, Height = 15 };
            var rightHand = new Ellipse() { Stroke = stroke, Width = 15, Height = 15 };
            var leftHandTip = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 8, Height = 8 };
            var rightHandTip = new Ellipse() { Stroke = stroke, Width = 8, Height = 8 };
            CanvasSpace.Children.Add(leftHand);
            CanvasSpace.Children.Add(rightHand);
            //CanvasSpace.Children.Add(leftHandTip);
            //CanvasSpace.Children.Add(rightHandTip);

            //var avgFps = replayModel.Frames.Average(x => x.I);
            //var staticFrameCount = replayModel.Frames.Count;
            //do
            //{
            //    replayModel.Frames.RemoveAt(rand.Next(0, replayModel.Frames.Count));
            //} while (staticFrameCount / avgFps * 50 < replayModel.Frames.Count);

            var player = new Player() { ID = playerID, LeftHand = leftHand, RightHand = rightHand, LeftHandTip = leftHandTip, RightHandTip = rightHandTip, ReplayModel = replayModel };
            var listViewItem = new ListViewItem() { Content = playerInfo.Name + $" | 0", Background = player.LeftHand.Stroke };
            Players.Add(player);
            _items.Add(listViewItem);
            return true;
        }

        public async void AddNote(string noteInfo)
        {
            var x = Convert.ToInt32(noteInfo.Substring(0, 1));
            var y = Convert.ToInt32(noteInfo.Substring(1, 1));
            var direction = Convert.ToInt32(noteInfo.Substring(2, 1));
            var type = Convert.ToInt32(noteInfo.Substring(3, 1));

            Dispatcher.Invoke(async () =>
            {
                var rec = new System.Windows.Shapes.Rectangle() { Stroke = System.Windows.Media.Brushes.White, Width = 70, Height = 70, Fill = type == 0 ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.White, Opacity = 0.2 };
                var recDirection = new System.Windows.Shapes.Rectangle() { Stroke = System.Windows.Media.Brushes.White, Width = 70, Height = 14, Fill = System.Windows.Media.Brushes.Black, Opacity = 1 };
                CanvasSpace.Children.Add(rec);
                CanvasSpace.Children.Add(recDirection);
                var posx = CanvasSpace.Width / 4 * x + 110;
                var posy = CanvasSpace.Height / 3 * y + 100;
                var posxdir = posx + 15;
                var posydir = posy + 65;
                Canvas.SetLeft(rec, posx);
                Canvas.SetBottom(rec, posy);
                Canvas.SetLeft(recDirection, posxdir);
                Canvas.SetBottom(recDirection, posydir);


                if (direction == 1) recDirection.RenderTransform = new RotateTransform(0, -30, 0); //down 
                if (direction == 2) recDirection.RenderTransform = new RotateTransform(90, 25, 30); //left 
                if (direction == 0) recDirection.RenderTransform = new RotateTransform(180, 30, 30);//up
                if (direction == 3) recDirection.RenderTransform = new RotateTransform(270, 30, 30);//right

                if (direction == 4)//upleft
                {
                    rec.RenderTransform = new RotateTransform(315, 0, 0);
                    recDirection.RenderTransform = new RotateTransform(315, 45, -40);
                }
                if (direction == 5)//upright
                {
                    rec.RenderTransform = new RotateTransform(45, 0, 0);
                    recDirection.RenderTransform = new RotateTransform(45, -60, -40);
                }
                if (direction == 6)//downleft
                {
                    rec.RenderTransform = new RotateTransform(45, 0, 0);
                    recDirection.RenderTransform = new RotateTransform(45, -35, -35);
                }
                if (direction == 7)//downright
                {
                    rec.RenderTransform = new RotateTransform(315, 0, 0);
                    recDirection.RenderTransform = new RotateTransform(315, -15, -15);
                }



                for (var i = 0; i < 3; i++)
                {
                    await Task.Delay(50 / 3);
                    rec.Width += 10;
                    rec.Height += 10;
                    Canvas.SetLeft(rec, posx - 5 * i);
                    Canvas.SetBottom(rec, posy - 5 * i);
                    Canvas.SetLeft(recDirection, posxdir - 5 * i);
                    Canvas.SetBottom(recDirection, posydir - 5 * i);
                }

                CanvasSpace.Children.Remove(rec);
                CanvasSpace.Children.Remove(recDirection);
            });
        }

        public class Player
        {
            public string ID { get; set; }
            public Ellipse LeftHand { get; set; }

            public Ellipse RightHand { get; set; }
            public Ellipse LeftHandTip { get; set; }
            public Ellipse RightHandTip { get; set; }

            public ReplayModel ReplayModel { get; set; }
        }

        public async Task<ReplayModel> GetReplayModel(string url)
        {
            //Get content from website as string

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var f = (string)JsonConvert.DeserializeObject(content);
                    var jsonObject = JsonConvert.DeserializeObject<ReplayModel>(f);
                    return jsonObject;
                }
            }
            return null;
        }

        public async Task PrepareSongFile(string hash)
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



            using (ZipArchive archive = ZipFile.OpenRead(AppContext.BaseDirectory + $@"Audio/ZipTemp/Download/{hash}.zip"))
            {

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.Contains(".egg"))
                    {
                        if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.egg")) File.Delete(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.egg");
                        entry.ExtractToFile(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.egg");
                    }
                    if (entry.FullName.Contains(".jpg") || entry.FullName.Contains(".png") && !File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.jpg"))
                    {
                        if (entry.FullName.Contains("cover"))
                        {
                            if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.jpg")) File.Delete(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.jpg");
                            entry.ExtractToFile(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.jpg");
                        }
                    }
                }
            }

            FFMpegCore.GlobalFFOptions.Configure(new FFOptions { BinaryFolder = AppContext.BaseDirectory + @"Audio/ffmpeg" });
            FFMpegCore.FFMpegArguments.FromFileInput(AppContext.BaseDirectory + $@"Audio\ZipTemp/Extract/{songID}.egg")
                .OutputToFile(AppContext.BaseDirectory + $@"Audio\{songID}.mp3", true, options =>
                options.WithAudioCodec(audioCodec: AudioCodec.LibMp3Lame))
                .ProcessSynchronously();

            if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.jpg"))
            {
                ImageBrush ib = new ImageBrush();
                ib.ImageSource = BitmapToImageSource(GrayScaleFilter(new Bitmap(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.jpg")));
                ib.Opacity = 0.04;
                CanvasSpace.Background = ib;
            }
            else
            {
                CanvasSpace.Background = System.Windows.Media.Brushes.Black;
            }


            await Task.Delay(2000);
            File.Delete(AppContext.BaseDirectory + $@"Audio\ZipTemp/Extract/{songID}.egg");
        }

        private BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap)
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

        public Bitmap GrayScaleFilter(Bitmap image)
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

        public async Task PlaySong(int delay)
        {
            mediaPlayer.Open(new Uri(AppContext.BaseDirectory + $@"Audio\{songID}.mp3"));
            await Task.Delay(delay);
            mediaPlayer.Play();
            var sw = new Stopwatch();
            sw.Start();

            while (true)
            {
                await Task.Delay(100);
                songTime = sw.ElapsedMilliseconds / 1000.000;
                Dispatcher.Invoke(() => { SongTimeLabel.Content = songTime; });
            }
        }


        public static EulerAngles ToEulerAngles(Quaternion q)
        {
            EulerAngles angles = new();

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.w * q.x + q.y * q.z);
            double cosr_cosp = 1 - 2 * (q.x * q.x + q.y * q.y);
            angles.roll = Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = 2 * (q.w * q.y - q.z * q.x);
            if (Math.Abs(sinp) >= 1)
            {
                angles.pitch = Math.CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.pitch = Math.Asin(sinp);
            }

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (q.w * q.z + q.x * q.y);
            double cosy_cosp = 1 - 2 * (q.y * q.y + q.z * q.z);
            angles.yaw = Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }

        public class EulerAngles
        {
            public double roll; // x
            public double pitch; // y
            public double yaw; // z
        }

        public class Quaternion
        {
            public double w;
            public double x;
            public double y;
            public double z;
        }
    }
}
