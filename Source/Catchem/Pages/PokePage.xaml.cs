using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Catchem.Classes;
using Catchem.Interfaces;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Tasks;
using POGOProtos.Enums;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for PlayerPage.xaml
    /// </summary>
    public partial class PokePage : IBotPage
    {
        private BotWindowData _bot;
        private bool _loadingUi;
        private bool _inRefresh;
        public PokePage()
        {
            InitializeComponent();
        }

        private readonly Dictionary<string, bool> _enabledSortings = new Dictionary<string, bool>();

        public void SetBot(BotWindowData bot)
        {
            _loadingUi = true;
            _bot = bot;
            UpdatePokemonsCount();
            UpdateLists();
            _loadingUi = false;
        }

        private void DoPresorting()
        {
            //PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.SelectedItems.Clear();
        }

        private void SortByCpClick(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button,"Cp");
        }

        private void ToggleSortByKey(Button btn, string sortKey, bool desc = true)
        {
            if (btn == null) return;
            if (!_enabledSortings.ContainsKey(sortKey))
            {
                var nextColor = FindResource("LightBlueButtonColor") as LinearGradientBrush;
                btn.Background = nextColor;
                _enabledSortings.Add(sortKey, true);
                PokeListBox.Items.SortDescriptions.Add(new SortDescription(sortKey, desc ? ListSortDirection.Descending : ListSortDirection.Ascending));
            }
            else
            {
                var nextColor = FindResource("NormalButtonColor") as LinearGradientBrush;
                btn.Background = nextColor;
                _enabledSortings.Remove(sortKey);
                PokeListBox.Items.SortDescriptions.Remove(
                    PokeListBox.Items.SortDescriptions.FirstOrDefault(x => x.PropertyName == sortKey));
            }
        }

        private void sortById_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button, "PokemonId", false);
        }

        private void sortByCatch_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button,"Timestamp");
        }

        private void SortByIvClick(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button,"Iv");
        }

        private void sortByAz_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button,"Name", false);
        }

        private void sortByFav_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button,"Favoured");
        }
        private void sortByCandy_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button,"Candy");
        }

        private void sortByDps_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button,"SumDps");
        }

        private void SortByMaxCp_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button, "MaxCp");
        }

        private void SortByPossibleCp_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button, "PossibleCp");
        }

        private void SortByLevel_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button, "Level");
        }

        private async void RefreshPokemons()
        {
            if (_bot == null || !_bot.Started || _inRefresh) return;
            _inRefresh = true;
            Action<IEvent> action = (evt) => _bot.Session.EventDispatcher.Send(evt);
            await PokemonListTask.Execute(_bot.Session, action);
            _inRefresh = false;
        }

        public void UpdatePokemons()
        {
            if (_bot != null && PokeListBox != null)
            {
                Dispatcher.Invoke(new ThreadStart(delegate
                {
                    PokeListBox.ItemsSource = _bot?.PokemonList;
                }));
            }
        }

        public void UpdateLists()
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                PokeListBox.ItemsSource = _bot.PokemonList;
                EggBox.ItemsSource = _bot.EggList;
                ManualActionsList.ItemsSource = _bot.ActionList;
            }));
        }

        public void UpdatePokemonsCount()
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                PokeInventoryStatus.Text = $"({_bot.PokemonList.Count}/{_bot.MaxPokemonStorageSize})";
                PokeEggsCount.Text = _bot.EggList != null ? $"({_bot.EggList.Count}/9)" : "(?/9)";
            }));
        }

        public void ClearData()
        {
            if (PokeListBox != null)
                PokeListBox.ItemsSource = null;
            if (EggBox != null)
                EggBox.ItemsSource = null;
        }

        private void refreshPokemonList_Click(object sender, RoutedEventArgs e)
        {
            RefreshPokemons();
            if (EggBox != null && _bot != null)
                EggBox.ItemsSource = _bot?.EggList;
        }

        private void PokeWrapper_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var uGrid = sender as UniformGrid;
            if (uGrid == null) return;
            uGrid.Columns = (int)(uGrid.ActualWidth/150);
        }

        public void UpdateFamilyCandies(PokemonFamilyId familyCandyId, int candyEarnedCount)
        {
            if (_bot == null) return;
            foreach (var poke in _bot.PokemonList.Where(x=>x.Family == familyCandyId))
            {
                poke.Candy += candyEarnedCount;
            }
        }
    }
}
