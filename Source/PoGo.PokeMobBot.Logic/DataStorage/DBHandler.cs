using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.API;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using System.Data.Entity.SqlServerCompact;

namespace PoGo.PokeMobBot.Logic.DataStorage
{
    public static class DbHandler
    {
        private static readonly Queue<BotDbData> DataQueue = new Queue<BotDbData>();
        private static readonly BotContext Db = new BotContext();
        private static bool _run = true;
        private static Task _dbWorker;
        

        public static void StopDb()
        {
            _run = false;
            Db.Database.Connection.Close();
            //may be close db connection here
        }

        public static void CheckCreated()
        {
            Db.CheckCreated();
        }

        private static void CheckWorkerRunning()
        {
            if (_dbWorker != null && !_dbWorker.IsCanceled && !_dbWorker.IsCompleted && !_dbWorker.IsFaulted) return;
            _dbWorker = Task.Run(DbWorker);
        }

        #region readers

        public static IEnumerable<PokeStop> GetPokestopsForCoords(double lat, double lng, double distance)
        {
            distance = distance / 1000;
            using (var db = new BotContext())
            {
                return
                    db.PokeStops.Where(
                        x =>
                            12742*
                            SqlCeFunctions.Asin(
                                SqlCeFunctions.SquareRoot(SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Latitude - lat)/2)*
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*
                                                                         (x.Latitude - lat)/2) +
                                                        SqlCeFunctions.Cos(SqlCeFunctions.Pi()/180*lat)*
                                                        SqlCeFunctions.Cos(SqlCeFunctions.Pi()/180*x.Latitude)*
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Longitude - lng)/2)*
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Longitude - lng)/2))) <
                            distance).ToList();
            }
        }

        public static IEnumerable<PokemonSeen> GetPokemonSeenForCoords(double lat, double lng, double distance)
        {
            distance = distance / 1000;
            using (var db = new BotContext())
            {
                return
                    db.PokemonSeen.Where(
                        x =>
                            12742 *
                            SqlCeFunctions.Asin(
                                SqlCeFunctions.SquareRoot(SqlCeFunctions.Sin(SqlCeFunctions.Pi() / 180 * (x.Latitude - lat) / 2) *
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi() / 180 *
                                                                         (x.Latitude - lat) / 2) +
                                                        SqlCeFunctions.Cos(SqlCeFunctions.Pi() / 180 * lat) *
                                                        SqlCeFunctions.Cos(SqlCeFunctions.Pi() / 180 * x.Latitude) *
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi() / 180 * (x.Longitude - lng) / 2) *
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi() / 180 * (x.Longitude - lng) / 2))) <
                            distance).ToList();
            }
        }

        public static IEnumerable<GeoLatLonAlt> GetAltitudeForCoords(double lat, double lng, double distance)
        {
            distance = distance/1000;
            using (var db = new BotContext())
            {
                return
                    db.MapzenAlt.Where(
                        x =>
                            12742*
                            SqlCeFunctions.Asin(
                                SqlCeFunctions.SquareRoot(SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lat - lat)/2)*
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*
                                                                         (x.Lat - lat)/2) +
                                                        SqlCeFunctions.Cos(SqlCeFunctions.Pi()/180*lat)*
                                                        SqlCeFunctions.Cos(SqlCeFunctions.Pi()/180*x.Lat)*
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lon - lng)/2)*
                                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lon - lng)/2))) <
                            distance).OrderBy(x =>
                                12742*
                                SqlCeFunctions.Asin(
                                    SqlCeFunctions.SquareRoot(SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lat - lat)/2)*
                                                            SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*
                                                                             (x.Lat - lat)/2) +
                                                            SqlCeFunctions.Cos(SqlCeFunctions.Pi()/180*lat)*
                                                            SqlCeFunctions.Cos(SqlCeFunctions.Pi()/180*x.Lat)*
                                                            SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lon - lng)/2)*
                                                            SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lon - lng)/2)))).ToList();
            }
        }

        public static bool ElevationDataExists(double lat, double lng, double distance)
        {
            distance = distance / 1000;
            using (var db = new BotContext())
            {
                var res = false;
                try
                {
                    res =
                        db.MapzenAlt.Any(
                            x =>
                                12742*
                                SqlCeFunctions.Asin(
                                    SqlCeFunctions.SquareRoot(
                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lat - lat)/2)*
                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*
                                                           (x.Lat - lat)/2) +
                                        SqlCeFunctions.Cos(SqlCeFunctions.Pi()/180*lat)*
                                        SqlCeFunctions.Cos(SqlCeFunctions.Pi()/180*x.Lat)*
                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lon - lng)/2)*
                                        SqlCeFunctions.Sin(SqlCeFunctions.Pi()/180*(x.Lon - lng)/2))) <
                                distance);
                }
                catch (Exception)
                {
                    //ignore
                }
                return res;
            }
        }

        public static string GetPokeStopName(string id)
        {
            using (var db = new BotContext())
            {
                var targetPs = db.PokeStops.FirstOrDefault(x=> x.Id == id);
                return targetPs == null ? "PokeStop" : targetPs.Name;
            }
        }


        #endregion


        #region dbHandlers

        private static async Task DbWorker()
        {
            var delay = 50;
            while (_run)
            {
                    if (DataQueue.Count > 0)
                    {
                        var dataToPush = DataQueue.Dequeue();
                        if (dataToPush == null) continue;
                        switch (dataToPush.Type)
                        {
                            case BotDataType.Pokestops:
                                AddPokestopToDb((IEnumerable<FortData>) dataToPush.Data);
                                break;
                            case BotDataType.PokestopInfo:
                               UpdatePokestopData((Tuple<string,string,string,string>)dataToPush.Data);
                                break;
                            case BotDataType.MapzenElevation:
                                AddMapzenAltDataToDb((IEnumerable<GeoLatLonAlt>) dataToPush.Data);
                                break;
                            case BotDataType.Pokemons:
                                AddPokemonsData((IEnumerable<MapPokemon>) dataToPush.Data);
                                break;
                            case BotDataType.Pokemon:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                if (DataQueue.Count > 20)
                {
                    delay = 0;
                }
                else if (delay > 0 && DataQueue.Count == 0)
                {
                    delay = 50;
                }
                await Task.Delay(delay);
            }
        }

        private static void AddPokemonsData(IEnumerable<MapPokemon> pokes)
        {
            var dbChanged = false;
            foreach (var p in pokes)
            {
                var mon = new PokemonSeen
                {
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    PokemonId = p.PokemonId,
                    SeenTime = DateTime.Now,
                    SpawnpointId = p.SpawnPointId
                };
                Db.PokemonSeen.Add(mon);
                if (!dbChanged) dbChanged = true;
            }
            if (dbChanged)
                Db.SaveChanges();
        }

        private static void AddMapzenAltDataToDb(IEnumerable<GeoLatLonAlt> data)
        {
            try
            {
                var dbChanged = false;
                foreach (var alt in data)
                {
                    Db.MapzenAlt.Add(alt);
                    if (!dbChanged) dbChanged = true;
                }
                if (dbChanged)
                    Db.SaveChanges();
            }
            catch (Exception)
            {
                //ignore
            }
        }


        private static void AddPokestopToDb(IEnumerable<FortData> data)
        {
            var dbChanged = false;
            var q = Db.PokeStops;
            var psNew = data.Where(x => q.All(v=>v.Id != x.Id));
            foreach (var ps in psNew)
            {
                var pokestop = new PokeStop
                {
                    Id = ps.Id,
                    Latitude = ps.Latitude,
                    Longitude = ps.Longitude,
                    Name = "PokeStop",
                    Description = "",
                    Url = ""
                };
                Db.PokeStops.Add(pokestop);
                if (!dbChanged) dbChanged = true;
            }
            if (dbChanged)
                Db.SaveChanges();
        }
        private static void UpdatePokestopData(Tuple<string, string, string, string> data) //item1 Uid, 2 - name, 3 - desc, 4 url
        {
            var targetPs = Db.PokeStops.FirstOrDefault(x => x.Id == data.Item1);
            if (targetPs == null) return;
            targetPs.Name = data.Item2;
            targetPs.Description = data.Item3;
            targetPs.Url = data.Item4;
            Db.Entry(targetPs).State = EntityState.Modified;
            Db.SaveChanges();
        }
        #endregion

        #region push methods

        public static void PushPokestopInfoToDb(string uid, string name, string desc, string url)
        {
            CheckWorkerRunning();
            DataQueue.Enqueue(new BotDbData(BotDataType.PokestopInfo, Tuple.Create(uid, name, desc, url)));
        }

        public static void PushPokestopsToDb(IEnumerable<FortData> pokestops)
        {
            CheckWorkerRunning();
            DataQueue.Enqueue(new BotDbData(BotDataType.Pokestops, pokestops));
        }
        public static void PushPokemonsToDb(IEnumerable<MapPokemon> pokes)
        {
            CheckWorkerRunning();
            DataQueue.Enqueue(new BotDbData(BotDataType.Pokemons, pokes));
        }

        public static void PushMapzenAltToDb(IEnumerable<GeoLatLonAlt> data)
        {
            CheckWorkerRunning();
            DataQueue.Enqueue(new BotDbData(BotDataType.MapzenElevation, data));
        }
        #endregion
    }

    internal class BotDbData
    {
        public BotDataType Type;
        public object Data;

        public BotDbData(BotDataType type, object data)
        {
            Type = type;
            Data = data;
        }
    }

    internal enum BotDataType
    {
        Pokestops,
        PokestopInfo,
        MapzenElevation,
        Pokemons,
        Pokemon
    }
}
