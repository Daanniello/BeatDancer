﻿using System;
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

        public string[] text;

        public Gamemode(MainWindow mainWindow, GameModes gamemode)
        {
            this.mainWindow = mainWindow;
            SelectedGamemode = gamemode;

            text = new string[] {
                $"This video shows {mainWindow.Players.Count} Beat Saber plays at once",
                $"This video contains rapid flashes" };

            switch (SelectedGamemode)
            {
                case GameModes.None:
                    break;
                case GameModes.BattleRoyale:
                    text = new string[] {
                $"This video shows {mainWindow.Players.Count} Beat Saber plays at once",
                $"Battle Royale: Every couple seconds a player is eliminated",
                $"This video contains rapid flashes"
            };
                    break;
                case GameModes.ComboDrop:
                    text = new string[] {
                $"This video shows {mainWindow.Players.Count} Beat Saber plays at once",
                $"Combo Drop: Whenever a player loses combo, the player will get eliminated",
                $"This video contains rapid flashes"
            };
                    break;
                case GameModes.ComboDropSafe:
                    text = new string[] {
                $"This video shows {mainWindow.Players.Count} Beat Saber plays at once",
                $"Combo Drop: Whenever a player loses combo, the player will get eliminated",
                $"This video contains rapid flashes"
            };
                    break;
                case GameModes.PerfectAcc:
                    text = new string[] {
                $"This video shows {mainWindow.Players.Count} Beat Saber plays at once",
                $"Perfect Acc: Whenever a player hits lower as {mainWindow.perfectAccAmount}, the player gets eliminated",
                $"This video contains rapid flashes"
            };
                    break;
                default:
                    break;
            }
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
