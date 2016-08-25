using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Catchem.Classes;
using Catchem.Events;
using Catchem.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Enums;
using Path = System.IO.Path;
using TelegramMessageEvent = PoGo.PokeMobBot.Logic.Event.TelegramMessageEvent;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for TelegramPage.xaml
    /// </summary>
    public partial class TelegramPage
    {
        private bool _windowClosing;
        public readonly Telegram TlgrmBot = new Telegram();
        private TelegramSettings _tlgrmSettings;
        private const string TlgrmFilePath = "tlgrm.json";
        private bool _loadingUi;
        private readonly Queue<TelegramCommand> _commandsQueue = new Queue<TelegramCommand>();
        private readonly Queue<string> _logQueue = new Queue<string>();
        private readonly TelegramListener _listener;

        public TelegramPage()
        {
            InitializeComponent();
            ReadTelegramSettings();
            LoadSettings();
            _listener = new TelegramListener(this);
            TlgrmBot.EventDispatcher.EventReceived += evt => _listener.Listen(evt);

            if (_tlgrmSettings.AutoStart)
            {
                TlgrmBot.Start(_tlgrmSettings.ApiKey);
            }
           
            TelegramLogWorker();
            TelegramCommandWorker();
        }
        public void TurnOff()
        {
            _windowClosing = true;
            TlgrmBot?.Stop();
        }
        #region UI handlers

        public void SaveSettings()
        {
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), TlgrmFilePath);
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new StringEnumConverter {CamelCaseText = true});
            jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;

            _tlgrmSettings.SerializeDataJson(settingsPath);
        }

        private void LoadSettings()
        {
            _loadingUi = true;

            PokemonList.ItemsSource = _tlgrmSettings.AutoReportPokemon;
            OwnerBox.ItemsSource = _tlgrmSettings.Owners;

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

            _loadingUi = false;
        }

        private void ElementPropertyChanged(object sender, EventArgs e)
        {
            if (_loadingUi) return;
            UiHandlers.HandleUiElementChangedEvent(sender, _tlgrmSettings);
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

                _tlgrmSettings = SerializeUtils.DeserializeDataJson<TelegramSettings>(settingsPath) ?? new TelegramSettings();
                return;
            }
            _tlgrmSettings = new TelegramSettings();
        }

        private void AddPokemonToReport_Click(object sender, RoutedEventArgs e)
        {

            if (AddToAutoReport.SelectedIndex <= -1) return;
            var pokemonId = (PokemonId) AddToAutoReport.SelectedItem;
            if (!_tlgrmSettings.AutoReportPokemon.Contains(pokemonId))
                _tlgrmSettings.AutoReportPokemon.Add(pokemonId);
            AddToAutoReport.SelectedIndex = -1;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            TlgrmBot?.Start(_tlgrmSettings?.ApiKey);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            TlgrmBot?.Stop();
        }

        private void AddBotOwner_Click(object sender, RoutedEventArgs e)
        {
            var name = BotOwnerTextBox.Text;
            if (name[0] == '@') name = name.Substring(1);
            _tlgrmSettings.Owners.Add(new TelegramBotOwner()
            {
                TelegramName = name
            });
            BotOwnerTextBox.Clear();
        }

        #endregion

        public void TelegramCommandReceiver(string sender, string command, long chatId, string[] args)
        {
            if (!CheckOwner(sender)) return;

            var cmd = new TelegramCommand
            {
                Sender = sender,
                Command = command,
                Args = args ?? new string[0],
                ChatId = chatId,
                Time = DateTime.Now
            };
            _commandsQueue.Enqueue(cmd);
        }

        public void TelegramMessageReceiver(string message)
        {
            if (message == null) return;
            _logQueue.Enqueue(message);
        }

        private void PokemonCaught(PokemonId pokemon, int cp, double iv, string profileName, string botNick, double level, PokemonMove move1, PokemonMove move2)
        {
            if ((!_tlgrmSettings.AutoReportSelectedPokemon || !_tlgrmSettings.AutoReportPokemon.Contains(pokemon)) &&
                (cp <= _tlgrmSettings.ReportAllPokemonsAboveCp || _tlgrmSettings.ReportAllPokemonsAboveCp <= 0)) return;
            string messageToSend = $"[{botNick}]({profileName}) got {pokemon}! CP:{cp}, Iv:{iv.ToN1()}, Level:{level.ToN1()}, Move 1: {move1}, Move 2: {move2}";

            foreach (var owner in _tlgrmSettings.Owners.Where(x=>x.ChatId != 0))
            {
                TlgrmBot.SendToTelegram(messageToSend, owner.ChatId);
            }
        }

        private async void TelegramCommandWorker()
        {
            while (!_windowClosing)
            {
                if (_commandsQueue.Count > 0)
                {
                    var t = _commandsQueue.Dequeue();
                    switch (t.Command)
                    {
                        case "/start":
                            HandleHelp(t.ChatId);
                            RefreshOwnerChatId(t.Sender, t.ChatId);
                            break;
                        case "help":
                            HandleHelp(t.ChatId);
                            break;
                        case "listbots":
                            HandleListBots(t.ChatId);
                            break;
                        case "start":
                            HandleToggle(t.ChatId, true, t.Args);
                            break;
                        case "stop":
                            HandleToggle(t.ChatId, false, t.Args);
                            break;
                        default:
                            HandleUnknownCommand(t.ChatId);
                            break;
                    }
                }
                await Task.Delay(10);
            }
        }

        private bool CheckOwner(string senderName)
        {
            return _tlgrmSettings.Owners.Any(x => x.TelegramName == senderName);
        }

        private void RefreshOwnerChatId(string senderName, long chatId)
        {
            var targetOwner = _tlgrmSettings?.Owners?.FirstOrDefault(x => x.TelegramName == senderName);
            if (targetOwner == null) return;
            targetOwner.ChatId = chatId;
        }

        private void HandleUnknownCommand(long chatId)
        {
            TlgrmBot.SendToTelegram("Unknown command!", chatId);
        }

        private void HandleToggle(long chatId, bool start, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                TlgrmBot.SendToTelegram($"Wrong {(start ? "start" : "stop")} command!", chatId);
                return;
            }
            if (args[0] == "all")
            {
                foreach (var bot in MainWindow.BotsCollection)
                {
                    if (start)
                    {
                        if (!bot.Started)
                        {
                            bot.Start();
                        }
                    }
                    else
                    {
                        if (bot.Started)
                        {
                            bot.Stop();
                        }
                    }
                }
                TlgrmBot.SendToTelegram($"{(start ? "Started" : "Stopped")} all bots", chatId);
                return;
            }
            int botNum;
            if (int.TryParse(args[0], out botNum))
            {
                botNum--;
                var targetBot = MainWindow.BotsCollection.ElementAtOrDefault(botNum);
                if (targetBot != null)
                {
                    if (start)
                        targetBot.Start();
                    else
                        targetBot.Stop();

                    TlgrmBot.SendToTelegram($"Bot {targetBot.ProfileName} {(start ? "started" : "stopped")}!", chatId);
                }
                else
                {
                    TlgrmBot.SendToTelegram("Bot with that index not found!", chatId);
                }
                return;
            }
            TlgrmBot.SendToTelegram($"Wrong {(start ? "start" : "stop")} command!", chatId);
        }

        private void HandleHelp(long chatId)
        {
            var helpMsg = "The following commands are avaliable: \n" +
                                     "- listbots \n" +
                                     "- start [bot Number / all] \n" +
                                     "- stop [bot Number / all]";
            TlgrmBot.SendToTelegram(helpMsg, chatId);
        }

        private void HandleListBots(long chatId)
        {
            var botNumber = 0;
            var botStringBuilder = new StringBuilder();
            botStringBuilder.AppendLine("Current Bots Avaliable:");
            foreach (var bot in MainWindow.BotsCollection)
            {
                botStringBuilder.AppendLine($"{++botNumber}) {bot.ProfileName} [{(bot.Started ? "RUNNING" : "STOPPED")}]");
            }
            if (botNumber == 0)
                TlgrmBot.SendToTelegram("There are no bots created", chatId);
            if (botNumber > 0)
                TlgrmBot.SendToTelegram(botStringBuilder.ToString(), chatId);
        }


        private async void TelegramLogWorker()
        {
            while (!_windowClosing)
            {
                if (_logQueue.Count > 0)
                {
                    var t = _logQueue.Dequeue();
                    LogBox.AppendParagraph(t, Colors.Aquamarine);
                    if (LogBox.Document.Blocks.Count > 200)
                    {
                        var toRemove = LogBox.Document.Blocks.ElementAt(0);
                        LogBox.Document.Blocks.Remove(toRemove);
                    }
                    LogBox.ScrollToEnd();
                }
                await Task.Delay(10);
            }
        }




        public class TelegramCommand
        {
            public string Command;
            public string[] Args;
            public long ChatId;
            public DateTime Time;
            public string Sender { get; set; }
        }

        public class TelegramSettings
        {
            public ObservableCollection<PokemonId> AutoReportPokemon = new ObservableCollection<PokemonId>();
            public ObservableCollection<TelegramBotOwner> Owners = new ObservableCollection<TelegramBotOwner>();
            public bool AutoStart = false;
            public bool AutoReportSelectedPokemon = false;
            public string ApiKey = "";
            public int ReportAllPokemonsAboveCp = 0;
        }

        public class TelegramBotOwner : CatchemNotified
        {
            private string _telegramName;
            public string TelegramName
            {
                get { return _telegramName; }
                set { _telegramName = value;
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

        public class TelegramListener
        {
            public TelegramListener(TelegramPage receiver)
            {
                _receiver = receiver;
            }

            private readonly TelegramPage _receiver;

            private void HandleEvent(TelegramMessageEvent eve)
            {
                _receiver.TelegramMessageReceiver($"[{DateTime.Now.ToString("hh:mm:ss")}] " + eve.Message);
            }

            private void HandleEvent(TelegramCommandEvent eve)
            {
                _receiver.TelegramCommandReceiver(eve.Sender, eve.Command, eve.ChatId, eve.Args);
            }

            private void HandleEvent(TelegramPokemonCaughtEvent eve)
            {
                _receiver.PokemonCaught(eve.PokemonId, eve.Cp, eve.Iv, eve.ProfileName, eve.BotNicnname, eve.Level, eve.Move1, eve.Move2);
            }

            public void Listen(IEvent evt)
            {
                try
                {
                    dynamic eve = evt;
                    HandleEvent(eve);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}
