using GMap.NET;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using POGOProtos.Data;
using POGOProtos.Inventory.Item;
using static System.String;
using LogLevel = PoGo.PokeMobBot.Logic.Logging.LogLevel;

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
                _bot = value;
                SelectBot(_bot);
            }
        }

        private GMapMarker _playerMarker;
        private GMapRoute _playerRoute;

        private bool _loadingUi;
        private bool _followThePlayerMarker;

        public MainWindow()
        {
            InitializeComponent();
            InitWindowsComtrolls();
            InitializeMap();
            BotWindow = this;
            LogWorker();
            MarkersWorker();
            MovePlayer();
            InitBots();
        }

        private void InitWindowsComtrolls()
        {
            authBox.ItemsSource = Enum.GetValues(typeof(AuthType));
        }

        private async void InitializeMap()
        {
            pokeMap.Bearing = 0;
            pokeMap.CanDragMap = true;
            pokeMap.DragButton = MouseButton.Left;
            pokeMap.MaxZoom = 18;
            pokeMap.MinZoom = 2;
            pokeMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            pokeMap.ShowCenter = false;
            pokeMap.ShowTileGridLines = false;
            pokeMap.Zoom = 18;            
            GMap.NET.MapProviders.GMapProvider.WebProxy = System.Net.WebRequest.GetSystemWebProxy();
            GMap.NET.MapProviders.GMapProvider.WebProxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            pokeMap.MapProvider = GMap.NET.MapProviders.GMapProviders.GoogleMap;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            if (Bot != null)
                pokeMap.Position = new PointLatLng(Bot.Lat, Bot.Lng);
            await Task.Delay(10);
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
                case "item_new":
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        GotNewItems(session, objData);
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
                var family = (PokemonFamilyId)objData[4];
                var candy = (int)objData[5];
                var pokemonToUpdate = receiverBot.PokemonList.FirstOrDefault(x => x.Id == (ulong)objData[0]);
                if (pokemonToUpdate == null) return;
                pokemonToUpdate.Cp = (int)objData[2];
                pokemonToUpdate.Iv = (double)objData[3];               
                foreach (var pokemon in receiverBot.PokemonList.Where(x => x.Family == family))
                {
                    pokemon.Candy = candy;
                }

            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LostItem(ISession session, object[] objData)
        {
            var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
            var lostAmount = (int) objData[1];
            if (receiverBot != null)
            {
                var targetItem = receiverBot.ItemList.FirstOrDefault(x => x.Id == (ItemId)objData[0]);
                if (targetItem == null) return;
                if (targetItem.Amount <= lostAmount)
                    receiverBot.ItemList.Remove(targetItem);
                else
                    targetItem.Amount -= lostAmount;
            }
            UpdateItemCollection(session);
        }

        private void GotNewItems(ISession session, object[] objData)
        {
            try
            {
                var newItems = (List<Tuple<ItemId, int>>)objData[0];
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                foreach (var item in newItems)
                {
                    var targetItem = receiverBot.ItemList.FirstOrDefault(x => x.Id == item.Item1);
                    if (targetItem != null)
                        targetItem.Amount += item.Item2;
                    else
                        receiverBot.ItemList.Add(new ItemUiData(
                            item.Item1, 
                            item.Item1.ToInventorySource(), 
                            item.Item1.ToInventoryName(), 
                            item.Item2));
                }
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
            targetBot.PlayerName = (string)objData[0];
            targetBot.MaxItemStorageSize = (int)objData[1];
            targetBot.MaxPokemonStorageSize = (int)objData[2];
            targetBot.Team = (TeamColor)objData[4];
            targetBot.Coins = (int)objData[3];
            if (targetBot == Bot)
                UpdatePlayerTab(targetBot);
        }

        private void UpdatePlayerTab(BotWindowData targetBot)
        {            
            l_coins.Content = targetBot.Coins;
            Playername.Content = targetBot.PlayerName;
            switch (targetBot.Team)
            {
                case TeamColor.Neutral:
                    team_image.Source = Properties.Resources.team_neutral.LoadBitmap();
                    break;
                case TeamColor.Blue:
                    team_image.Source = Properties.Resources.team_mystic.LoadBitmap();
                    break;
                case TeamColor.Red:
                    team_image.Source = Properties.Resources.team_valor.LoadBitmap();
                    break;
                case TeamColor.Yellow:
                    team_image.Source = Properties.Resources.team_instinct.LoadBitmap();
                    break;
            }
            l_poke_inventory.Content = $"({targetBot.PokemonList.Count}/{targetBot.MaxPokemonStorageSize})";
            l_inventory.Content = $"({Bot.ItemList.Sum(x => x.Amount)}/{Bot.MaxItemStorageSize})";
        }

        private void LostPokemon(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                var targetPokemon = receiverBot?.PokemonList.FirstOrDefault(x => x.Id == (ulong) objData[0]);
                if (targetPokemon == null) return;
                receiverBot.PokemonList.Remove(targetPokemon);
                if (objData[1] != null && objData[2] != null)
                {
                    var family = (PokemonFamilyId)objData[1];
                    var candy = (int)objData[2];
                    foreach (var pokemon in receiverBot.PokemonList.Where(x => x.Family == family))
                    {
                        pokemon.Candy = candy;
                    }
                }
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
                var pokemonId = (PokemonId) objData[1];
                var family = (PokemonFamilyId) objData[4];
                var candy = (int)objData[5];
                receiverBot.PokemonList.Add(new PokemonUiData(
                    (ulong) objData[0],
                    pokemonId,
                    pokemonId.ToInventorySource(), 
                    pokemonId.ToString(), 
                    (int) objData[2], 
                    (double) objData[3],
                    family,
                    candy,
                    (ulong)DateTime.UtcNow.ToUnixTime()));
                foreach (var pokemon in receiverBot.PokemonList.Where(x => x.Family == family))
                {
                    pokemon.Candy = candy;
                    pokemon.UpdateTags(receiverBot.Logic);
                }
                
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async void BuildPokemonList(ISession session, object[] objData)
        {
            try
            {
                var receiverBot = BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot != null)
                {
                    receiverBot.PokemonList = new ObservableCollection<PokemonUiData>();
                    receiverBot.PokemonList.CollectionChanged += delegate { UpdatePokemonCollection(session); };
                    var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
                    var pokemonSettings = (await session.Inventory.GetPokemonSettings()).ToList();
                    foreach (var pokemon in (List<Tuple<PokemonData, double, int>>) objData[0])
                    {
                        var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.Item1.PokemonId);
                        var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                        var mon = new PokemonUiData(
                            pokemon.Item1.Id,
                            pokemon.Item1.PokemonId,
                            pokemon.Item1.PokemonId.ToInventorySource(),
                            (pokemon.Item1.Nickname == "" ? pokemon.Item1.PokemonId.ToString() : pokemon.Item1.Nickname),
                            pokemon.Item1.Cp,
                            pokemon.Item2,
                            family.FamilyId,
                            family.Candy_,
                            pokemon.Item1.CreationTimeMs);
                        receiverBot.PokemonList.Add(mon);
                        mon.UpdateTags(receiverBot.Logic);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                PokeListBox.ItemsSource = Bot.PokemonList;
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

                ItemListBox.ItemsSource = Bot.ItemList;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void UpdateItemCollection(ISession session)
        {
            if (Bot == null || session != CurSession) return;
            l_inventory.Content = $"({Bot.ItemList.Sum(x => x.Amount)}/{Bot.MaxItemStorageSize})";
        }

        private void UpdateCoords(ISession session, object[] objData)
        {
            try
            {
                if (session != CurSession)
                {
                    var botReceiver = BotsCollection.FirstOrDefault(x => x.Session == session);
                    if (botReceiver == null) return;
                    botReceiver.Lat = botReceiver._lat = (double) objData[0];
                    botReceiver.Lng = botReceiver._lng = (double) objData[1];
                }
                else
                {
                    Bot.MoveRequired = true;
                    if (Math.Abs(Bot._lat) < 0.001 && Math.Abs(Bot._lng) < 0.001)
                    {
                        Bot.Lat = Bot._lat = (double) objData[0];
                        Bot.Lng = Bot._lng = (double) objData[1];
                        Dispatcher.BeginInvoke(new ThreadStart(delegate { pokeMap.Position = new PointLatLng(Bot.Lat, Bot.Lng); }));
                    }
                    else
                    {
                        Bot.Lat = (double) objData[0];
                        Bot.Lng = (double) objData[1];
                    }

                    if (_playerMarker == null)
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(DrawPlayerMarker));
                    }
                    else
                    {
                        Bot.GotNewCoord = true;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void DrawPlayerMarker()
        {
            if (_playerMarker == null)
            {
                _playerMarker = new GMapMarker(new PointLatLng(Bot.Lat, Bot.Lng))
                {
                    Shape = Properties.Resources.trainer.ToImage("Player"),
                    Offset = new Point(-14, -40),
                    ZIndex = 15
                };
                pokeMap.Markers.Add(_playerMarker);
                _playerRoute = Bot.PlayerRoute;
                pokeMap.Markers.Add(_playerRoute);
            }
            else
            {
                _playerMarker.Position = new PointLatLng(Bot.Lat, Bot.Lng);
            }
            if (Bot.ForceMoveMarker != null && !pokeMap.Markers.Contains(Bot.ForceMoveMarker))
                pokeMap.Markers.Add(Bot.ForceMoveMarker);
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

        private async void MovePlayer()
        {
            const int delay = 25;
            while (!_windowClosing)
            {
                if (Bot != null && _playerMarker != null && Bot.Started)
                {
                    if (Bot.MoveRequired)
                    {
                        if (Bot.GotNewCoord)
                        {
                            // ReSharper disable once PossibleLossOfFraction
                            Bot.LatStep = (Bot.Lat - Bot._lat)/(2000/delay);
                            // ReSharper disable once PossibleLossOfFraction
                            Bot.LngStep = (Bot.Lng - Bot._lng)/(2000/delay);
                            Bot.GotNewCoord = false;
                            UpdateCoordBoxes();
                            Bot.PushNewRoutePoint(new PointLatLng(Bot.Lat, Bot.Lng));
                            pokeMap.UpdateLayout();
                            _playerRoute.RegenerateShape(pokeMap);
                        }

                        Bot._lat += Bot.LatStep;
                        Bot._lng += Bot.LngStep;
                        _playerMarker.Position = new PointLatLng(Bot._lat, Bot._lng);
                        if (Math.Abs(Bot._lat - Bot.Lat) < 0.000000001 && Math.Abs(Bot._lng - Bot.Lng) < 0.000000001)
                            Bot.MoveRequired = false;
                        if (_followThePlayerMarker)
                            pokeMap.Position = new PointLatLng(Bot._lat, Bot._lng);
                    }
                }
                await Task.Delay(delay);
            }
        }


        private async void MarkersWorker()
        {
            while (!_windowClosing)
            {
                if (Bot?.MarkersQueue.Count > 0)
                {
                    try
                    {
                        var newMapObj = Bot.MarkersQueue.Dequeue();
                        switch (newMapObj.OType)
                        {
                            case "ps":
                                if (!Bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
                                    {
                                        Shape = Properties.Resources.pstop.ToImage("PokeStop"), Offset = new Point(-16, -32), ZIndex = 5
                                    };
                                    pokeMap.Markers.Add(marker);
                                    Bot.MapMarkers.Add(newMapObj.Uid, marker);
                                }
                                break;
                            case "ps_lured":
                                if (!Bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
                                    {
                                        Shape = Properties.Resources.pstop_lured.ToImage("Lured PokeStop"), Offset = new Point(-16, -32), ZIndex = 5
                                    };
                                    pokeMap.Markers.Add(marker);
                                    Bot.MapMarkers.Add(newMapObj.Uid, marker);
                                }
                                break;
                            case "pm_rm":
                                if (Bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    pokeMap.Markers.Remove(Bot.MapMarkers[newMapObj.Uid]);
                                    Bot.MapMarkers.Remove(newMapObj.Uid);
                                }
                                break;
                            case "forcemove_done":
                                if (Bot.ForceMoveMarker != null)
                                {
                                    pokeMap.Markers.Remove(Bot.ForceMoveMarker);
                                    Bot.ForceMoveMarker = null;
                                }
                                break;
                            case "pm":
                                if (!Bot.MapMarkers.ContainsKey(newMapObj.Uid))
                                {
                                    CreatePokemonMarker(newMapObj);
                                }
                                break;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                await Task.Delay(10);
            }
        }

        private void CreatePokemonMarker(NewMapObject newMapObj)
        {
            var pokemon = (PokemonId) Enum.Parse(typeof(PokemonId), newMapObj.OName);

            var marker = new GMapMarker(new PointLatLng(newMapObj.Lat, newMapObj.Lng))
            {
                Shape = pokemon.ToImage(), Offset = new Point(-15, -30), ZIndex = 10
            };
            pokeMap.Markers.Add(marker);
            Bot.MapMarkers.Add(newMapObj.Uid, marker);
        }

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

        private static async void EvolvePokemon(ISession session, PokemonUiData pokemon)
        {
            await EvolveSpecificPokemonTask.Execute(session, pokemon.Id);
        }

        private static async void LevelUpPokemon(ISession session, PokemonUiData pokemon, bool toMax = false)
        {
            await LevelUpSpecificPokemonTask.Execute(session, pokemon.Id, toMax);
        }

        private static async void TransferPokemon(ISession session, PokemonUiData pokemon)
        {
            await TransferPokemonTask.Execute(session, pokemon.Id);
        }

        private static async void RecycleItem(ISession session, ItemUiData item, int amount, CancellationToken cts)
        {
            await RecycleSpecificItemTask.Execute(session, item.Id, amount, cts);
        }

        #endregion

        #region Controll's events

        private void authBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            var comboBox = sender as ComboBox;
            if (comboBox != null)
                Bot.GlobalSettings.Auth.AuthType = (AuthType) comboBox.SelectedItem;
        }

        private void loginBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            var box = sender as TextBox;
            if (box == null) return;
            if (Bot.GlobalSettings.Auth.AuthType == AuthType.Google)
                Bot.GlobalSettings.Auth.GoogleUsername = box.Text;
            else
                Bot.GlobalSettings.Auth.PtcUsername = box.Text;
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            var box = sender as PasswordBox;
            if (box == null) return;
            if (Bot.GlobalSettings.Auth.AuthType == AuthType.Google)
                Bot.GlobalSettings.Auth.GooglePassword = box.Password;
            else
                Bot.GlobalSettings.Auth.PtcPassword = box.Password;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Visibility = Visibility.Visible;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.Visibility = Visibility.Collapsed;

            // Do something with the Input
            var input = InputTextBox.Text;

            
            if (!Directory.Exists(SubPath + "\\" + input))
            {
                var dir = Directory.CreateDirectory(SubPath + "\\" + input);
                var settings = GlobalSettings.Load(dir.FullName) ?? GlobalSettings.Load(dir.FullName);
                InitBot(settings, input);
            }
            else
            {
                MessageBox.Show("Profile with that name already exists");
            }
            // Clear InputBox.
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
            session.Navigation.UpdatePositionEvent += (lat, lng) => session.EventDispatcher.Send(new UpdatePositionEvent {Latitude = lat, Longitude = lng});

            newBot.Stats.DirtyEvent += () => { StatsOnDirtyEvent(newBot); };

            newBot._lat = settings.LocationSettings.DefaultLatitude;
            newBot._lng = settings.LocationSettings.DefaultLongitude;

            newBot.Machine.SetFailureState(new LoginState());

            BotsCollection.Add(newBot);
        }

        private void SelectBot(BotWindowData newBot)
        {
            if (Bot != null)
            {
                Bot.GlobalSettings.StoreData(SubPath + "\\" + Bot.ProfileName);
                Bot.EnqueData();
                ClearPokemonData();
            }
            foreach (var marker in newBot.MapMarkers.Values)
            {
                pokeMap.Markers.Add(marker);
            }
            if (Bot != null)
            {
                pokeMap.Position = new PointLatLng(Bot._lat, Bot._lng);
                DrawPlayerMarker();
                StatsOnDirtyEvent(Bot);
                Bot.ErrorsCount = 0;
            }
            UpdatePlayerTab(Bot);
            RebuildUi();
        }

        private void UpdatePokemonCollection(ISession session)
        {
            if (Bot == null || session != CurSession) return;
            //PokeListBox.Items.Refresh();
            l_poke_inventory.Content = $"({Bot.PokemonList.Count}/{Bot.MaxPokemonStorageSize})";
        }

        // ReSharper disable once InconsistentNaming
        private void StatsOnDirtyEvent(BotWindowData bot)
        {
            if (bot == null) throw new ArgumentNullException(nameof(bot));
            Dispatcher.BeginInvoke(new ThreadStart(bot.UpdateXppH));
            if (Bot == bot)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    l_StarDust.Content = Bot.Stats?.TotalStardust;
                    l_Stardust_farmed.Content = Bot.Stats?.TotalStardust == 0 ? 0 : Bot.Stats?.TotalStardust - CurSession?.Profile?.PlayerData?.Currencies[1].Amount;
                    l_xp.Content = Bot.Stats?._exportStats?.CurrentXp;
                    l_xp_farmed.Content = Bot.Stats?.TotalExperience;
                    l_Pokemons_farmed.Content = Bot.Stats?.TotalPokemons;
                    l_Pokemons_transfered.Content = Bot.Stats?.TotalPokemonsTransfered;
                    l_Pokestops_farmed.Content = Bot.Stats?.TotalPokestops;
                    l_level.Content = Bot.Stats?._exportStats?.Level;
                    l_level_nextime.Content = $"{Bot.Stats?._exportStats?.HoursUntilLvl.ToString("00")}:{Bot.Stats?._exportStats?.MinutesUntilLevel.ToString("00")}";
                }));
            }
        }

        private void ClearPokemonData()
        {
            consoleBox.Document.Blocks.Clear();
            pokeMap.Markers.Clear();
            _playerMarker = null;
            Bot.LatStep = Bot.LngStep = 0;
            PokeListBox.ItemsSource = null;
            ItemListBox.ItemsSource = null;
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
            settings_grid.IsEnabled = true;
            if (!tabControl.IsEnabled)
                tabControl.IsEnabled = true;
            if (grid_pickBot.Visibility == Visibility.Visible)
                grid_pickBot.Visibility = Visibility.Collapsed;

            authBox.SelectedItem = Bot.GlobalSettings.Auth.AuthType;
            if (Bot.GlobalSettings.Auth.AuthType == AuthType.Google)
            {
                loginBox.Text = Bot.GlobalSettings.Auth.GoogleUsername;
                passwordBox.Password = Bot.GlobalSettings.Auth.GooglePassword;
            }
            else
            {
                loginBox.Text = Bot.GlobalSettings.Auth.PtcUsername;
                passwordBox.Password = Bot.GlobalSettings.Auth.PtcPassword;
            }
            sl_moveSpeedFactor.Value = Bot.GlobalSettings.LocationSettings.MoveSpeedFactor;
            #region Mapping settings to UIElements

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<TextBox>())
            {
                string val;
                if (Extensions.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.Text = val;
            }

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<PasswordBox>())
            {
                string val;
                if (Extensions.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.Password = val;
            }

            foreach (var uiElem in settings_grid.GetLogicalChildCollection<CheckBox>())
            {
                bool val;
                if (Extensions.GetValueByName(uiElem.Name.Substring(2), Bot.GlobalSettings, out val))
                    uiElem.IsChecked = val;
            }

            #endregion

            PokeListBox.ItemsSource = Bot.PokemonList;
            ItemListBox.ItemsSource = Bot.ItemList;
            ToEvolveList.ItemsSource = Bot.PokemonsToEvolve;
            NotToTransferList.ItemsSource = Bot.PokemonsNotToTransfer;
            PokemonsNotToCatchList.ItemsSource = Bot.PokemonsNotToCatch;
            PokemonToUseMasterballList.ItemsSource = Bot.PokemonToUseMasterball;

            _loadingUi = false;
        }

        #region Windows UI Methods

        private void UpdateCoordBoxes()
        {
            c_DefaultLatitude.Text = Bot.GlobalSettings.LocationSettings.DefaultLatitude.ToString(CultureInfo.InvariantCulture);
            c_DefaultLongitude.Text = Bot.GlobalSettings.LocationSettings.DefaultLongitude.ToString(CultureInfo.InvariantCulture);
        }

        private void mi_evolvePokemon_Click(object sender, RoutedEventArgs e)
        {
            if (PokeListBox.SelectedIndex == -1) return;
            var pokemon = GetSelectedPokemon();
            if (pokemon == null) return;
            EvolvePokemon(CurSession, pokemon);
        }

        private void mi_transferPokemon_Click(object sender, RoutedEventArgs e)
        {
            if (PokeListBox.SelectedIndex == -1) return;
            var pokemon = GetSelectedPokemon();
            if (pokemon == null) return;
            TransferPokemon(CurSession, pokemon);
        }

        private void mi_levelupPokemon_Click(object sender, RoutedEventArgs e)
        {
            if (PokeListBox.SelectedIndex == -1) return;
            var pokemon = GetSelectedPokemon();
            if (pokemon == null) return;
            LevelUpPokemon(CurSession, pokemon);
        }
        private void mi_maxlevelupPokemon_Click(object sender, RoutedEventArgs e)
        {
            if (PokeListBox.SelectedIndex == -1) return;
            var pokemon = GetSelectedPokemon();
            if (pokemon == null) return;
            LevelUpPokemon(CurSession, pokemon, true);
        }

        private void mi_recycleItem_Click(object sender, RoutedEventArgs e)
        {
            if (ItemListBox.SelectedIndex == -1) return;
            var item = GetSekectedItem();
            if (item == null) return;
            int amount;
            var inputDialog = new InputDialogSample("Please, enter amout to recycle:", "1", true);
            if (inputDialog.ShowDialog() != true) return;
            if (int.TryParse(inputDialog.Answer, out amount))
                RecycleItem(CurSession, item, amount, Bot.CancellationToken);
        }

        private ItemUiData GetSekectedItem()
        {
            return (ItemUiData)ItemListBox.SelectedItem;
        }

        private PokemonUiData GetSelectedPokemon()
        {
            return (PokemonUiData)PokeListBox.SelectedItem;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            // NoButton Clicked! Let's hide our InputBox.
            InputBox.Visibility = Visibility.Collapsed;

            // Clear InputBox.
            InputTextBox.Text = Empty;
        }

        private void pokeMap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(pokeMap);
            //Getting real coordinates from mouse click
            var mapPos = pokeMap.FromLocalToLatLng((int) mousePos.X, (int) mousePos.Y);
            var lat = mapPos.Lat;
            var lng = mapPos.Lng;

            if (Bot == null) return;
            if (Bot.Started)
            {
                if (Bot.ForceMoveMarker == null)
                {
                    Bot.ForceMoveMarker = new GMapMarker(mapPos)
                    {
                        Shape = Properties.Resources.force_move.ToImage(), Offset = new Point(-24, -48), ZIndex = int.MaxValue
                    };
                    pokeMap.Markers.Add(Bot.ForceMoveMarker);
                }
                else
                {
                    Bot.ForceMoveMarker.Position = mapPos;
                }
                CurSession.StartForceMove(lat, lng);
            }
            else
            {
                Bot.Lat = Bot._lat = lat;
                Bot.Lng = Bot._lng = lng;
                Bot.GlobalSettings.LocationSettings.DefaultLatitude = lat;
                Bot.GlobalSettings.LocationSettings.DefaultLongitude = lng;
                DrawPlayerMarker();
                UpdateCoordBoxes();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _windowClosing = true;
            if (Bot == null || _loadingUi) return;
            Bot.GlobalSettings.StoreData(SubPath + "\\" + Bot.ProfileName);
            foreach (var b in BotsCollection)
            {
                b.Stop();
            }
        }

        private void SortByCpClick(object sender, RoutedEventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Cp", ListSortDirection.Descending));
        }

        private void sortById_Click(object sender, RoutedEventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("PokemonId", ListSortDirection.Ascending));
        }

        private void sortByCatch_Click(object sender, RoutedEventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Timestamp", ListSortDirection.Ascending));
        }

        private void SortByIvClick(object sender, RoutedEventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Iv", ListSortDirection.Descending));
        }

        private void sortByAz_Click(object sender, RoutedEventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            PokeListBox.Items.SortDescriptions.Clear();
            PokeListBox.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void sl_moveSpeedFactor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sl = (sender as Slider);
            if (sl == null || l_moveSpeedFactor == null) return;
            l_moveSpeedFactor.Content = sl.Value;
            if (Bot == null || _loadingUi) return;
            Bot.GlobalSettings.LocationSettings.MoveSpeedFactor = sl.Value;
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
                if (Bot == bot)
                {
                    ClearPokemonData();
                }
            }
        }

        private void btn_botStop_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var bot = btn?.DataContext as BotWindowData;
            if (bot == null) return;
            bot.Stop();
            if (Bot == bot)
            {
                ClearPokemonData();
            }
        }

        private void btn_botStart_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var bot = btn?.DataContext as BotWindowData;
            bot?.Start();
        }

        private void batch_Yes_Click(object sender, RoutedEventArgs e)
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            batchInput.Visibility = Visibility.Collapsed;

            // Do something with the Input
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
            // Clear InputBox.
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

        #region Property <-> Settings

        private void HandleUiElementChangedEvent(object uiElement)
        {
            var box = uiElement as TextBox;
            if (box != null)
            {
                var propName = box.Name.Replace("c_", "");
                Extensions.SetValueByName(propName, box.Text, Bot.GlobalSettings);
                return;
            }
            var chB = uiElement as CheckBox;
            if (chB != null)
            {
                var propName = chB.Name.Replace("c_", "");
                Extensions.SetValueByName(propName, chB.IsChecked, Bot.GlobalSettings);
            }
            var passBox = uiElement as PasswordBox;
            if (passBox != null)
            {
                var propName = passBox.Name.Replace("c_", "");
                Extensions.SetValueByName(propName, passBox.Password, Bot.GlobalSettings);
            }
        }

        private void BotPropertyChanged(object sender, EventArgs e)
        {
            if (Bot == null || _loadingUi) return;
            HandleUiElementChangedEvent(sender);
        }

        #endregion

        #endregion

        #region Android Device Tests

        private void b_getDataFromRealPhone_Click(object sender, RoutedEventArgs e)
        {
            StartFillFromRealDevice();
        }

        private void mapFollowThePlayer_Checked(object sender, RoutedEventArgs e)
        {
            var box = sender as CheckBox;
            if (box?.IsChecked != null) _followThePlayerMarker = (bool)box.IsChecked;
        }

        private async void StartFillFromRealDevice()
        {
            var dd = await Adb.GetDeviceData();
            c_DeviceId.Text = Bot.GlobalSettings.Device.DeviceId = dd.DeviceId;
            c_AndroidBoardName.Text = Bot.GlobalSettings.Device.AndroidBoardName = dd.AndroidBoardName;
            c_AndroidBootloader.Text = Bot.GlobalSettings.Device.AndroidBootLoader = dd.AndroidBootloader;
            c_DeviceBrand.Text = Bot.GlobalSettings.Device.DeviceBrand = dd.DeviceBrand;
            c_DeviceModel.Text = Bot.GlobalSettings.Device.DeviceModel = dd.DeviceModel;
            c_DeviceModelIdentifier.Text = Bot.GlobalSettings.Device.DeviceModelIdentifier = dd.DeviceModelIdentifier;
            c_HardwareManufacturer.Text = Bot.GlobalSettings.Device.HardwareManufacturer = dd.HardwareManufacturer;
            c_HardwareModel.Text = Bot.GlobalSettings.Device.HardWareModel = dd.HardwareModel;
            c_FirmwareBrand.Text = Bot.GlobalSettings.Device.FirmwareBrand = dd.FirmwareBrand;
            c_FirmwareTags.Text = Bot.GlobalSettings.Device.FirmwareTags = dd.FirmwareTags;
            c_FirmwareType.Text = Bot.GlobalSettings.Device.FirmwareType = dd.FirmwareType;
            c_FirmwareFingerprint.Text = Bot.GlobalSettings.Device.FirmwareFingerprint = dd.FirmwareFingerprint;
        }
        private void b_generateRandomDeviceId_Click(object sender, RoutedEventArgs e)
        {
            c_DeviceId.Text = DeviceSettings.RandomString(16, "0123456789abcdef");
        }


        #endregion

        public class EnumDescriptionConverter : IValueConverter
        {
            private static string GetEnumDescription(Enum enumObj)
            {
                var fieldInfo = enumObj.GetType().GetField(enumObj.ToString());

                var attribArray = fieldInfo.GetCustomAttributes(false);

                if (attribArray.Length == 0)
                {
                    return enumObj.ToString();
                }
                else
                {
                    var attrib = attribArray[0] as DescriptionAttribute;
                    return attrib != null ? attrib.Description : "";
                }
            }

            object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var myEnum = (Enum)value;
                var description = GetEnumDescription(myEnum);
                return description;
            }

            object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return string.Empty;
            }
        }

        private void btn_removeFromList_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var pokemonId = (PokemonId)btn?.DataContext;          
            var parentList = btn.Tag as ListBox;
            var source = parentList?.ItemsSource as ObservableCollection<PokemonId>;
            if (source != null && source.Contains(pokemonId))
                source.Remove(pokemonId);
        }

        private void AddPokemonToEvolve_Click(object sender, RoutedEventArgs e)
        {
            if (AddToEvolveCb.SelectedIndex > -1)
            {
                var pokemonId = (PokemonId)AddToEvolveCb.SelectedItem;
                if (!Bot.PokemonsToEvolve.Contains(pokemonId))
                    Bot.PokemonsToEvolve.Add(pokemonId);
                AddToEvolveCb.SelectedIndex = -1;
            }
        }

        private void NotToTransferBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NotToTransferCb.SelectedIndex > -1)
            {
                var pokemonId = (PokemonId)NotToTransferCb.SelectedItem;
                if (!Bot.PokemonsNotToTransfer.Contains(pokemonId))
                    Bot.PokemonsNotToTransfer.Add(pokemonId);
                NotToTransferCb.SelectedIndex = -1;
            }
        }

        private void PokemonsNotToCatchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PokemonsNotToCatchCb.SelectedIndex > -1)
            {
                var pokemonId = (PokemonId)PokemonsNotToCatchCb.SelectedItem;
                if (!Bot.PokemonsNotToCatch.Contains(pokemonId))
                    Bot.PokemonsNotToCatch.Add(pokemonId);
                PokemonsNotToCatchCb.SelectedIndex = -1;
            }
        }

        private void PokemonToUseMasterballBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PokemonToUseMasterballCb.SelectedIndex > -1)
            {
                var pokemonId = (PokemonId)PokemonToUseMasterballCb.SelectedItem;
                if (!Bot.PokemonToUseMasterball.Contains(pokemonId))
                    Bot.PokemonToUseMasterball.Add(pokemonId);
                PokemonToUseMasterballCb.SelectedIndex = -1;
            }
        }
    }
}
