using System;
using System.Linq;
using System.Windows.Media;
using Catchem.Classes;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

namespace Catchem.MainWindowHelpers
{
    internal static class DebugHelper
    {
        internal static void SeedTheBot(BotWindowData bot)
        {
            SeedEggs(bot);
            SeedTestConsole(bot);
            SeedPokemons(bot);
            SeedPokedex(bot);
            SeedItems(bot);
        }

        private static void SeedItems(BotWindowData bot)
        {
            var inc = new ItemUiData(ItemId.ItemIncenseOrdinary, "Incense", 5, bot);
            bot.ItemList.Add(inc);
            bot.UsedItem(ItemId.ItemIncenseOrdinary, DateTime.UtcNow.AddMinutes(19).ToUnixTime());
            var luckyEgg = new ItemUiData(ItemId.ItemLuckyEgg, "Lucky Egg", 5, bot);
            bot.ItemList.Add(luckyEgg);
            bot.UsedItem(ItemId.ItemLuckyEgg, DateTime.UtcNow.AddMinutes(0.2).ToUnixTime());
            bot.ItemList.Add(new ItemUiData(ItemId.ItemSpecialCamera, "Camera", 1, bot));
        }

        internal static void SeedEggs(BotWindowData bot)
        {
            bot.EggList.Add(new PokeEgg()
            {
                Distance = 5,
                EggId = 12123124123,
                EggIncubatorId = "ewqe1231231221",
                IncubatorType = ItemId.ItemIncubatorBasicUnlimited,
                PokemonUidInside = 123123123,
                TargetDistance = 38,
                WalkedDistance = 35.2
            });

            bot.EggList.Add(new PokeEgg()
            {
                Distance = 10,
                EggId = 12123121234123,
                EggIncubatorId = "ewqe1212331231221",
                IncubatorType = ItemId.ItemIncubatorBasic,
                PokemonUidInside = 312312123,
                TargetDistance = 43,
                WalkedDistance = 35.2
            });

            bot.EggList.Add(new PokeEgg()
            {
                Distance = 10,
                EggId = 1223,
                EggIncubatorId = "",
                PokemonUidInside = 0,
                TargetDistance = 10,
                WalkedDistance = 35.2
            });
        }

        internal static void SeedPokemons(BotWindowData bot)
        {
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Mew, "Mew the awesome!", 1337, 99.9,
                PokemonFamilyId.FamilyMew, 4200, 9001, true, true, 97, PokemonMove.Moonblast, PokemonMove.Thunder,
                PokemonType.Psychic, PokemonType.Flying, 9000, PokemonInfo.GetBaseStats(PokemonId.Mew), 90, 15, 100000,
                42, 15, 7, 0.732f, 101, 100, new[] {PokemonId.Mewtwo,}, true));
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Mewtwo, "Mr.Kickass", 9001, 100,
                PokemonFamilyId.FamilyMewtwo, 47, 9001, true, true, 97, PokemonMove.HyperBeam, PokemonMove.PsychoCutFast,
                PokemonType.Psychic, PokemonType.Flying, 10000, PokemonInfo.GetBaseStats(PokemonId.Mewtwo), 90, 0,
                100000, 42, 13, 15, 0.732f, 101, 100, new[] {PokemonId.Mewtwo,}, false)); //PokemonId.Mew.ToInventorySource(),
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Zapdos, "Thunder", 1337, 100,
                PokemonFamilyId.FamilyZapdos, 47, 9001, true, true, 97, PokemonMove.HyperBeam, PokemonMove.PsychoCutFast,
                PokemonType.Psychic, PokemonType.Flying, 3000, PokemonInfo.GetBaseStats(PokemonId.Zapdos), 90, 4, 100000,
                42, 10, 15, 0.732f, 101, 100, new[] {PokemonId.Mewtwo,}, false));
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Articuno, "Ice-ice-baby", 4048, 100,
                PokemonFamilyId.FamilyArticuno, 47, 9001, true, true, 97, PokemonMove.HyperBeam,
                PokemonMove.PsychoCutFast, PokemonType.Psychic, PokemonType.Flying, 5000,
                PokemonInfo.GetBaseStats(PokemonId.Articuno), 90, 5, 100000, 42, 7, 9, 0.732f, 101, 100,
                new[] {PokemonId.Mewtwo, PokemonId.Articuno}, false));
            bot.PokemonList.Add(new PokemonUiData(bot, 123455678, PokemonId.Moltres, "Popcorn machine", 4269, 100,
                PokemonFamilyId.FamilyMoltres, 47, 9001, true, true, 97, PokemonMove.HyperBeam,
                PokemonMove.PsychoCutFast, PokemonType.Psychic, PokemonType.Flying, 5000,
                PokemonInfo.GetBaseStats(PokemonId.Moltres), 90, 13, 100000, 42, 14, 15, 0.732f, 101, 100,
                new[] {PokemonId.Mewtwo,}, false));
        }

        internal static void SeedTestConsole(BotWindowData bot)
        {
            bot.LogQueue.Enqueue(
                Tuple.Create(
                    "Check fresh updates at https://github.com/Lunat1q/Catchem-PoGo and join our friendly Discord chat: https://discord.me/Catchem",
                    Colors.LawnGreen));
        }

        internal static void SeedPokedex(BotWindowData bot)
        {
            var bulbasaur = bot.PokeDex.FirstOrDefault(x => x.Id == PokemonId.Bulbasaur);
            if (bulbasaur != null)
            {
                bulbasaur.CapturedTimes = 16;
                bulbasaur.SeenTimes = 24;
            }
            var charizard = bot.PokeDex.FirstOrDefault(x => x.Id == PokemonId.Charizard);
            if (charizard != null)
            {
                charizard.CapturedTimes = 1;
                charizard.SeenTimes = 4;
            }

            var blastoise = bot.PokeDex.FirstOrDefault(x => x.Id == PokemonId.Blastoise);
            if (blastoise != null)
            {
                blastoise.CapturedTimes = 0;
                blastoise.SeenTimes = 1;
            }
        }
    }
}
