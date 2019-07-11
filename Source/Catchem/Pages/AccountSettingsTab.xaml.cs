using System.Diagnostics;
using System.Windows.Navigation;
using Catchem.Classes;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for AccountSettingsTab.xaml
    /// </summary>
    public partial class AccountSettingsTab
    {
        public AccountSettingsTab()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        public void SetBot(BotWindowData bot)
        {
            BotSettingsPage.SetBot(bot);
            BotPlayerPage.SetBot(bot);
            BotPokemonListPage.SetBot(bot);
            BotMapPage.SetBot(bot);
            BotPokePage.SetBot(bot);
            BotPokedexPage.SetBot(bot);
        }

        public void ClearData()
        {
            BotPlayerPage?.ClearData();
            BotMapPage?.ClearData();
            BotPokePage?.ClearData();
            BotPokedexPage?.ClearData();
        }

        public void SetData()
        {
            BotPokePage?.UpdateLists();
            BotPokedexPage?.UpdateLists();
        }
    }
}
