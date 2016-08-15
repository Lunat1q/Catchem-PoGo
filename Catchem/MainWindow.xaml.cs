using GMap.NET;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Enums;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Catchem.Classes;
using Catchem.Extensions;
using POGOProtos.Data;
using POGOProtos.Inventory.Item;
using static System.String;
using LogLevel = PoGo.PokeMobBot.Logic.Logging.LogLevel;
using System.Reflection;

namespace Catchem
{
    public partial class MainWindow
    {
        public static MainWindow BotWindow;
        private bool _windowClosing;
        private const string SubPath = "Profiles";

        public ObservableCollection<BotWindowData> BotsCollection = new ObservableCollection<BotWindowData>();

        private ISession CurSession => Bot.Session;

        private BotWindowData _bot;
        public BotWindowData Bot
        {
            get { return _bot; }
            set
            {
                if (value == _bot) return;
                EnqueOldBot();
                _bot = value;
                NewBotSelected();
            }
        }
        private bool _loadingUi;

        public MainWindow()
        {
            InitializeComponent();
            InitWindowsControlls();
            BotWindow = this;
            LogWorker();
            InitBots();
            BotMapPage.SetSettingsPage(BotSettingsPage);
            SetVersionTag();
        }

        public void SetVersionTag(Version remoteVersion = null)
        {
            this.Title = $"Catchem - v{Assembly.GetExecutingAssembly().GetName().Version} {(remoteVersion != null ? $"New release - {remoteVersion}" : "")}";
        }

        private void InitWindowsControlls()
        {
            BotSettingsPage.SubPath = SubPath;
        }

        internal void InitBots()
        {
            Logger.SetLogger(new WpfLogger(LogLevel.Info), SubPath);
            botsBox.ItemsSource = BotsCollection;
            grid_pickBot.Visibility = Visibility.Visible;
            foreach (var item in Directory.GetDirectories(SubPath))
            {
                if (item != SubPath + "\\Logs")
                {
                    InitBot(GlobalSettings.Load(item), Path.GetFileName(item));
                }
            }
        }


