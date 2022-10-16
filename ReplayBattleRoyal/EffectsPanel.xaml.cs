using ReplayBattleRoyal.Entities;
using ReplayBattleRoyal.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReplayBattleRoyal
{
    /// <summary>
    /// Interaction logic for EffectsPanel.xaml
    /// </summary>
    public partial class EffectsPanel : Window
    {
        private List<Player> currentPlayerList;
        private Dictionary<Player, Brush> originalColors = new Dictionary<Player, Brush>();
        private Dictionary<Player, double> originalSize = new Dictionary<Player, double>();
        private int originalTrailSize = 0;
        private Brush originalNoteColorLeft;
        private Brush originalNoteColorRight;
        private Brush originalNoteColorLeftArrow;
        private Brush originalNoteColorRightArrow;

        private MainWindow mainWindow;

        private Color allPlayersColor;

        public EffectsPanel(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;           
        }

        public void InitializeEffectPanel(List<Player> Players)
        {
            foreach (var player in Players)
            {
                originalColors.Add(player, player.LeftHand.Stroke);
                originalSize.Add(player, player.TrailListLeft.First().StrokeThickness);
            }
            originalTrailSize = Players.First().TrailListLeft.Count;
            currentPlayerList = Players;

            originalNoteColorLeft = mainWindow.playInstance.NoteColorLeft;
            originalNoteColorRight = mainWindow.playInstance.NoteColorRight;
            originalNoteColorLeftArrow = mainWindow.playInstance.NoteColorLeftArrow;
            originalNoteColorRightArrow = mainWindow.playInstance.NoteColorRightArrow;
        }

        private void ClrPcker_Background_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            allPlayersColor = (Color)e.NewValue;
        }

        private void ChangeAllPlayersColorsButton_Click(object sender, RoutedEventArgs e)
        {
            var color = new SolidColorBrush(allPlayersColor);
            ChangeAllColor(color);
        }

        private void ChangeColorRandom_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllRandomColor();
        }

        private void ChangeBlackWhiteButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllColor(Brushes.White);
            ChangeNoteColors(Brushes.White, Brushes.Black, Brushes.Black, Brushes.White);
            ChangeAllLeaderboardColor(Brushes.White);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var player in currentPlayerList)
            {
                ChangeColor(player, originalColors.FirstOrDefault(x => x.Key == player).Value);
                ChangeSize(player, originalSize.FirstOrDefault(x => x.Key == player).Value);
                SetTrail(player, originalTrailSize);

                var item = mainWindow.leaderboard.GetPlayer(player.Name);
                if (item != null) item.Background = originalColors.FirstOrDefault(x => x.Key == player).Value;
            }

            mainWindow.playInstance.NoteColorLeft = originalNoteColorLeft;
            mainWindow.playInstance.NoteColorRight = originalNoteColorRight;
            mainWindow.playInstance.NoteColorLeftArrow = originalNoteColorLeftArrow;
            mainWindow.playInstance.NoteColorRightArrow = originalNoteColorRightArrow;
        }

        private void MakeEveryoneSmallerButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllSize(-0.5);
        }
        private void MakeEveryoneBiggerButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllSize(+0.5);
        }

        public void ChangeAllSize(double value, bool isIncrease = true)
        {
            if (!isIncrease) foreach (var player in currentPlayerList) ChangeSize(player, value);
            else foreach (var player in currentPlayerList) ChangeSize(player, player.TrailListLeft.First().StrokeThickness + value);
        }

        public void ChangeSize(Player player, double size)
        {
            player.LeftHandTip.StrokeThickness = size;
            player.RightHandTip.StrokeThickness = size;

            foreach (var trail in player.TrailListLeft) trail.StrokeThickness = size;
            foreach (var trail in player.TrailListRight) trail.StrokeThickness = size;
        }

        public void ChangeNoteColors(Brush left, Brush right, Brush leftArrow, Brush rightArrow)
        {
            mainWindow.playInstance.NoteColorLeft = left;
            mainWindow.playInstance.NoteColorRight = right;
            mainWindow.playInstance.NoteColorLeftArrow = leftArrow;
            mainWindow.playInstance.NoteColorRightArrow = rightArrow;
        }

        public void ChangeAllLeaderboardColor(Brush color)
        {
            foreach (var player in currentPlayerList)
            {
                ChangeLeaderboardColor(player, Brushes.White);
            }
        }

        public void ChangeLeaderboardColor(Player player, Brush color)
        {
            var item = mainWindow.leaderboard.GetPlayer(player.Name);
            if (item != null) item.Background = color;
        }

        private void ChangeAllColor(Brush color)
        {
            if (color == null) return;
            foreach (var player in currentPlayerList) ChangeColor(player, color);
        }

        public void ChangeAllRandomColor()
        {
            var random = new Random();
            var color = new SolidColorBrush(ColorManager.ColorFromHSV(random.Next(0, 360), random.Next(75, 100) / 100.00, 1));
            ChangeAllColor(color);
        }
        public void ChangeColor(Player player, Brush color)
        {
            if (color == null) return;
            player.LeftHand.Fill = color;
            player.LeftHandTip.Stroke = color;
            player.RightHand.Fill = color;
            player.RightHandTip.Stroke = color;
            player.Head.Stroke = color;

            foreach (var trail in player.TrailListLeft) trail.Stroke = color;
            foreach (var trail in player.TrailListRight) trail.Stroke = color;
        }

        private void MakeEveryonesTrailSmallerButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllTrail(-1);
        }
        private void MakeEveryonesTrailBiggerButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllTrail(1);
        }

        private void ChangeAllTrail(int value)
        {
            foreach (var player in currentPlayerList) ChangeTrail(player, value);
        }

        private void SetTrail(Player player, int value)
        {
            if (value < 2) return;

            var linel = new Line
            {
                Stroke = player.LeftHand.Stroke,
                Fill = player.LeftHand.Stroke,
                StrokeThickness = 10,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 1
            };

            var liner = new Line
            {
                Stroke = player.LeftHand.Stroke,
                Fill = player.LeftHand.Stroke,
                StrokeThickness = 10,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 1
            };

            if (player.TrailListLeft.Count < value) for (var i = 0; i < value - player.TrailListLeft.Count; i++)
                {
                    mainWindow.CanvasSpace.Children.Add(linel);
                    mainWindow.CanvasSpace.Children.Add(liner);
                    player.TrailListLeft.Add(linel);
                    player.TrailListRight.Add(liner);

                }
            if (player.TrailListLeft.Count > value) for (var i = 0; i > player.TrailListLeft.Count - value; i++)
                {
                    mainWindow.CanvasSpace.Children.Remove(player.TrailListLeft.Last());
                    mainWindow.CanvasSpace.Children.Remove(player.TrailListRight.Last());
                    player.TrailListLeft.Remove(player.TrailListLeft.Last());
                    player.TrailListRight.Remove(player.TrailListRight.Last());
                }
        }

        private void ChangeTrail(Player player, int value)
        {
            if (value < 0 && player.TrailListLeft.Count <= 2) return;

            var linel = new Line
            {
                Stroke = player.LeftHand.Stroke,
                Fill = player.LeftHand.Stroke,
                StrokeThickness = 10,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 1
            };

            var liner = new Line
            {
                Stroke = player.LeftHand.Stroke,
                Fill = player.LeftHand.Stroke,
                StrokeThickness = 10,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 1
            };

            if (value > 0) for (var i = 0; i < value; i++)
                {
                    mainWindow.CanvasSpace.Children.Add(linel);
                    mainWindow.CanvasSpace.Children.Add(liner);
                    player.TrailListLeft.Add(linel);
                    player.TrailListRight.Add(liner);

                }
            if (value < 0) for (var i = 0; i > value; i--)
                {
                    mainWindow.CanvasSpace.Children.Remove(player.TrailListLeft.Last());
                    mainWindow.CanvasSpace.Children.Remove(player.TrailListRight.Last());
                    player.TrailListLeft.Remove(player.TrailListLeft.Last());
                    player.TrailListRight.Remove(player.TrailListRight.Last());
                }
        }

        private void ActivateLightshowButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow.lightshowIsActivated) mainWindow.lightshowIsActivated = false;
            else mainWindow.lightshowIsActivated = true;
        }

        private void HighlightRandomPersonButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllSize(5, false);
            ChangeAllColor(Brushes.White);
            var randomPlayer = mainWindow.Players[new Random().Next(0, mainWindow.Players.Count)];
            ChangeColor(randomPlayer, randomPlayer.LeftHand.Stroke);
            ChangeAllLeaderboardColor(Brushes.White);
            ChangeLeaderboardColor(randomPlayer, randomPlayer.LeftHand.Stroke);
            ChangeSize(randomPlayer, 20);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                Dispatcher.Invoke(() => { mainWindow.CanvasSpace.RenderTransform = new RotateTransform(new Random().Next(-360, 360), mainWindow.CanvasSpace.Width / 2, mainWindow.CanvasSpace.Height / 2); });

                //for (var i = 0; i < 360; i++)
                //{
                //    Dispatcher.Invoke(() => { mainWindow.CanvasSpace.RenderTransform = new RotateTransform(i, mainWindow.CanvasSpace.Width / 2, mainWindow.CanvasSpace.Height / 2); });
                //    await Task.Delay(5000 / 360);
                //}
            });

        }

        private void ShowDebugButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow.SongTimeLabel.Visibility == Visibility.Visible)
            {
                mainWindow.SongTimeLabel.Visibility = Visibility.Hidden;
                mainWindow.SpeedLabel.Visibility = Visibility.Hidden;
                mainWindow.TimeLabel.Visibility = Visibility.Hidden;
                mainWindow.TimeLabelLead.Visibility = Visibility.Hidden;

                mainWindow.LeadLabelText.Visibility = Visibility.Hidden;
                mainWindow.SpeedLabelText.Visibility = Visibility.Hidden;
                mainWindow.SubsLabelText.Visibility = Visibility.Hidden;
                mainWindow.TimeLabelText.Visibility = Visibility.Hidden;
                mainWindow.SongNameLabel.Visibility = Visibility.Hidden;
                mainWindow.LeadNameLabelText.Visibility = Visibility.Hidden;
            }
            else
            {
                mainWindow.SongTimeLabel.Visibility = Visibility.Visible;
                mainWindow.SpeedLabel.Visibility = Visibility.Visible;
                mainWindow.TimeLabel.Visibility = Visibility.Visible;
                mainWindow.TimeLabelLead.Visibility = Visibility.Visible;

                mainWindow.LeadLabelText.Visibility = Visibility.Visible;
                mainWindow.SpeedLabelText.Visibility = Visibility.Visible;
                mainWindow.SubsLabelText.Visibility = Visibility.Visible;
                mainWindow.TimeLabelText.Visibility = Visibility.Visible;
                mainWindow.SongNameLabel.Visibility = Visibility.Visible;
                mainWindow.LeadNameLabelText.Visibility = Visibility.Visible;
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            new PreparationWindow().Show();
            mainWindow.Close();
            this.Close();
        }
    }
}
