using Newtonsoft.Json;
using ReplayBattleRoyal.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReplayBattleRoyal.Entities
{
    public class Player
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public System.Windows.Media.Color color { get; set; }
        public bool hasLead { get; set; }
        public System.Windows.Shapes.Rectangle Head { get; set; }
        public Ellipse LeftHand { get; set; }
        public Ellipse RightHand { get; set; }
        public Ellipse LeftHandTip { get; set; }
        public Ellipse RightHandTip { get; set; }

        public QuaternionCalculator.Point CurrentLeftHandTipPoint { get; set; }
        public QuaternionCalculator.Point CurrentRightHandTipPoint { get; set; }

        public Frame CurrentFrame { get; set; }

        public ReplayModel ReplayModel { get; set; }
        public double BiggestFrameOffsetTime { get; set; }
        public List<Line> TrailListLeft { get; set; }
        public List<Line> TrailListRight { get; set; }

        private MainWindow mainWindow;

        public Player(MainWindow mainWindow, string playerID, System.Windows.Media.Color color)
        {
            this.color = color;
            this.ID = playerID;
            this.mainWindow = mainWindow;
        }

        public async Task<Player> InitiateAveragePlayer()
        {
            await mainWindow.Dispatcher.Invoke(async () =>
            {
                var stroke = new SolidColorBrush(color);
                var leftHand = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 25, Height = 25 };
                var rightHand = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 25, Height = 25 };
                var leftHandTip = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 15, Height = 15 };
                var rightHandTip = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 15, Height = 15 };
                var head = new System.Windows.Shapes.Rectangle() { Stroke = stroke, StrokeThickness = 4, Width = 50, Height = 25, RadiusX = 2, RadiusY = 2 };
                mainWindow.CanvasSpace.Children.Add(leftHand);
                mainWindow.CanvasSpace.Children.Add(rightHand);
                mainWindow.CanvasSpace.Children.Add(head);
                //CanvasSpace.Children.Add(leftHandTip);
                //CanvasSpace.Children.Add(rightHandTip);

                LeftHand = leftHand;
                RightHand = rightHand;
                LeftHandTip = leftHandTip;
                RightHandTip = rightHandTip;
                Name = "Average Player";
                color = color;
                Head = head;

                TrailListLeft = new List<Line>();
                TrailListRight = new List<Line>();
                var trailMax = 8;
                for (var i = 0; i < trailMax; i++)
                {

                    var left = new Line()
                    {
                        Stroke = LeftHand.Stroke,
                        Fill = LeftHand.Stroke,
                        StrokeThickness = 10,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Opacity = 1
                    };
                    var right = new Line()
                    {
                        Stroke = LeftHand.Stroke,
                        Fill = LeftHand.Stroke,
                        StrokeThickness = 10,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Opacity = 1
                    };

                    TrailListLeft.Add(left);
                    TrailListRight.Add(right);
                    mainWindow.CanvasSpace.Children.Add(left);
                    mainWindow.CanvasSpace.Children.Add(right);
                }
            });

            return this;
        }

        public async Task<bool> LoadPlayer()
        {
            try
            {
                var playerInfo = await mainWindow.leaderboard.scoresaberClient.Api.Players.GetPlayer(Convert.ToInt64(ID));
                var replayModel = await GetReplayModel(mainWindow.songID.ToString(), ID);

                if (replayModel == null || playerInfo == null) return false;
                if (replayModel.Frames == null || replayModel.Info.LeftHanded == true) return false; //TODO: include left hand mode
                //Remove player if the frames count don't match others
                if (mainWindow.Players.Count > 0) if (replayModel.Frames.Last().A + 10 < mainWindow.Players.Average(x => x.ReplayModel.Frames.Last().A)) return false;
                //Remove player if scores are bugged out 
                if (replayModel.Scores.Sum() <= 0) return false;
                //Remove player if start frame is way too late 
                if (replayModel.Frames.First(x => x.A > 0).A > 10) return false;


                await mainWindow.Dispatcher.Invoke(async () =>
                {
                    var stroke = new SolidColorBrush(color);
                    var leftHand = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 25, Height = 25 };
                    var rightHand = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 25, Height = 25 };
                    var leftHandTip = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 15, Height = 15 };
                    var rightHandTip = new Ellipse() { Stroke = stroke, Fill = stroke, Width = 15, Height = 15 };
                    var head = new System.Windows.Shapes.Rectangle() { Stroke = stroke, StrokeThickness = 4, Width = 50, Height = 25, RadiusX = 2, RadiusY = 2 };
                    mainWindow.CanvasSpace.Children.Add(leftHand);
                    mainWindow.CanvasSpace.Children.Add(rightHand);
                    mainWindow.CanvasSpace.Children.Add(head);
                    //CanvasSpace.Children.Add(leftHandTip);
                    //CanvasSpace.Children.Add(rightHandTip);

                    LeftHand = leftHand;
                    RightHand = rightHand;
                    LeftHandTip = leftHandTip;
                    RightHandTip = rightHandTip;
                    ReplayModel = replayModel;
                    Name = playerInfo.Name;
                    color = color;
                    Head = head;
                    CurrentLeftHandTipPoint = new QuaternionCalculator.Point() { x = 0, y = 0 };
                    CurrentRightHandTipPoint = new QuaternionCalculator.Point() { x = 0, y = 0 };
                    CurrentFrame = new Frame() { R = new H() { P = new Room(), R = new Room() }, L = new H() { P = new Room(), R = new Room() }, H = new H() { P = new Room(), R = new Room() } };

                    //If someone has the same name, add a number at the end of it.
                    if (mainWindow.Players.FirstOrDefault(x => x.Name.Split(" *(")[0] == Name) != null) Name += $" *({mainWindow.Players.Where(x => x.Name.Split(" *(")[0] == Name).Count()})";

                    TrailListLeft = new List<Line>();
                    TrailListRight = new List<Line>();
                    var trailMax = 3;
                    for (var i = 0; i < trailMax; i++)
                    {

                        var left = new Line()
                        {
                            Stroke = LeftHand.Stroke,
                            Fill = LeftHand.Stroke,
                            StrokeThickness = 10,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            Opacity = 1
                        };
                        var right = new Line()
                        {
                            Stroke = LeftHand.Stroke,
                            Fill = LeftHand.Stroke,
                            StrokeThickness = 10,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            Opacity = 1
                        };

                        TrailListLeft.Add(left);
                        TrailListRight.Add(right);
                        mainWindow.CanvasSpace.Children.Add(left);
                        mainWindow.CanvasSpace.Children.Add(right);
                    }

                    mainWindow.Players.Add(this);
                    mainWindow.leaderboard.AddPlayer(Name, leftHand.Stroke);
                    mainWindow.leaderboard.RefreshLeaderboard();
                });

                //Fix the combo array from wrong data
                var combos = ReplayModel.Combos.ToArray();
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

                //Store scores array into backup variable
                storedScores.AddRange(ReplayModel.Scores.ToArray());

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //Variables specific for this function
        private double positionxoldleft = 0;
        private double positionyoldleft = 0;
        private double positionxoldright = 0;
        private double positionyoldright = 0;

        private int trailIndex = 0;

        public void DrawPlayer(Player player, Frame frame, double canvasWidth, double canvasHeight, double zoomx, double zoomy, double offsetHeight)
        {
            var centerWidth = canvasWidth / 2;
            var centerHeight = canvasHeight / 2;

            //Set sabertip positions
            if (player.Name == "Average Player")
            {
                CurrentLeftHandTipPoint = new QuaternionCalculator.Point() { x = mainWindow.Players.Average(x => x.CurrentLeftHandTipPoint.x), y = mainWindow.Players.Average(x => x.CurrentLeftHandTipPoint.y) };
                CurrentRightHandTipPoint = new QuaternionCalculator.Point() { x = mainWindow.Players.Average(x => x.CurrentRightHandTipPoint.x), y = mainWindow.Players.Average(x => x.CurrentRightHandTipPoint.y) };

                frame.R.P.X = mainWindow.Players.Average(x => x.CurrentFrame.R.P.X);
                frame.R.P.Y = mainWindow.Players.Average(x => x.CurrentFrame.R.P.Y);
                frame.R.P.Z = mainWindow.Players.Average(x => x.CurrentFrame.R.P.Z);

                frame.L.P.X = mainWindow.Players.Average(x => x.CurrentFrame.L.P.X);
                frame.L.P.Y = mainWindow.Players.Average(x => x.CurrentFrame.L.P.Y);
                frame.L.P.Z = mainWindow.Players.Average(x => x.CurrentFrame.L.P.Z);

                frame.H.P.X = mainWindow.Players.Average(x => x.CurrentFrame.H.P.X);
                frame.H.P.Y = mainWindow.Players.Average(x => x.CurrentFrame.H.P.Y);
                frame.H.R.Z = mainWindow.Players.Average(x => x.CurrentFrame.H.R.Z);

                frame.A = 1;
                CurrentFrame = frame;

            }
            else
            {
                CurrentLeftHandTipPoint = QuaternionCalculator.RotateSaber(new QuaternionCalculator.Point { x = frame.R.P.X * 2, y = frame.R.P.Y * 2, z = frame.R.P.Z * 2 }, 2.3, new QuaternionCalculator.Quaternion { x = frame.R.R.X, y = frame.R.R.Y, z = frame.R.R.Z, w = (double)frame.R.R.W });
                CurrentRightHandTipPoint = QuaternionCalculator.RotateSaber(new QuaternionCalculator.Point { x = frame.L.P.X * 2, y = frame.L.P.Y * 2, z = frame.L.P.Z * 2 }, 2.3, new QuaternionCalculator.Quaternion { x = frame.L.R.X, y = frame.L.R.Y, z = frame.L.R.Z, w = (double)frame.L.R.W });
                CurrentFrame = frame;
            }

            mainWindow.Dispatcher.Invoke(() =>
            {
                //Set Right hand tip position
                Canvas.SetLeft(player.RightHandTip, CurrentRightHandTipPoint.x * 225 + 625);
                Canvas.SetBottom(player.RightHandTip, CurrentRightHandTipPoint.y * 225 - 100);
                //Set Left hand tip position
                Canvas.SetLeft(player.LeftHandTip, CurrentLeftHandTipPoint.x * 225 + 600);
                Canvas.SetBottom(player.LeftHandTip, CurrentLeftHandTipPoint.y * 225 - 100);
                //Set Left Hand Positions
                Canvas.SetLeft(player.LeftHand, centerWidth + CurrentFrame.L.P.X * zoomx);
                Canvas.SetBottom(player.LeftHand, centerHeight + offsetHeight + (CurrentFrame.L.P.Y + (1.7 - player.ReplayModel.Info.Height))/*Removes height differences*/ * zoomy);
                //Set Right Hand Positions
                Canvas.SetLeft(player.RightHand, centerWidth + CurrentFrame.R.P.X * zoomx);
                Canvas.SetBottom(player.RightHand, centerHeight + offsetHeight + (CurrentFrame.R.P.Y + (1.7 - player.ReplayModel.Info.Height))/*Removes height differences*/ * zoomy);
                //Set head Positions
                Canvas.SetLeft(player.Head, centerWidth + CurrentFrame.H.P.X * zoomx);
                Canvas.SetBottom(player.Head, centerHeight + offsetHeight + (CurrentFrame.H.P.Y + (1.7 - player.ReplayModel.Info.Height))/*Removes height differences*/ * zoomy + 200);
                //Set head Rotation
                player.Head.RenderTransform = new RotateTransform(CurrentFrame.H.R.Z * 90, player.Head.Width / 2, player.Head.Height / 2);


                if (hasLead) mainWindow.TimeLabelLead.Content = CurrentFrame.A;
                else mainWindow.TimeLabel.Content = CurrentFrame.A;

                //Give positions to each trail
                if (player.Name == "Average Player" || player.ReplayModel.Frames.IndexOf(CurrentFrame) > 1)
                {
                    if (trailIndex == player.TrailListLeft.Count) trailIndex = 0;

                    player.DrawTrail(positionxoldleft, positionyoldleft, positionxoldright, positionyoldright, trailIndex);

                    positionxoldleft = Canvas.GetLeft(player.LeftHandTip);
                    positionyoldleft = Canvas.GetBottom(player.LeftHandTip);
                    positionxoldright = Canvas.GetLeft(player.RightHandTip);
                    positionyoldright = Canvas.GetBottom(player.RightHandTip);

                    trailIndex++;
                }

            });
        }

        public async void DrawTrail(double positionxoldleft, double positionyoldleft, double positionxoldright, double positionyoldright, int trailIndex)
        {
            TrailListLeft[trailIndex].X1 = positionxoldleft + LeftHandTip.Width / 2;
            TrailListLeft[trailIndex].Y1 = mainWindow.CanvasSpace.Height - LeftHandTip.Height / 2 - positionyoldleft;
            TrailListLeft[trailIndex].X2 = Canvas.GetLeft(LeftHandTip) + LeftHandTip.Width / 2;
            TrailListLeft[trailIndex].Y2 = mainWindow.CanvasSpace.Height - LeftHandTip.Height / 2 - Canvas.GetBottom(LeftHandTip);

            TrailListRight[trailIndex].X1 = positionxoldright + RightHandTip.Width / 2;
            TrailListRight[trailIndex].Y1 = mainWindow.CanvasSpace.Height - RightHandTip.Height / 2 - positionyoldright;
            TrailListRight[trailIndex].X2 = Canvas.GetLeft(RightHandTip) + RightHandTip.Width / 2;
            TrailListRight[trailIndex].Y2 = mainWindow.CanvasSpace.Height - RightHandTip.Height / 2 - Canvas.GetBottom(RightHandTip);
        }

        private List<long> storedCombo = new List<long>();
        public double currentScore = 0;
        public double currentMaxScore = 0;
        private List<long> storedScores = new List<long>();
        private int comboMultiplier = 1;
        private int notesTillNextMultiplier = 2;
        private bool shouldCalculateBeginCombo = true;

        public (double, double) CalculateCurrentScoreAndMaxScore(double noteTime)
        {

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
                if (mainWindow.gameMode.SelectedGamemode == Gamemode.GameModes.PerfectAcc)
                {
                    if (storedScores.First() < mainWindow.perfectAccAmount) mainWindow.playInstance.RemovePlayer(this);
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

            ReplayModel.NoteTime.Remove(noteTime);
            ReplayModel.Combos.Remove(ReplayModel.Combos.First());

            return (currentScore, currentMaxScore);
        }

        public async Task<ReplayModel> GetReplayModel(string songID, string playerID)
        {
            var ssDecoder = new SSDecoder.Controllers.SSDecoder().GetEmpDetails(playerID, songID);
            var json = JsonConvert.SerializeObject(ssDecoder);
            var jsonObject = JsonConvert.DeserializeObject<ReplayModel>(json);
            return jsonObject;
        }
    }
}
