using FFMpegCore;
using FFMpegCore.Enums;
using Newtonsoft.Json;
using ReplayBattleRoyal.Managers;
using ScoreSaberLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static ScoreSaberLib.Models.LeaderboardInfoModel;
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
        public List<ListViewItem> listViewItems = new List<ListViewItem>();
        private Leaderboard leaderboardInfo;
        private Random random = new Random();
        private EffectsPanel effectsPanel;
        private MapDetailsModel mapDetails;
        private double _speedFactor = 12.0;

        public bool streamMode = false;
        public GameModes gameMode = GameModes.None;

        public System.Windows.Media.Brush NoteColorLeft { get; set; }
        public System.Windows.Media.Brush NoteColorRight { get; set; }
        public System.Windows.Media.Brush NoteColorLeftArrow { get; set; }
        public System.Windows.Media.Brush NoteColorRightArrow { get; set; }

        public enum GameModes
        {
            None,
            BattleRoyale,
        }

        public MainWindow()
        {
            InitializeComponent();

            _scoresaberClient = new ScoreSaberClient();
            //387215 7.5
            Start(301228, 25, country: null, streamMode: false, GameModes.BattleRoyale);
        }

        public async Task Start(int songID, int playerAmount = 1, string country = null, bool streamMode = false, GameModes gameMode = GameModes.None)
        {
            this.streamMode = streamMode;
            this.gameMode = gameMode;

            NoteColorLeft = System.Windows.Media.Brushes.Red;
            NoteColorRight = System.Windows.Media.Brushes.Blue;
            NoteColorLeftArrow = System.Windows.Media.Brushes.White;
            NoteColorRightArrow = System.Windows.Media.Brushes.White;

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

            if (gameMode == GameModes.BattleRoyale)
            {
                BatteRoyalTimerLabel.Visibility = Visibility.Visible;
            }
            else
            {
                BatteRoyalTimerLabel.Visibility = Visibility.Hidden;
            }

            this.songID = songID;
            LoadingLabel.Content = $"Loading... Getting song info";
            leaderboardInfo = await _scoresaberClient.Api.Leaderboards.GetLeaderboardInfoByID(songID);

            this.hashCode = leaderboardInfo.SongHash.ToLower();
            var leaderboardScores = new List<Score>();

            //Load leaderboard players 
            for (var i = 0; i <= Math.Round(playerAmount / 10.0); i++) leaderboardScores.AddRange(await _scoresaberClient.Api.Leaderboards.GetLeaderboardScoresByID(songID, country, page: i + 1));

            SongNameLabel.Content = leaderboardInfo.SongName;

            var gridView = ListViewPlayers.View as GridView;
            gridView.Columns.First().Width = 550;

            LoadingLabel.Content = $"Loading... preparing song file";
            await PrepareSongFile(hashCode);

            //Load in all players 
            LoadingLabel.Content = $"Loading... start loading players";

            var playerCount = playerAmount;
            var playersLoaded = 0;

            //for (var i = 0; i < playerAmount; i++)
            //{
            //    if (await LoadInPlayer($"{leaderboardScores[i].LeaderboardPlayerInfo.Id}")) playersLoaded++;
            //    else playerCount--;
            //    LoadingLabel.Content = $"Loading {playersLoaded}/{playerCount}...";
            //}

            //Parallel loading
            var tasks = new List<Task>();
            for (var i = 0; i < playerAmount; i++)
            {               
                tasks.Add(Task.Run(async () =>
                {
                    var isFinishedCorrectly = await LoadInPlayer($"{leaderboardScores[i].LeaderboardPlayerInfo.Id}");
                    if (isFinishedCorrectly) playersLoaded++;
                    else playerCount--;

                    Dispatcher.Invoke(() => { LoadingLabel.Content = $"Loading {playersLoaded}/{playerCount}..."; });
                }));
                await Task.Delay(300);
            }
            await Task.WhenAll(tasks);

            //Count down if stream mode is activated
            if (streamMode)
            {
                for (var i = 10; i > 0; i--)
                {
                    await Task.Delay(1000);
                    LoadingLabel.Content = $"Starting play in {i}";
                }
            }

            //Open effects panel
            effectsPanel = new EffectsPanel(this, Players);
            effectsPanel.Show();

            //Show intro
            var textList = new string[] {
                $"This video shows {Players.Count} Beat Saber plays at once",
                $"Every couple seconds a player is eliminated",
                $"This video contains rapid flashes"
            };

            if (streamMode) await MediaManager.ShowIntro(this, textList);

            LoadingLabel.Visibility = Visibility.Hidden;
            StartPlay();
        }

        public async void StartPlay()
        {
            ListViewPlayers.ItemsSource = listViewItems;

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
            if (gameMode == GameModes.BattleRoyale) StartEliminatingPlayers(Players.Count, Convert.ToInt32(Math.Round(Players.First().ReplayModel.Frames.Last().A)));
            Task.WaitAll(tasks.ToArray());
        }

        public async Task StartEliminatingPlayers(int playerAmount, int songDuration)
        {
            var startAmount = playerAmount;
            do
            {
                var timeToWait = (songDuration / startAmount);
                Dispatcher.Invoke(() => { BatteRoyalTimerLabel.Content = timeToWait; });
                for (var i = 0; i < timeToWait; i++)
                {
                    await Task.Delay(1000);
                    Dispatcher.Invoke(() => { BatteRoyalTimerLabel.Content = Convert.ToInt32(BatteRoyalTimerLabel.Content) - 1; });
                }
                EliminateLastPlayer();

                playerAmount--;
            } while (playerAmount > 1);
        }

        public void EliminateLastPlayer()
        {
            Dispatcher.Invoke(() =>
            {
                var toRemove = listViewItems.OrderByDescending(x => x.Content.ToString().Split(" ")[0].Trim()).Last();
                listViewItems.Remove(toRemove);
                var playerToRemove = Players.FirstOrDefault(x => toRemove.Content.ToString().Contains(x.Name));

                if (!playerToRemove.hasLead)
                {
                    Players.Remove(playerToRemove);
                    CanvasSpace.Children.Remove(playerToRemove.LeftHand);
                    CanvasSpace.Children.Remove(playerToRemove.RightHand);
                    CanvasSpace.Children.Remove(playerToRemove.LeftHandTip);
                    CanvasSpace.Children.Remove(playerToRemove.RightHandTip);
                    foreach (var trail in playerToRemove.TrailListLeft) CanvasSpace.Children.Remove(trail);
                    foreach (var trail in playerToRemove.TrailListRight) CanvasSpace.Children.Remove(trail);
                } //If player has lead, keep it playing but hidden
                else
                {
                    CanvasSpace.Children.Remove(playerToRemove.LeftHand);
                    CanvasSpace.Children.Remove(playerToRemove.RightHand);
                    CanvasSpace.Children.Remove(playerToRemove.LeftHandTip);
                    CanvasSpace.Children.Remove(playerToRemove.RightHandTip);
                    foreach (var trail in playerToRemove.TrailListLeft) CanvasSpace.Children.Remove(trail);
                    foreach (var trail in playerToRemove.TrailListRight) CanvasSpace.Children.Remove(trail);
                }
                ListViewPlayers.Items.Refresh();
            });
        }

        public async void Play(Player player, double width, double height, bool hasLead)
        {
            player.hasLead = hasLead;
            var zoomx = width / 2.4;
            var zoomy = height / 4.8;
            var offsetHeight = 0 - (height / 4.35);

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
            var storedCombo = new List<long>();
            storedCombo.AddRange(player.ReplayModel.Combos.ToArray());
            var storedScores = new List<long>();
            storedScores.AddRange(player.ReplayModel.Scores.ToArray());
            var storedLightshowData = new List<Event>();
            storedLightshowData.AddRange(mapDetails.Events.ToArray());

            //Add trails for every player 


            double positionxoldleft = 0;
            double positionyoldleft = 0;
            double positionxoldright = 0;
            double positionyoldright = 0;

            double currentScore = 0;
            double currentMaxScore = 0;
            bool shouldCalculateBeginCombo = true;
            int comboMultiplier = 1;
            int notesTillNextMultiplier = 2;

            var trailIndex = 0;

            var count = 0;
            foreach (var frame in player.ReplayModel.Frames)
            {
                //Stop if player has been eliminated by battleRoyalMode
                if (!Players.Contains(player)) break;
                //Stop if its the last frame
                if (frame == player.ReplayModel.Frames[player.ReplayModel.Frames.Count() - 2]) break;
                //Skip paused frames 
                if (player.ReplayModel.Frames[player.ReplayModel.Frames.IndexOf(frame) + 1].A - frame.A == 0) continue;

                //Add note if the time has come
                if (player.ReplayModel.NoteTime.Count != 0)
                {
                    var noteTime = player.ReplayModel.NoteTime.First();
                    if (noteTime < frame.A + 0.005)
                    {
                        ////Check if a lightnign event happens
                        //var lightdata = storedLightshowData.First();
                        //if (lightdata.Time < frame.A)
                        //{
                        //    storedLightshowData.Remove(lightdata);
                        //}

                        //Calculate current score and max Score 
                        var combo = storedCombo.First();
                        if (storedScores.First() > 0)
                        {
                            if (combo <= 1)
                            {
                                if (comboMultiplier == 1)
                                {
                                    comboMultiplier = 1;
                                    notesTillNextMultiplier = 2;
                                }
                                else if (comboMultiplier == 2)
                                {
                                    comboMultiplier = 1;
                                    notesTillNextMultiplier = 2;
                                }
                                else if (comboMultiplier == 4)
                                {
                                    comboMultiplier = 2;
                                    notesTillNextMultiplier = 4;
                                }
                                else if (comboMultiplier == 8)
                                {
                                    comboMultiplier = 4;
                                    notesTillNextMultiplier = 8;
                                }
                            }
                            else
                            {
                                if (notesTillNextMultiplier > 0) notesTillNextMultiplier--;
                            }

                            if (notesTillNextMultiplier == 0)
                            {
                                if (comboMultiplier == 1)
                                {
                                    comboMultiplier = 2;
                                    notesTillNextMultiplier = 4;
                                }
                                else if (comboMultiplier == 2)
                                {
                                    comboMultiplier = 4;
                                    notesTillNextMultiplier = 8;
                                }
                                else if (comboMultiplier == 4)
                                {
                                    comboMultiplier = 8;
                                }
                            }

                            currentScore += comboMultiplier * storedScores.First();


                            //Calculate max score
                            if (combo < 2)
                            {
                                if (shouldCalculateBeginCombo) currentMaxScore += 1 * 115;
                                else currentMaxScore += 8 * 115;
                            }
                            else if (combo < 6)
                            {
                                if (shouldCalculateBeginCombo) currentMaxScore += 2 * 115;
                                else currentMaxScore += 8 * 115;
                            }
                            else if (combo < 14)
                            {
                                if (shouldCalculateBeginCombo) currentMaxScore += 4 * 115;
                                else currentMaxScore += 8 * 115;
                            }
                            else if (combo >= 14)
                            {
                                currentMaxScore += 8 * 115;
                                shouldCalculateBeginCombo = false;
                            }
                        }
                        storedCombo.Remove(storedCombo.First());
                        storedScores.Remove(storedScores.First());

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
                            Dispatcher.Invoke(async () =>
                            {
                                var item = listViewItems.FirstOrDefault(x => x.Content.ToString().Contains(player.Name));
                                item.BorderBrush = System.Windows.Media.Brushes.Red;
                                item.BorderThickness = new Thickness(5);
                                await Task.Delay(1000);
                                item.BorderBrush = player.LeftHand.Stroke;
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

                    if (frame.A > storedNoteTimes.First())
                    {
                        var lastNoteTimePassed = storedNoteTimes.Where(x => x < frame.A).Last();
                        var index = storedNoteTimes.IndexOf(lastNoteTimePassed);

                        Dispatcher.Invoke(() =>
                        {
                            var item = listViewItems.FirstOrDefault(x => x.Content.ToString().Contains(player.Name));
                            if (item != null && player.ReplayModel.NoteTime.Count() != 0)
                            {
                                float acc = (float)Math.Round((currentScore * 100) / currentMaxScore, 2);
                                var combo = player.ReplayModel.Combos.First();
                                var score = $"{acc}% {combo}";
                                var spacesa = "";
                                var spacesc = "";
                                for (var i = 0; i < 5 % acc.ToString().Length; i++) spacesa += "  ";
                                for (var i = 0; i < 9 % combo.ToString().Length; i++) spacesc += "  ";
                                if (player.ReplayModel.Combos.Count() != 0) item.Content = $"{acc}{spacesa}%    {combo}{spacesc}            {player.Name}";
                                ListViewPlayers.ItemsSource = listViewItems.OrderByDescending(x => x.Content.ToString().Split(" ")[0].Trim());
                                ListViewPlayers.Items.Refresh();
                            }
                        });
                    }

                }

                var centerWidth = width / 2;
                var centerHeight = height / 2;

                Dispatcher.Invoke(() =>
                {

                    //Set sabertip positions
                    var tipRight = SaberTipCalculator.RotateSaber(new SaberTipCalculator.Point { x = frame.R.P.X * 2, y = frame.R.P.Y * 2, z = frame.R.P.Z * 2 }, 2.3, new SaberTipCalculator.Quaternion { x = frame.R.R.X, y = frame.R.R.Y, z = frame.R.R.Z, w = (double)frame.R.R.W });
                    var tipLeft = SaberTipCalculator.RotateSaber(new SaberTipCalculator.Point { x = frame.L.P.X * 2, y = frame.L.P.Y * 2, z = frame.L.P.Z * 2 }, 2.3, new SaberTipCalculator.Quaternion { x = frame.L.R.X, y = frame.L.R.Y, z = frame.L.R.Z, w = (double)frame.L.R.W });

                    Canvas.SetLeft(player.RightHandTip, tipRight.x * 225 + 625);
                    Canvas.SetBottom(player.RightHandTip, tipRight.y * 225 - 100);

                    Canvas.SetLeft(player.LeftHandTip, tipLeft.x * 225 + 600);
                    Canvas.SetBottom(player.LeftHandTip, tipLeft.y * 225 - 100);



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
                        if (trailIndex == player.TrailListLeft.Count) trailIndex = 0;

                        DrawTrail(player, positionxoldleft, positionyoldleft, positionxoldright, positionyoldright, player.TrailListLeft, player.TrailListRight, trailIndex);

                        positionxoldleft = Canvas.GetLeft(player.LeftHandTip);
                        positionyoldleft = Canvas.GetBottom(player.LeftHandTip);
                        positionxoldright = Canvas.GetLeft(player.RightHandTip);
                        positionyoldright = Canvas.GetBottom(player.RightHandTip);

                        trailIndex++;
                    }

                });

                count++;
            }

            if (hasLead) await Dispatcher.Invoke(async () => { await MediaManager.ShowOutro(this); });
        }

        public async void DrawTrail(Player player, double positionxoldleft, double positionyoldleft, double positionxoldright, double positionyoldright, List<Line> trailListLeft, List<Line> trailListRight, int trailIndex)
        {
            trailListLeft[trailIndex].X1 = positionxoldleft + player.LeftHandTip.Width / 2;
            trailListLeft[trailIndex].Y1 = CanvasSpace.Height - player.LeftHandTip.Height / 2 - positionyoldleft;
            trailListLeft[trailIndex].X2 = Canvas.GetLeft(player.LeftHandTip) + player.LeftHandTip.Width / 2;
            trailListLeft[trailIndex].Y2 = CanvasSpace.Height - player.LeftHandTip.Height / 2 - Canvas.GetBottom(player.LeftHandTip);

            trailListRight[trailIndex].X1 = positionxoldright + player.RightHandTip.Width / 2;
            trailListRight[trailIndex].Y1 = CanvasSpace.Height - player.RightHandTip.Height / 2 - positionyoldright;
            trailListRight[trailIndex].X2 = Canvas.GetLeft(player.RightHandTip) + player.RightHandTip.Width / 2;
            trailListRight[trailIndex].Y2 = CanvasSpace.Height - player.RightHandTip.Height / 2 - Canvas.GetBottom(player.RightHandTip);
        }

        public async Task<bool> LoadInPlayer(string playerID)
        {
            var playerInfo = await _scoresaberClient.Api.Players.GetPlayer(Convert.ToInt64(playerID));
            var replayModel = await GetReplayModel($"https://sspreviewdecode.azurewebsites.net/?playerID={playerID}&songID={songID}");

            if (replayModel == null || playerInfo == null) return false;
            if (replayModel.Frames == null || replayModel.Info.LeftHanded == true) return false; //TODO: include left hand mode

            await Dispatcher.Invoke(async () =>
            {

                var color = ColorManager.ColorFromHSV(random.Next(0, 360), random.Next(75, 100) / 100.00, 1);
                var stroke = new SolidColorBrush(color);
                var leftHand = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 25, Height = 25 };
                var rightHand = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 25, Height = 25 };
                var leftHandTip = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 15, Height = 15 };
                var rightHandTip = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 15, Height = 15 };
                CanvasSpace.Children.Add(leftHand);
                CanvasSpace.Children.Add(rightHand);
                CanvasSpace.Children.Add(leftHandTip);
                CanvasSpace.Children.Add(rightHandTip);

                var player = new Player() { ID = playerID, LeftHand = leftHand, RightHand = rightHand, LeftHandTip = leftHandTip, RightHandTip = rightHandTip, ReplayModel = replayModel, Name = playerInfo.Name, color = color };
                var listViewItem = new ListViewItem() { Content = $"0 0           {playerInfo.Name}", Background = player.LeftHand.Stroke, FontSize = 30, FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei UI") };

                player.TrailListLeft = new List<Line>();
                player.TrailListRight = new List<Line>();
                var trailMax = 6;
                for (var i = 0; i < trailMax; i++)
                {

                    var left = new Line()
                    {
                        Stroke = player.LeftHand.Stroke,
                        Fill = player.LeftHand.Stroke,
                        StrokeThickness = 10,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Opacity = 1
                    };
                    var right = new Line()
                    {
                        Stroke = player.LeftHand.Stroke,
                        Fill = player.LeftHand.Stroke,
                        StrokeThickness = 10,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Opacity = 1
                    };

                    player.TrailListLeft.Add(left);
                    player.TrailListRight.Add(right);
                    CanvasSpace.Children.Add(left);
                    CanvasSpace.Children.Add(right);

                }

                Players.Add(player);
                listViewItems.Add(listViewItem);
            });

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
                var rec = new System.Windows.Shapes.Rectangle() { Stroke = System.Windows.Media.Brushes.White, Width = 110, Height = 110, Fill = type == 0 ? NoteColorLeft : NoteColorRight, Opacity = 1, RadiusX = 10, RadiusY = 10 };

                var converter = TypeDescriptor.GetConverter(typeof(Geometry));
                string pathData = "M50,50 L150,90 L250,50 L250,30 L50,30 Z";
                var arrow = new System.Windows.Shapes.Path() { Data = (Geometry)converter.ConvertFrom(pathData), Fill = type == 0 ? NoteColorLeftArrow : NoteColorRightArrow, Stroke = System.Windows.Media.Brushes.White, StrokeThickness = 2, Width = 100, Height = 30, Stretch = Stretch.Fill };


                CanvasSpace.Children.Add(rec);
                CanvasSpace.Children.Add(arrow);
                var posx = CanvasSpace.Width / 4 * x + 110;
                var posy = CanvasSpace.Height / 3 * y + 100;
                var posxdir = posx + 15;
                var posydir = posy + 65;
                Canvas.SetLeft(rec, posx);
                Canvas.SetBottom(rec, posy);
                Canvas.SetLeft(arrow, posxdir);
                Canvas.SetBottom(arrow, posydir);


                if (direction == 0) arrow.RenderTransform = new RotateTransform(180, 48, 35);//up
                if (direction == 1) arrow.RenderTransform = new RotateTransform(0, -45, -20); //down 
                if (direction == 2) arrow.RenderTransform = new RotateTransform(90, 50, 28); //left 
                if (direction == 3) arrow.RenderTransform = new RotateTransform(270, 48, 33);//right

                if (direction == 4)//upleft
                {
                    rec.RenderTransform = new RotateTransform(315, 0, 0);
                    arrow.RenderTransform = new RotateTransform(135, 78, 8);
                }
                if (direction == 5)//upright
                {
                    rec.RenderTransform = new RotateTransform(45, 0, 0);
                    arrow.RenderTransform = new RotateTransform(225, 22.5, 60);
                }
                if (direction == 6)//downleft
                {
                    rec.RenderTransform = new RotateTransform(45, 0, 0);
                    arrow.RenderTransform = new RotateTransform(45, -5, -25);
                }
                if (direction == 7)//downright
                {
                    rec.RenderTransform = new RotateTransform(315, 0, 0);
                    arrow.RenderTransform = new RotateTransform(315, -35, -30);
                }



                for (var i = 0; i < 3; i++)
                {
                    await Task.Delay(50 / 3);
                    rec.Width += 10;
                    rec.Height += 10;
                    Canvas.SetLeft(rec, posx - 5 * i);
                    Canvas.SetBottom(rec, posy - 5 * i);
                    Canvas.SetLeft(arrow, posxdir - 5 * i);
                    Canvas.SetBottom(arrow, posydir - 5 * i);
                }

                CanvasSpace.Children.Remove(rec);
                CanvasSpace.Children.Remove(arrow);
            });
        }


        public class Player
        {
            public string ID { get; set; }
            public string Name { get; set; }

            public System.Windows.Media.Color color { get; set; }

            public bool hasLead { get; set; }
            public Ellipse LeftHand { get; set; }

            public Ellipse RightHand { get; set; }
            public Ellipse LeftHandTip { get; set; }
            public Ellipse RightHandTip { get; set; }

            public ReplayModel ReplayModel { get; set; }

            public List<Line> TrailListLeft { get; set; }
            public List<Line> TrailListRight { get; set; }
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

        public MapDetailsModel GetMapDetailsModel()
        {
            var json = File.ReadAllText(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{ leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "")}.dat");
            var mapDetails = JsonConvert.DeserializeObject<MapDetailsModel>(json);
            return mapDetails;
        }

        public async Task PrepareSongFile(string hash)
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
                    if (entry.FullName.Contains($"{leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "")}.dat"))
                    {
                        if (File.Exists(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "")}.dat")) File.Delete(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "")}.dat");
                        entry.ExtractToFile(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{leaderboardInfo.Difficulty.DifficultyRaw.Replace("_", "").Replace("Solo", "")}.dat");
                    }
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
                ib.ImageSource = MediaManager.BitmapToImageSource(MediaManager.GrayScaleFilter(new Bitmap(AppContext.BaseDirectory + $@"Audio/ZipTemp/Extract/{songID}.jpg")));
                ib.Opacity = 0.04;
                CanvasSpace.Background = ib;
            }
            else
            {
                CanvasSpace.Background = System.Windows.Media.Brushes.Black;
            }

            mapDetails = GetMapDetailsModel();


            await Task.Delay(2000);
            File.Delete(AppContext.BaseDirectory + $@"Audio\ZipTemp/Extract/{songID}.egg");
        }

        public async Task PlaySong(int delay)
        {
            mediaPlayer.Open(new Uri(AppContext.BaseDirectory + $@"Audio\{songID}.mp3"));
            await Task.Delay(delay);
            mediaPlayer.Play();

            //var noteTimings = new List<double>();
            //foreach (var time in Players.First().ReplayModel.NoteTime) noteTimings.Add(time);
            //AudioManager.PlayHitSounds(noteTimings);

            var sw = new Stopwatch();
            sw.Start();

            while (true)
            {
                await Task.Delay(100);
                songTime = sw.ElapsedMilliseconds / 1000.000;
                Dispatcher.Invoke(() => { SongTimeLabel.Content = songTime; });
            }
        }

    }
}
