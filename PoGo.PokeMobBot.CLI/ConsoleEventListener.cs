#region using directives

using System;
using System.Globalization;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.CLI
{
    public class ConsoleEventListener
    {
        public void HandleEvent(ProfileEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventProfileLogin,
                evt.Profile.PlayerData.Username ?? ""));
        }

        public void HandleEvent(ErrorEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Error);
        }

        public void HandleEvent(NoticeEvent evt, ISession session)
        {
            Logger.Write(evt.ToString());
        }

        public void HandleEvent(DebugEvent evt, ISession session)
        {
#if DEBUG
            Logger.Write(evt.ToString(), LogLevel.Debug);
#endif
        }

        public void HandleEvent(WarnEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Warning);

            if (evt.RequireInput)
            {
                Logger.Write(session.Translation.GetTranslation(TranslationString.RequireInputText));
                Console.ReadKey();
            }
        }

        public void HandleEvent(PlayerLevelUpEvent evt, ISession session)
        {
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventLevelUpRewards, evt.Items));
        }

        public void HandleEvent(UseLuckyEggEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventUsedLuckyEgg, evt.Count),
                LogLevel.Egg);
        }

        public void HandleEvent(UseLuckyEggMinPokemonEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventUseLuckyEggMinPokemonCheck, evt.Diff, evt.CurrCount, evt.MinPokemon));
        }

        public void HandleEvent(PokemonEvolveEvent evt, ISession session)
        {
            Logger.Write(evt.Result == EvolvePokemonResponse.Types.Result.Success
                ? session.Translation.GetTranslation(TranslationString.EventPokemonEvolvedSuccess, session.Translation.GetPokemonName(evt.Id), evt.Exp)
                : session.Translation.GetTranslation(TranslationString.EventPokemonEvolvedFailed, session.Translation.GetPokemonName(evt.Id), evt.Result,
                    evt.Id),
                LogLevel.Evolve);
        }

        public void HandleEvent(TransferPokemonEvent evt, ISession session)
        {
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventPokemonTransferred, session.Translation.GetPokemonName(evt.Id), evt.Cp,
                    evt.Perfection.ToString("0.00"), evt.BestCp, evt.BestPerfection.ToString("0.00"), evt.FamilyCandies),
                LogLevel.Transfer);
        }

        public void HandleEvent(ItemRecycledEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventItemRecycled, evt.Count, evt.Id),
                LogLevel.Recycling);
        }

        public void HandleEvent(EggIncubatorStatusEvent evt, ISession session)
        {
            Logger.Write(evt.WasAddedNow
                ? session.Translation.GetTranslation(TranslationString.IncubatorPuttingEgg, evt.KmRemaining)
                : session.Translation.GetTranslation(TranslationString.IncubatorStatusUpdate, evt.KmRemaining),
                LogLevel.Egg);
        }

        public void HandleEvent(EggHatchedEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.IncubatorEggHatched,
                session.Translation.GetPokemonName(evt.PokemonId), evt.Level, evt.Cp, evt.MaxCp, evt.Perfection),
                LogLevel.Egg);
        }

        public void HandleEvent(FortUsedEvent evt, ISession session)
        {
            var itemString = evt.InventoryFull
                ? session.Translation.GetTranslation(TranslationString.InvFullPokestopLooting)
                : evt.Items;
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventFortUsed, evt.Name, evt.Exp, evt.Gems,
                    itemString),
                LogLevel.Pokestop);
        }

        public void HandleEvent(FortFailedEvent evt, ISession session)
        {
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventFortFailed, evt.Name, evt.Try, evt.Max),
                LogLevel.Pokestop, ConsoleColor.DarkRed);
        }

        public void HandleEvent(FortTargetEvent evt, ISession session)
        {
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventFortTargeted, evt.Name,
                    Math.Round(evt.Distance)),
                LogLevel.Info, ConsoleColor.DarkRed);
        }

        public void HandleEvent(PokemonCaptureEvent evt, ISession session)
        {
            Func<ItemId, string> returnRealBallName = a =>
            {
                switch (a)
                {
                    case ItemId.ItemPokeBall:
                        return session.Translation.GetTranslation(TranslationString.Pokeball);
                    case ItemId.ItemGreatBall:
                        return session.Translation.GetTranslation(TranslationString.GreatPokeball);
                    case ItemId.ItemUltraBall:
                        return session.Translation.GetTranslation(TranslationString.UltraPokeball);
                    case ItemId.ItemMasterBall:
                        return session.Translation.GetTranslation(TranslationString.MasterPokeball);
                    default:
                        return session.Translation.GetTranslation(TranslationString.CommonWordUnknown);
                }
            };

            var catchType = evt.CatchType;
            LogLevel caughtEscapeFlee;

            string strStatus;
            switch (evt.Status)
            {
                case CatchPokemonResponse.Types.CatchStatus.CatchError:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusError);
                    caughtEscapeFlee = LogLevel.Error;
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchEscape:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusEscape);
                    caughtEscapeFlee = LogLevel.Escape;
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchFlee:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusFlee);
                    caughtEscapeFlee = LogLevel.Flee;
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchMissed:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusMissed);
                    caughtEscapeFlee = LogLevel.Escape;
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchSuccess:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusSuccess);
                    caughtEscapeFlee = LogLevel.Caught;
                    break;
                default:
                    strStatus = evt.Status.ToString();
                    caughtEscapeFlee = LogLevel.Error;
                    break;
            }

            var catchStatus = evt.Attempt > 1
                ? session.Translation.GetTranslation(TranslationString.CatchStatusAttempt, strStatus, evt.Attempt)
                : session.Translation.GetTranslation(TranslationString.CatchStatus, strStatus);

            var familyCandies = evt.FamilyCandies > 0
                ? session.Translation.GetTranslation(TranslationString.Candies, evt.FamilyCandies)
                : "";

            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventPokemonCapture, catchStatus, catchType, session.Translation.GetPokemonName(evt.Id),
                    evt.Level, evt.Cp, evt.MaxCp, evt.Perfection.ToString("0.00"), evt.Probability,
                    evt.Distance.ToString("F2"),
                    returnRealBallName(evt.Pokeball), evt.BallAmount, evt.Exp, familyCandies), caughtEscapeFlee);
        }

        public void HandleEvent(NoPokeballEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventNoPokeballs, session.Translation.GetPokemonName(evt.Id), evt.Cp),
                LogLevel.Caught);
        }

        public void HandleEvent(UseBerryEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.UseBerry, evt.Count),
                LogLevel.Berry);
        }

        public void HandleEvent(SnipeScanEvent evt, ISession session)
        {
            Logger.Write(evt.PokemonId == PokemonId.Missingno
                ? session.Translation.GetTranslation(TranslationString.SnipeScan,
                    $"{evt.Bounds.Latitude},{evt.Bounds.Longitude}")
                : session.Translation.GetTranslation(TranslationString.SnipeScanEx, session.Translation.GetPokemonName(evt.PokemonId),
                    evt.Iv > 0 ? evt.Iv.ToString(CultureInfo.InvariantCulture) : "unknown",
                    $"{evt.Bounds.Latitude},{evt.Bounds.Longitude}"));
        }

        public void HandleEvent(DisplayHighestsPokemonEvent evt, ISession session)
        {
            string strHeader;
            //PokemonData | CP | IV | Level | MOVE1 | MOVE2
            switch (evt.SortedBy)
            {
                case "Level":
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestsLevelHeader);
                    break;
                case "IV":
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestsPerfectHeader);
                    break;
                case "CP":
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestsCpHeader);
                    break;
                case "MOVE1":
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestMove1Header);
                    break;
                case "MOVE2":
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestMove2Header);
                    break;
                default:
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestsHeader);
                    break;
            }
            var strPerfect = session.Translation.GetTranslation(TranslationString.CommonWordPerfect);
            var strName = session.Translation.GetTranslation(TranslationString.CommonWordName).ToUpper();

            Logger.Write($"====== {strHeader} ======", LogLevel.Info, ConsoleColor.Yellow);
            Logger.Write($">  {"CP/BEST".PadLeft(8, ' ')}{(evt.DisplayPokemonMaxPoweredCp ? "/POWERED" : "")} |\t{strPerfect.PadLeft(6, ' ')}\t| LVL | {strName.PadRight(10, ' ')} | {("MOVE1").PadRight(18, ' ')} | {("MOVE2").PadRight(6, ' ')} {(evt.DisplayPokemonMovesetRank ? "| MoveRankVsAveType |" : "")}", LogLevel.Info, ConsoleColor.Yellow);
            foreach (var pokemon in evt.PokemonList)
                Logger.Write(
                  $"# {pokemon.PokeData.Cp.ToString().PadLeft(4, ' ')}/{pokemon.PerfectCp.ToString().PadLeft(4, ' ')}{(evt.DisplayPokemonMaxPoweredCp ? "/" + pokemon.MaximumPoweredCp.ToString().PadLeft(4, ' ') : "")} | {pokemon.Perfection.ToString("0.00")}%\t | {pokemon.Level.ToString("00")} | {pokemon.PokeData.PokemonId.ToString().PadRight(10, ' ')} | {pokemon.Move1.ToString().PadRight(18, ' ')} | {pokemon.Move2.ToString().PadRight(13, ' ')} {(evt.DisplayPokemonMovesetRank ? "| " + pokemon.AverageRankVsTypes : "")}",
                    LogLevel.Info, ConsoleColor.Yellow);
        }

        public void HandleEvent(UpdateEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Update);
        }

        public void Listen(IEvent evt, ISession session)
        {
            dynamic eve = evt;

            try
            {
                HandleEvent(eve, session);
            }
                // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }
    }
}