        public void ReceiveMsg(string msgType, ISession session, params object[] objData)
        {
            if (session == null) return;
            switch (msgType)
            {
                case "bot_failure":
                    HandleFailure(session, (bool) objData[0], (bool) objData[1]);
                    break;
                case "log":
                    PushNewConsoleRow(session, (string)objData[0], (Color)objData[1]);
                    break;
                case "err":
                    PushNewError(session);
                    break;
                case "ps":
                    PushNewPokestop(session, (IEnumerable<FortData>)objData[0]);
                    break;
                case "pm":
                    PushNewPokemons(session, (IEnumerable<MapPokemon>)objData[0]);
                    break;
                case "pmw":
                    PushNewWildPokemons(session, (IEnumerable<WildPokemon>)objData[0]);
                    break;
                case "pm_rm":
                    PushRemovePokemon(session, (MapPokemon)objData[0]);
                    break;                
                case "p_loc":
                    UpdateCoords(session, objData);
                    break;
                case "pm_list":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        BuildPokemonList(session, objData);
                    }));
                    break;
                case "item_list":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        BuildItemList(session, objData);
                    }));
                    break;
                case "new_version":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        SetVersionTag((Version)objData[0]);
                    }));
                    break;
                case "item_new":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        GotNewItems(session, objData);
                    }));
                    break;
                case "route_next":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        DrawNextRoute(session, (List<Tuple<double, double>>)objData[0]);
                    }));
                    break;
                case "item_rem":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        LostItem(session, objData);
                    }));
                    break;
                case "pm_new":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        GotNewPokemon(session, objData);
                    }));
                    break;
                case "pm_rem":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        LostPokemon(session, objData);
                    }));
                    break;
                case "pm_upd":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        PokemonChanged(session, objData);
                    }));
                    break;
                case "profile_data":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        UpdateProfileInfo(session, objData);
                    }));
                    break;
                case "forcemove_done":
                    PushRemoveForceMoveMarker(session);
                    break;
            }
        }

        private void HandleFailure(ISession session, bool shutdown, bool stop)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null || !receiverBot.Started) return;
            //if (shutdown)
            //    Environment.Exit(0);
            if (!stop) return;
            receiverBot.Stop();
            ClearPokemonData(receiverBot);
        }

        private void DrawNextRoute(ISession session, List<Tuple<double, double>> list)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null || !receiverBot.Started) return;
            receiverBot.PushNewPathRoute(list);
            BotMapPage.UpdatePathRoute();
        }

        private void PushNewError(ISession session)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null) return;
            receiverBot.ErrorsCount++;
        }

        private void PokemonChanged(ISession session, object[] objData)
        {
            try
            {
                if ((ulong)objData[0] == 0) return;
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.PokemonUpdated((ulong)objData[0], (int)objData[2], (double)objData[3], (PokemonFamilyId)objData[4], (int)objData[5]);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LostItem(ISession session, object[] objData)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null) return;
            receiverBot.LostItem((ItemId?)objData[0], (int?)objData[1]);
            UpdateItemCollection(session);
        }

        private void GotNewItems(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.GotNewItems((List<Tuple<ItemId, int>>)objData[0]);
                UpdateItemCollection(session);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void UpdateProfileInfo(ISession session, object[] objData)
        {
            var targetBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (targetBot == null) return;
            targetBot.NewPlayerData((string)objData[0], (int)objData[1], (int)objData[2], (TeamColor)objData[4], (int)objData[5], (int)objData[3]);
            if (targetBot == Bot)
                BotPlayerPage.UpdatePlayerTab();
        }

        private void LostPokemon(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.LostPokemon((ulong)objData[0], (PokemonFamilyId?)objData[1], (int?)objData[2]);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void GotNewPokemon(ISession session, object[] objData)
        {
            try
            {
                if ((ulong) objData[0] == 0) return;
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.GotNewPokemon((ulong)objData[0], (PokemonId)objData[1], (int)objData[2], (double)objData[3], (PokemonFamilyId)objData[4], (int)objData[5]);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void BuildPokemonList(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                var receivedList = (List<Tuple<PokemonData, double, int>>) objData[0];
                receiverBot.BuildPokemonList(receivedList);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                BotPlayerPage.UpdatePokemons();
            }
        }

        private void BuildItemList(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.ItemList = new ObservableCollection<ItemUiData>();
                receiverBot.ItemList.CollectionChanged += delegate { UpdateItemCollection(session); };
                ((List<ItemData>) objData[0]).ForEach(x => receiverBot.ItemList.Add(new ItemUiData(x.ItemId, x.ItemId.ToInventorySource(), x.ItemId.ToInventoryName(), x.Count)));
                if (session != CurSession) return;

                BotPlayerPage.UpdateItems(); 
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void UpdateItemCollection(ISession session)
        {
            if (Bot == null || session != CurSession) return;
            BotPlayerPage.UpdateInventoryCount();
        }

        private void UpdateCoords(ISession session, object[] objData)
        {
            try
            {
                var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (botReceiver == null) return;
                botReceiver.Lat = (double)objData[0];
                botReceiver.Lng = (double)objData[1];
                botReceiver.PushNewRoutePoint(new PointLatLng(botReceiver.Lat, botReceiver.Lng));
                if (session != CurSession)
                {
                    botReceiver._lat = botReceiver.Lat;
                    botReceiver._lng = botReceiver.Lng;
                }
                else
                {
                    BotSettingsPage.UpdateCoordBoxes();
                    BotMapPage.UpdateCurrentBotCoords(botReceiver);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #region DataFlow - Push

        private void PushNewConsoleRow(ISession session, string rowText, Color rowColor)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            botReceiver?.LogQueue.Enqueue(Tuple.Create(rowText, rowColor));
        }

        private void PushRemoveForceMoveMarker(ISession session)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            var nMapObj = new NewMapObject("forcemove_done", "", 0, 0, "");
            botReceiver?.MarkersQueue.Enqueue(nMapObj);
        }

        private void PushRemovePokemon(ISession session, MapPokemon mapPokemon)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            var nMapObj = new NewMapObject("pm_rm", mapPokemon.PokemonId.ToString(), mapPokemon.Latitude, mapPokemon.Longitude, mapPokemon.EncounterId.ToString());
            botReceiver?.MarkersQueue.Enqueue(nMapObj);
        }

        private void PushNewPokemons(ISession session, IEnumerable<MapPokemon> pokemons)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            foreach (var pokemon in pokemons)
            {
                if (botReceiver.MapMarkers.ContainsKey(pokemon.EncounterId.ToString()) || botReceiver.MarkersQueue.Count(x => x.Uid == pokemon.EncounterId.ToString()) != 0) continue;
                var nMapObj = new NewMapObject("pm", pokemon.PokemonId.ToString(), pokemon.Latitude, pokemon.Longitude, pokemon.EncounterId.ToString());
                botReceiver.MarkersQueue.Enqueue(nMapObj);
            }
        }

        private void PushNewWildPokemons(ISession session, IEnumerable<WildPokemon> pokemons)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            foreach (var pokemon in pokemons)
            {
                if (botReceiver.MapMarkers.ContainsKey(pokemon.EncounterId.ToString()) || botReceiver.MarkersQueue.Count(x => x.Uid == pokemon.EncounterId.ToString()) != 0) continue;
                var nMapObj = new NewMapObject("pm", pokemon.PokemonData.PokemonId.ToString(), pokemon.Latitude, pokemon.Longitude, pokemon.EncounterId.ToString());
                botReceiver.MarkersQueue.Enqueue(nMapObj);
            }
        }

        private void PushNewPokestop(ISession session, IEnumerable<FortData> pstops)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            var fortDatas = pstops as FortData[] ?? pstops.ToArray();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < fortDatas.Length; i++)
            {
                try
                {
                    try
                    {
                        if (botReceiver.MapMarkers.ContainsKey(fortDatas[i].Id) || botReceiver.MarkersQueue.Count(x => x.Uid == fortDatas[i].Id) != 0)
                            continue;
                    }
                    catch (Exception) //ex)
                    {
                        // ignored
                    }
                    var lured = fortDatas[i].LureInfo?.LureExpiresTimestampMs > DateTime.UtcNow.ToUnixTime();
                    var nMapObj = new NewMapObject("ps" + (lured ? "_lured" : ""), "PokeStop", fortDatas[i].Latitude, fortDatas[i].Longitude, fortDatas[i].Id);
                    botReceiver.MarkersQueue.Enqueue(nMapObj);
                }
                catch (Exception) //ex)
                {
                    i--;
                }
            }
        }

        #endregion

        #region Async Workers
        
        private async void LogWorker()
        {
            while (!_windowClosing)
            {
                if (Bot?.LogQueue.Count > 0)
                {
                    var t = Bot.LogQueue.Dequeue();
                    Bot.Log.Add(t);
                    consoleBox.AppendParagraph(t.Item1, t.Item2);
                    if (consoleBox.Document.Blocks.Count > 100)
                    {
                        var toRemove = consoleBox.Document.Blocks.ElementAt(0);
                        consoleBox.Document.Blocks.Remove(toRemove);
                    }
                }
                await Task.Delay(10);
            }
        }
        #endregion

        #region Controll's events
        private void button_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Visibility = Visibility.Visible;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Visibility = Visibility.Collapsed;
            var input = InputTextBox.Text;
            if (!Directory.Exists(SubPath + "\\" + input))
            {
                var dir = Directory.CreateDirectory(SubPath + "\\" + input);
                var settings = GlobalSettings.Load(dir.FullName) ?? GlobalSettings.Load(dir.FullName);
                InitBot(settings, input);
            }
            else
                MessageBox.Show("Profile with that name already exists");
            
            InputTextBox.Text = Empty;
        }

        private void InitBot(GlobalSettings settings, string profileName = "Unknown")
        {
            var newBot = CreateBowWindowData(settings, profileName);
            var session = new Session(newBot.Settings, newBot.Logic);
            session.Client.ApiFailure = new ApiFailureStrategy(session);

            newBot.Session = session;
            session.EventDispatcher.EventReceived += evt => newBot.Listener.Listen(evt, session);
            session.EventDispatcher.EventReceived += evt => newBot.Aggregator.Listen(evt, session);
            session.Navigation.UpdatePositionEvent += (lat, lng, alt) => session.EventDispatcher.Send(new UpdatePositionEvent {Latitude = lat, Longitude = lng, Altitude = alt});

            newBot.PokemonList.CollectionChanged += delegate { UpdatePokemonCollection(session); };

            newBot.Stats.DirtyEvent += () => { StatsOnDirtyEvent(newBot); };

            newBot._lat = settings.LocationSettings.DefaultLatitude;
            newBot._lng = settings.LocationSettings.DefaultLongitude;
            newBot.Machine.SetFailureState(new LoginState());

            BotsCollection.Add(newBot);
        }

        private void EnqueOldBot()
        {
            if (Bot == null) return;
            Bot.GlobalSettings.StoreData(SubPath + "\\" + Bot.ProfileName);
            Bot.EnqueData();
            ClearPokemonData(Bot);
        }

        private void NewBotSelected()
        {
            if (Bot == null)
            {
                if (tabControl.IsEnabled)
                    tabControl.IsEnabled = false;
                if (grid_pickBot.Visibility == Visibility.Collapsed)
                    grid_pickBot.Visibility = Visibility.Visible;
                return;
            }
            
            if (Bot != null)
            {
                StatsOnDirtyEvent(Bot);
                Bot.ErrorsCount = 0;
            }
            RebuildUi();
        }

        public void UpdatePokemonCollection(ISession session)
        {
            if (Bot == null || session != CurSession) return;
            BotPlayerPage.UpdatePokemonsCount();
        }

        private void StatsOnDirtyEvent(BotWindowData bot)
        {
            if (bot == null) return;
            Dispatcher.BeginInvoke(new ThreadStart(bot.UpdateXppH));
            if (Bot == bot)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    BotPlayerPage.UpdateRunTimeData();
                }));
            }
            bot.CheckForMaxCatch();
        }

        public void ClearPokemonData(BotWindowData calledBot)
        {
            if (Bot != calledBot) return;
            consoleBox.Document.Blocks.Clear();
            Bot.LatStep = Bot.LngStep = 0;
            BotMapPage.ClearData();
            BotPlayerPage.ClearData();
        }

        private static BotWindowData CreateBowWindowData(GlobalSettings s,string name)
        {
            var stats = new Statistics();

            return new BotWindowData(name, s, new StateMachine(), stats, new StatisticsAggregator(stats), new WpfEventListener(), new ClientSettings(s), new LogicSettings(s));
        }

        private void RebuildUi()
        {
            if (Bot == null || _loadingUi) return;

            _loadingUi = true;            
            if (!tabControl.IsEnabled)
                tabControl.IsEnabled = true;
            if (grid_pickBot.Visibility == Visibility.Visible)
                grid_pickBot.Visibility = Visibility.Collapsed;

            BotSettingsPage.SetBot(Bot);
            BotPlayerPage.SetBot(Bot);
            BotPokemonListPage.SetBot(Bot);
            BotMapPage.SetBot(Bot);

            _loadingUi = false;
        }

        #region Windows UI Methods
        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Visibility = Visibility.Collapsed;
            InputTextBox.Text = Empty;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _windowClosing = true;
            BotMapPage.WindowClosing = true;
            if (Bot == null || _loadingUi) return;
            Bot.GlobalSettings.StoreData(SubPath + "\\" + Bot.ProfileName);
            foreach (var b in BotsCollection)
            {
                b.Stop();
            }
        }

        private void btn_StartAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var bot in BotsCollection)
                bot.Start();
        }

        private void btn_StopAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var bot in BotsCollection)
            {
                bot.Stop();
                ClearPokemonData(bot);
            }
        }

        private void batch_Yes_Click(object sender, RoutedEventArgs e)
        {
            batchInput.Visibility = Visibility.Collapsed;
            var input = batch_botText.Text;

            var inputRows = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var row in inputRows)
            {
                try
                {
                    var rowData = row.Split(';');
                    var auth = char.ToUpper(rowData[0][0]) + rowData[0].Substring(1);
                    var login = rowData[1];
                    var pass = rowData[2];
                    var proxy = rowData.Length > 3 ? rowData[3] : "";
                    var proxyLogin = rowData.Length > 4 ? rowData[4] : "";
                    var proxyPass = rowData.Length > 5 ? rowData[5] : "";
                    var path = login;
                    var created = false;
                    do
                    {
                        if (!Directory.Exists(SubPath + "\\" + path))
                        {
                            CreateBotFromClone(path, login, auth, pass, proxy, proxyLogin, proxyPass);
                            created = true;
                        }
                        else
                        {
                            path += DeviceSettings.RandomString(4);
                        }
                    } while (!created);
                }
                catch
                {
                    //ignore
                }
            }
            batch_botText.Text = Empty;
        }

        private void CreateBotFromClone(string path, string login, string auth, string pass, string proxy, string proxyLogin, string proxyPass)
        {
            var dir = Directory.CreateDirectory(SubPath + "\\" + path);
            var settings = GlobalSettings.Load(dir.FullName) ?? GlobalSettings.Load(dir.FullName);
            if (Bot != null)
                settings = Bot.GlobalSettings.Clone();

            //set new settings
            Enum.TryParse(auth, out settings.Auth.AuthType);
            if (settings.Auth.AuthType == AuthType.Google)
            {
                settings.Auth.GoogleUsername = login;
                settings.Auth.GooglePassword = pass;
            }
            else
            {
                settings.Auth.PtcUsername = login;
                settings.Auth.PtcPassword = pass;
            }
            if (proxy != "")
            {
                settings.Auth.UseProxy = true;
                settings.Auth.ProxyUri = proxy;
                settings.Auth.ProxyLogin = proxyLogin;
                settings.Auth.ProxyPass = proxyPass;
            }
            settings.Device.DeviceId = DeviceSettings.RandomString(16, "0123456789abcdef");
            settings.StoreData(dir.FullName);
            InitBot(settings, path);
        }
        private void batch_No_Click(object sender, RoutedEventArgs e)
        {
            batchInput.Visibility = Visibility.Collapsed;
            batch_botText.Text = Empty;
        }

        private void btn_BatchCreation_Click(object sender, RoutedEventArgs e)
        {
            batchInput.Visibility = Visibility.Visible;
        }
        #endregion

        #endregion

        private void mi_RemoveBot_Click(object sender, RoutedEventArgs e)
        {
            var bot = botsBox.SelectedItem as BotWindowData;
            if (bot == null) return;
            BotsCollection.Remove(bot);
            Directory.Delete(SubPath + "\\" + bot.ProfileName, true);
        }
    }
}
