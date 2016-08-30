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
using Catchem.Events;
using PoGo.PokeMobBot.Logic.API;
using PoGo.PokeMobBot.Logic.PoGoUtils;

// ReSharper disable PossibleLossOfFraction

namespace Catchem
{
    public partial class MainWindow
    {
        public static MainWindow BotWindow;
        public CatchemSettings GlobalCatchemSettings = new CatchemSettings();
        private bool _windowClosing;
        private const string SubPath = "Profiles";

        public static ObservableCollection<BotWindowData> BotsCollection = new ObservableCollection<BotWindowData>();

        private ISession CurSession => Bot?.Session;
        private readonly Queue<BotRpcMessage> _messageQueue = new Queue<BotRpcMessage>();

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

            //global settings
            GlobalCatchemSettings.Load();

            InitWindowsControlls();
            BotWindow = this;
            LogWorker();
            RpcWorker();
            InitBots();
            SettingsView.BotMapPage.SetSettingsPage(SettingsView.BotSettingsPage);
            SetVersionTag();

            SettingsView.BotMapPage.SetGlobalSettings(GlobalCatchemSettings);
            GlobalMapView.SetGlobalSettings(GlobalCatchemSettings);
            SettingsView.BotSettingsPage.SetGlobalSettings(GlobalCatchemSettings);
            RouteCreatorView.SetGlobalSettings(GlobalCatchemSettings);
        }


        public void SetVersionTag(Version remoteVersion = null)
        {
            Title = $"Catchem - v{Assembly.GetExecutingAssembly().GetName().Version} {(remoteVersion != null ? $"New release - {remoteVersion}" : "")}";
        }

        private void InitWindowsControlls()
        {
            SettingsView.BotSettingsPage.SubPath = SubPath;
        }

        internal void InitBots()
        {
            Logger.SetLogger(new WpfLogger(LogLevel.Debug), SubPath);
            botsBox.ItemsSource = BotsCollection;
            SettingsView.grid_pickBot.Visibility = Visibility.Visible;
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
            _messageQueue.Enqueue(new BotRpcMessage(msgType, session, objData));
        }

