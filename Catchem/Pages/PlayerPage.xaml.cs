using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Catchem.Classes;
using Catchem.Extensions;
using Catchem.Interfaces;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using POGOProtos.Enums;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for PlayerPage.xaml
    /// </summary>
    public partial class PlayerPage : IBotPage
    {
        private BotWindowData _bot;
        private ISession CurSession => _bot.Session;
        private bool _loadingUi;
        private bool _inRefresh;
        private bool _inRefreshItems;
        public PlayerPage()
        {
            InitializeComponent();
        }

        public void SetBot(BotWindowData bot)
        {
            _loadingUi = true;
            _bot = bot;
            UpdatePlayerTab();
            UpdateLists();
            _loadingUi = false;
            UpdateRunTimeData();
        }

        private void DoPresorting()
        {
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.SelectedItems.Clear();
        }

        private void SortByCpClick(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Cp", ListSortDirection.Descending));
        }

        private void sortById_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("PokemonId", ListSortDirection.Ascending));
        }

        private void sortByCatch_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Timestamp", ListSortDirection.Ascending));
        }

        private void SortByIvClick(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Iv", ListSortDirection.Descending));
        }

        private void sortByAz_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private async void RefreshPokemons()
        {
            if (_bot == null || !_bot.Started || _inRefresh) return;
            _inRefresh = true;
            Action<IEvent> action = (evt) => _bot.Session.EventDispatcher.Send(evt);
            await PokemonListTask.Execute(_bot.Session, action);
            _inRefresh = false;
        }

        private void mi_recycleItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemListBox.SelectedIndex == -1 || !_bot.Started) return;
            var item = GetSelectedItem();
            if (item == null) return;
            int amount;
            var inputDialog = new SupportForms.InputDialog("Please, enter amout to recycle:", "1", true);
            if (inputDialog.ShowDialog() != true) return;
            if (int.TryParse(inputDialog.Answer, out amount))
                RecycleItem(CurSession, item, amount, _bot.CancellationToken);

        }

        private async void RecycleItem(ISession session, ItemUiData item, int amount, CancellationToken cts)
        {
            await RecycleSpecificItemTask.Execute(session, item.Id, amount, cts);
        }

        private ItemUiData GetSelectedItem()
        {
            return (ItemUiData)ItemListBox.SelectedItem;
        }

        public void UpdatePlayerTeam()
        {
            switch (_bot.Team)
            {
                case TeamColor.Neutral:
                    team_image.Source = Properties.Resources.team_neutral.LoadBitmap();
                    break;
                case TeamColor.Blue:
                    team_image.Source = Properties.Resources.team_mystic.LoadBitmap();
                    break;
                case TeamColor.Red:
                    team_image.Source = Properties.Resources.team_valor.LoadBitmap();
                    break;
                case TeamColor.Yellow:
                    team_image.Source = Properties.Resources.team_instinct.LoadBitmap();
                    break;
            }
        }

        public void UpdatePlayerTab()
        {
            l_coins.Content = _bot.Coins;
            Playername.Content = _bot.PlayerName;
            UpdatePlayerTeam();
            l_poke_inventory.Content = $"({_bot.PokemonList.Count}/{_bot.MaxPokemonStorageSize})";
            l_inventory.Content = $"({_bot.ItemList.Sum(x => x.Amount)}/{_bot.MaxItemStorageSize})";
            l_StarDust.Content = _bot.StartStarDust;
            _bot.StarDust = _bot.StartStarDust;
        }

        public void UpdatePokemons()
        {
            if (_bot != null && PokeListBox != null)
                PokeListBox.ItemsSource = _bot?.PokemonList;
        }

        public void UpdateItems()
        {
            if (_bot != null && ItemListBox != null)
                ItemListBox.ItemsSource = _bot.ItemList;
        }

        public void UpdateLists()
        {
            PokeListBox.ItemsSource = _bot.PokemonList;
            ItemListBox.ItemsSource = _bot.ItemList;
        }

        public void UpdateInventoryCount()
        {
            l_inventory.Content = $"({_bot.ItemList.Sum(x => x.Amount)}/{_bot.MaxItemStorageSize})";
        }

        public void UpdatePokemonsCount()
        {
            l_poke_inventory.Content = $"({_bot.PokemonList.Count}/{_bot.MaxPokemonStorageSize})";
        }

        public void UpdateRunTimeData()
        {
            var farmedDust = _bot.Session?.Stats?.TotalStardust == 0 ? 0 : _bot.Session?.Stats?.TotalStardust - _bot.StartStarDust;
            var dustpH = farmedDust / _bot.Ts.TotalHours;
            if (dustpH != null)
            {
                var farmedDustH = _bot?.Ts.TotalHours < 0.001 ? "~" : ((double)dustpH).ToString("0");
                l_Stardust_farmed.Content = $"{farmedDust} ({farmedDustH}/h)";
            }
            if (_bot.Session?.Stats?.ExportStats == null) return;
            if (_bot.Session?.Stats.TotalStardust > 0)
                _bot.StarDust = _bot.Session.Stats.TotalStardust;
            l_xp.Content = _bot.Session?.Stats.ExportStats.CurrentXp;
            l_xp_farmed.Content = _bot.Session?.Stats.TotalExperience;
            l_Pokemons_farmed.Content = _bot.Session?.Stats.TotalPokemons;
            l_Pokemons_transfered.Content = _bot.Session?.Stats.TotalPokemonsTransfered;
            l_Pokestops_farmed.Content = _bot.Session?.Stats.TotalPokestops;
            l_level.Content = _bot.Session?.Stats.ExportStats.Level;
            NextLevelInTextBox.Text =
                $"{_bot.Session?.Stats.ExportStats.HoursUntilLvl.ToString("00")}:{_bot.Session?.Stats.ExportStats.MinutesUntilLevel.ToString("00")} ({_bot.Session?.Stats.ExportStats.CurrentXp}/{_bot.Session?.Stats.ExportStats.LevelupXp})";
            LevelProgressBar.Value = (int)(_bot.Session?.Stats.ExportStats.CurrentXp*100/_bot.Session?.Stats.ExportStats.LevelupXp);
        }

        public void ClearData()
        {
            PokeListBox.ItemsSource = null;
            ItemListBox.ItemsSource = null;
        }

        private void mi_refreshItems_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || !_bot.Started || _inRefreshItems) return;
            RefreshItems();
        }

        private async void RefreshItems()
        {
            _inRefreshItems = true;
            Action<IEvent> action = (evt) => CurSession.EventDispatcher.Send(evt);
            await InventoryListTask.Execute(CurSession, action);
            _inRefreshItems = false;
        }

        private async void SelectTeam(TeamColor clr)
        {
            await SetPlayerTeamTask.Execute(CurSession, clr);
        }

        private void refreshPokemonList_Click(object sender, RoutedEventArgs e)
        {
            RefreshPokemons();
        }

        private void sortByFav_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Favoured", ListSortDirection.Descending));
        }
        private void sortByCandy_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Candy", ListSortDirection.Descending));
        }
        private void team_image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_bot == null || !_bot.Started || _bot.Team != TeamColor.Neutral || _bot.Level < 5) return;
            var inputDialog = new SupportForms.InputDialog("Please, select a team:", null, false, 0, new List<object>{TeamColor.Blue, TeamColor.Yellow, TeamColor.Red});
            if (inputDialog.ShowDialog() != true || inputDialog.ObjectAnswer == null) return;
            var team = (TeamColor)inputDialog.ObjectAnswer;
            SelectTeam(team);
        }

        private void PokeWrapper_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var uGrid = sender as UniformGrid;
            if (uGrid == null) return;
            uGrid.Columns = (int)(uGrid.ActualWidth/150);
        }

        private void ManualMaintenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || !_bot.Started) return;
            _bot.Session.Runtime.StopsHit = 999;
        }


    }
}
