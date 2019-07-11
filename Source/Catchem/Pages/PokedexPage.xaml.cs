using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Catchem.Interfaces;
using Catchem.Classes;
using Catchem.UiTranslation;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for PokedexPage.xaml
    /// </summary>
    public partial class PokedexPage : IBotPage
    {
        private BotWindowData _bot;
        private bool _loadingUi;

        private readonly Dictionary<string, bool> _enabledSortings = new Dictionary<string, bool>();

        public PokedexPage()
        {
            InitializeComponent();
        }

        public void SetBot(BotWindowData bot)
        {
            _loadingUi = true;
            _bot = bot;
            UpdatePokedexCount();
            UpdateLists();
            _loadingUi = false;
        }

        public void ClearData()
        {
            if (PokedexListBox != null)
                PokedexListBox.ItemsSource = null;
        }

        public void UpdateLists()
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                PokedexListBox.ItemsSource = _bot.PokeDex;
            }));
        }

        private void PokedexListBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var uGrid = sender as UniformGrid;
            if (uGrid == null) return;
            uGrid.Columns = (int)(uGrid.ActualWidth / 150);
        }

        public void UpdatePokedexCount()
        {
            Dispatcher.Invoke(new ThreadStart( delegate
            {
                var seen = _bot?.PokeDex?.Count(x => x.Seen);
                var caught = _bot?.PokeDex?.Count(x => x.Captured);
                var seenTranslation = TranslationEngine.GetDynamicTranslationString("%SEEN%", "Seen");
                var capturedTranslation = TranslationEngine.GetDynamicTranslationString("%CAUGHT%", "Caught");
                var totalTranslation = TranslationEngine.GetDynamicTranslationString("%TOTAL%", "Total");
                PokedexStatusText.Text = $"{seenTranslation}: {seen} / {capturedTranslation}: {caught} / {totalTranslation}: 151";
            }));
        }

        private void SortBySeen_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button, "SeenTimes");
        }

        private void sortById_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button, "Id", false);
        }

        private void sortByAz_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button, "PokemonName", false);
        }

        private void DoPresorting()
        {
            PokedexListBox.SelectedItems.Clear();
        }

        private void ToggleSortByKey(Button btn, string sortKey, bool desc = true)
        {
            if (btn == null) return;
            if (!_enabledSortings.ContainsKey(sortKey))
            {
                var nextColor = FindResource("LightBlueButtonColor") as LinearGradientBrush;
                btn.Background = nextColor;
                _enabledSortings.Add(sortKey, true);
                PokedexListBox.Items.SortDescriptions.Add(new SortDescription(sortKey, desc ? ListSortDirection.Descending : ListSortDirection.Ascending));
            }
            else
            {
                var nextColor = FindResource("NormalButtonColor") as LinearGradientBrush;
                btn.Background = nextColor;
                _enabledSortings.Remove(sortKey);
                PokedexListBox.Items.SortDescriptions.Remove(
                    PokedexListBox.Items.SortDescriptions.FirstOrDefault(x => x.PropertyName == sortKey));
            }
        }

        private void SortByCaught_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || _loadingUi) return;
            DoPresorting();
            ToggleSortByKey(sender as Button, "CapturedTimes");
        }
    }
}
