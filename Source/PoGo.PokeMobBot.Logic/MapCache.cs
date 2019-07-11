using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;

namespace PoGo.PokeMobBot.Logic
{
    public class MapCache
    {
        private List<FortCacheItem> _FortDatas = new List<FortCacheItem>();
        private List<PokemonCacheItem> _MapPokemons = new List<PokemonCacheItem>();
        private List<FortCacheItem> _GymDatas = new List<FortCacheItem>();
        public IEnumerable<FortData> baseFortDatas;
        public IEnumerable<FortData> baseGymDatas;
        public Dictionary<string, long> RecentlyUsedPokestops = new Dictionary<string, long>();
        public Dictionary<ulong, long> RecentlyCaughtPokemons = new Dictionary<ulong, long>();

        public DateTime lastUpdateTime = DateTime.Now.Subtract(new TimeSpan(9999999));
        public int ScanDelay = 20000;


        public async Task UpdateMapDatas(ISession session)
        {
            #region "Forts"
            var mapObjects = await session.Client.Map.GetMapObjectsTuple();

            // Wasn't sure how to make this pretty. Edit as needed.
            IEnumerable<FortData> pokeStops;
            IEnumerable<FortData> gyms;

            // Wasn't sure how to make this pretty. Edit as needed.
            if (session.LogicSettings.Teleport)
            {
                pokeStops = mapObjects.Item1.MapCells.SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Checkpoint &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    );
                gyms = mapObjects.Item1.MapCells.SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Gym &&
                            ( 
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    );
            }
            else
            {
                pokeStops = mapObjects.Item1.MapCells.SelectMany(i => i.Forts)
                    .Where(
                        i =>
                            i.Type == FortType.Checkpoint &&
                            ( // Make sure PokeStop is within max travel distance, unless it's set to 0.
                                LocationUtils.CalculateDistanceInMeters(
                                    session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                                    i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                            session.LogicSettings.MaxTravelDistanceInMeters == 0
                    );
                gyms = mapObjects.Item1.MapCells.SelectMany(i => i.Forts)
                   .Where(
                       i =>
                           i.Type == FortType.Gym &&
                           ( 
                               LocationUtils.CalculateDistanceInMeters(
                                   session.Settings.DefaultLatitude, session.Settings.DefaultLongitude,
                                   i.Latitude, i.Longitude) < session.LogicSettings.MaxTravelDistanceInMeters) ||
                           session.LogicSettings.MaxTravelDistanceInMeters == 0
                   );
            }
            baseFortDatas = pokeStops;
            baseFortDatas = gyms;
            _FortDatas.Clear();
            _GymDatas.Clear();
            foreach (var fort in pokeStops)
            {
                _FortDatas.Add(new FortCacheItem(fort));
            }

            foreach (var gym in gyms)
            {
                _GymDatas.Add(new FortCacheItem(gym));
            }


            #endregion


            #region "Pokemon"

            var pokemons = mapObjects.Item1.MapCells.SelectMany(i => i.CatchablePokemons)
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
            if (DateTime.Now.Subtract(lastUpdateTime).TotalMilliseconds > ScanDelay || _FortDatas.Count == 0)
            {
                await UpdateMapDatas(session);
            }

            return _FortDatas;
        }

        public async Task<List<FortCacheItem>> GymDatas(ISession session)
        {
            if (DateTime.Now.Subtract(lastUpdateTime).TotalMilliseconds > ScanDelay || _GymDatas.Count == 0)
            {
                await UpdateMapDatas(session);
            }

            return _GymDatas;
        }

        private void UsedPokestopsCleanup()
        {
            var stamp = DateTime.UtcNow.ToUnixTime();
            var toRemove = RecentlyUsedPokestops?.Where(x => x.Value < stamp).ToList();
            var removeQueue = new Queue<string>();
            if (toRemove == null) return;
            try
            {
                for (var i = 0; i < toRemove.Count(); i++)
                {
                    removeQueue.Enqueue(toRemove[i].Key);
                }
            }
            catch (Exception)
            {
                //ignore
            }
            while (removeQueue.Count > 0)
            {
                var r = removeQueue.Dequeue();
                RecentlyUsedPokestops.Remove(r);
            }
        }

        private void CaughtPokemonsCleanup()
        {
            var stamp = DateTime.UtcNow.ToUnixTime();
            var toRemove = RecentlyCaughtPokemons?.Where(x => x.Value < stamp).ToList();
            var removeQueue = new Queue<ulong>();
            if (toRemove == null) return;
            try
            {
                for (var i = 0; i < toRemove.Count(); i++)
                {
                    removeQueue.Enqueue(toRemove[i].Key);
                }
            }
            catch (Exception)
            {
                //ignore
            }
            while (removeQueue.Count > 0)
            {
                var r = removeQueue.Dequeue();
                RecentlyCaughtPokemons.Remove(r);
            }
        }

        public bool CheckPokestopUsed(FortCacheItem fort)
        {
            UsedPokestopsCleanup();
            var stamp = DateTime.UtcNow.ToUnixTime();
            var check1 =
                _FortDatas?.Any(x => x != null && x.Id == fort?.Id && (x.Used || x.CooldownCompleteTimestampMS > stamp)) ??
                false;
            var check2 = RecentlyUsedPokestops.ContainsKey(fort.Id);
            return check1 || check2;
        }

        public bool CheckPokemonCaught(ulong id)
        {
            CaughtPokemonsCleanup();
            return RecentlyCaughtPokemons.ContainsKey(id);
        }

        public void PokemonCaught(PokemonCacheItem pokemon)
        {
            CaughtPokemonsCleanup();
            if (!RecentlyCaughtPokemons.ContainsKey(pokemon.EncounterId))
                RecentlyCaughtPokemons.Add(pokemon.EncounterId, DateTime.UtcNow.AddMinutes(15).ToUnixTime());
        }

        public void UsedPokestop(FortCacheItem stop, ISession session)
        {
            var stamp = DateTime.UtcNow.AddMinutes(5).ToUnixTime();
            foreach (FortCacheItem result in _FortDatas)
            {
                if (result.Id == stop.Id)
                {
                    result.Used = true;
                    result.CooldownCompleteTimestampMS = stamp;
                    session.Runtime.lastPokeStopId = stop.Id;
                    session.Runtime.lastPokeStopCoordinate = new GeoCoordinate(stop.Latitude, stop.Longitude);
                    if (session.Runtime.TargetStopID == stop.Id)
                        session.Runtime.BreakOutOfPathing = true;
                }

            }
            if (!RecentlyUsedPokestops.ContainsKey(stop.Id))
            {
                RecentlyUsedPokestops.Add(stop.Id, stamp);
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
        public List<ItemId> ActiveFortModifier;
        //public ByteString ActiveFortModifier;
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
            ActiveFortModifier = fort.ActiveFortModifier.ToList();
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
