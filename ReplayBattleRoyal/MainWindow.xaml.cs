using ReplayBattleRoyal.Entities;
using ReplayBattleRoyal.GameModes;
using ReplayBattleRoyal.Managers;
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
        //Canvas
        public double canvasWidth;
        public double canvasHeight;

        //Map
        public int songID;

        //Players
        public List<Player> Players = new List<Player>();
        public Player AveragePlayer;

        //Instance
        public PlayInstance playInstance;
        public double songTime = 0;
        public bool streamMode = false;
        private double _speedFactor = 12.35;

        public Entities.Leaderboard leaderboard;
        private int backgroundVideoDelay;

        private int playerStartAmount;

        private double _startSpeedFactor;

        public Gamemode gameMode;

        //Effects
        private EffectsPanel effectsPanel;
        public bool lightshowIsActivated = false;
        public int perfectAccAmount = 105;

        //General
        private Random random = new Random();

        public MainWindow()
        {            
            InitializeComponent();
            //Start(songID, playerAmount, country, streamMode, gamemode, withBackgroundVideo, backgroundDelay);
        }

        public async Task Start(double startSpeedFactor, int songID, int playerAmount = 1, string country = null, bool streamMode = false, Gamemode.GameModes selectedGameMode = Gamemode.GameModes.None, bool useBackgroundVideo = false, int backgrounVideoDelay = 0)
        {
            _speedFactor = startSpeedFactor;
            this.streamMode = streamMode;
            this.backgroundVideoDelay = backgrounVideoDelay;
            this.songID = songID;
            gameMode = new Gamemode(this, selectedGameMode);
            leaderboard = new Leaderboard(this);
            await leaderboard.GetLeaderboardInfo(songID);
            playInstance = new PlayInstance(this);
            canvasWidth = CanvasSpace.Width;
            canvasHeight = CanvasSpace.Height;
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

            //Show effects panel 
            effectsPanel = new EffectsPanel(this);
            effectsPanel.Show();

            //Initialize gamemode 
            gameMode.InitializeGamemode();

            //Gettings SongInfo
            LoadingLabel.Content = $"Loading... Getting song info";
            var leaderboardInfo = leaderboard.leaderboardInfo;
            SongNameLabel.Content = leaderboardInfo.SongName;

            //Load leaderboard players 
            var leaderboardScores = new List<Score>();
            if (selectedGameMode == Gamemode.GameModes.SkillIssue)
            {
                var playerAmountHalved = playerAmount / 2;
                for (var i = 0; i < Math.Round(playerAmountHalved / 5.0); i++) leaderboardScores.AddRange(await leaderboard.scoresaberClient.Api.Leaderboards.GetLeaderboardScoresByID(songID, country, page: i + 1));
                for (var i = 0; i < Math.Round(playerAmountHalved / 5.0); i++) leaderboardScores.AddRange(await leaderboard.scoresaberClient.Api.Leaderboards.GetLeaderboardScoresByID(songID, country, page: i + 1 + 26));
            }
            else
            {
                for (var i = 0; i <= Math.Round(playerAmount / 10.0); i++) leaderboardScores.AddRange(await leaderboard.scoresaberClient.Api.Leaderboards.GetLeaderboardScoresByID(songID, country, page: i + 1));
            }

            //Resize leaderboard view
            var gridView = ListViewPlayers.View as GridView;
            gridView.Columns.First().Width = 550;

            //Prepare song file
            LoadingLabel.Content = $"Loading... preparing song file";
            await playInstance.PrepareSongFile(leaderboardInfo.SongHash.ToLower(), useBackgroundVideo);

            //Load in all players. Parallel loading
            LoadingLabel.Content = $"Loading... start loading players";
            var playerCount = playerAmount;
            var playersLoaded = 0;
            var tasks = new List<Task>();
            //Mix up leaderboard for fair loading
            if (selectedGameMode == Gamemode.GameModes.SkillIssue) leaderboardScores = leaderboardScores.OrderByDescending(x => random.Next(0, playerCount)).ToList();
            for (var i = 0; i < playerAmount; i++)
            {
                var color = ColorManager.ColorFromHSV(random.Next(150, 400), random.Next(90, 100) / 100.00, 1);
                //If game mode skillissue. group them in colors
                if (selectedGameMode == Gamemode.GameModes.SkillIssue) color = leaderboardScores[i].Rank < 300 ? Colors.Red : Colors.LightGray;

                tasks.Add(Task.Run(async () =>
                {
                    var isFinishedCorrectly = await LoadInPlayer($"{leaderboardScores[i].LeaderboardPlayerInfo.Id}", color);
                    if (isFinishedCorrectly) playersLoaded++;
                    else playerCount--;

                    Dispatcher.Invoke(() => { LoadingLabel.Content = $"Loading {playersLoaded}/{playerCount}..."; });
                }));

                await Task.Delay(4000);
            }
            await Task.WhenAll(tasks);

            AveragePlayer = await new Player(this, "000", Colors.White).InitiateAveragePlayer();

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

            //Init effects panel
            effectsPanel.InitializeEffectPanel(Players);

            //Show intro
            if (streamMode) await MediaManager.ShowIntro(this, gameMode, playerCount, leaderboardInfo.SongName, leaderboardInfo.SongAuthorName, leaderboardInfo.LevelAuthorName, leaderboardInfo.CoverImage);
            LoadingLabel.Visibility = Visibility.Hidden;
            StartPlay(1);
        }

        public async void StartPlay(int skipAmount = 0)
        {
            //Remove double time values
            Players.ForEach(x => x.ReplayModel.Frames = x.ReplayModel.Frames.GroupBy(s => s.A)
                                                 .Select(grp => grp.FirstOrDefault())
                                                 .OrderBy(s => s.A)
                                                 .ToList());

            //Gives the correct person the lead
            var potentialLeadList = new List<Player>();
            foreach (var player in Players)
            {
                var corrupt = false;
                double biggestOffset = 0;
                for (var i = 0; i < player.ReplayModel.Frames.Count - 1; i++)
                {
                    if (player.ReplayModel.Frames[i + 1].A == 0 && player.ReplayModel.Frames[i].A == 0) continue;
                    var offset = player.ReplayModel.Frames[i + 1].A - player.ReplayModel.Frames[i].A;
                    if (biggestOffset < offset) biggestOffset = offset;
                    if (offset > 5)
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
                        if (player.BiggestFrameOffsetTime < currentLead.BiggestFrameOffsetTime)
                        {
                            currentLead.hasLead = false;
                            potentialLeadList.Add(currentLead);
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

            //Check if lead has to be skipped
            if (skipAmount > 0 && potentialLeadList.Count >= skipAmount)
            {
                leadPlayer.hasLead = false;
                leadPlayer = potentialLeadList[^skipAmount];
                leadPlayer.hasLead = true;
            }

            //Gets the lead player 
            if (leadPlayer != null) LeadNameLabelText.Content = leadPlayer.Name;
            else LeadNameLabelText.Content = "NO LEAD";

            //Start all players in tasks
            var tasks = new List<Task>();
            foreach (var player in Players)
            {
                var task = new Task(() =>
                {
                    Play(player);
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



        public async void Play(Player player)
        {
            var hasLead = player.hasLead;
            var zoomx = canvasWidth / 2.4;
            var zoomy = canvasHeight / 4.8;
            var offsetHeight = 0 - (canvasHeight / 4.35);

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

            var storedLightshowData = new List<Event>();
            storedLightshowData.AddRange(playInstance.mapDetails.Events.ToArray());

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
                    if (storedNoteTimesHitsound.Count > 0 && hasLead)
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
                            var note = player.ReplayModel.NoteInfos.First();
                            playInstance.AddNote(note, noteAnimationDelay);
                            player.ReplayModel.NoteInfos.Remove(note);
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
                        // calculate Player score and max score
                        var currentScoreAndMaxScore = player.CalculateCurrentScoreAndMaxScore(noteTime);
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
                                    Dispatcher.Invoke(() => playInstance.RemovePlayer(player));
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
                    var errorCorrectionLimit = 5;
                    if (frame.A < songTime - 0.01) _speedFactor += (songTime - frame.A) * 0.1;
                    else if (frame.A > songTime + 0.01) _speedFactor -= (frame.A - songTime) * 0.1;
                    else _speedFactor = (lowestSpeedFactor + highestSpeedFactor) / 2;

                    if (_speedFactor > startSpeedFactor + errorCorrectionLimit) _speedFactor = startSpeedFactor + errorCorrectionLimit;
                    if (_speedFactor < startSpeedFactor - errorCorrectionLimit) _speedFactor = startSpeedFactor - errorCorrectionLimit;

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
                var waitTime = TimeSpan.FromSeconds(timeTillNextFrame) / _speedFactor;
                await Task.Delay(waitTime);

                //Add points from the player on the leaderboard
                if (count % (8 * Players.Count) == 0)
                {
                    if (frame.A > storedNoteTimes.First())
                    {
                        var lastNoteTimePassed = storedNoteTimes.Where(x => x < frame.A).Last();
                        var index = storedNoteTimes.IndexOf(lastNoteTimePassed);
                        leaderboard.GivePointsToPlayer(player, player.currentScore, player.currentMaxScore);
                    }
                }

                //Draw Player
                player.DrawPlayer(player, frame, canvasWidth, canvasHeight, zoomx, zoomy, offsetHeight);

                //Draw average player
                if (hasLead)
                {
                    AveragePlayer.ReplayModel = new ReplayModel() { Info = new Info() { Height = Players.Average(x => x.ReplayModel.Info.Height) } };
                    var f = new Frame() { R = new H() { P = new Room(), R = new Room() }, L = new H() { P = new Room(), R = new Room() }, H = new H() { P = new Room(), R = new Room() } };
                    AveragePlayer.DrawPlayer(AveragePlayer, f, canvasWidth, canvasHeight, zoomx, zoomy, offsetHeight);
                }

                count++;
            }

            //When all frames ended, show outro
            if (hasLead) await Dispatcher.Invoke(async () => { await MediaManager.ShowOutro(this); });
        }

        public async Task<bool> LoadInPlayer(string playerID, System.Windows.Media.Color color)
        {
            var player = new Player(this, playerID, color);
            return await player.LoadPlayer();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(playInstance.mediaPlayer != null) playInstance.mediaPlayer.Stop();
        }
    }
}
