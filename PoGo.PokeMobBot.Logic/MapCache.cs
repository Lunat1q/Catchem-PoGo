using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using Google.Protobuf;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Rpc;
using POGOProtos.Data;
using POGOProtos.Enums;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;

namespace PoGo.PokeMobBot.Logic
{
    public class MapCache
    {
        private List<FortCacheItem> _FortDatas = new List<FortCacheItem>();
        private List<PokemonCacheItem> _MapPokemons = new List<PokemonCacheItem>();
        public IEnumerable<FortData> baseFortDatas;
        
        public DateTime lastUpdateTime = DateTime.Now.Subtract(new TimeSpan(9999999));
        public int ScanDelay = 20000;

        public MapCache()
        {
            
        }



        public async Task UpdateMapDatas(ISession session)
        {
            #region "Forts"
            var mapObjects = await session.Client.Map.GetMapObjects();

            // Wasn't sure how to make this pretty. Edit as needed.
            var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts);
            session.EventDispatcher.Send(new PokeStopListEvent { Forts = pokeStops.ToList() });

            // Wasn't sure how to make this pretty. Edit as needed.
            if (session.LogicSettings.Teleport)
            {
                pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Checkpoint &&
                            i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    );
            }
            else
            {
                pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Checkpoint &&
                            i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime() &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    );
            }
            baseFortDatas = pokeStops;
            _FortDatas.Clear();
            foreach (var fort in pokeStops)
            {
                _FortDatas.Add(new FortCacheItem(fort));
            }

            #endregion


            #region "Pokemon"

            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons)
                .OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude,
                            session.Client.CurrentLongitude,
                            i.Latitude, i.Longitude));

            _MapPokemons.Clear();
            foreach (var pokemon in pokemons)
            {
                _MapPokemons.Add(new PokemonCacheItem(pokemon));   
            }
            #endregion
            //var timestamp = DateTime.Now.ToString("H-mm-ss-fff");
            //Console.WriteLine($"{timestamp} Updated MapObjects {pokeStops.Count()} Pokestops upserted - {pokemons.Count()} pokemon upserted" );
            lastUpdateTime = DateTime.Now;
        }

        public async Task<List<PokemonCacheItem>> MapPokemons(ISession session)
        {
            if (DateTime.Now.Subtract(lastUpdateTime).TotalMilliseconds > ScanDelay)
            {
                await UpdateMapDatas(session);
            }
            return _MapPokemons.Where(i => i.Caught == false).ToList();
        }

        public async Task<List<FortCacheItem>> FortDatas(ISession session)
        {
            if (DateTime.Now.Subtract(lastUpdateTime).TotalMilliseconds > ScanDelay)
            {
                await UpdateMapDatas(session);
            }

            return _FortDatas;
        }

        public void UsedPokestop(FortCacheItem stop)
        {
            foreach(FortCacheItem result in _FortDatas)
            {
                if (result.Id == stop.Id)
                {
                    result.Used = true;
                    result.CooldownCompleteTimestampMS = DateTime.UtcNow.AddMinutes(5).ToUnixTime();
                    RuntimeSettings.lastPokeStopId = stop.Id;
                    RuntimeSettings.lastPokeStopCoordinate = new GeoCoordinate(stop.Latitude, stop.Longitude);
                    if (RuntimeSettings.TargetStopID == stop.Id)
                        RuntimeSettings.BreakOutOfPathing = true;
                }

            }
        }

    }

    public class PokemonCacheItem
    {
        public string SpawnPointId;
        public ulong EncounterId;
        public PokemonId PokemonId;
        public long ExpirationTimestampMs;
        public double Latitude;
        public double Longitude;
        public MapPokemon BaseMapPokemon;
        public bool Caught = false;
        public PokemonCacheItem(MapPokemon pokemon)
        {
            SpawnPointId = pokemon.SpawnPointId;
            EncounterId = pokemon.EncounterId;
            PokemonId = pokemon.PokemonId;
            ExpirationTimestampMs = pokemon.ExpirationTimestampMs;
            Latitude = pokemon.Latitude;
            Longitude = pokemon.Longitude;
            BaseMapPokemon = pokemon;
            Caught = false;
        }
    }

    public class FortCacheItem
    {
        public ByteString ActiveFortModifier;
        public long CooldownCompleteTimestampMS;
        public bool Enabled;
        public int GuardPokemonCp;
        public long GymPoints;
        public string Id;
        public PokemonId GuardPokemonId;
        public bool IsInBattle;
        public long LastModifiedTimestampMs;
        public double Latitude;
        public double Longitude;
        public FortLureInfo LureInfo;
        public FortType Type;
        public FortSponsor Sponsor;
        public FortData BaseFortData;
        public bool Used = false;

        public FortCacheItem(FortData fort)
        {
            ActiveFortModifier = fort.ActiveFortModifier;
            CooldownCompleteTimestampMS = fort.CooldownCompleteTimestampMs;
            Enabled = fort.Enabled;
            GuardPokemonCp = fort.GuardPokemonCp;
            GymPoints = fort.GymPoints;
            Id = fort.Id;
            GuardPokemonId = fort.GuardPokemonId;
            IsInBattle = fort.IsInBattle;
            LastModifiedTimestampMs = fort.LastModifiedTimestampMs;
            Latitude = fort.Latitude;
            Longitude = fort.Longitude;
            LureInfo = fort.LureInfo;
            Type = fort.Type;
            Sponsor = fort.Sponsor;
            BaseFortData = fort;
        }

        
    }
}
