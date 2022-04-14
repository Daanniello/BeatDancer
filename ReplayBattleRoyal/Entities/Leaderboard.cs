using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReplayBattleRoyal.Entities
{
    public class Leaderboard
    {
        public List<ListViewItem> listViewItems = new List<ListViewItem>();
        private MainWindow mainWindow;

        public Leaderboard(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            mainWindow.ListViewPlayers.ItemsSource = listViewItems;
        }

        public Player GetLastPlayer()
        {
            var item = listViewItems.OrderByDescending(x => x.Content.ToString().Split(" ")[0].Trim()).Last();
            var player = mainWindow.Players.LastOrDefault(x => item.Content.ToString().Contains(x.Name));
            return player;
        }

        public void RemovePlayer(ListViewItem item)
        {
            listViewItems.Remove(item);
        }

        public ListViewItem GetPlayer(string name)
        {
            return listViewItems.FirstOrDefault(x => x.Content.ToString().Contains(name));
        }

        public void AddPlayer(string Name, Brush color)
        {
            var listViewItem = new ListViewItem() { Content = $"0 0           {Name}", Background = color, FontSize = 30, FontFamily = new System.Windows.Media.FontFamily("Microsoft YaHei UI") };
            listViewItems.Add(listViewItem);
        }

        public void RefreshLeaderboard()
        {
            mainWindow.ListViewPlayers.Items.Refresh();
        }

        public void OrderLeaderboardByAcc()
        {
            var orderedListview = listViewItems.OrderByDescending(x => x.Content.ToString().Split(" ")[0].Trim());
            mainWindow.ListViewPlayers.ItemsSource = orderedListview;
            RefreshLeaderboard();
        }

        public void GivePointsToPlayer(Player player, double currentScore, double currentMaxScore)
        {
            mainWindow.Dispatcher.Invoke(() =>
            {
                var item = GetPlayer(player.Name);
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

                    OrderLeaderboardByAcc();                              
                }
            });
        }
    }
}
