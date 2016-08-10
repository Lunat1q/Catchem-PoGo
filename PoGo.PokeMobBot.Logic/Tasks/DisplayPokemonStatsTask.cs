#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.DataDumper;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class DisplayPokemonStatsTask
    {
        public static List<ulong> PokemonId = new List<ulong>();


        public static List<ulong> PokemonIdcp = new List<ulong>();

        public static async Task Execute(ISession session)
        {

            var trainerLevel = 40;

            var highestsPokemonCp = await session.Inventory.GetHighestsCp(session.LogicSettings.AmountOfPokemonToDisplayOnStart);
            var pokemonPairedWithStatsCp = highestsPokemonCp.Select(pokemon => new PokemonAnalysis(pokemon, trainerLevel)).ToList();

            var highestsPokemonCpForUpgrade = await session.Inventory.GetHighestsCp(50);
            var pokemonPairedWithStatsCpForUpgrade = highestsPokemonCpForUpgrade.Select(pokemon => new PokemonAnalysis(pokemon, trainerLevel)).ToList();

            var highestsPokemonPerfect = await session.Inventory.GetHighestsPerfect(session.LogicSettings.AmountOfPokemonToDisplayOnStart);
            var pokemonPairedWithStatsIv = highestsPokemonPerfect.Select(pokemon => new PokemonAnalysis(pokemon, trainerLevel)).ToList();

            var highestsPokemonIvForUpgrade = await session.Inventory.GetHighestsPerfect(50);
            var pokemonPairedWithStatsIvForUpgrade = highestsPokemonIvForUpgrade.Select(pokemon => new PokemonAnalysis(pokemon, trainerLevel)).ToList();

            session.EventDispatcher.Send(
                new DisplayHighestsPokemonEvent
                {
                    SortedBy = "CP",
                    PokemonList = pokemonPairedWithStatsCp,
                    DisplayPokemonMaxPoweredCp = session.LogicSettings.DisplayPokemonMaxPoweredCp,
                    DisplayPokemonMovesetRank = session.LogicSettings.DisplayPokemonMovesetRank
                });
            if (session.LogicSettings.Teleport)
                await Task.Delay(session.LogicSettings.DelayDisplayPokemon);
            else
                await Task.Delay(500);

            session.EventDispatcher.Send(
                new DisplayHighestsPokemonEvent
                {
                    SortedBy = "IV",
                    PokemonList = pokemonPairedWithStatsIv,
                    DisplayPokemonMaxPoweredCp = session.LogicSettings.DisplayPokemonMaxPoweredCp,
                    DisplayPokemonMovesetRank = session.LogicSettings.DisplayPokemonMovesetRank
                });

            var allPokemonInBag = session.LogicSettings.PrioritizeIvOverCp
                ? await session.Inventory.GetHighestsPerfect(1000)
                : await session.Inventory.GetHighestsCp(1000);
            if (session.LogicSettings.DumpPokemonStats)
            {
                const string dumpFileName = "PokeBagStats";
                string toDumpCSV = "Name,Level,CP,IV,Move1,Move2\r\n";
                string toDumpTXT = "";
                Dumper.ClearDumpFile(session, dumpFileName);
                Dumper.ClearDumpFile(session, dumpFileName, "csv");

                foreach (var pokemon in allPokemonInBag)
                {
                    toDumpTXT += $"NAME: {session.Translation.GetPokemonName(pokemon.PokemonId).PadRight(16, ' ')}Lvl: {PokemonInfo.GetLevel(pokemon).ToString("00")}\t\tCP: {pokemon.Cp.ToString().PadRight(8, ' ')}\t\t IV: {PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}%\t\t\tMOVE1: {pokemon.Move1}\t\t\tMOVE2: {pokemon.Move2}\r\n";
                    toDumpCSV += $"{session.Translation.GetPokemonName(pokemon.PokemonId)},{PokemonInfo.GetLevel(pokemon).ToString("00")},{pokemon.Cp},{PokemonInfo.CalculatePokemonPerfection(pokemon).ToString("0.00")}%,{pokemon.Move1},{pokemon.Move2}\r\n";
                }

                Dumper.Dump(session, toDumpTXT, dumpFileName);
                Dumper.Dump(session, toDumpCSV, dumpFileName, "csv");
            }
            if (session.LogicSettings.Teleport)
                await Task.Delay(session.LogicSettings.DelayDisplayPokemon);
            else
                await Task.Delay(500);
        }
    }
}
