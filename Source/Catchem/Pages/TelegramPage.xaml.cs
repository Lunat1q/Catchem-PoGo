using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Catchem.Classes;
using Catchem.Events;
using Catchem.Extensions;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Enums;
using TelegramMessageEvent = PoGo.PokeMobBot.Logic.Event.Global.TelegramMessageEvent;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for TelegramPage.xaml
    /// </summary>
    public partial class TelegramPage
    {
        private bool _windowClosing;
        public readonly Telegram TlgrmBot = new Telegram();
        private readonly TelegramSettings _tlgrmSettings = new TelegramSettings();
        private bool _loadingUi;
        private readonly Queue<TelegramCommand> _commandsQueue = new Queue<TelegramCommand>();
        private readonly Queue<string> _logQueue = new Queue<string>();
        private readonly TelegramListener _listener;

        public TelegramPage()
        {
            InitializeComponent();
            _tlgrmSettings.Load();
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
            _tlgrmSettings.SaveSettings();
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

        private void AddPokemonToReport_Click(object sender, RoutedEventArgs e)
        {

            if (AddToAutoReport.SelectedIndex <= -1) return;
            var pokemonId = (PokemonId) AddToAutoReport.SelectedItem;
            if (!_tlgrmSettings.AutoReportPokemon.Contains(pokemonId))
                _tlgrmSettings.AutoReportPokemon.Add(pokemonId);
            AddToAutoReport.SelectedIndex = -1;
            _tlgrmSettings?.SaveSettings();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            TlgrmBot?.Start(_tlgrmSettings?.ApiKey);
            _tlgrmSettings?.SaveSettings();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            TlgrmBot?.Stop();
            _tlgrmSettings?.SaveSettings();
        }

        private void AddBotOwner_Click(object sender, RoutedEventArgs e)
        {
            var name = BotOwnerTextBox.Text;
            if (name.Length < 2) return;
            if (name[0] == '@') name = name.Substring(1);
            _tlgrmSettings.Owners.Add(new TelegramBotOwner()
            {
                TelegramName = name
            });
            BotOwnerTextBox.Clear();
            _tlgrmSettings?.SaveSettings();
        }

        #endregion

        public void TelegramCommandReceiver(string sender, string command, long chatId, string[] args)
        {
            if (!RefreshOwnerChatId(sender, chatId)) return;

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

        private void PokemonCaught(PokemonId pokemon, int cp, double iv, string profileName, string botNick,
            double level, PokemonMove? move1, PokemonMove? move2, string captureType, double lng, double lat)
        {
            if ((!_tlgrmSettings.AutoReportSelectedPokemon || !_tlgrmSettings.AutoReportPokemon.Contains(pokemon)) &&
                (cp <= _tlgrmSettings.ReportAllPokemonsAboveCp || !_tlgrmSettings.UseCpReport) &&
                (iv <= _tlgrmSettings.ReportAllPokemonsAboveIv || !_tlgrmSettings.UseIvReport))
                return;
            if (captureType.Length < 1) return;
            string messageToSend =
                $"[{botNick}]({profileName}) {captureType} {pokemon}! CP:{cp}, Iv:{iv.ToN1()}, Level:{level.ToN1()}, Move 1: {move1}, Move 2: {move2}";
            if ("Caught" == captureType)
                messageToSend +=
                    $" https://maps.google.com/?q={lat.ToString(CultureInfo.InvariantCulture)},{lng.ToString(CultureInfo.InvariantCulture)}";
            foreach (var owner in _tlgrmSettings.Owners.Where(x => x.ChatId != 0))
            {
                SendToTelegram(messageToSend, owner.ChatId);
                //if ("Caught" == captureType)
                //{
                //    SendLocationToTelegram(lat, lng, owner.ChatId);
                //}
            }
        }

        private async void SendToTelegram(string message, long chatId, bool markDown = false, string[][] keys = null)
        {
            try
            {
                await TlgrmBot.SendToTelegram(message, chatId, markDown, keys);
            }
            catch (Exception)
            {
                //ignore throw;
            }
           
        }

        private async void SendLocationToTelegram(double lat, double lng, long chatId)
        {
            try
            {
                await TlgrmBot.SendLocationToTelegram((float)lat, (float)lng, chatId);
            }
            catch (Exception)
            {
                //ignore throw;
            }

        }

        private void PlayerLevelUp(int level, string items, string botNicName, string profileName)
        {
           if (!_tlgrmSettings.ReportLevelUp)
                return;
            string messageToSend =
                $"[{botNicName}]({profileName}) Leveled up to {level}! Rewards: {items}";
                
            foreach (var owner in _tlgrmSettings.Owners.Where(x => x.ChatId != 0))
            {
                SendToTelegram(messageToSend, owner.ChatId);
            }
        }

        private void CaptchaRequired(string botNicName, string profileName)
        {
            if (!_tlgrmSettings.ReportLevelUp)
                return;
            string messageToSend =
                $"[{botNicName}]({profileName}) Require captcha input";

            foreach (var owner in _tlgrmSettings.Owners.Where(x => x.ChatId != 0))
            {
                SendToTelegram(messageToSend, owner.ChatId);
            }
        }

        private async void TelegramCommandWorker()
        {
            while (!_windowClosing)
            {
                if (_commandsQueue.Count > 0)
                {
                    var t = _commandsQueue.Dequeue();
                    if (string.IsNullOrEmpty(t.Command) || t.Command.Length == 1) return;
                    if (t.Command[0] == '/')
                        t.Command = t.Command.Substring(1);
                    switch (t.Command)
                    {
                        case "start":
                            HandleHelp(t.ChatId);
                            break;
                        case "help":
                            HandleHelp(t.ChatId);
                            break;
                        case "bots":
                            HandleListBots(t.ChatId);
                            break;
                        case "botstart":
                            HandleToggle(t.ChatId, true, t.Args);
                            break;
                        case "botstop":
                            HandleToggle(t.ChatId, false, t.Args);
                            break;
                        case "top":
                            HandleTop(t.ChatId, t.Args);
                            break;
                        case "status":
                            HandleStatus(t.ChatId, t.Args);
                            break;
                        case "report":
                            HandleReport(t.ChatId, t.Args);
                            break;
                        case "reportabovecp":
                            HandleReportAboveCp(t.ChatId, t.Args);
                            break;
                        case "reportaboveiv":
                            HandleReportAboveIv(t.ChatId, t.Args);
                            break;
                        case "poke":
                            HandlePokes(t.ChatId, t.Args);
                            break;
                        default:
                            HandleUnknownCommand(t.ChatId);
                            break;
                    }
                }
                await Task.Delay(10);
            }
        }

        private bool RefreshOwnerChatId(string senderName, long chatId)
        {
            var targetOwner = _tlgrmSettings?.Owners?.FirstOrDefault(x => x.TelegramName == senderName);
            if (targetOwner == null) return false;
            targetOwner.ChatId = chatId;
            return true;
        }

        private void HandleUnknownCommand(long chatId)
        {
            SendToTelegram("Unknown command!", chatId);
        }

        private void HandleTop(long chatId, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                SendToTelegram("No bot was selected", chatId);
                return;
            }
            if (args.Length != 2)
            {
                SendToTelegram("Invalid Command Structure, try 'top 1 cp'", chatId);
                return;
            }
            int botNum;
            if (!int.TryParse(args[0], out botNum)) return;
            if (botNum <= 0 || botNum > MainWindow.BotsCollection.Count)
            {
                SendToTelegram("Invalid bot number", chatId);
                return;
            }

            BotWindowData targetBot;
            if (!GetBotByIndex(chatId, --botNum, out targetBot)) return;

            //var rank = 1;
            var topPokemon = new StringBuilder();
            IEnumerable<string> sb = null;
            if (args[1].ToLower() == "cp")
            {
                topPokemon.AppendLine($"Top 10 Highest CP Poke for {targetBot.ProfileName}: \n");
                sb = targetBot.PokemonList?.OrderByDescending(x => x.Cp).Take(10)
                    .Select((x, i) => BuildPokemonRow(i + 1, x));

            }
            if (args[1].ToLower() == "iv")
            {
                topPokemon.AppendLine($"Top 10 Highest IV Poke for {targetBot.ProfileName}: \n");
                sb = targetBot.PokemonList?.OrderByDescending(x => x.Iv).Take(10)
                    .Select((x, i) => BuildPokemonRow(i + 1, x));
            }
            if (sb != null)
                foreach (var s in sb)
                    topPokemon.AppendLine(s);

            SendToTelegram(topPokemon.ToString(), chatId);
        }

        private static IEnumerable<PokemonUiData> HandlePokesCpOrIv(string typeFilter, IEnumerable<PokemonUiData> pokes)
            => pokes.OrderByDescending(p => (typeFilter == "iv" ? p.Iv : p.Cp));

        private static IEnumerable<PokemonUiData> HandlePokesById(string pokeId, IEnumerable<PokemonUiData> pokes)
            => pokes.Where(p => p.PokemonId.ToString().ToLower() == pokeId);

        private void HandlePokes(long chatId, string[] args)
        {
            if (args.Length == 0)
            {
                SendToTelegram("Invalid Command, try:\n"
                    +"poke <bot #> [iv/cp] [poke id]\n"
                    + "examples:\n"
                    + "poke 1\n"
                    + "poke 1 cp\n"
                    + "poke 1 cp pidgey", chatId);
                return;
            }
            args = args.Select(arg => arg.ToLower()).ToArray();

            int botNum;
            if (!int.TryParse(args[0], out botNum)) return;
            if (botNum < 1 || botNum > MainWindow.BotsCollection.Count)
            {
                SendToTelegram("Invalid bot number", chatId);
                return;
            }
            BotWindowData targetBot;

            if (!GetBotByIndex(chatId, --botNum, out targetBot)) return;

            var resultSb = new StringBuilder();
            List<PokemonUiData> pokeList = targetBot.PokemonList.ToList();
            string[] arg1Expected = { "cp","iv" };

            resultSb.AppendLine($"List of Pokes for {targetBot.ProfileName}:");

            if (args.Length >= 2 && arg1Expected.Contains(args[1]))
            {
                pokeList = HandlePokesCpOrIv(args[1], pokeList).ToList();
                //resultSb.AppendLine($"Order by: {args[1].ToUpper()}");
            }

            if (args.Length >= 3)
            {
                pokeList = HandlePokesById(args[2], pokeList).ToList();
                resultSb.AppendLine($"{pokeList.Count} pokes found!\n");
            }

            var pokeListStr = pokeList.Select((x, i) => BuildPokemonRow(i + 1, x));

            foreach (var s in pokeListStr)
                resultSb.AppendLine(s);

            SendToTelegram(resultSb.ToString(), chatId);
        }

        private bool GetBotByIndex(long chatId, int botNum, out BotWindowData bot)
        {
            bot = MainWindow.BotsCollection.ElementAtOrDefault(botNum);
            if (bot == null) return false;
            if (bot.Started) return true;
            SendToTelegram("Bot needs to be started before you can run this command", chatId);
            return false;
        }

        private static string BuildPokemonRow(int indx, PokemonUiData pokemon)
            => $"{indx}) CP:{pokemon.Cp} IV:{pokemon.Iv.ToN1()} - {pokemon.Name}";

        private void HandleStatus(long chatId, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                SendToTelegram("No bot selected", chatId);
                return;
            }
            if (args.Length > 1)
            {
                SendToTelegram("Invalid Command try 'status 0'", chatId);
                return;
            }
            int botNum;
            if (int.TryParse(args[0], out botNum))
            {
                BotWindowData targetBot;
                if (GetBotByIndex(chatId, --botNum, out targetBot))
                {
                    var status = new StringBuilder();
                    status.AppendLine($"Current Status for {targetBot.ProfileName}:");
                    status.AppendLine($"Current State: {targetBot.Session.State}");
                    if (targetBot.Session.State == BotState.Paused)
                        status.AppendLine($"Paused for: {targetBot.PauseTs}:");
                    status.AppendLine($"Current bot Run Time: {targetBot.Ts}");
                    status.AppendLine($"Level: {targetBot.Level} Exp/h: {targetBot.Xpph.ToN1()}");
                    status.AppendLine(
                        $"Stardust {targetBot.StarDust}, Farmed: {(targetBot.StarDust > 0 ? targetBot.StarDust - targetBot.StartStarDust : 0)} ({targetBot.StardustRate.ToN1()}/h)");
                    status.AppendLine(
                        $"Poke Caught: {targetBot.Session?.Stats?.TotalPokemons} ({targetBot.PokemonsRate.ToN1()}/h)");
                    status.AppendLine(
                        $"PokeStops spinned: {targetBot.Session?.Stats?.TotalPokestops} ({targetBot.PokestopsRate.ToN1()}/h)");
                    // Status.AppendLine($"Team: {targetBot.Stats.}");
                    SendToTelegram(status.ToString(), chatId);
                }
            }
            else
            {
                SendToTelegram("Unknown command!", chatId);
            }
        }

        private void HandleToggle(long chatId, bool start, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                SendToTelegram($"Wrong {(start ? "start" : "stop")} command!", chatId);
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
                SendToTelegram($"{(start ? "Started" : "Stopped")} all bots", chatId);
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

                    SendToTelegram($"Bot {targetBot.ProfileName} {(start ? "started" : "stopped")}!", chatId);
                }
                else
                {
                    SendToTelegram("Bot with that index not found!", chatId);
                }
                return;
            }
            SendToTelegram($"Wrong {(start ? "start" : "stop")} command!", chatId);
        }

        private void HandleHelp(long chatId)
        {
            var helpMsg = "The following commands are avaliable: \n" +
                          "- /bots \n" +
                          "- botstart [bot Number / all] \n" +
                          "- botstop [bot Number / all] \n" +
                          "- poke [bot Number] [iv/cp] [poke id] \n" +
                          "- status [bot Number] \n" +
                          "- top [bot Number] [cp/iv] \n" +
                          "- report [enable/disable] \n" +
                          "- reportabovecp [cp (0 to disable)] \n" +
                          "- reportaboveiv [iv (0 to disable)]";

            SendToTelegram(helpMsg, chatId, keys: new[] { new[] { "/help", "/bots" }, MainWindow.BotsCollection.Select((x, v) => "/status " + (v + 1)).ToArray()});
        }

        private void HandleListBots(long chatId)
        {
            var botNumber = 0;
            var botStringBuilder = new StringBuilder();
            botStringBuilder.AppendLine("Current Bots Avaliable:");
            foreach (var bot in MainWindow.BotsCollection)
            {
                botStringBuilder.AppendLine(
                    $"{++botNumber}) {bot.ProfileName} [{(bot.Started ? "RUNNING" : "STOPPED")}]");
            }
            if (botNumber == 0)
                SendToTelegram("There are no bots created", chatId);
            if (botNumber > 0)
                SendToTelegram(botStringBuilder.ToString(), chatId);
        }

        private void HandleReport(long chatId, string[] args)
        {
            if (args.Length <= 0 || args.Length > 1)
            {
                SendToTelegram("Error Invalid Command Structure!", chatId);
                return;
            }
            if (args[0].ToLower() == "enable")
            {
                if (CbAutoReportSelectedPokemon.IsChecked == true)
                {
                    SendToTelegram("Reporting Selected Pokemon is already Enabled.", chatId);
                    return;
                }
                CbAutoReportSelectedPokemon.IsChecked = true;
                SendToTelegram("Reporting Selected Pokemon set to Enabled.", chatId);
                SaveSettings();
                return;
            }
            if (args[0].ToLower() == "disable")
            {
                if (CbAutoReportSelectedPokemon.IsChecked == false)
                {
                    SendToTelegram("Reporting Selected Pokemon is already Disabled.", chatId);
                    return;
                }
                CbAutoReportSelectedPokemon.IsChecked = false;
                SendToTelegram("Reporting Selected Pokemon set to Disabled.", chatId);
                SaveSettings();
                return;
            }
            SendToTelegram("Error Invalid Command Structure!", chatId);
        }

        private void HandleReportAboveCp(long chatId, string[] args)
        {
            if (args.Length <= 0 || args.Length > 1)
            {
                SendToTelegram("Error Invalid Command Structure!", chatId);
                return;
            }
            int cp;
            if (int.TryParse(args[0], out cp))
            {
                if (cp < 0 || cp > 5000)
                {
                    SendToTelegram("Error Invalid CP Entered!", chatId);
                    return;
                }
                TbReportAllPokemonsAboveCp.Text = cp.ToString();
                SaveSettings();
                SendToTelegram($"Set Report Above CP to: {cp}", chatId);
            }
            else
            {
                SendToTelegram("Error Invalid CP Entered!", chatId);
            }
        }


        private void HandleReportAboveIv(long chatId, string[] args)
        {
            if (args.Length <= 0 || args.Length > 1)
            {
                SendToTelegram("Error Invalid Command Structure!", chatId);
                return;
            }
            int iv;
            if (int.TryParse(args[0], out iv))
            {
                if (iv < 0 || iv > 100)
                {
                    SendToTelegram("Error Invalid IV Entered!", chatId);
                    return;
                }
                TbReportAllPokemonsAboveIv.Text = iv.ToString();
                SaveSettings();
                SendToTelegram($"Set Report Above IV to: {iv}", chatId);
            }
            else
            {
                SendToTelegram("Error Invalid IV Entered!", chatId);
            }
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
                _receiver.PokemonCaught(eve.PokemonId, eve.Cp, eve.Iv, eve.ProfileName, eve.BotNicnname, eve.Level,
                    eve.Move1, eve.Move2, eve.CaptureType, eve.Lng, eve.Lat);
            }

            private void HandleEvent(TelegramPlayerLevelUpEvent eve)
            {
                _receiver.PlayerLevelUp(eve.Level, eve.Items, eve.BotNicName, eve.ProfileName);
            }

            private void HandleEvent(TelegramCaptchaRequiredEvent eve)
            {
                _receiver.CaptchaRequired(eve.BotNicName, eve.ProfileName);
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
