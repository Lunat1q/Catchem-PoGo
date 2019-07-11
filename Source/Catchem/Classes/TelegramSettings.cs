using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Enums;

namespace Catchem.Classes
{
    public class TelegramSettings
    {
        public const string TlgrmFilePath = "tlgrm.json";
        private const string ConfFolder = "Config";
        public ObservableCollection<PokemonId> AutoReportPokemon = new ObservableCollection<PokemonId>();
        public ObservableCollection<TelegramBotOwner> Owners = new ObservableCollection<TelegramBotOwner>();
        public bool AutoStart = false;
        public bool AutoReportSelectedPokemon = false;
        public string ApiKey = "";
        public int ReportAllPokemonsAboveCp = 0;
        public int ReportAllPokemonsAboveIv = 0;
        public bool ReportLevelUp = false;
        public bool ReportCaptcha = false;
        [JsonIgnore]
        public bool UseCpReport => (ReportAllPokemonsAboveCp > 0);
        [JsonIgnore]
        public bool UseIvReport => (ReportAllPokemonsAboveIv > 0);


        public void Load()
        {
            try
            {
                var oldSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), TlgrmFilePath);
                var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ConfFolder, TlgrmFilePath);

                if (File.Exists(oldSettingsPath))
                {
                    if (File.Exists(settingsPath))
                    {
                        File.Move(settingsPath, settingsPath + ".bak");
                    }
                    File.Move(oldSettingsPath, settingsPath);
                    Task.Delay(1000);
                }

                if (File.Exists(settingsPath))
                {
                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.Converters.Add(new StringEnumConverter {CamelCaseText = true});
                    jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;

                    var input = File.ReadAllText(settingsPath);
                    JsonConvert.PopulateObject(input, this, jsonSettings);
                }
                else
                {
                    SaveSettings();
                }
            }
            catch (Exception)
            {
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), ConfFolder, TlgrmFilePath);
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
            jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;

            this.SerializeDataJson(settingsPath);
        }
    }

    public class TelegramBotOwner : CatchemNotified
    {
        private string _telegramName;

        public string TelegramName
        {
            get { return _telegramName; }
            set
            {
                _telegramName = value;
                OnPropertyChanged();
            }
        }

        private long _chatId;

        public long ChatId
        {
            get { return _chatId; }
            set
            {
                _chatId = value;
                OnPropertyChanged();
            }
        }
    }
}
