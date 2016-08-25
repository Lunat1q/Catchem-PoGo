#region using directives

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using Newtonsoft.Json.Linq;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class SniperInfo
    {
        public ulong EncounterId { get; set; }
        public DateTime ExpirationTimestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public PokemonId Id { get; set; }
        public string SpawnPointId { get; set; }
        public PokemonMove Move1 { get; set; }
        public PokemonMove Move2 { get; set; }
        public double IV { get; set; }

        [JsonIgnore]
        public DateTime TimeStampAdded { get; set; } = DateTime.Now;
    }

    public class PokemonLocation
    {
        public PokemonLocation(double latitude, double longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public long Id { get; set; }
        [JsonProperty("expires")]
        public double ExpirationTime { get; set; }
        [JsonProperty("latitude")]
        public double latitude { get; set; }
        [JsonProperty("longitude")]
        public double longitude { get; set; }        
        public int PokemonId { get; set; }
        [JsonProperty("pokemon_id")]
        public PokemonId PokemonName { get; set; }

        public bool Equals(PokemonLocation obj)
        {
            return Math.Abs(latitude - obj.latitude) < 0.0001 && Math.Abs(longitude - obj.longitude) < 0.0001;
        }

        public override bool Equals(object obj) // contains calls this here
        {
            var p = obj as PokemonLocation;
            if (p == null) // no cast available
            {
                return false;
            }

            return Math.Abs(latitude - p.latitude) < 0.0001 && Math.Abs(longitude - p.longitude) < 0.0001;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return latitude.ToString("0.0000") + ", " + longitude.ToString("0.0000");
        }
    }

    public class ScanResult
    {
        public string Error { get; set; }
        [JsonProperty("pokemons")]
        public List<PokemonLocation> Pokemon { get; set; }
    }

    public static class SnipePokemonTask
    {
        public static List<PokemonLocation> LocsVisited = new List<PokemonLocation>();
        private static readonly List<SniperInfo> SnipeLocations = new List<SniperInfo>();
        private static DateTime _lastSnipe = DateTime.MinValue;
        private const string _pokeSniperURI = "http://pokesnipers.com/api/v1/pokemon.json";

        public static Task AsyncStart(Session session, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => Start(session, cancellationToken), cancellationToken);
        }

        public static async Task<bool> CheckPokeballsToSnipe(int minPokeballs, ISession session,
            CancellationToken cancellationToken)
        {
            var pokeBallsCount = await session.Inventory.GetItemAmountByType(ItemId.ItemPokeBall);
            pokeBallsCount += await session.Inventory.GetItemAmountByType(ItemId.ItemGreatBall);
            pokeBallsCount += await session.Inventory.GetItemAmountByType(ItemId.ItemUltraBall);
            pokeBallsCount += await session.Inventory.GetItemAmountByType(ItemId.ItemMasterBall);

            if (pokeBallsCount < minPokeballs)
            {
                session.EventDispatcher.Send(new NoticeEvent
                {
                    Message =
                        session.Translation.GetTranslation(TranslationString.NotEnoughPokeballsToSnipe, pokeBallsCount,
                            minPokeballs)
                });
                return false;
            }

            return true;
        }

        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            if (_lastSnipe.AddMilliseconds(session.LogicSettings.MinDelayBetweenSnipes) > DateTime.Now)
                return;

            if (await CheckPokeballsToSnipe(session.LogicSettings.MinPokeballsToSnipe, session, cancellationToken))
            {
                if (session.LogicSettings.PokemonToSnipe != null)
                {
                    var st = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var t = DateTime.Now.ToUniversalTime() - st;
                    var currentTimestamp = t.TotalMilliseconds;

                    var pokemonIds = session.LogicSettings.PokemonToSnipe.Pokemon;

                    if (session.LogicSettings.UseSnipeLocationServer)
                    {
                        var locationsToSnipe = SnipeLocations; // inital
                        if (session.LogicSettings.UsePokeSnipersLocationServer)
                        {
                            locationsToSnipe = new List<SniperInfo>(SnipeLocations);
                            locationsToSnipe = locationsToSnipe.Where(q => q.Id == PokemonId.Missingno || pokemonIds.Contains(q.Id)).ToList();
                        }
                        else
                        {
                            locationsToSnipe = SnipeLocations?.Where(q =>
                            (!session.LogicSettings.UseTransferIvForSnipe ||
                             (q.IV == 0 && !session.LogicSettings.SnipeIgnoreUnknownIv) ||
                             (q.IV >= session.Inventory.GetPokemonTransferFilter(q.Id).KeepMinIvPercentage)) &&
                            !LocsVisited.Contains(new PokemonLocation(q.Latitude, q.Longitude))
                            && !(q.ExpirationTimestamp != default(DateTime) &&
                                 q.ExpirationTimestamp > new DateTime(2016) &&
                                 // make absolutely sure that the server sent a correct datetime
                                 q.ExpirationTimestamp < DateTime.Now) &&
                            (q.Id == PokemonId.Missingno || pokemonIds.Contains(q.Id))).ToList() ??
                                               new List<SniperInfo>();
                        }

                        _lastSnipe = DateTime.Now;

                        if (locationsToSnipe.Any())
                        {   
                            foreach (var location in locationsToSnipe)
                            {
                                session.EventDispatcher.Send(new SnipeScanEvent
                                {
                                    Bounds = new Location(location.Latitude, location.Longitude),
                                    PokemonId = location.Id,
                                    Iv = location.IV
                                });

                                if (
                                    !await
                                        CheckPokeballsToSnipe(session.LogicSettings.MinPokeballsWhileSnipe + 1, session,
                                            cancellationToken))
                                    return;

                                if (!await
                                    Snipe(session, pokemonIds, location.Latitude, location.Longitude, cancellationToken))
                                {
                                    return;
                                }
                                LocsVisited.Add(new PokemonLocation(location.Latitude, location.Longitude));
                            }
                        }
                    }
                    else
                    {
                        foreach (var location in session.LogicSettings.PokemonToSnipe.Locations)
                        {
                            session.EventDispatcher.Send(new SnipeScanEvent
                            {
                                Bounds = location,
                                PokemonId = PokemonId.Missingno
                            });

                            var scanResult = SnipeScanForPokemon(session, location); // initialize
                            List<PokemonLocation> locationsToSnipe = new List<PokemonLocation>();

                            if (session.LogicSettings.UsePokeSnipersLocationServer)
                            {
                                PokeSniperScanForPokemon(session, location);
                            }
                            else
                            {
                                if (scanResult.Pokemon != null)
                                {
                                    var filteredPokemon = scanResult.Pokemon.Where(q => pokemonIds.Contains((PokemonId)q.PokemonName));
                                    var notVisitedPokemon = filteredPokemon.Where(q => !LocsVisited.Contains(q));
                                    var notExpiredPokemon = notVisitedPokemon.Where(q => q.ExpirationTime < currentTimestamp);

                                    locationsToSnipe.AddRange(notExpiredPokemon);
                                }
                            }

                            if (scanResult.Pokemon != null)
                            {
                                var filteredPokemon = scanResult.Pokemon.Where(q => pokemonIds.Contains((PokemonId)q.PokemonName));
                                var notVisitedPokemon = filteredPokemon.Where(q => !LocsVisited.Contains(q));
                                var notExpiredPokemon = notVisitedPokemon.Where(q => q.ExpirationTime < currentTimestamp);

                                locationsToSnipe.AddRange(notExpiredPokemon);
                            }

                            _lastSnipe = DateTime.Now;

                            if (locationsToSnipe.Any())
                            {
                                foreach (var pokemonLocation in locationsToSnipe)
                                {
                                    if (
                                        !await
                                            CheckPokeballsToSnipe(session.LogicSettings.MinPokeballsWhileSnipe + 1,
                                                session, cancellationToken))
                                        return;

                                    if (!await
                                        Snipe(session, pokemonIds, pokemonLocation.latitude, pokemonLocation.longitude,
                                            cancellationToken))
                                    {
                                        return;
                                    }

                                    LocsVisited.Add(pokemonLocation);
                                }
                            }
                            else
                            {
                                session.EventDispatcher.Send(new NoticeEvent
                                {
                                    Message = session.Translation.GetTranslation(TranslationString.NoPokemonToSnipe)
                                });
                            }
                        }
                    }
                }
            }
        }

        private static async Task<bool> Snipe(ISession session, IEnumerable<PokemonId> pokemonIds, double Latitude,
            double Longitude, CancellationToken cancellationToken)
        {
            var CurrentLatitude = session.Client.CurrentLatitude;
            var CurrentLongitude = session.Client.CurrentLongitude;

            session.EventDispatcher.Send(new SnipeModeEvent { Active = true });

            await
                session.Client.Player.UpdatePlayerLocation(Latitude,
                    Longitude, session.Client.CurrentAltitude);

            session.EventDispatcher.Send(new UpdatePositionEvent
            {
                Longitude = Longitude,
                Latitude = Latitude
            });

            var mapObjects = session.Client.Map.GetMapObjects().Result;
            var catchablePokemon =
                mapObjects.MapCells.SelectMany(q => q.CatchablePokemons)
                    .Where(q => pokemonIds.Contains(q.PokemonId))
                    .ToList();

            await session.Client.Player.UpdatePlayerLocation(CurrentLatitude, CurrentLongitude,
                session.Client.CurrentAltitude);

            foreach (var pokemon in catchablePokemon)
            {
                //var pokemon = new PokemonCacheItem(_pokemon);
                cancellationToken.ThrowIfCancellationRequested();

                EncounterResponse encounter;
                try
                {
                    await
                        session.Client.Player.UpdatePlayerLocation(Latitude, Longitude, session.Client.CurrentAltitude);

                    encounter =
                        session.Client.Encounter.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnPointId).Result;
                }
                finally
                {
                    await
                        session.Client.Player.UpdatePlayerLocation(CurrentLatitude, CurrentLongitude, session.Client.CurrentAltitude);
                }

                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    session.EventDispatcher.Send(new UpdatePositionEvent
                    {
                        Latitude = CurrentLatitude,
                        Longitude = CurrentLongitude
                    });

                    if (!await CatchPokemonTask.Execute(session, encounter, new PokemonCacheItem(pokemon), cancellationToken))
                    {
                        // Don't snipe any more pokemon if we ran out of one kind of pokeballs.
                        session.EventDispatcher.Send(new SnipeModeEvent { Active = false });
                        return false;
                    }
                }
                else if (encounter.Status == EncounterResponse.Types.Status.PokemonInventoryFull)
                {
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message =
                            session.Translation.GetTranslation(
                                TranslationString.InvFullTransferManually)
                    });

                    // Don't snipe any more pokemon if inventory is full.
                    session.EventDispatcher.Send(new SnipeModeEvent { Active = false });
                    return false;
                }
                else
                {
                    session.EventDispatcher.Send(new WarnEvent
                    {
                        Message =
                            session.Translation.GetTranslation(
                                TranslationString.EncounterProblem, encounter.Status)
                    });
                }

                if (
                    !Equals(catchablePokemon.ElementAtOrDefault(catchablePokemon.Count - 1),
                        pokemon))
                {
                    await Task.Delay(session.LogicSettings.DelayBetweenPokemonCatch, cancellationToken);
                }
            }

            session.EventDispatcher.Send(new SnipeModeEvent { Active = false });
            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions, cancellationToken);

            return true;
        }

        private static ScanResult SnipeScanForPokemon(ISession session, Location location)
        {
            var formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };

            var offset = session.LogicSettings.SnipingScanOffset;
            // 0.003 = half a mile; maximum 0.06 is 10 miles
            if (offset < 0.001) offset = 0.003;
            if (offset > 0.06) offset = 0.06;

            var boundLowerLeftLat = (location.Latitude - offset).ToString(formatter);
            var boundLowerLeftLng = (location.Longitude - offset).ToString(formatter);
            var boundUpperRightLat = (location.Latitude + offset).ToString(formatter);
            var boundUpperRightLng = (location.Longitude + offset).ToString(formatter);

            var uri = $"http://skiplagged.com/api/pokemon.php?bounds={boundLowerLeftLat},{boundLowerLeftLng},{boundUpperRightLat},{boundUpperRightLng}";

            ScanResult scanResult;

            try
            {
                var request = WebRequest.CreateHttp(uri);
                request.Accept = "application/json";
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";
                request.Timeout = session.LogicSettings.SnipeRequestTimeoutSeconds;
                request.ReadWriteTimeout = 32000;

                var resp = request.GetResponse();
                var reader = new StreamReader(resp.GetResponseStream());
                var fullresp = reader.ReadToEnd();

                scanResult = JsonConvert.DeserializeObject<ScanResult>(fullresp);

                if (scanResult.Error != string.Empty && scanResult.Error != null)
                {
                    if (scanResult.Error.Contains("down for maintenance") || scanResult.Error.Contains("illegal request"))
                        session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.SkipLaggedMaintenance) });
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError &&
                    ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.WebErrorNotFound) });
                            break;
                        case HttpStatusCode.GatewayTimeout:
                            session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.WebErrorGatewayTimeout) });
                            break;
                        case HttpStatusCode.BadGateway:
                            session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.WebErrorBadGateway) });
                            break;
                        default:
                            session.EventDispatcher.Send(new WarnEvent { Message = ex.ToString() });
                            break;
                    }
                }
                else if (ex.Status == WebExceptionStatus.Timeout)
                {
                    session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.SkipLaggedTimeout) });
                }
                else
                {
                    session.EventDispatcher.Send(new ErrorEvent { Message = ex.ToString() });
                }

                scanResult = new ScanResult { Pokemon = new List<PokemonLocation>() };
            }
            catch (Exception ex)
            {
                session.EventDispatcher.Send(new ErrorEvent { Message = ex.ToString() });
                scanResult = new ScanResult { Pokemon = new List<PokemonLocation>() };
            }

            return scanResult;
        }

        private static ScanResult PokeSniperScanForPokemon(ISession session, Location location)
        {
            var formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };

            var offset = session.LogicSettings.SnipingScanOffset;
            // 0.003 = half a mile; maximum 0.06 is 10 miles
            if (offset < 0.001) offset = 0.003;
            if (offset > 0.06) offset = 0.06;

            ScanResult scanResult;
            try
            {
                var request = WebRequest.CreateHttp(_pokeSniperURI);
                request.Accept = "application/json";
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";
                request.Timeout = session.LogicSettings.SnipeRequestTimeoutSeconds;
                request.ReadWriteTimeout = 32000;

                var resp = request.GetResponse();
                var reader = new StreamReader(resp.GetResponseStream());
                var fullresp = reader.ReadToEnd().Replace(" M", "Male").Replace(" F", "Female");

                dynamic pokesniper = JsonConvert.DeserializeObject(fullresp);
                JArray results = pokesniper.results;
                SnipeLocations.Clear();

                foreach (var result in results)
                {
                    PokemonId id;
                    Enum.TryParse(result.Value<string>("name"), out id);
                    double iv;
                    Double.TryParse(result.Value<string>("iv"), out iv);
                    var a = new SniperInfo
                    {
                        Id = id,
                        IV = iv,
                        Latitude = Convert.ToDouble(result.Value<string>("coords").Split(',')[0]),
                        Longitude = Convert.ToDouble(result.Value<string>("coords").Split(',')[1]),
                        ExpirationTimestamp = DateTime.Now
                    };
                    SnipeLocations.Add(a);
                }
            }
            catch (Exception ex)
            {
                // most likely System.IO.IOException
                session.EventDispatcher.Send(new ErrorEvent { Message = ex.ToString() });
                scanResult = new ScanResult { Pokemon = new List<PokemonLocation>() };
            }
            return null;
        }

        public static async Task Start(Session session, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (session.LogicSettings.UsePokeSnipersLocationServer)
                {
                    var st = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var t = DateTime.Now.ToUniversalTime() - st;
                    var currentTimestamp = t.TotalMilliseconds;
                    var pokemonIds = session.LogicSettings.PokemonToSnipe.Pokemon;

                    var formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };

                    var offset = session.LogicSettings.SnipingScanOffset;
                    // 0.003 = half a mile; maximum 0.06 is 10 miles
                    if (offset < 0.001) offset = 0.003;
                    if (offset > 0.06) offset = 0.06;

                    ScanResult scanResult;
                    try
                    {
                        var request = WebRequest.CreateHttp(_pokeSniperURI);
                        request.Accept = "application/json";
                        request.Method = "GET";
                        request.Timeout = session.LogicSettings.SnipeRequestTimeoutSeconds;
                        request.ReadWriteTimeout = 32000;

                        var resp = request.GetResponse();
                        var reader = new StreamReader(resp.GetResponseStream());
                        var fullresp = reader.ReadToEnd().Replace(" M", "Male").Replace(" F", "Female");

                        dynamic pokesniper = JsonConvert.DeserializeObject(fullresp);
                        JArray results = pokesniper.results;
                        SnipeLocations.Clear();

                        foreach (var result in results)
                        {
                            PokemonId id;
                            Enum.TryParse(result.Value<string>("name"), out id);
                            double iv;
                            Double.TryParse(result.Value<string>("iv"), out iv);
                            var a = new SniperInfo
                            {
                                Id = id,
                                IV = iv,
                                Latitude = Convert.ToDouble(result.Value<string>("coords").Split(',')[0]),
                                Longitude = Convert.ToDouble(result.Value<string>("coords").Split(',')[1]),
                                ExpirationTimestamp = DateTime.Now
                            };
                            SnipeLocations.Add(a);
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.ProtocolError &&
                            ex.Response != null)
                        {
                            var resp = (HttpWebResponse)ex.Response;
                            switch (resp.StatusCode)
                            {
                                case HttpStatusCode.NotFound:
                                    session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.WebErrorNotFound) });
                                    break;
                                case HttpStatusCode.GatewayTimeout:
                                    session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.WebErrorGatewayTimeout) });
                                    break;
                                case HttpStatusCode.BadGateway:
                                    session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.WebErrorBadGateway) });
                                    break;
                                default:
                                    session.EventDispatcher.Send(new WarnEvent { Message = ex.ToString() });
                                    break;
                            }
                        }
                        else if (ex.Status == WebExceptionStatus.Timeout)
                        {
                            session.EventDispatcher.Send(new WarnEvent { Message = session.Translation.GetTranslation(TranslationString.SkipLaggedTimeout) });
                        }
                        else
                        {
                            session.EventDispatcher.Send(new ErrorEvent { Message = ex.ToString() });
                        }

                        scanResult = new ScanResult { Pokemon = new List<PokemonLocation>() };
                    }
                    catch (Exception ex)
                    {
                        session.EventDispatcher.Send(new ErrorEvent { Message = ex.ToString() });
                        scanResult = new ScanResult { Pokemon = new List<PokemonLocation>() };
                    }
                    await Task.Delay(5000, cancellationToken);
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var lClient = new TcpClient();
                        lClient.Connect(session.LogicSettings.SnipeLocationServer,
                            session.LogicSettings.SnipeLocationServerPort);

                        var sr = new StreamReader(lClient.GetStream());

                        while (lClient.Connected)
                        {
                            var line = sr.ReadLine();
                            if (line == null)
                                throw new Exception("Unable to ReadLine from sniper socket");

                            var info = JsonConvert.DeserializeObject<SniperInfo>(line);

                            if (SnipeLocations.Any(x =>
                                Math.Abs(x.Latitude - info.Latitude) < 0.0001 &&
                                Math.Abs(x.Longitude - info.Longitude) < 0.0001))
                                // we might have different precisions from other sources
                                continue;

                            SnipeLocations.RemoveAll(x => DateTime.Now > x.TimeStampAdded.AddMinutes(15));
                            SnipeLocations.Add(info);
                        }
                    }
                    catch (SocketException)
                    {
                        // this is spammed to often. Maybe add it to debug log later
                    }
                    catch (Exception ex)
                    {
                        // most likely System.IO.IOException
                        session.EventDispatcher.Send(new ErrorEvent { Message = ex.ToString() });
                    }
                    await Task.Delay(5000, cancellationToken);
                }
               
            }
        }
    }
}