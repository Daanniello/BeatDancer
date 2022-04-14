using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ReplayBattleRoyal.GameModes
{
    public class Gamemode
    {
        public GameModes SelectedGamemode = GameModes.None;
        private MainWindow mainWindow;

        public Gamemode(MainWindow mainWindow, GameModes gamemode)
        {
            this.mainWindow = mainWindow;
            SelectedGamemode = gamemode;
        }

        public string GetIntroText()
        {
            var text = "";

            switch (SelectedGamemode)
            {
                case GameModes.BattleRoyale:
                    text = $"Every couple seconds a player is eliminated";
                break;
                case GameModes.ComboDrop:
                    text = $"Whenever a player loses combo, the player will get eliminated";
               
                    break;
                case GameModes.ComboDropSafe:
                    text = $"Whenever a player loses combo, the player will get eliminated";
                    break;
                case GameModes.PerfectAcc:
                    text = $"Whenever a player hits lower as {mainWindow.perfectAccAmount}, the player gets eliminated";             
                    break;
                default:
                    break;
            }

            return text;
        }

        public void InitializeGamemode()
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                if (SelectedGamemode == Gamemode.GameModes.BattleRoyale) mainWindow.BatteRoyalTimerLabel.Visibility = Visibility.Visible;
                else mainWindow.BatteRoyalTimerLabel.Visibility = Visibility.Hidden;
            });
        }

        public async Task StartBattleRoyal()
        {
            var playerAmount = mainWindow.Players.Count;
            var songDuration = Convert.ToInt32(Math.Round(mainWindow.Players.First().ReplayModel.Frames.Last().A));

            var startAmount = playerAmount;
            do
            {
                var timeToWait = Math.Round((double)(songDuration / startAmount), 1);
                Dispatcher.CurrentDispatcher.Invoke(() => { mainWindow.BatteRoyalTimerLabel.Content = timeToWait; });

                startTimer(timeToWait);

                await Task.Delay(TimeSpan.FromSeconds(timeToWait));

                mainWindow.EliminateLastPlayer();

                playerAmount--;
            } while (playerAmount > 1);

            async void startTimer(double timeToWait)
            {
                for (var i = 0; i < timeToWait; i++)
                {
                    await Task.Delay(1000);
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        var currentTime = Convert.ToDouble(mainWindow.BatteRoyalTimerLabel.Content);
                        mainWindow.BatteRoyalTimerLabel.Content = currentTime - 1;
                    });
                }
            }
        }

        public enum GameModes
        {
            None,
            BattleRoyale,
            ComboDrop,
            ComboDropSafe,
            PerfectAcc
        }
    }
}
