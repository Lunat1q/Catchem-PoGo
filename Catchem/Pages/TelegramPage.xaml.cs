using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Catchem.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Enums;
using POGOProtos.Enums;
using Path = System.IO.Path;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for TelegramPage.xaml
    /// </summary>
    public partial class TelegramPage : UserControl
    {

        private bool _tlgrmWorking = false;
        private TelegramSettings _tlgrmSettings;
        private const string TlgrmFilePath = "tlgrm.json";
        public bool LoadingUi { get; set; }

        public TelegramPage()
        {
            InitializeComponent();
            ReadTelegramSettings();
            LoadSettings();

            if (_tlgrmSettings.AutoStart)
            {
                
            }
        }

        private void LoadSettings()
        {
            LoadingUi = true;

            PokemonList.ItemsSource = _tlgrmSettings.AutoReportPokemon;

            foreach (var uiElem in TlgrmGrid.GetLogicalChildCollection<TextBox>())
            {
                string val;
                if (UiHandlers.GetValueByName(uiElem.Name.Substring(2), _tlgrmSettings, out val))
                    uiElem.Text = val;
            }

            foreach (var uiElem in TlgrmGrid.GetLogicalChildCollection<PasswordBox>())
            {
                string val;
                if (UiHandlers.GetValueByName(uiElem.Name.Substring(2), _tlgrmSettings, out val))
                    uiElem.Password = val;
            }

            foreach (var uiElem in TlgrmGrid.GetLogicalChildCollection<CheckBox>())
            {
                bool val;
                if (UiHandlers.GetValueByName(uiElem.Name.Substring(2), _tlgrmSettings, out val))
                    uiElem.IsChecked = val;
            }

            foreach (var uiElem in TlgrmGrid.GetLogicalChildCollection<ComboBox>())
            {
                Enum val;
                if (UiHandlers.GetValueByName(uiElem.Name.Substring(2), _tlgrmSettings, out val))
                {
                    var valType = val.GetType();
                    uiElem.ItemsSource = Enum.GetValues(valType);
                    uiElem.SelectedItem = val;
                }
            }

            LoadingUi = false;
        }

        


        private void ReadTelegramSettings()
        {
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), TlgrmFilePath);
            if (File.Exists(settingsPath))
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.Converters.Add(new StringEnumConverter {CamelCaseText = true});
                jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;

                _tlgrmSettings = SerializeUtils.DeserializeDataJson<TelegramSettings>(settingsPath);
                return;
            }
            _tlgrmSettings = new TelegramSettings();
        }

        private Queue<TelegramCommand> _commandsQueue = new Queue<TelegramCommand>();

        public void TelegramMessageReceiver(string message)
        {
            var messageFractions = message.Split(' ');
            if (messageFractions.Length < 1) return;
            var cmd = new TelegramCommand
            {
                Command = messageFractions[0],
                Args = messageFractions.Where((x, i) => i > 0).ToArray(),
                Time = DateTime.Now
            };
        }



        public class TelegramCommand
        {
            public string Command;
            public string[] Args;
            public DateTime Time;
        }

        public class TelegramSettings
        {
            public ObservableCollection<PokemonId> AutoReportPokemon = new ObservableCollection<PokemonId>
            {
                PokemonId.Mew,
                PokemonId.Mewtwo
            };
            public bool AutoStart = false;
            public string ApiKey = "";
        }

        private void CbAutoStart_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void AddPokemonToReport_Click(object sender, RoutedEventArgs e)
        {
            
            if (AddToAutoReport.SelectedIndex <= -1) return;
            var pokemonId = (PokemonId)AddToAutoReport.SelectedItem;
            if (!_tlgrmSettings.AutoReportPokemon.Contains(pokemonId))
                _tlgrmSettings.AutoReportPokemon.Add(pokemonId);
            AddToAutoReport.SelectedIndex = -1;
        }
    }
}
