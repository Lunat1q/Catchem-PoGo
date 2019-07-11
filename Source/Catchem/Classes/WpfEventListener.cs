using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Catchem.Extensions;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Enums;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Event.Egg;
using PoGo.PokeMobBot.Logic.Event.Fort;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.GUI;
using PoGo.PokeMobBot.Logic.Event.Item;
using PoGo.PokeMobBot.Logic.Event.Logic;
using PoGo.PokeMobBot.Logic.Event.Obsolete;
using PoGo.PokeMobBot.Logic.Event.Player;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;

namespace Catchem.Classes
{
    public class WpfEventListener
    {
        public void HandleEvent(ProfileEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventProfileLogin,
                evt.Profile.PlayerData.Username ?? ""), session: session);
            var playerData = session.Profile?.PlayerData;
            if (playerData == null) return;
            Logger.PushToUi(MainRpc.Profile, session, playerData.Username,
                playerData.MaxItemStorage, playerData.MaxPokemonStorage,
                playerData.Currencies[0].Amount, playerData.Team, playerData.Currencies[1].Amount);
        }

        public void HandleEvent(ErrorEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Error, session: session);
            Logger.PushToUi(MainRpc.Error, session);
        }

        public void HandleEvent(NoticeEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), session: session);
        }


        public void HandleEvent(DebugEvent evt, ISession session)
	    {
	#if DEBUG
	            Logger.Write(evt.ToString(), LogLevel.Debug, session: session);
	#endif
        }


        public void HandleEvent(PlayerLevelUpEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.LevelUp) + evt.Items, session: session);
            Logger.PushToUi(MainRpc.Lvl, session, evt.Level, evt.InventoryFull, evt.Items);
        }

        public void HandleEvent(TelegramMessageEvent evt, ISession session)
        {
           Logger.Write(evt.Message, LogLevel.Telegram, session: session);
        }

        public void HandleEvent(WarnEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Warning, session: session);

            if (evt.RequireInput)
            {
                Logger.Write(session.Translation.GetTranslation(TranslationString.RequireInputText), session: session);
                Console.ReadKey();
            }
        }

        public void HandleEvent(UseLuckyEggEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventUsedLuckyEgg, evt.Count),
                LogLevel.Egg, session: session);
        }

        public void HandleEvent(UseLuckyEggMinPokemonEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventUseLuckyEggMinPokemonCheck, evt.Diff, evt.CurrCount, evt.MinPokemon), session: session);
        }

        public void HandleEvent(FortLureStartedEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.SetLure, evt.Name, evt.Dist.ToN1(), evt.LureCountLeft), LogLevel.LureModule, session: session);
            Logger.PushToUi(MainRpc.PokestopSetLure, session, evt.Id);
        }

        public void HandleEvent(ItemUsedEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.ItemUsed, session, evt.Id, evt.ExpireMs);
        }

        public void HandleEvent(PokemonEvolveEvent evt, ISession session)
        {
            Logger.Write(evt.Result == EvolvePokemonResponse.Types.Result.Success
                ? session.Translation.GetTranslation(TranslationString.EventPokemonEvolvedSuccess, session.Translation.GetPokemonName(evt.Id), evt.Exp)
                : session.Translation.GetTranslation(TranslationString.EventPokemonEvolvedFailed, session.Translation.GetPokemonName(evt.Id), evt.Result,
                    evt.Id),
                LogLevel.Evolve, session: session);

            if (evt.Result == EvolvePokemonResponse.Types.Result.Success)
                Logger.PushToUi(MainRpc.PokemonLost, session, evt.Uid, null, null);
        }
        public void HandleEvent(PokemonEvolveDoneEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EvolvedInto, evt.Id, evt.Cp, evt.Perfection.ToString("0.00")), session: session);

            Logger.PushToUi(MainRpc.PokemonNew, session, evt, "Evolved");
        }
        
        public void HandleEvent(PokemonStatsChangedEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.PokemonUpdate, session, evt);
        }

        public void HandleEvent(TeamSetEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.TeamSet, session, evt.Color);
        }
        public void HandleEvent(GymPokeEvent evt, ISession session)
        {
            var defendersInfo = new List<string>();
            if (evt.GymState?.Memberships != null)
                foreach (var defender in evt.GymState.Memberships)
                {
                    defendersInfo.Add(
                        $"{defender.TrainerPublicProfile.Name} ({defender.PokemonData.PokemonId} - {defender.PokemonData.Cp})");
                }
            var guardList = defendersInfo.Count > 0 ? defendersInfo.Aggregate((x, v) => x + ", " + v) : "";
            var gymDesc = string.IsNullOrEmpty(evt.Description) ? "" : $" ({evt.Description})";
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.GymTouch, evt.Name, gymDesc,
                    evt.GymState?.FortData.OwnedByTeam, evt.GymState?.FortData.GymPoints, guardList), LogLevel.Gym,
                session: session);
            Logger.PushToUi(MainRpc.GymPoke, session, evt.Id, evt.Name, evt.GymState?.FortData.OwnedByTeam, evt.Lat, evt.Lon);
        }

        public void HandleEvent(EggFoundEvent evt, ISession session)
        {
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EggFound, evt.Distance.ToN1(), evt.PokeStopName),
                LogLevel.Info, session: session);
        }

        public void HandleEvent(BotCompleteFailureEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.BotFailure, session, evt.Shutdown, evt.Stop, evt.Message);
        }

        public void HandleEvent(NextRouteEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.RouteNext, session, evt.Coords);
        }

        public void HandleEvent(UpdatePositionEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.PlayerLocation, session, evt.Latitude, evt.Longitude, evt.Altitude);
        }

        public void HandleEvent(TransferPokemonEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventPokemonTransferred, session.Translation.GetPokemonName(evt.Id), evt.Cp,
                    evt.Perfection.ToString("0.00"), evt.BestCp, evt.BestPerfection.ToString("0.00"), evt.FamilyCandies),
                LogLevel.Transfer, session: session);
            Logger.PushToUi(MainRpc.PokemonLost, session, evt.Uid, evt.Family, evt.FamilyCandies);
        }

        public void HandleEvent(PokeStopListEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.PokeStops, session, evt.Forts);
        }

        public void HandleEvent(ForceMoveDoneEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.ForceMoveDone, session);
        }

        public void HandleEvent(PokemonsFoundEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.Pokemons, session, evt.Pokemons);
        }
        public void HandleEvent(PokemonsWildFoundEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.PokemonsWild, session, evt.Pokemons);
        }        

        public void HandleEvent(PokemonDisappearEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.PokemonMapRemove, session, evt.EncounterId);
        }

        public void HandleEvent(ItemRecycledEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventItemRecycled, evt.Count, evt.Id),
                LogLevel.Recycling, session: session);
            Logger.PushToUi(MainRpc.ItemRemove, session, evt.Id, evt.Count);
        }

        

        public void HandleEvent(ItemLostEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.ItemRemove, session, evt.Id, evt.Count);
        }

        public void HandleEvent(PokestopsOptimalPathEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.PokestopsOpt, session, evt.Coords);
        }

        public void HandleEvent(EggIncubatorStatusEvent evt, ISession session)
        {
            Logger.Write(evt.WasAddedNow
                ? session.Translation.GetTranslation(TranslationString.IncubatorPuttingEgg, evt.KmRemaining)
                : session.Translation.GetTranslation(TranslationString.IncubatorStatusUpdate, evt.KmRemaining),
                LogLevel.Egg, session: session);
        }

        public void HandleEvent(EggHatchedEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.IncubatorEggHatched,
                session.Translation.GetPokemonName(evt.Id), evt.Level, evt.Cp, evt.MaxCp, evt.Perfection),
                LogLevel.Egg, session: session);
            Logger.PushToUi(MainRpc.PokemonNew, session, evt, "Hatched");
        }

        public void HandleEvent(FortUsedEvent evt, ISession session)
        {
            var itemString = evt.InventoryFull
                ? session.Translation.GetTranslation(TranslationString.InvFullPokestopLooting)
                : evt.Items;
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventFortUsed, evt.Name, evt.Exp, evt.Gems,
                    itemString),
                LogLevel.Pokestop, session: session);
            Logger.PushToUi(MainRpc.PokestopInfo, session, evt.Id, evt.Name, evt.Description, evt.url);
        }

        public void HandleEvent(FortFailedEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventFortFailed, evt.Name, evt.Try, evt.Max),
                LogLevel.Pokestop, ConsoleColor.DarkRed, session);
        }

        public void HandleEvent(FortTargetEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventFortTargeted, evt.Name,
                    Math.Round(evt.Distance)),
                LogLevel.Info, ConsoleColor.DarkRed, session);
        }

        public void HandleEvent(PokemonActionDoneEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.PokeActionDone, session, evt.Uid);
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
                    Logger.PushToUi(MainRpc.PokemonNew, session, evt, "Caught");
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
                session.Translation.GetTranslation(TranslationString.EventPokemonCapture, catchStatus, catchType,
                    session.Translation.GetPokemonName(evt.Id),
                    evt.Level, evt.Cp, evt.MaxCp, evt.Perfection.ToString("0.00"), evt.Probability,
                    evt.Distance.ToString("F2"),
                    returnRealBallName(evt.Pokeball), evt.BallAmount, evt.Exp, familyCandies), caughtEscapeFlee, session: session);
        }

        public void HandleEvent(NoPokeballEvent evt, ISession session)
        {
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventNoPokeballs,
                    session.Translation.GetPokemonName(evt.Id), evt.Cp),
                LogLevel.Caught, session: session);
        }

        public void HandleEvent(UseBerryEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.UseBerry, evt.Count),
                LogLevel.Berry, session: session);
        }

        public void HandleEvent(SnipeScanEvent evt, ISession session)
        {
            Logger.Write(evt.PokemonId == PokemonId.Missingno
                ? session.Translation.GetTranslation(TranslationString.SnipeScan,
                    $"{evt.Bounds.Latitude},{evt.Bounds.Longitude}")
                : session.Translation.GetTranslation(TranslationString.SnipeScanEx, session.Translation.GetPokemonName(evt.PokemonId),
                    evt.Iv > 0 ? evt.Iv.ToString(CultureInfo.InvariantCulture) : "unknown",
                    $"{evt.Bounds.Latitude},{evt.Bounds.Longitude}"), session: session);
        }
        
        public void HandleEvent(PokemonListEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.PokemonList, session, evt.PokemonList);
        }
        
        public void HandleEvent(InventoryListEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.ItemList, session, evt.Items);
        }
        public void HandleEvent(InventoryNewItemsEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.ItemNew, session, evt.Items);
        }

        public void HandleEvent(NewVersionEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.NewVersion, session, evt.v);
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

            Logger.Write($"====== {strHeader} ======", LogLevel.Info, ConsoleColor.Yellow, session);
            Logger.Write(
                $">  {"CP/BEST".PadLeft(8, ' ')}{(evt.DisplayPokemonMaxPoweredCp ? "/POWERED" : "")} |\t{strPerfect.PadLeft(6, ' ')}\t| LVL | {strName.PadRight(10, ' ')} | {"MOVE1".PadRight(18, ' ')} | {"MOVE2".PadRight(6, ' ')} {(evt.DisplayPokemonMovesetRank ? "| MoveRankVsAveType |" : "")}",
                LogLevel.Info, ConsoleColor.Yellow, session);
            if (evt.PokemonList != null)
                foreach (var pokemon in evt.PokemonList)
                    Logger.Write(
                        $"# {pokemon.PokeData.Cp.ToString().PadLeft(4, ' ')}/{pokemon.PerfectCp.ToString().PadLeft(4, ' ')}{(evt.DisplayPokemonMaxPoweredCp ? "/" + pokemon.MaximumPoweredCp.ToString().PadLeft(4, ' ') : "")} | {pokemon.Perfection.ToString("0.00")}%\t | {pokemon.Level.ToString("00")} | {pokemon.PokeData.PokemonId.ToString().PadRight(10, ' ')} | {pokemon.Move1.ToString().PadRight(18, ' ')} | {pokemon.Move2.ToString().PadRight(13, ' ')} {(evt.DisplayPokemonMovesetRank ? "| " + pokemon.AverageRankVsTypes : "")}",
                        LogLevel.Info, ConsoleColor.Yellow, session);
            else
            {
                Logger.Write(
                        session.Translation.GetTranslation(TranslationString.PokeListEmpty),
                        LogLevel.Info, ConsoleColor.Yellow, session);
            }
        }

        public void HandleEvent(UpdateEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Update, session: session);
        }
		public void HandleEvent(PokemonFavoriteEvent evt, ISession session)  //added by Lars
        {
            var message = $"{evt.Pokemon,-13} CP: {evt.Cp,-4} IV: {evt.Iv,-4:#.00}% Candies: {evt.Candies}";
		    var msg =
		        session.Translation.GetTranslation(
		            evt.Favoured ? TranslationString.PokemonFavorite : TranslationString.PokemonUnFavorite, message);
            Logger.Write(msg, LogLevel.Favorite, session: session);
            Logger.PushToUi(MainRpc.PokemonFav, session, evt.Uid, evt.Favoured);
        }

        public void HandleEvent(InvalidKeepAmountEvent evt, ISession session) //added by Lars
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.CheckingForMaximumInventorySize, evt.Count, evt.Max), LogLevel.Warning, session: session);
        }

        public void HandleEvent(BuddySetEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.Buddy, session, "Set", evt.UpdatedBuddy);
        }

        public void HandleEvent(BuddyWalkedEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.Buddy, session, "Walked", evt.CandyEarnedCount, evt.FamilyCandyId, evt.Success);
        }

        public void HandleEvent(CheckChallengeEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.Challenge, session, "Check", evt.ChallengeUrl, evt.ShowChallenge);
        }

        public void HandleEvent(VerifyChallengeEvent evt, ISession session)
        {
            Logger.PushToUi(MainRpc.Challenge, session, "Verify", evt.Success);
        }

        public void HandleEvent(EggsListEvent evt, ISession session)
        {

        }
        public void HandleEvent(PlayerStatsEvent evt, ISession session)
        {

        }

        public void HandleEvent(PokemonSettingsEvent evt, ISession session)
        {

        }
        public void HandleEvent(SnipeEvent evt, ISession session)
        {

        }
        public void HandleEvent(SnipeModeEvent evt, ISession session)
        {

        }
      
        public void Listen(IEvent evt, ISession session)
        {
            try
            {
                dynamic eve = evt;
                HandleEvent(eve, session);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