        // ReSharper disable once UnusedParameter.Local
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
            SettingsView.BotMapPage.UpdatePathRoute();
        }

        private void DrawNextOptRoute(ISession session, List<Tuple<double, double>> list)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null || !receiverBot.Started || receiverBot != Bot) return;
            SettingsView.BotMapPage.UpdateOptPathRoute(list);
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
                receiverBot.PokemonUpdated((ulong)objData[0], (int)objData[2], (double)objData[3], (PokemonFamilyId)objData[4], (int)objData[5], (bool)objData[6], (string)objData[7]);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void PokemonFavouriteChanged(ISession session, object[] objData)
        {
            try
            {
                if (!(objData[0] is ulong)) return;
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.PokemonFavUpdated((ulong)objData[0], (bool)objData[1]);
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
                SettingsView.BotPlayerPage.UpdatePlayerTab();
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
                receiverBot.GotNewPokemon((ulong) objData[0], (PokemonId) objData[1], (int) objData[2],
                    (double) objData[3], (PokemonFamilyId) objData[4], (int) objData[5], false, false,
                    (double) objData[6],
                    (PokemonMove) objData[7], (PokemonMove) objData[8], (PokemonType) objData[9],
                    (PokemonType) objData[10], (int) objData[11], (int)objData[12], (int)objData[13], (int)objData[14], (int)objData[15]);
                TelegramView.TlgrmBot.EventDispatcher.Send(new TelegramPokemonCaughtEvent
                {
                    PokemonId = (PokemonId)objData[1],
                    Cp = (int)objData[2],
                    Iv = (double)objData[3],
                    ProfileName = receiverBot.ProfileName,
                    BotNicnname = receiverBot.PlayerName,
                    Level = (double)objData[6],
                    Move1 = (PokemonMove?)objData[7] ?? PokemonMove.MoveUnset,
                    Move2 = (PokemonMove?)objData[8] ?? PokemonMove.MoveUnset
                });
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void SetPlayerTeam(ISession session, object[] paramObjects)
        {
            if (paramObjects == null || paramObjects.Length == 0 || !(paramObjects[0] is TeamColor)) return;
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null) return;
            receiverBot.Team = (TeamColor) paramObjects[0];
            if (CurSession == session)
                SettingsView.BotPlayerPage.UpdatePlayerTeam();
        }

        private void BuildPokemonList(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                if (objData[0] == null)
                {
                    receiverBot.LogQueue.Enqueue(
                        Tuple.Create("Can't retrieve pokemon list, servers are unstable or you can be banned!", Colors.Red));
                    return;
                }
                var receivedList = (List<Tuple<PokemonData, double, int>>) objData[0];
                receiverBot.BuildPokemonList(receivedList);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                SettingsView.BotPlayerPage.UpdatePokemons();
            }
        }

        private void BuildItemList(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                if (objData[0] == null)
                {
                    receiverBot.LogQueue.Enqueue(
                        Tuple.Create("Can't retrieve items list, servers are unstable or you can be banned!", Colors.Red));
                    return;
                }
                receiverBot.ItemList.Clear();
                foreach (var x in ((List<ItemData>) objData[0]))
                {
                    if (x.Count > 0)
                        receiverBot.ItemList.Add(new ItemUiData(x.ItemId, x.ItemId.ToInventorySource(),
                            x.ItemId.ToInventoryName(), x.Count));
                }
                if (session != CurSession) return;

                SettingsView.BotPlayerPage.UpdateItems(); 
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void UpdateItemCollection(ISession session)
        {
            if (Bot == null || session != CurSession) return;
            SettingsView.BotPlayerPage.UpdateInventoryCount();
        }

        private void UpdateCoords(ISession session, object[] objData)
        {
            try
            {
                var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (botReceiver == null) return;

                botReceiver.Lat = (double)objData[0];
                botReceiver.Lng = (double)objData[1];

                botReceiver.LatStep = (botReceiver.Lat - botReceiver._lat) / (2000 / Pages.MapPage.Delay);
                botReceiver.LngStep = (botReceiver.Lng - botReceiver._lng) / (2000 / Pages.MapPage.Delay);

                botReceiver.PushNewRoutePoint(new PointLatLng(botReceiver.Lat, botReceiver.Lng));
                if (session != CurSession)
                {
                    botReceiver._lat = botReceiver.Lat;
                    botReceiver._lng = botReceiver.Lng;
                }
                else
                //if (session == CurSession)
                {
                    SettingsView.BotSettingsPage.UpdateCoordBoxes();
                    SettingsView.BotMapPage.UpdateCurrentBotCoords(botReceiver);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #region DataFlow - Push

        private static void PushNewConsoleRow(ISession session, string rowText, Color rowColor)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            botReceiver.LogQueue.Enqueue(Tuple.Create(rowText, rowColor));
            if (botReceiver.LogQueue.Count > 100)
            {
                botReceiver.LogQueue.Dequeue();
        }
        }

        private static void PushRemoveForceMoveMarker(ISession session)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            var nMapObj = new NewMapObject("forcemove_done", "", 0, 0, "");
            botReceiver?.MarkersQueue.Enqueue(nMapObj);
        }

        private void PushRemovePokemon(ISession session, MapPokemon mapPokemon)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            var nMapObj = new NewMapObject("pm_rm", mapPokemon.PokemonId.ToString(), mapPokemon.Latitude, mapPokemon.Longitude, mapPokemon.EncounterId.ToString());
            var queueCleanup = botReceiver.MarkersQueue.Where(x => x.Uid == mapPokemon.PokemonId.ToString()).ToList();
            if (queueCleanup.Any() && botReceiver != Bot)
            {
                botReceiver.MarkersQueue =
                    new Queue<NewMapObject>(botReceiver.MarkersQueue.Where(x => queueCleanup.Any(v => x == v)));
            }
            else
            {
                botReceiver.MarkersQueue.Enqueue(nMapObj);
            }
        }

        private static void PushNewPokemons(ISession session, IEnumerable<MapPokemon> pokemons)
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

        private static void PushNewWildPokemons(ISession session, IEnumerable<WildPokemon> pokemons)
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

        private static void PushNewPokestop(ISession session, IEnumerable<FortData> pstops)
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
                    SettingsView.consoleBox.AppendParagraph(t.Item1, t.Item2);
                    if (SettingsView.consoleBox.Document.Blocks.Count > 100)
                    {
                        var toRemove = SettingsView.consoleBox.Document.Blocks.ElementAt(0);
                        SettingsView.consoleBox.Document.Blocks.Remove(toRemove);
                    }
                    SettingsView.consoleBox.ScrollToEnd();
                }
                await Task.Delay(10);
            }
        }

        private async void RpcWorker()
        {
            int delay = 5;
            while (!_windowClosing)
            {
                if (_messageQueue.Count > 0)
                {
                    var message = _messageQueue.Dequeue();
                    //I dont like busy waiting :/
                    if (message == null) continue;
                    try
                    {
                        switch (message.Type)
                        {
                            case "bot_failure":
                                HandleFailure(message.Session, (bool) message.ParamObjects[0],
                                    (bool) message.ParamObjects[1]);
                                break;
                            case "log":
                                PushNewConsoleRow(message.Session, (string) message.ParamObjects[0],
                                    (Color) message.ParamObjects[1]);
                                break;
                            case "err":
                                PushNewError(message.Session);
                                break;
                            case "ps":
                                PushNewPokestop(message.Session, (IEnumerable<FortData>) message.ParamObjects[0]);
                                break;
                            case "pm":
                                PushNewPokemons(message.Session, (IEnumerable<MapPokemon>) message.ParamObjects[0]);
                                break;
                            case "pmw":
                                PushNewWildPokemons(message.Session, (IEnumerable<WildPokemon>) message.ParamObjects[0]);
                                break;
                            case "pm_rm":
                                PushRemovePokemon(message.Session, (MapPokemon) message.ParamObjects[0]);
                                break;
                            case "p_loc":
                                UpdateCoords(message.Session, message.ParamObjects);
                                break;
                            case "pm_list":
                                BuildPokemonList(message.Session, message.ParamObjects);
                                break;
                            case "item_list":
                                BuildItemList(message.Session, message.ParamObjects);
                                break;
                            case "new_version":
                                SetVersionTag((Version) message.ParamObjects[0]);
                                break;
                            case "item_new":
                                GotNewItems(message.Session, message.ParamObjects);
                                break;
                            case "route_next":
                                if (message.ParamObjects[0] != null)
                                    DrawNextRoute(message.Session, (List<Tuple<double, double>>) message.ParamObjects[0]);
                                break;
                            case "item_rem":
                                LostItem(message.Session, message.ParamObjects);
                                break;
                            case "pm_new":
                                GotNewPokemon(message.Session, message.ParamObjects);
                                break;
                            case "pm_rem":
                                LostPokemon(message.Session, message.ParamObjects);
                                break;
                            case "pm_upd":
                                PokemonChanged(message.Session, message.ParamObjects);
                                break;
                            case "team_set":
                                SetPlayerTeam(message.Session, message.ParamObjects);
                                break;
                            case "pm_fav":
                                PokemonFavouriteChanged(message.Session, message.ParamObjects);
                                break;
                            case "profile_data":
                                UpdateProfileInfo(message.Session, message.ParamObjects);
                                break;
                            case "forcemove_done":
                                PushRemoveForceMoveMarker(message.Session);
                                break;
                            case "ps_opt":
                                if (message.ParamObjects[0] != null)
                                    DrawNextOptRoute(message.Session, (List<Tuple<double, double>>)message.ParamObjects[0]);
                                break;
                            default:
                                PushNewConsoleRow(Bot?.Session, "Unknown message detected!", Colors.Red);
                                break;
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.Write(ex.Message, session: message.Session);
                    }
                }

                if (_messageQueue.Count > 50)
                    delay = 1;
                else if (delay == 1 && _messageQueue.Count == 0)
                    delay = 5;
                
                await Task.Delay(delay);
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
            try
            {
                var newBot = CreateBowWindowData(settings, profileName);
                var session = new Session(newBot.Settings, newBot.Logic);
                session.Client.ApiFailure = new ApiFailureStrategy(session);
                newBot.GlobalSettings.MapzenAPI.SetSession(session);

                newBot.Session = session;
                session.EventDispatcher.EventReceived += evt => newBot.Listener.Listen(evt, session);
                session.EventDispatcher.EventReceived += evt => newBot.Aggregator.Listen(evt, session);
                session.Navigation.UpdatePositionEvent += (lat, lng, alt) => session.EventDispatcher.Send(new UpdatePositionEvent {Latitude = lat, Longitude = lng, Altitude = alt});

                newBot.PokemonList.CollectionChanged += delegate { UpdatePokemonCollection(session); };
                newBot.ItemList.CollectionChanged += delegate { UpdateItemCollection(session); };

                newBot.Stats.DirtyEvent += () => { StatsOnDirtyEvent(newBot); };

                newBot._lat = settings.LocationSettings.DefaultLatitude;
                newBot._lng = settings.LocationSettings.DefaultLongitude;
                newBot.Machine.SetFailureState(new LoginState());
                GlobalMapView.addMarker(newBot.GlobalPlayerMarker);

                if (newBot.Logic.UseCustomRoute)
                {
                    if (!IsNullOrEmpty(newBot.GlobalSettings.LocationSettings.CustomRouteName))
                    {
                        var route =
                            GlobalCatchemSettings.Routes.FirstOrDefault(
                                x => x.Name.ToLower() == newBot.GlobalSettings.LocationSettings.CustomRouteName);
                        if (route != null)
                        {
                            newBot.GlobalSettings.LocationSettings.CustomRoute = route.Route;
                        }
                    }
                }
                else if (!IsNullOrEmpty(newBot.GlobalSettings.LocationSettings.CustomRouteName))
                {
                    newBot.GlobalSettings.LocationSettings.CustomRouteName = "";
                }
    #if DEBUG
                    SeedTheBot(newBot);
    #endif
                BotsCollection.Add(newBot);
                if (newBot.GlobalSettings.AutoStartThisProfile)
                    newBot.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Initializing of new bot failed! ex:\r\n" + ex.Message, "FatalError",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SeedTheBot(BotWindowData bot)
        {
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Mew, "Mew the awesome!", 1337, 99.9, PokemonFamilyId.FamilyMew, 42, 9001, true, true, 97, PokemonMove.Moonblast, PokemonMove.Thunder, PokemonType.Psychic, PokemonType.Flying, 9000, PokemonInfo.GetBaseStats(PokemonId.Mew), 90, 99, 100000, 42));
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Mewtwo, "Mr.Kickass", 9001, 100, PokemonFamilyId.FamilyMewtwo, 47, 9001, true, true, 97, PokemonMove.HyperBeam, PokemonMove.PsychoCutFast, PokemonType.Psychic, PokemonType.Flying, 10000, PokemonInfo.GetBaseStats(PokemonId.Mewtwo), 90, 99, 100000, 42));//PokemonId.Mew.ToInventorySource(),
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Zapdos, "Thunder", 1337, 100, PokemonFamilyId.FamilyZapdos, 47, 9001, true, true, 97, PokemonMove.HyperBeam, PokemonMove.PsychoCutFast, PokemonType.Psychic, PokemonType.Flying, 3000, PokemonInfo.GetBaseStats(PokemonId.Zapdos), 90, 99, 100000, 42));
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Articuno, "Ice-ice-baby", 4048, 100, PokemonFamilyId.FamilyArticuno, 47, 9001, true, true, 97, PokemonMove.HyperBeam, PokemonMove.PsychoCutFast, PokemonType.Psychic, PokemonType.Flying, 5000, PokemonInfo.GetBaseStats(PokemonId.Articuno), 90, 99, 100000, 42));
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Moltres, "Popcorn machine", 4269, 100, PokemonFamilyId.FamilyMoltres, 47, 9001, true, true, 97, PokemonMove.HyperBeam, PokemonMove.PsychoCutFast, PokemonType.Psychic, PokemonType.Flying, 5000, PokemonInfo.GetBaseStats(PokemonId.Moltres), 90, 99, 100000, 42));
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
                if (SettingsView.tabControl.IsEnabled)
                    SettingsView.tabControl.IsEnabled = false;
                if (SettingsView.grid_pickBot.Visibility == Visibility.Collapsed)
                    SettingsView.grid_pickBot.Visibility = Visibility.Visible;
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
            SettingsView.BotPlayerPage.UpdatePokemonsCount();
        }

        private void StatsOnDirtyEvent(BotWindowData bot)
        {
            if (bot == null) return;
            Dispatcher.BeginInvoke(new ThreadStart(bot.UpdateRunTime));
            if (Bot == bot)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    SettingsView.BotPlayerPage.UpdateRunTimeData();
                }));
            }
            bot.CheckForMaxCatch();
        }

        public void ClearPokemonData(BotWindowData calledBot)
        {
            if (Bot != calledBot) return;
            SettingsView.consoleBox.Document.Blocks.Clear();
            Bot.LatStep = Bot.LngStep = 0;
            SettingsView.BotMapPage.ClearData();
            SettingsView.BotPlayerPage.ClearData();
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
            if (!SettingsView.tabControl.IsEnabled)
                SettingsView.tabControl.IsEnabled = true;
            if (SettingsView.grid_pickBot.Visibility == Visibility.Visible)
                SettingsView.grid_pickBot.Visibility = Visibility.Collapsed;
            if (transit.SelectedIndex != 0) ChangeTransistorTo(0);
            SettingsView.BotSettingsPage.SetBot(Bot);
            SettingsView.BotPlayerPage.SetBot(Bot);
            SettingsView.BotPokemonListPage.SetBot(Bot);
            SettingsView.BotMapPage.SetBot(Bot);

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
            GlobalCatchemSettings.Save();
            SettingsView.BotMapPage.WindowClosing = true;
            if (Bot == null || _loadingUi) return;
            Bot.GlobalSettings.StoreData(SubPath + "\\" + Bot.ProfileName);
            TelegramView.SaveSettings();
            TelegramView.TurnOff();
            foreach (var b in BotsCollection)
            {
                b.Stop();
            }
            MapzenAPI.SaveKnownCoords();
        }

        private void btn_StartAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var bot in BotsCollection)
            {
                bot.Start();
                Task.Delay(330);
            }
        }
        private void InputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as System.Windows.Controls.TextBox;
            if (tb == null) return;
            if (tb.Text == @"Profile name here...")
                tb.Text = "";
        }
        private void btn_changeViewSettingsMap_Click(object sender, RoutedEventArgs e)
        {
            GlobalMapView.FitTheStuff();
            ChangeTransistorTo(1);
        }


        private void btn_ChangeViewToSettings_Click(object sender, RoutedEventArgs e)
        {
            ChangeTransistorTo(0);
        }

        private void btn_ChangeViewToTelegram_Click(object sender, RoutedEventArgs e)
        {
            ChangeTransistorTo(2);
        }

        private void btn_ChangeViewToRouteCreator_Click(object sender, RoutedEventArgs e)
        {
            ChangeTransistorTo(3);
        }

        private void ChangeTransistorTo(int i)
        {
            if (transit.SelectedIndex != i)
            {
                transit.SelectedIndex = i;
            }
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
                    var desiredName = rowData.Length > 6 ? rowData[6] : "";
                    var path = login;
                    var lat = rowData.Length > 7 ? rowData[7] : "";
                    var lon = rowData.Length > 8 ? rowData[8] : "";
                    var created = false;
                    do
                    {
                        if (!Directory.Exists(SubPath + "\\" + path))
                        {
                            CreateBotFromClone(path, login, auth, pass, proxy, proxyLogin, proxyPass, desiredName, lat, lon);
                            created = true;
                        }
                        else
                        {
                            path += DeviceSettings.RandomString(4);
                        }
                    } while (!created);
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            batch_botText.Text = Empty;
        }

        private void CreateBotFromClone(string path, string login, string auth, string pass, string proxy,
            string proxyLogin, string proxyPass, string desiredName, string lat, string lon)
        {
            var dir = Directory.CreateDirectory(SubPath + "\\" + path);
            var settings = GlobalSettings.Load(dir.FullName) ?? GlobalSettings.Load(dir.FullName);
            if (Bot != null)
            {
                settings = Bot.GlobalSettings.Clone();
                var profilePath = dir.FullName;
                var profileConfigPath = Path.Combine(profilePath, "config");
                settings.ProfilePath = profilePath;
                settings.ProfileConfigPath = profileConfigPath;
                settings.GeneralConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "config");
            }

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
            if (desiredName != "")
            {
                settings.DesiredNickname = desiredName;
                settings.StartUpSettings.AutoCompleteTutorial = true;
            }
            if (lat != "")
            {
                lat.GetVal(out settings.LocationSettings.DefaultLatitude);
            }
            if (lon != "")
            {
                lon.GetVal(out settings.LocationSettings.DefaultLongitude);
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
            try
            {
                var bot = botsBox.SelectedItem as BotWindowData;
                if (bot == null) return;
                BotsCollection.Remove(bot);
                Directory.Delete(SubPath + "\\" + bot.ProfileName, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There is an error while trying to delete your bot profile! ex:\r\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
