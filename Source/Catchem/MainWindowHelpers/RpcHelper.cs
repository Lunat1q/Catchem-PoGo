using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Catchem.Classes;
using PoGo.PokeMobBot.Logic.DataStorage;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Enums;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;

namespace Catchem.MainWindowHelpers
{
    public static class RpcHelper
    {
        internal static void PushNewError(ISession session)
        {
            var receiverBot = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
            if (receiverBot == null) return;
            receiverBot.ErrorsCount++;
        }

        internal static void PokemonChanged(ISession session, object[] objData)
        {
            try
            {
                var evt = objData[0] as PokemonStatsChangedEvent;
                if (evt == null) return;
                var receiverBot = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.PokemonUpdated(evt.Uid, evt.Cp, evt.Iv,
                    evt.Family, evt.Candy, evt.Favourite, evt.Name,
                    evt.MaxCp, evt.IvAtk, evt.IvDef, evt.Cpm, evt.Weight, evt.Level, evt.Stamina, evt.StaminaMax);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        internal static void PokemonActionDone(ISession session, object[] objData)
        {
            try
            {
                var isUid = objData[0] is ulong;
                if (!isUid) return;
                var uid = (ulong)objData[0];
                var receiverBot = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.PokemonActionDone(uid);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        internal static void PokemonFavouriteChanged(ISession session, object[] objData)
        {
            try
            {
                if (!(objData[0] is ulong)) return;
                var receiverBot = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
                if (receiverBot == null) return;
                receiverBot.PokemonFavUpdated((ulong) objData[0], (bool) objData[1]);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        internal static void PushNewConsoleRow(ISession session, string rowText, Color rowColor)
        {
            var botReceiver = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            botReceiver.LogQueue.Enqueue(Tuple.Create(rowText, rowColor));
            if (botReceiver.LogQueue.Count > 100)
            {
                botReceiver.LogQueue.Dequeue();
            }
        }

        internal static void PushNewPokemons(ISession session, IEnumerable<MapPokemon> pokemons)
        {
            var botReceiver = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            var mapPokemons = pokemons as IList<MapPokemon> ?? pokemons.ToList();
            var mapMarkers = botReceiver.MapMarkers.ToDictionary(x=> x.Key, x=>x.Value);
            var markersQueue = botReceiver.MarkersQueue.ToList();
            foreach (var pokemon in mapPokemons)
            {
                if (mapMarkers.ContainsKey(pokemon.EncounterId.ToString()) ||
                    markersQueue.Any(x => x.Uid == pokemon.EncounterId.ToString())) continue;
                var nMapObj = new NewMapObject(MapPbjectType.Pokemon, pokemon.PokemonId.ToString(), pokemon.Latitude,
                    pokemon.Longitude, pokemon.EncounterId.ToString());
                if (botReceiver.Started)
                    botReceiver.MarkersQueue.Enqueue(nMapObj);
            }
            DbHandler.PushPokemonsToDb(mapPokemons);
        }

        internal static void PushNewWildPokemons(ISession session, IEnumerable<WildPokemon> pokemons)
        {
            var botReceiver = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            foreach (var pokemon in pokemons)
            {
                if (botReceiver.MapMarkers.ContainsKey(pokemon.EncounterId.ToString()) ||
                    botReceiver.MarkersQueue.Count(x => x.Uid == pokemon.EncounterId.ToString()) != 0) continue;
                var nMapObj = new NewMapObject(MapPbjectType.Pokemon, pokemon.PokemonData.PokemonId.ToString(),
                    pokemon.Latitude, pokemon.Longitude, pokemon.EncounterId.ToString());
                if (botReceiver.Started)
                    botReceiver.MarkersQueue.Enqueue(nMapObj);
            }
        }

        internal static void PushNewGymPoke(ISession session, object[] paramObjects)
        {
            var botReceiver = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;

            try
            {
                var id = (string) paramObjects[0];
                var name = (string) paramObjects[1];
                var team = (TeamColor?) paramObjects[2];
                var lat = (double) paramObjects[3];
                var lon = (double) paramObjects[4];

                if (botReceiver.MapMarkers.ContainsKey(id) ||
                    botReceiver.MarkersQueue.Count(x => x.Uid == id) != 0) return;
                var nMapObj = new NewMapObject(MapPbjectType.Gym, name, lat,
                    lon, id, team);
                if (botReceiver.Started)
                    botReceiver.MarkersQueue.Enqueue(nMapObj);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        internal static void PushNewPokestop(ISession session, IEnumerable<FortData> pstops)
        {
            var botReceiver = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
            if (botReceiver == null) return;
            var fortDatas = pstops as FortData[] ?? pstops.ToArray();

            DbHandler.PushPokestopsToDb(fortDatas);

            for (var i = 0; i < fortDatas.Length; i++)
            {
                try
                {
                    try
                    {
                        var i1 = i;

                        var haveThatMarker = botReceiver.MapMarkers.ContainsKey(fortDatas[i1].Id) ||
                                             botReceiver.MarkersQueue.Any(x => x.Uid == fortDatas[i1].Id);
                        if (haveThatMarker)
                            continue;
                    }
                    catch (Exception ex) //ex)
                    {
                        Logger.Write("[PS_FAIL]" + ex.Message);
                        // ignored
                    }
                    var lured = fortDatas[i].LureInfo?.LureExpiresTimestampMs > DateTime.UtcNow.ToUnixTime();
                    var nMapObj = new NewMapObject(lured ? MapPbjectType.PokestopLured : MapPbjectType.Pokestop,
                        "PokeStop", fortDatas[i].Latitude, fortDatas[i].Longitude, fortDatas[i].Id);
                    if (botReceiver.Started)
                        botReceiver.MarkersQueue.Enqueue(nMapObj);
                }
                catch (Exception ex) //ex)
                {
                    Logger.Write("[PS_FAIL]" + ex.Message);
                    i--;
                }
            }
        }

        internal static void UpdatePsInDatabase(object[] paramObjects)
        {
            try
            {
                var uid = (string) paramObjects[0];
                var name = (string) paramObjects[1];
                var desc = (string) paramObjects[2];
                var url = (string) paramObjects[3];
                DbHandler.PushPokestopInfoToDb(uid, name, desc, url);
            }
            catch (Exception ex)
            {
                Logger.Write(ex.Message, LogLevel.Error);
            }
        }

        internal static void UpdateLure(ISession session, bool lured, string uid)
        {
            var botReceiver = MainWindow.BotsCollection.FirstOrDefault(x => x.Session == session);
            botReceiver?.MarkersQueue.Enqueue(lured
                ? new NewMapObject(MapPbjectType.SetLured, "", 0, 0, uid)
                : new NewMapObject(MapPbjectType.SetUnLured, "", 0, 0, uid));
        }
    }
}
