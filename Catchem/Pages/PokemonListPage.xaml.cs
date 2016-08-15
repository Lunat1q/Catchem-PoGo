using System.Windows;
using Catchem.Classes;
using Catchem.Interfaces;
using POGOProtos.Enums;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for PokemonListPage.xaml
    /// </summary>
    public partial class PokemonListPage : IBotPage
    {
        private BotWindowData _bot;

        public PokemonListPage()
        {
            InitializeComponent();
        }

        public void SetBot(BotWindowData bot)
        {
            _bot = bot;
            UpdateLists();
        }

        public void ClearData()
        {
            ToEvolveList.ItemsSource = null;
            NotToTransferList.ItemsSource = null;
            PokemonsNotToCatchList.ItemsSource = null;
            PokemonToUseMasterballList.ItemsSource = null;
        }

        public void UpdateLists()
        {
            ToEvolveList.ItemsSource = _bot.PokemonsToEvolve;
            NotToTransferList.ItemsSource = _bot.PokemonsNotToTransfer;
            PokemonsNotToCatchList.ItemsSource = _bot.PokemonsNotToCatch;
            PokemonToUseMasterballList.ItemsSource = _bot.PokemonToUseMasterball;
        }

        private void AddPokemonToEvolve_Click(object sender, RoutedEventArgs e)
        {
            if (AddToEvolveCb.SelectedIndex <= -1) return;
            var pokemonId = (PokemonId)AddToEvolveCb.SelectedItem;
            if (!_bot.PokemonsToEvolve.Contains(pokemonId))
                _bot.PokemonsToEvolve.Add(pokemonId);
            AddToEvolveCb.SelectedIndex = -1;
        }

        private void NotToTransferBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NotToTransferCb.SelectedIndex > -1)
            {
                var pokemonId = (PokemonId)NotToTransferCb.SelectedItem;
                if (!_bot.PokemonsNotToTransfer.Contains(pokemonId))
                    _bot.PokemonsNotToTransfer.Add(pokemonId);
                NotToTransferCb.SelectedIndex = -1;
            }
        }
        private void PokemonsNotToCatchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PokemonsNotToCatchCb.SelectedIndex > -1)
            {
                var pokemonId = (PokemonId)PokemonsNotToCatchCb.SelectedItem;
                if (!_bot.PokemonsNotToCatch.Contains(pokemonId))
                    _bot.PokemonsNotToCatch.Add(pokemonId);
                PokemonsNotToCatchCb.SelectedIndex = -1;
            }
        }

        private void PokemonToUseMasterballBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PokemonToUseMasterballCb.SelectedIndex > -1)
            {
                var pokemonId = (PokemonId)PokemonToUseMasterballCb.SelectedItem;
                if (!_bot.PokemonToUseMasterball.Contains(pokemonId))
                    _bot.PokemonToUseMasterball.Add(pokemonId);
                PokemonToUseMasterballCb.SelectedIndex = -1;
            }
        }
    }
}
