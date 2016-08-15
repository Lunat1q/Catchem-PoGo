using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Catchem.Classes;
using POGOProtos.Enums;

namespace Catchem
{
    public partial class Styles
    {
        private void btn_botStop_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var bot = btn?.DataContext as BotWindowData;
            if (bot == null) return;
            bot.Stop();
            MainWindow.BotWindow.ClearPokemonData(bot);
        }

        private void btn_botStart_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var bot = btn?.DataContext as BotWindowData;
            bot?.Start();
        }

        private void btn_removeFromList_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.DataContext == null) return;
            var pokemonId = (PokemonId)btn.DataContext;
            var parentList = btn.Tag as ListBox;
            var source = parentList?.ItemsSource as ObservableCollection<PokemonId>;
            if (source != null && source.Contains(pokemonId))
                source.Remove(pokemonId);
        }
    }
}
