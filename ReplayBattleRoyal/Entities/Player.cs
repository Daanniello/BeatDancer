using Newtonsoft.Json;
using ScoreSaberLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

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
        public ReplayModel ReplayModel { get; set; }
        public double BiggestFrameOffsetTime { get; set; }
        public List<Line> TrailListLeft { get; set; }
        public List<Line> TrailListRight { get; set; }

        public Player(string playerID, System.Windows.Media.Color color)
        {
            this.color = color;
            this.ID = playerID;
        }

        public async Task<bool> LoadPlayer(MainWindow mainWindow)
        {
            try
            {
                var playerInfo = await mainWindow.ScoresaberClient.Api.Players.GetPlayer(Convert.ToInt64(ID));
                var replayModel = await GetReplayModel($"https://sspreviewdecode.azurewebsites.net/?playerID={ID}&songID={mainWindow.songID}");

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

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async void DrawTrail(MainWindow mainWindow, double positionxoldleft, double positionyoldleft, double positionxoldright, double positionyoldright, int trailIndex)
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

        public async Task<ReplayModel> GetReplayModel(string url)
        {
            //Get content from website as string
            try
            {
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
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
