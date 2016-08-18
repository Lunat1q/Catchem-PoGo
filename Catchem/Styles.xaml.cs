using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Catchem.Classes;
using POGOProtos.Enums;

namespace Catchem
{
    public partial class Styles
    {
        //private void btn_botStop_Click(object sender, RoutedEventArgs e)
        //{
        //    var btn = sender as Button;
        //    var bot = btn?.DataContext as BotWindowData;
        //    if (bot == null) return;
        //    bot.Stop();
        //    MainWindow.BotWindow.ClearPokemonData(bot);
        //}

        private void btn_botStart_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var bot = btn?.DataContext as BotWindowData;
            if (bot == null) return;
            if (bot.Started)
            {
                bot.Stop();
                btn.Background = new LinearGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop
                        {
                            Color = Color.FromArgb(255,83 ,192,177),
                            Offset = 1
                        },
                        new GradientStop
                        {
                            Color = Color.FromArgb(255,176,238,156),
                            Offset = 0
                        }
                    }
                };
                btn.Content = "START";
            }
            else
            {
                bot.Start();
                btn.Background = new LinearGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop
                        {
                            Color = Color.FromArgb(255,192,79 ,83 ),
                            Offset = 1
                        },
                        new GradientStop
                        {
                            Color = Color.FromArgb(255,238,178,156),
                            Offset = 0
                        }
                    }
                };
                btn.Content = "STOP";
            }
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
