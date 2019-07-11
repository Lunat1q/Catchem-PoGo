using GMap.NET;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Enums;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using PokemonGo.RocketAPI.Enums;
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
using System.Windows.Controls;
using System.Windows.Shell;
using Catchem.Events;
using Catchem.MainWindowHelpers;
using Catchem.SupportForms;
using Catchem.UiTranslation;
using PoGo.PokeMobBot.Logic.DataStorage;
using PoGo.PokeMobBot.Logic.Enums;
using PoGo.PokeMobBot.Logic.Event.Player;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.Tasks;
using PoGo.PokeMobBot.Logic.Utils;

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
        private readonly WpfEventListener _listener;
        private readonly StatisticsAggregator _statisticsAggregator;
        private readonly SelfIntegrity _self;
        //protected string MagicString = "chineese cunts";
        //private long _nextMagic;

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
            _self = new SelfIntegrity();
            InitializeComponent();
            BotWindow = this;
            //FillMagic();
            Translation.InsertMissedLines();

            DbHandler.CheckCreated();
            TranslationEngine.Initialize();
            LanguageBox.ItemsSource = TranslationEngine.LangList;
            //global settings
            GlobalCatchemSettings.Load();
            LanguageBox.SelectedItem = GlobalCatchemSettings.UiLanguage;

            InitWindowsControlls();

            Task.Run(LogWorker);
            Task.Run(RpcWorker);
            Task.Run(() => _self.AntiPiracyWorker());
            Task.Run(InitBots);

            SelfIntegrity.PiracyCheck2();

            //InitBots();
            SettingsView.BotMapPage.SetSettingsPage(SettingsView.BotSettingsPage);
            SetVersionTag();

            SettingsView.BotMapPage.SetGlobalSettings(GlobalCatchemSettings);
            GlobalMapView.SetGlobalSettings(GlobalCatchemSettings);
            SettingsView.BotSettingsPage.SetGlobalSettings(GlobalCatchemSettings);
            RouteCreatorView.SetGlobalSettings(GlobalCatchemSettings);

            _listener = new WpfEventListener();
            _statisticsAggregator = new StatisticsAggregator();
        }

        //protected async void FillMagic()
        //{
        //    //MagicString = await SettingsView.BotSettingsPage.MagicCalc();
        //}


        private void ApplyLanguage(string langCode)
        {
            GlobalCatchemSettings.UiLanguage = langCode;
            TranslationEngine.SetLanguage(langCode);
            TranslationEngine.ApplyLanguage(SettingsView);
            TranslationEngine.ApplyLanguage(MenuGrid);
            TranslationEngine.ApplyLanguage(batchInput);
            TranslationEngine.ApplyLanguage(InputBox);
            TranslationEngine.ApplyLanguage(GlobalMapView);
            TranslationEngine.ApplyLanguage(RouteCreatorView);
            TranslationEngine.ApplyLanguage(TelegramView);
        }

        public void SetVersionTag(Version remoteVersion = null)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Title =
                    $"Catchem - v{Assembly.GetExecutingAssembly().GetName().Version} {(remoteVersion != null ? $"New release - {remoteVersion}" : "")}";
            }));
        }

        private void InitWindowsControlls()
        {
            SettingsView.BotSettingsPage.SubPath = SubPath;
        }

        internal async Task InitBots()
        {
            try
            {
                Logger.SetLogger(new WpfLogger(LogLevel.Debug), SubPath);
                Dispatcher.Invoke(new ThreadStart(delegate
                {
                    botsBox.ItemsSource = BotsCollection;
                    SettingsView.GridPickBotAndPromoText.Visibility = Visibility.Visible;
                }));

                foreach (var item in Directory.GetDirectories(SubPath))
                {
                    if (item != SubPath + "\\Logs")
                    {
                        var gs = GlobalSettings.Load(item);
                        await Dispatcher.BeginInvoke(new ThreadStart(delegate { InitBot(gs, Path.GetFileName(item)); }));
                    }
                }

                //If we have few bots no need to collapse them
                if (BotsCollection.Count < 4)
                    foreach (var b in BotsCollection)
                        b.ExpandedPanel = true;
            }
            catch (Exception)
            {
                //ignore
            }
        }


        public void ReceiveMsg(MainRpc msgType, ISession session, params object[] objData)
        {
            if (session == null) return;
            _messageQueue.Enqueue(new BotRpcMessage(msgType, session, objData));
        }

        private void HandleFailure(ISession session, bool shutdown, bool stop, string message)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null || !receiverBot.Started) return;
            if (shutdown)
                Environment.Exit(0);
            if (!stop) return;
            receiverBot.Stop();
            ClearPokemonData(receiverBot);
            if (!IsNullOrEmpty(message))
            {
                receiverBot.LogQueue.Enqueue(Tuple.Create(message, Colors.Red));
            }
            if (session.LogicSettings.RestartBotOnFailure)
            {
                RestartBotAfterFailure(session);
            }
        }

        private static async void RestartBotAfterFailure(ISession session)
        {
            var i = 0;
            var retries = session.LogicSettings.FailureMaxRetries;
            if (retries <= 0 || retries >= 11)
            {
                retries = 3;
            }
            while (i <= retries)
            {
                i++;
                if (session.LogicSettings.FailureRestartDelay > 0)
                {
                    await Task.Delay(session.LogicSettings.FailureRestartDelay);
                }
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null || receiverBot.Started) return;
                try
                {
                    receiverBot.Start();
                }
                catch (Exception)
                {
                    //ignore
                }
                if (!receiverBot.Started)
                {
                    receiverBot.LogQueue.Enqueue(Tuple.Create(TranslationEngine.GetDynamicTranslationString("%RESTART_FAILURE%", "Failed to Restart Bot!"), Colors.Red));
                    continue;
                }
                break;
            }
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

        private void LostItem(ISession session, object[] objData)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null) return;
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                receiverBot.LostItem((ItemId?) objData[0],
                    (int?) objData[1]);
            }));
            UpdateItemCollection(session);
        }

        private void ItemUsed(ISession session, object[] objData)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null) return;
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                receiverBot.UsedItem((ItemId?)objData[0],
                    (long?)objData[1]);
            }));
            UpdateItemCollection(session);
        }

        private void GotNewItems(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                Dispatcher.BeginInvoke(
                    new ThreadStart(delegate { receiverBot.GotNewItems((List<Tuple<ItemId, int>>) objData[0]); }));
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
            targetBot.NewPlayerData((string) objData[0], (int) objData[1], (int) objData[2], (TeamColor) objData[4],
                (int) objData[5], (int) objData[3]);
            if (targetBot == Bot)
            {
                SettingsView.BotPlayerPage.UpdatePlayerTab();
                SettingsView.BotPokePage.UpdatePokemonsCount();
                SettingsView.BotPokedexPage.UpdatePokedexCount();
            }
        }

        private void LostPokemon(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                Dispatcher.BeginInvoke(
                    new ThreadStart(
                        delegate
                        {
                            receiverBot.LostPokemon((ulong) objData[0], (PokemonFamilyId?) objData[1], (int?) objData[2]);
                        }));
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
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                var evt = objData[0] as BaseNewPokemonEvent;
                var captureType = (string) objData[1];
                if (evt == null || IsNullOrEmpty(captureType)) return;
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    receiverBot.GotNewPokemon(
                        (BaseNewPokemonEvent) objData[0], captureType);
                }));
                TelegramView.TlgrmBot.EventDispatcher.Send(new TelegramPokemonCaughtEvent
                {
                    PokemonId = evt.Id,
                    Cp = evt.Cp,
                    Iv = evt.Perfection,
                    ProfileName = receiverBot.ProfileName,
                    BotNicnname = receiverBot.PlayerName,
                    Level = evt.Level,
                    Move1 = evt.Move1,
                    Move2 = evt.Move2,
                    CaptureType = captureType,
                    Lat = evt.Latitude,
                    Lng = evt.Longitude
                });
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Player_Leveled(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    TelegramView.TlgrmBot.EventDispatcher.Send(new TelegramPlayerLevelUpEvent
                    {
                        Level = receiverBot.Level,
                        InventoryFull = (bool) objData[1],
                        Items = (string) objData[2],
                        ProfileName = receiverBot.ProfileName,
                        BotNicName = receiverBot.PlayerName
                    });
                }));
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
                        Tuple.Create("Can't retrieve pokemon list, servers are unstable or you can be banned!",
                            Colors.Red));
                    return;
                }
                var receivedList = (List<PokemonData>) objData[0];
                Dispatcher.BeginInvoke(new ThreadStart(delegate { receiverBot.BuildPokemonList(receivedList); }));
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                SettingsView.BotPokePage.UpdatePokemons();
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
                Dispatcher.Invoke(new ThreadStart(delegate { receiverBot.ItemList.Clear(); }));
                foreach (var x in (List<ItemData>) objData[0])
                {
                    if (x.Count > 0)
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            receiverBot.ItemList.Add(
                                new ItemUiData(x.ItemId,
                                    x.ItemId.ToInventoryName(), x.Count, receiverBot));
                        }));
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

                botReceiver.Lat = (double) objData[0];
                botReceiver.Lng = (double) objData[1];

                botReceiver.LatStep = (botReceiver.Lat - botReceiver._lat)/(2000/Pages.MapPage.Delay);
                botReceiver.LngStep = (botReceiver.Lng - botReceiver._lng)/(2000/Pages.MapPage.Delay);

                botReceiver.PushNewRoutePoint(new PointLatLng(botReceiver.Lat, botReceiver.Lng));
                if (session != CurSession)
                {
                    Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        botReceiver._lat = botReceiver.Lat;
                        botReceiver._lng = botReceiver.Lng;
                    }));
                }
                else
                {
                    Dispatcher.Invoke(new ThreadStart(delegate
                    {
                        SettingsView.BotSettingsPage.UpdateCoordBoxes();
                        SettingsView.BotMapPage.UpdateCurrentBotCoords(botReceiver);
                    }));
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async void Buddy(ISession session, object[] param)
        {
            if (session == null || param[0] == null) return;
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            switch ((string)param[0])
            {
                case "Set":
                    if (param.Length != 2) return;
                    var newBuddy = (BuddyPokemon) param[1];
                    session.Profile.PlayerData.BuddyPokemon = newBuddy;
                    session.BuddyPokemon = await session.Inventory.GetBuddyPokemon(session.Profile.PlayerData.BuddyPokemon.Id);
                    SettingsView.BotPlayerPage.UpdateBuddyPokemon(botReceiver);
                    break;
                case "Walked":
                    if (param.Length != 4) return;
                    var candyEarnedCount = (int) param[1];
                    var familyCandyId = (PokemonFamilyId) param[2];
                    var success = (bool) param[3];
                    if (success && familyCandyId != PokemonFamilyId.FamilyUnset)
                    {
                        SettingsView.BotPlayerPage.UpdateBuddyCandies(candyEarnedCount);
                        SettingsView.BotPokePage.UpdateFamilyCandies(familyCandyId, candyEarnedCount);
                    }
                    break;
            }

            var walked = botReceiver.Session?.PlayerStats?.KmWalked -
                                 botReceiver.Session?.Profile?.PlayerData?.BuddyPokemon?.StartKmWalked;
            if (walked != null)
            {
                SettingsView.BotPlayerPage.UpdateBuddyWalked((double)walked);
            }
        }

        private void Challenge(ISession session, object[] param)
        {
            if (session == null || param[0] == null) return;
            var targetBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (targetBot == null) return;
            switch ((string) param[0])
            {
                case "Check":
                    if (param.Length != 3) return;
                    var challengeUrl = (string) param[1];
                    var showChallenge = (bool) param[2];
                    if (showChallenge)
                    {
                        targetBot.Session.CaptchaChallenge = true;
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            TelegramView.TlgrmBot.EventDispatcher.Send(new TelegramCaptchaRequiredEvent
                            {
                                BotNicName = targetBot.PlayerName,
                                ProfileName = targetBot.ProfileName
                            });
                            var captchaWindow = new ChallengeBox();
                            captchaWindow.DoChallenge(session, challengeUrl);
                        }));
                    }
                    break;
                case "Verify":
                    if (param.Length != 2) return;
                    var success = (bool) param[1];
                    if (success)
                        targetBot.Session.CaptchaChallenge = false;
                    break;
                default:
                    break;
            }
        }


        #region DataFlow - Push

        private void PushRemoveForceMoveMarker(ISession session)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            var nMapObj = new NewMapObject(MapPbjectType.ForceMoveDone, "", 0, 0, "");
            if (botReceiver.Started)
            {
                botReceiver.MarkersQueue.Enqueue(nMapObj);
                MarkTaskbarIcon("force_move");
            }
        }


        private void PushRemovePokemon(ISession session, ulong encounterId)
        {
            var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            var nMapObj = new NewMapObject(MapPbjectType.PokemonRemove, "", 0, 0, encounterId.ToString());
            var queueCleanup = botReceiver.MarkersQueue.Where(x => x.Uid == encounterId.ToString()).ToList();
            if (queueCleanup.Any() && botReceiver != Bot && !GlobalCatchemSettings.KeepPokeMarkersOnMap)
            {
                botReceiver.MarkersQueue =
                    new Queue<NewMapObject>(botReceiver.MarkersQueue.Where(x => queueCleanup.All(v => x != v)));
            }
            else
            {
                if (botReceiver.Started)
                    botReceiver.MarkersQueue.Enqueue(nMapObj);
            }
        }

        #endregion

        #region Async Workers

        private async Task LogWorker()
        {
            var delay = 10;
            while (!_windowClosing)
            {
                try
                {
                    if (Bot != null && Bot.LogQueue.Count > 0)
                    {
                        var t = Bot.LogQueue?.Dequeue();
                        if (t == null)
                            continue;
                        Bot.Log?.Add(t);
                        Dispatcher?.Invoke(new ThreadStart(delegate
                        {
                            try
                            {
                                SettingsView.ConsoleBox.AppendParagraph(t.Item1, t.Item2);
                                if (SettingsView.ConsoleBox.Document.Blocks.Count >
                                    GlobalCatchemSettings.ConsoleRowsToShow)
                                {
                                    var toRemove = SettingsView?.ConsoleBox?.Document?.Blocks.ElementAtOrDefault(0);
                                    if (toRemove != null)
                                        SettingsView?.ConsoleBox?.Document?.Blocks.Remove(toRemove);
                                }
                                SettingsView.ConsoleBox?.ScrollToEnd();
                            }
                            catch (Exception ex)
                            {
                                Logger.Write(ex.Message);
                            }
                        }));
                        if (Bot?.LogQueue.Count > 20)
                        {
                            delay = 0;
                        }
                        else if (Bot?.LogQueue.Count == 0)
                        {
                            delay = 10;
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Write(ex.Message);
                }
                await Task.Delay(delay);
            }
        }

        private async Task RpcWorker()
        {
            var delay = 5;
            while (!_windowClosing)
            {
                if (_messageQueue.Count > 0)
                {
                    //PiracyCheck2();
                    var message = _messageQueue.Dequeue();
                    //I dont like busy waiting :/
                    if (message == null) continue;
                    try
                    {
                        switch (message.Type)
                        {
                            case MainRpc.BotFailure:
                                HandleFailure(message.Session, (bool) message.ParamObjects[0],
                                    (bool) message.ParamObjects[1], (string) message.ParamObjects[2]);
                                break;
                            case MainRpc.Log:
                                RpcHelper.PushNewConsoleRow(message.Session, (string) message.ParamObjects[0],
                                    (Color) message.ParamObjects[1]);
                                break;
                            case MainRpc.Error:
                                RpcHelper.PushNewError(message.Session);
                                break;
                            case MainRpc.PokeStops:
                                RpcHelper.PushNewPokestop(message.Session, (IEnumerable<FortData>) message.ParamObjects[0]);
                                break;
                            case MainRpc.Pokemons:
                                RpcHelper.PushNewPokemons(message.Session, (IEnumerable<MapPokemon>) message.ParamObjects[0]);
                                break;
                            case MainRpc.PokemonsWild:
                                RpcHelper.PushNewWildPokemons(message.Session, (IEnumerable<WildPokemon>) message.ParamObjects[0]);
                                break;
                            case MainRpc.PokemonMapRemove:
                                PushRemovePokemon(message.Session, (ulong) message.ParamObjects[0]);
                                break;
                            case MainRpc.PokestopSetLure:
                                RpcHelper.UpdateLure(message.Session, true, (string) message.ParamObjects[0]);
                                break;
                            case MainRpc.PlayerLocation:
                                UpdateCoords(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.PokemonList:
                                BuildPokemonList(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.ItemList:
                                BuildItemList(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.NewVersion:
                                SetVersionTag((Version) message.ParamObjects[0]);
                                break;
                            case MainRpc.ItemNew:
                                GotNewItems(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.RouteNext:
                                if (message.ParamObjects[0] != null)
                                    DrawNextRoute(message.Session, (List<Tuple<double, double>>) message.ParamObjects[0]);
                                break;
                            case MainRpc.ItemRemove:
                                LostItem(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.PokemonNew:
                                GotNewPokemon(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.PokemonLost:
                                LostPokemon(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.PokemonUpdate:
                                RpcHelper.PokemonChanged(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.TeamSet:
                                SetPlayerTeam(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.PokemonFav:
                                RpcHelper.PokemonFavouriteChanged(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.Profile:
                                UpdateProfileInfo(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.ForceMoveDone:
                                PushRemoveForceMoveMarker(message.Session);
                                break;
                            case MainRpc.GymPoke:
                                RpcHelper.PushNewGymPoke(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.PokestopsOpt:
                                if (message.ParamObjects[0] != null)
                                    DrawNextOptRoute(message.Session,
                                        (List<Tuple<double, double>>) message.ParamObjects[0]);
                                break;
                            case MainRpc.Lvl:
                                Player_Leveled(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.PokestopInfo:
                                RpcHelper.UpdatePsInDatabase(message.ParamObjects);
                                break;
                            case MainRpc.PokeActionDone:
                                RpcHelper.PokemonActionDone(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.ItemUsed:
                                ItemUsed(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.Buddy:
                                Buddy(message.Session, message.ParamObjects);
                                break;
                            case MainRpc.Challenge:
                                Challenge(message.Session, message.ParamObjects);
                                break;
                            default:
                                RpcHelper.PushNewConsoleRow(Bot?.Session, "Unknown message detected!", Colors.Red);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Write(ex.Message + " - " + message.Type, session: message.Session);
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
            if (input.Length == 0 || input.Length > 32) return;

            var invalidChars = Path.GetInvalidFileNameChars();
            input = input.RemoveUnwantedChars(invalidChars);

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
                session.EggWalker.Eggs = new MtObservableCollection<PokeEgg>();
                session.PokeDex = new MtObservableCollection<PokeDexRecord>();
                foreach (PokemonId pid in Enum.GetValues(typeof(PokemonId)))
                {
                    if (pid == PokemonId.Missingno) continue;
                    session.PokeDex.Add(new PokeDexRecord
                    {
                        Id = pid,
                        PokemonName = session.Translation.GetPokemonName(pid)
                    });
                }

                session.ActionQueue = new MtObservableCollection<ManualAction>();

                newBot.GlobalSettings.MapzenAPI.SetSession(session);

                newBot.Session = session;
                session.EventDispatcher.EventReceived += evt => _listener.Listen(evt, session);
                session.EventDispatcher.EventReceived += evt => _statisticsAggregator.Listen(evt, session);
                session.Navigation.UpdatePositionEvent +=
                    (lat, lng, alt) =>
                        session.EventDispatcher.Send(new UpdatePositionEvent
                        {
                            Latitude = lat,
                            Longitude = lng,
                            Altitude = alt
                        });

                newBot.PokemonList.CollectionChanged += delegate { UpdatePokemonCollection(session); };
                newBot.ItemList.CollectionChanged += delegate { UpdateItemCollection(session); };

                session.Stats.DirtyEvent += () => { StatsOnDirtyEvent(newBot); };

                newBot._lat = settings.LocationSettings.DefaultLatitude;
                newBot._lng = settings.LocationSettings.DefaultLongitude;
                newBot.Machine.SetFailureState(new LoginState());
                GlobalMapView.AddMarker(newBot.GlobalPlayerMarker);

                if (newBot.Logic.UseCustomRoute)
                {
                    if (!IsNullOrEmpty(newBot.GlobalSettings.LocationSettings?.CustomRouteName))
                    {
                        var route =
                            GlobalCatchemSettings?.Routes?.FirstOrDefault(
                                x => string.Equals(x.Name, newBot.GlobalSettings.LocationSettings.CustomRouteName,
                                    StringComparison.CurrentCultureIgnoreCase));
                        if (route != null)
                        {
                            newBot.GlobalSettings.LocationSettings.CustomRoute = route.Route;
                        }
                        else
                        {
                            newBot.GlobalSettings.LocationSettings.UseCustomRoute = false;
                            newBot.GlobalSettings.LocationSettings.CustomRouteName = "";
                        }
                    }
                }
                else if (!IsNullOrEmpty(newBot.GlobalSettings.LocationSettings.CustomRouteName))
                {
                    newBot.GlobalSettings.LocationSettings.CustomRouteName = "";
                }
#if DEBUG
                DebugHelper.SeedTheBot(newBot);
                MarkTaskbarIcon("force_move");
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
                if (SettingsView.GridPickBotAndPromoText.Visibility == Visibility.Collapsed)
                    SettingsView.GridPickBotAndPromoText.Visibility = Visibility.Visible;
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
            SettingsView.BotPokePage.UpdatePokemonsCount();
            SettingsView.BotPokedexPage.UpdatePokedexCount();
        }

        private void StatsOnDirtyEvent(BotWindowData bot)
        {
            if (bot == null) return;
            Dispatcher.BeginInvoke(new ThreadStart(bot.UpdateRunTime));
            if (Bot == bot)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate { SettingsView.BotPlayerPage.UpdateRunTimeData(); }));
                Dispatcher.BeginInvoke(new ThreadStart(delegate { SettingsView.BotPokedexPage.UpdatePokedexCount(); }));
            }
            bot.AutoPauseCheck();
        }

        public void ClearPokemonData(BotWindowData calledBot)
        {
            if (Bot != calledBot || Bot == null) return;
            Bot.LatStep = Bot.LngStep = 0;
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                SettingsView.ConsoleBox?.Document?.Blocks.Clear();
                SettingsView.ClearData();
            }));
        }

        public void SetPokemonData(BotWindowData calledBot)
        {
            if (Bot != calledBot || Bot == null) return;
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                SettingsView.SetData();
            }));
        }

        private static BotWindowData CreateBowWindowData(GlobalSettings s, string name)
        {
            return new BotWindowData(name, s, new StateMachine(), new ClientSettings(s), new LogicSettings(s));
        }

        private void RebuildUi()
        {
            if (Bot == null || _loadingUi) return;

            _loadingUi = true;
            if (!SettingsView.tabControl.IsEnabled)
                SettingsView.tabControl.IsEnabled = true;
            if (SettingsView.GridPickBotAndPromoText.Visibility == Visibility.Visible)
                SettingsView.GridPickBotAndPromoText.Visibility = Visibility.Collapsed;
            if (transit.SelectedIndex != 0) ChangeTransistorTo(0);
            SettingsView.SetBot(Bot);

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
            TelegramView.SaveSettings();
            Logger.FlushAndExit = true;
            TelegramView.TurnOff();
            DbHandler.StopDb();
            if (_loadingUi) return;
            Bot?.GlobalSettings.StoreData(SubPath + "\\" + Bot.ProfileName);
            foreach (var b in BotsCollection)
            {
                b.Stop();
            }
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
            var tb = sender as TextBox;
            if (tb == null) return;
            if (tb.Text == TranslationEngine.GetDynamicTranslationString("%PROFILE_HERE%",@"Profile name here..."))
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
            var bot = BotsCollection.FirstOrDefault();
            if (bot != null)
            {
                RouteCreatorView.SetMapPostion(bot.GlobalSettings.LocationSettings.DefaultLatitude,
                    bot.GlobalSettings.LocationSettings.DefaultLongitude);
            }
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

            var inputRows = input.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
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
                            CreateBotFromClone(path, login, auth, pass, proxy, proxyLogin, proxyPass, desiredName, lat,
                                lon);
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
                GlobalMapView.RemoveMarker(bot.GlobalPlayerMarker);
                BotsCollection.Remove(bot);
                Directory.Delete(SubPath + "\\" + bot.ProfileName, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There is an error while trying to delete your bot profile! ex:\r\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkTaskbarIcon(string markType)
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                if (IsActive) return;
                switch (markType)
                {
                    case "force_move":
                        TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                        TaskbarItemInfo.ProgressValue = 1;
                        TaskbarItemInfo.Overlay = Properties.Resources.force_move.LoadBitmap();
                        break;
                    default:
                        TaskbarItemInfo.Overlay = Properties.Resources.no_name.LoadBitmap();
                        break;
                }
            }));
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            TaskbarItemInfo.Overlay = null;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            TaskbarItemInfo.ProgressValue = 0;
        }

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null || cb.SelectedIndex < 0) return;
            ApplyLanguage(cb.SelectedItem as string);
        }
    }
}