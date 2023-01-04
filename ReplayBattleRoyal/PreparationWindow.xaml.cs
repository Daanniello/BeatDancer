using ReplayBattleRoyal.GameModes;
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

namespace ReplayBattleRoyal
{
    /// <summary>
    /// Interaction logic for PreparationWindow.xaml
    /// </summary>
    public partial class PreparationWindow : Window
    {
        public PreparationWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var songStartSpeed = Convert.ToDouble(SpeedFactorTextBox.Text);
            var songID = Convert.ToInt32(MapScoresaberIDTextbox.Text);
            var playerCount = Convert.ToInt32(PlayerAmountTextbox.Text);
            string country = CountryCodeTextbox.Text;
            if (country == "") country = null;
            var streamMode = false;
            if ((bool)StreamModeCheckBox.IsChecked) streamMode = true;

            var gameSpace = new MainWindow();
            gameSpace.Start(songStartSpeed, songID, playerCount, country, streamMode, Gamemode.GameModes.None, useBackgroundVideo: false, backgrounVideoDelay: -1850);
            gameSpace.Show();
            this.Hide();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }
    }
}
