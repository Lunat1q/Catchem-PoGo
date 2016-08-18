using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
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

        private void SortByCpClick(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Cp", ListSortDirection.Descending));
        }

        private void sortById_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("PokemonId", ListSortDirection.Ascending));
        }

        private void sortByCatch_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Timestamp", ListSortDirection.Ascending));
        }

        private void SortByIvClick(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Iv", ListSortDirection.Descending));
        }

        private void sortByAz_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void mi_evolvePokemon_Click(object sender, RoutedEventArgs e)
        {
            if (PokeListBox.SelectedIndex == -1) return;
            var pokemon = GetSelectedPokemon();
            if (pokemon == null) return;
            EvolvePokemon(CurSession, pokemon);
        }

        private void mi_transferPokemon_Click(object sender, RoutedEventArgs e)
        {
            if (PokeListBox.SelectedIndex == -1) return;
            var pokemon = GetSelectedPokemon();
            if (pokemon == null) return;
            TransferPokemon(CurSession, pokemon);
        }

        private void mi_levelupPokemon_Click(object sender, RoutedEventArgs e)
        {
            if (PokeListBox.SelectedIndex == -1) return;
            var pokemon = GetSelectedPokemon();
            if (pokemon == null) return;
            LevelUpPokemon(CurSession, pokemon);
            UpdateRunTimeData();
        }
        private void mi_maxlevelupPokemon_Click(object sender, RoutedEventArgs e)
        {
            if (PokeListBox.SelectedIndex == -1) return;
            var pokemon = GetSelectedPokemon();
            if (pokemon == null) return;
            LevelUpPokemon(CurSession, pokemon, true);
            UpdateRunTimeData();
        }

        private void mi_recycleItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemListBox.SelectedIndex == -1) return;
            var item = GetSelectedItem();
            if (item == null) return;
            int amount;
            var inputDialog = new SupportForms.InputDialogSample("Please, enter amout to recycle:", "1", true);
            if (inputDialog.ShowDialog() != true) return;
            if (int.TryParse(inputDialog.Answer, out amount))
                RecycleItem(CurSession, item, amount, _bot.CancellationToken);
        }

        private static async void EvolvePokemon(ISession session, PokemonUiData pokemon)
        {
            await EvolveSpecificPokemonTask.Execute(session, pokemon.Id);
        }

        private static async void LevelUpPokemon(ISession session, PokemonUiData pokemon, bool toMax = false)
        {
            await LevelUpSpecificPokemonTask.Execute(session, pokemon.Id, toMax);
        }

        private static async void TransferPokemon(ISession session, PokemonUiData pokemon)
        {
            await TransferPokemonTask.Execute(session, pokemon.Id);
        }

        private static async void RecycleItem(ISession session, ItemUiData item, int amount, CancellationToken cts)
        {
            await RecycleSpecificItemTask.Execute(session, item.Id, amount, cts);
        }

        private ItemUiData GetSelectedItem()
        {
            return (ItemUiData)ItemListBox.SelectedItem;
        }

        private PokemonUiData GetSelectedPokemon()
        {
            return (PokemonUiData)PokeListBox.SelectedItem;
        }

        public void UpdatePlayerTab()
        {
            l_coins.Content = _bot.Coins;
            Playername.Content = _bot.PlayerName;
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
            l_poke_inventory.Content = $"({_bot.PokemonList.Count}/{_bot.MaxPokemonStorageSize})";
            l_inventory.Content = $"({_bot.ItemList.Sum(x => x.Amount)}/{_bot.MaxItemStorageSize})";
            l_StarDust.Content = _bot.StartStarDust;
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
            var farmedDust = _bot.Stats?.TotalStardust == 0 ? 0 : _bot.Stats?.TotalStardust - _bot.StartStarDust;
            var dustpH = farmedDust / _bot.Ts.TotalHours;
            if (dustpH != null)
            {
                var farmedDustH = _bot?.Ts.TotalHours < 0.001 ? "~" : ((double)dustpH).ToString("0");
                l_Stardust_farmed.Content = $"{farmedDust} ({farmedDustH}/h)";
            }
            l_xp.Content = _bot.Stats?.ExportStats?.CurrentXp;
            l_xp_farmed.Content = _bot.Stats?.TotalExperience;
            l_Pokemons_farmed.Content = _bot.Stats?.TotalPokemons;
            l_Pokemons_transfered.Content = _bot.Stats?.TotalPokemonsTransfered;
            l_Pokestops_farmed.Content = _bot.Stats?.TotalPokestops;
            l_level.Content = _bot.Stats?.ExportStats?.Level;
            l_level_nextime.Content = $"{_bot.Stats?.ExportStats?.HoursUntilLvl.ToString("00")}:{_bot.Stats?.ExportStats?.MinutesUntilLevel.ToString("00")}";
        }

        public void ClearData()
        {
            PokeListBox.ItemsSource = null;
            ItemListBox.ItemsSource = null;
        }

        private void mi_refreshPokemonList_Click(object sender, RoutedEventArgs e)
        {
            RefreshPokemons();
        }

        private async void RefreshPokemons()
        {
            Action<IEvent> action = (evt) => CurSession.EventDispatcher.Send(evt);
            await PokemonListTask.Execute(CurSession, action);
        }

        private void mi_refreshItems_Click(object sender, RoutedEventArgs e)
        {
            RefreshItems();
        }

        private async void RefreshItems()
        {
            Action<IEvent> action = (evt) => CurSession.EventDispatcher.Send(evt);
            await InventoryListTask.Execute(CurSession, action);
        }
    }
}
