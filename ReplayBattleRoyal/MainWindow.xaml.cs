using ReplayBattleRoyal.Entities;
using ReplayBattleRoyal.GameModes;
using ReplayBattleRoyal.Managers;
using ScoreSaberLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static ScoreSaberLib.Models.LeaderboardScoresModel;
using Leaderboard = ReplayBattleRoyal.Entities.Leaderboard;

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
        public double songTime = 0;
        public ScoreSaberClient ScoresaberClient;
        public Entities.Leaderboard leaderboard;
        public ScoreSaberLib.Models.LeaderboardInfoModel.Leaderboard leaderboardInfo;
        private Random random = new Random();
        private EffectsPanel effectsPanel;
        private int backgroundVideoDelay;
        public PlayInstance playInstance;

        private int playerStartAmount;

        //10.9
        private double _speedFactor = 11;
        private double _startSpeedFactor;

        public bool streamMode = false;
        public Gamemode gameMode;

        //Effects
        public bool lightshowIsActivated = false;
        public int perfectAccAmount = 100;

        public MainWindow()
        {
            InitializeComponent();

            ScoresaberClient = new ScoreSaberClient();

            Start(408140, 15, country: null, streamMode: false, Gamemode.GameModes.None, useBackgroundVideo: true, backgrounVideoDelay: -2375);
        }

        public async Task Start(int songID, int playerAmount = 1, string country = null, bool streamMode = false, Gamemode.GameModes selectedGameMode = Gamemode.GameModes.None, bool useBackgroundVideo = false, int backgrounVideoDelay = 0)
        {
            this.streamMode = streamMode;
            gameMode = new Gamemode(this, selectedGameMode);
            leaderboard = new Leaderboard(this);
            playInstance = new PlayInstance(this);
            _startSpeedFactor = _speedFactor;           

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
                LeadNameLabelText.Visibility = Visibility.Hidden;
            }
            else
            {
                TransitionStartScreen.Visibility = Visibility.Hidden;
            }

            gameMode.InitializeGamemode();



            this.backgroundVideoDelay = backgrounVideoDelay;
            this.songID = songID;
            LoadingLabel.Content = $"Loading... Getting song info";
            leaderboardInfo = await ScoresaberClient.Api.Leaderboards.GetLeaderboardInfoByID(songID);

            this.hashCode = leaderboardInfo.SongHash.ToLower();
            var leaderboardScores = new List<Score>();

            //Load leaderboard players 
            for (var i = 0; i <= Math.Round(playerAmount / 10.0); i++) leaderboardScores.AddRange(await ScoresaberClient.Api.Leaderboards.GetLeaderboardScoresByID(songID, country, page: i + 1));

            SongNameLabel.Content = leaderboardInfo.SongName;

            var gridView = ListViewPlayers.View as GridView;
            gridView.Columns.First().Width = 550;

            LoadingLabel.Content = $"Loading... preparing song file";
            await playInstance.PrepareSongFile(hashCode, useBackgroundVideo);

            //Load in all players 
            LoadingLabel.Content = $"Loading... start loading players";

            var playerCount = playerAmount;
            var playersLoaded = 0;

            //Parallel loading
            var secureLead = 3;
            var tasks = new List<Task>();
            for (var i = 0; i < playerAmount; i++)
            {
                var color = ColorManager.ColorFromHSV(random.Next(150, 400), random.Next(90, 100) / 100.00, 1);

                if (i < secureLead)
                {
                    if (!await LoadInPlayer($"{leaderboardScores[i].LeaderboardPlayerInfo.Id}", color)) secureLead++;
                }
                else
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var isFinishedCorrectly = await LoadInPlayer($"{leaderboardScores[i].LeaderboardPlayerInfo.Id}", color);
                        if (isFinishedCorrectly) playersLoaded++;
                        else playerCount--;

                        Dispatcher.Invoke(() => { LoadingLabel.Content = $"Loading {playersLoaded}/{playerCount}..."; });
                    }));
                }
                await Task.Delay(4000);
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
                TransitionStartScreen.Visibility = Visibility.Visible;
            }

            //Open effects panel
            effectsPanel = new EffectsPanel(this, Players);
            effectsPanel.Show();

            //Show intro
            if (streamMode) await MediaManager.ShowIntro(this, gameMode.GetIntroText());
            LoadingLabel.Visibility = Visibility.Hidden;
            StartPlay();
        }

        public async void StartPlay()
        {
            var width = CanvasSpace.Width;
            var height = CanvasSpace.Height;

            var playerList = new List<Player>();
            //Gives the correct person lead
            foreach (var player in Players)
            {
                var corrupt = false;
                double biggestOffset = 0;
                for (var i = 0; i < player.ReplayModel.Frames.Count - 1; i++)
                {
                    if (player.ReplayModel.Frames[i + 1].A == 0 && player.ReplayModel.Frames[i].A == 0) continue;
                    var offset = player.ReplayModel.Frames[i + 1].A - player.ReplayModel.Frames[i].A;
                    if (biggestOffset < offset) biggestOffset = offset;
                    if (offset > 1)
                    {                        
                        corrupt = true;
                        break;
                    }
                }
                player.BiggestFrameOffsetTime = biggestOffset;
                if ((player.ReplayModel.Frames[100].A - player.ReplayModel.Frames[0].A) < 2 && !corrupt && (player.ReplayModel.Frames[100].A - player.ReplayModel.Frames[0].A) > 0.1)
                {
                    var currentLead = Players.FirstOrDefault(x => x.hasLead == true);
                    if (currentLead == null)
                    {
                        player.hasLead = true;
                    }
                    else
                    {
                        if(player.BiggestFrameOffsetTime < currentLead.BiggestFrameOffsetTime)
                        {
                            currentLead.hasLead = false;
                            player.hasLead = true;
                        }
                        else
                        {
                            player.hasLead = false;
                        }
                    }
                }
                else player.hasLead = false;
            }

            var leadPlayer = Players.FirstOrDefault(x => x.hasLead);
            if(leadPlayer != null) LeadNameLabelText.Content = leadPlayer.Name;
            else LeadNameLabelText.Content = "NO LEAD";


            //Start all players in tasks
            var tasks = new List<Task>();
            foreach (var player in Players)
            {
                var task = new Task(() =>
                {
                    Play(player, width, height);
                });
                tasks.Add(task);
            }

            //Player start amount 
            playerStartAmount = Players.Count();
            //Start playing background video if its been set
            if (BackgroundVideo.Source != null)
            {
                if (backgroundVideoDelay > 0) await Task.Delay(backgroundVideoDelay);
                BackgroundVideo.Play();
                if (backgroundVideoDelay < 0) await Task.Delay(backgroundVideoDelay * -1);
            }

            //Start all players
            tasks.ForEach(task => { task.Start(); });
            //Play Song
            playInstance.PlaySong();

            //Start eliminating players if battle royale mode 
            if (gameMode.SelectedGamemode == Gamemode.GameModes.BattleRoyale) gameMode.StartBattleRoyal();

            TransitionStartScreen.Visibility = Visibility.Hidden;

            Task.WaitAll(tasks.ToArray());
        }
       
        public void EliminateLastPlayer()
        {
            Dispatcher.Invoke(() =>
            {
                RemovePlayer(leaderboard.GetLastPlayer());
            });
        }

        public void RemovePlayer(Player player)
        {
            Dispatcher.Invoke(() =>
            {
                var playerToRemove = leaderboard.GetPlayer(player.Name);
                if (playerToRemove == null) return;

                //Perfect acc Mode
                if (gameMode.SelectedGamemode == Gamemode.GameModes.PerfectAcc || gameMode.SelectedGamemode == Gamemode.GameModes.ComboDropSafe)
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

                    ListViewPlayers.Items.Refresh();
                    return;
                }

                leaderboard.RemovePlayer(playerToRemove);                
                if (!player.hasLead) Players.Remove(player);
                CanvasSpace.Children.Remove(player.LeftHand);
                CanvasSpace.Children.Remove(player.RightHand);
                CanvasSpace.Children.Remove(player.LeftHandTip);
                CanvasSpace.Children.Remove(player.RightHandTip);
                CanvasSpace.Children.Remove(player.Head);
                foreach (var trail in player.TrailListLeft) CanvasSpace.Children.Remove(trail);
                foreach (var trail in player.TrailListRight) CanvasSpace.Children.Remove(trail);

                ListViewPlayers.Items.Refresh();
            });
        }

        public async void Play(Player player, double width, double height)
        {
            var hasLead = player.hasLead;
            var zoomx = width / 2.4;
            var zoomy = height / 4.8;
            var offsetHeight = 0 - (height / 4.35);

            //Remove start loading frames 
            player.ReplayModel.Frames.RemoveAll(x => x.A == 0);
            //player.ReplayModel.Frames.RemoveAll(x => x.A < player.ReplayModel.NoteTime.First());

            //await Task.Delay(TimeSpan.FromSeconds(player.ReplayModel.NoteTime.First()));
            await Task.Delay(100);

            var avgFps = player.ReplayModel.Frames.Average(x => x.I);

            var lowestSpeedFactor = _speedFactor - 0.1;
            var highestSpeedFactor = _speedFactor + 0.1;
            var startSpeedFactor = _speedFactor;

            var storedNoteTimes = new List<double>();
            storedNoteTimes.AddRange(player.ReplayModel.NoteTime.ToArray());
            var storedNoteTimesHitsound = new List<double>();
            storedNoteTimesHitsound.AddRange(player.ReplayModel.NoteTime.ToArray());
            //Add storedcombo but also fix possible errors in the data
            var storedCombo = new List<long>();
            var combos = player.ReplayModel.Combos.ToArray();
            for (var i = 0; i < combos.Length; i++)
            {
                if (i == 0 || i >= combos.Length - 1)
                {
                    storedCombo.Add(combos[i]);
                    continue;
                }
                if (combos[i + 1] > combos[i] && combos[i - 1] > combos[i] && combos[i] != 0)
                {
                    var combo = storedCombo.ElementAt(i - 1);
                    storedCombo.Remove(combo);
                    storedCombo.Add(combos[i]);
                    storedCombo.Add(combo);
                    continue;
                }
                storedCombo.Add(combos[i]);
            }

            var storedScores = new List<long>();
            storedScores.AddRange(player.ReplayModel.Scores.ToArray());
            var storedLightshowData = new List<Event>();
            storedLightshowData.AddRange(playInstance.mapDetails.Events.ToArray());

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
                    //Add Notes
                    if (storedNoteTimesHitsound.Count > 0)
                    {
                        var noteAnimationDelay = 0.300;
                        var noteTimeHitsound = storedNoteTimesHitsound.First();
                        //Check average note time before next note
                        if (player.ReplayModel.NoteTime.Count() > 5)
                        {
                            var noteRange = player.ReplayModel.NoteTime.GetRange(0, 5);
                            var noteDiff = (noteRange.Last() - noteRange.First()) / 5;
                            if (noteDiff < 0.1) noteAnimationDelay = noteDiff;
                        }

                        if (noteTimeHitsound < frame.A + noteAnimationDelay)
                        {
                            if (hasLead)
                            {
                                var note = player.ReplayModel.NoteInfos.First();
                                playInstance.AddNote(note, noteAnimationDelay);
                                player.ReplayModel.NoteInfos.Remove(note);
                            }
                            storedNoteTimesHitsound.Remove(noteTimeHitsound);
                        }
                    }


                    var noteTime = player.ReplayModel.NoteTime.First();
                    if (noteTime < frame.A)
                    {
                        //if (player.ReplayModel.NoteInfos.Count == 0) continue;

                        if (hasLead && lightshowIsActivated)
                        {
                            ////Check if a lightin event happens
                            var lightdata = storedLightshowData.First();
                            if (lightdata.Time < frame.A)
                            {
                                //Might not be good to do 12 and 13 away
                                if (true/*lightdata.Type != 12 || lightdata.Type != 13 || lightdata.Type != 8*/)
                                {
                                    Task.Run(async () =>
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            playInstance.lightEventBackgrounds[(int)lightdata.Type].Visibility = Visibility.Visible;
                                            //lightEventBackgrounds[(int)lightdata.Type].Opacity = lightdata.Value == 0 ? (double)0.1 : 1 / (double)lightdata.Value;
                                        });
                                        await Task.Delay(100);
                                        Dispatcher.Invoke(() =>
                                        {
                                            playInstance.lightEventBackgrounds[(int)lightdata.Type].Visibility = Visibility.Hidden;
                                            //lightEventBackgrounds[(int)lightdata.Type].Opacity = 0.1;
                                        });
                                    });
                                }

                                storedLightshowData.Remove(lightdata);
                            }
                        }

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

                            //Remove player if Perfect mode is on and player hits a low hit.
                            if (gameMode.SelectedGamemode == Gamemode.GameModes.PerfectAcc)
                            {
                                if (storedScores.First() < perfectAccAmount) RemovePlayer(player);
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
                                var item = leaderboard.GetPlayer(player.Name);
                                if (item != null)
                                {
                                    item.BorderBrush = System.Windows.Media.Brushes.Red;
                                    item.BorderThickness = new Thickness(5);
                                    await Task.Delay(1000);
                                    item.BorderBrush = player.LeftHand.Stroke;
                                }
                            });

                            //If ComboDrop gamemode remove player on miss
                            if (gameMode.SelectedGamemode == Gamemode.GameModes.ComboDrop || gameMode.SelectedGamemode == Gamemode.GameModes.ComboDropSafe)
                            {
                                if (leaderboard.listViewItems.Count > 1)
                                {
                                    Dispatcher.Invoke(() => RemovePlayer(player));
                                    continue;
                                }
                            }
                        }
                    }
                }


                //Force non-leads to keep up with the lead
                bool tooSlow = false;
                bool tooFast = false;
                double tooFastTime = 10;
                Dispatcher.Invoke(() =>
                {
                    var leadTime = Convert.ToDouble(TimeLabelLead.Content);
                    if (!hasLead && frame.A < leadTime - 0.01) tooSlow = true;
                    if (!hasLead && frame.A > leadTime + 0.01)
                    {
                        //delays the non-lead if he is way past the lead
                        if (frame.A - leadTime > 0.1) tooFastTime = (frame.A - leadTime) * 1000 / 2;
                        tooFast = true;
                    }
                });
                if (tooSlow) continue;
                if (tooFast) await Task.Delay(TimeSpan.FromMilliseconds(tooFastTime));

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
                if (count % (8 * Players.Count) == 0)
                {

                    if (frame.A > storedNoteTimes.First())
                    {
                        var lastNoteTimePassed = storedNoteTimes.Where(x => x < frame.A).Last();
                        var index = storedNoteTimes.IndexOf(lastNoteTimePassed);

                        Dispatcher.Invoke(() =>
                        {
                            var item = leaderboard.GetPlayer(player.Name);
                            if (item != null && player.ReplayModel.NoteTime.Count() != 0)
                            {
                                float acc = (float)Math.Round((currentScore * 100) / currentMaxScore, 2);
                                var combo = player.ReplayModel.Combos.First();
                                var score = $"{acc}% {combo}";
                                var spacesa = "";
                                var spacesc = "";
                                for (var i = 0; i < 5 % acc.ToString().Length; i++) spacesa += "  ";
                                for (var i = 0; i < 9 % combo.ToString().Length; i++) spacesc += "  ";
                                if (player.ReplayModel.Combos.Count() != 0) item.Content = $"{acc}{spacesa}%    {combo}{spacesc}    {player.Name}";
                                
                                leaderboard.OrderLeaderboardByAcc();
                                ////Add gradient opacity 
                                //double opacity = 1 - 1 / (double) orderedListview.Count() * (double)orderedListview.ToList().IndexOf(item);
                                //player.LeftHand.Opacity = opacity;
                                //player.RightHand.Opacity = opacity;
                                //player.LeftHandTip.Opacity = opacity;
                                //player.RightHandTip.Opacity = opacity;
                                //player.Head.Opacity = opacity;
                                //foreach (var trail in player.TrailListLeft) trail.Opacity = opacity;
                                //foreach (var trail in player.TrailListRight) trail.Opacity = opacity;


                                //Give top 30 more attention
                                //if (hasLead && orderedListview.Count() > 3)
                                //{
                                //    var count = 1;
                                //    foreach (var oli in orderedListview)
                                //    {
                                //        if (count <= 3)
                                //        {
                                //            orderedListview.ElementAt(count).FontSize = 30 + count * 10;
                                //            orderedListview.ElementAt(count).Width += 30 * count;
                                //        }
                                //        else
                                //        {
                                //            oli.Width = 460;
                                //            oli.FontSize = 30;
                                //        }
                                //        count++;
                                //    }
                                //}                               
                            }
                        });
                    }

                }

                //Set all object positions
                var centerWidth = width / 2;
                var centerHeight = height / 2;
                Dispatcher.Invoke(() =>
                {

                    //Set sabertip positions
                    var tipRight = QuaternionCalculator.RotateSaber(new QuaternionCalculator.Point { x = frame.R.P.X * 2, y = frame.R.P.Y * 2, z = frame.R.P.Z * 2 }, 2.3, new QuaternionCalculator.Quaternion { x = frame.R.R.X, y = frame.R.R.Y, z = frame.R.R.Z, w = (double)frame.R.R.W });
                    var tipLeft = QuaternionCalculator.RotateSaber(new QuaternionCalculator.Point { x = frame.L.P.X * 2, y = frame.L.P.Y * 2, z = frame.L.P.Z * 2 }, 2.3, new QuaternionCalculator.Quaternion { x = frame.L.R.X, y = frame.L.R.Y, z = frame.L.R.Z, w = (double)frame.L.R.W });

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

                    //Set head Positions
                    Canvas.SetLeft(player.Head, centerWidth + frame.H.P.X * zoomx);
                    Canvas.SetBottom(player.Head, centerHeight + offsetHeight + (frame.H.P.Y + (1.7 - player.ReplayModel.Info.Height))/*Removes height differences*/ * zoomy + 200);
                    //TODO: Set head Rotation
                    //var w = Math.Acos((double)frame.H.R.W) * 2;
                    //var ax = (double)frame.H.R.X / Math.Sin(Math.Acos(w));
                    //var ay = (double)frame.H.R.Y / Math.Sqrt(1 - w * w);
                    //var az = (double)frame.H.R.Z / Math.Sin(Math.Acos(w));                    

                    //var euler = QuaternionCalculator.ToEulerAngles(new QuaternionCalculator.Quaternion() { x = frame.H.R.X, y = frame.H.R.Y, z = frame.H.R.Z, w = (double)frame.H.R.W });
                    player.Head.RenderTransform = new RotateTransform(frame.H.R.Z * 90, player.Head.Width / 2, player.Head.Height / 2);



                    //Give positions to each trail
                    if (player.ReplayModel.Frames.IndexOf(frame) > 1)
                    {
                        if (trailIndex == player.TrailListLeft.Count) trailIndex = 0;

                        player.DrawTrail(this, positionxoldleft, positionyoldleft, positionxoldright, positionyoldright, trailIndex);

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

        public async Task<bool> LoadInPlayer(string playerID, System.Windows.Media.Color color)
        {
            var player = new Player(playerID, color);
            return await player.LoadPlayer(this);
        }
    }
}
