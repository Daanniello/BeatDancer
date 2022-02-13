using ReplayBattleRoyal.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ReplayBattleRoyal.MainWindow;

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

        public EffectsPanel(MainWindow mainWindow, List<Player> Players)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;

            foreach (var player in Players)
            {
                originalColors.Add(player, player.LeftHand.Stroke);
                originalSize.Add(player, player.TrailListLeft.First().StrokeThickness);
            }
            originalTrailSize = Players.First().TrailListLeft.Count;
            currentPlayerList = Players;

            originalNoteColorLeft = mainWindow.NoteColorLeft;
            originalNoteColorRight = mainWindow.NoteColorRight;
            originalNoteColorLeftArrow = mainWindow.NoteColorLeftArrow;
            originalNoteColorRightArrow = mainWindow.NoteColorRightArrow;
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
            ChangeNoteColors(Brushes.Black, Brushes.White, Brushes.White, Brushes.Black);
            ChangeAllLeaderboardColor(Brushes.White);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var player in currentPlayerList)
            {
                ChangeColor(player, originalColors.FirstOrDefault(x => x.Key == player).Value);
                ChangeSize(player, originalSize.FirstOrDefault(x => x.Key == player).Value);
                SetTrail(player, originalTrailSize);

                var item = mainWindow.listViewItems.FirstOrDefault(x => x.Content.ToString().Contains(player.Name));
                if(item != null) item.Background = originalColors.FirstOrDefault(x => x.Key == player).Value;
            }

            mainWindow.NoteColorLeft = originalNoteColorLeft;
            mainWindow.NoteColorRight = originalNoteColorRight;
            mainWindow.NoteColorLeftArrow = originalNoteColorLeftArrow;
            mainWindow.NoteColorRightArrow = originalNoteColorRightArrow;
        }

        private void MakeEveryoneSmallerButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllSize(-0.5);
        }
        private void MakeEveryoneBiggerButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeAllSize(+0.5);
        }

        public void ChangeAllSize(double value)
        {
            foreach (var player in currentPlayerList) ChangeSize(player, player.TrailListLeft.First().StrokeThickness + value);
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
            mainWindow.NoteColorLeft = left;
            mainWindow.NoteColorRight = right;
            mainWindow.NoteColorLeftArrow = leftArrow;
            mainWindow.NoteColorRightArrow = rightArrow;
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
            var item = mainWindow.listViewItems.FirstOrDefault(x => x.Content.ToString().Contains(player.Name));
            if(item != null) item.Background = color;
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
        private void ChangeColor(Player player, Brush color)
        {
            if (color == null) return;
            player.LeftHand.Fill = color;
            player.LeftHandTip.Stroke = color;
            player.RightHand.Fill = color;
            player.RightHandTip.Stroke = color;

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
    }
}
