#region using directives

using System;
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
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic
{
    public class StatisticsAggregator
    {

        public void HandleEvent(ProfileEvent evt, ISession session)
        {
            session.Stats.SetUsername(evt.Profile);
            session.Stats.RefreshStatAndCheckLevelup(session);
        }

        public void HandleEvent(ErrorEvent evt, ISession session)
        {
        }

        public void HandleEvent(NoticeEvent evt, ISession session)
        {
        }

        public void HandleEvent(FortLureStartedEvent evt, ISession session)
        {
        }

        public void HandleEvent(ItemUsedEvent evt, ISession session)
        {

        }

        public void HandleEvent(WarnEvent evt, ISession session)
        {
        }

        public void HandleEvent(UseLuckyEggEvent evt, ISession session)
        {
        }

        public void HandleEvent(PokemonEvolveEvent evt, ISession session)
        {
            session.Stats.TotalExperience += evt.Exp;
            session.Stats.RefreshStatAndCheckLevelup(session);
        }

        public void HandleEvent(TransferPokemonEvent evt, ISession session)
        {
            session.Stats.TotalPokemonsTransfered++;
            session.Stats.RefreshStatAndCheckLevelup(session);
        }

        public void HandleEvent(EggHatchedEvent evt, ISession session)
        {
            session.Stats.TotalPokemons++;
            session.Stats.HatchedNow++;
            session.Stats.RefreshStatAndCheckLevelup(session);
            session.Stats.RefreshPokeDex(session);
        }

        public void HandleEvent(VerifyChallengeEvent evt, ISession session)
        {
          
        }

        public void HandleEvent(CheckChallengeEvent evt, ISession session)
        {

        }

        public void HandleEvent(BuddySetEvent evt, ISession session)
        {

        }

        public void HandleEvent(BuddyWalkedEvent evt, ISession session)
        {

        }

        public void HandleEvent(ItemRecycledEvent evt, ISession session)
        {
            session.Stats.TotalItemsRemoved += evt.Count;
            session.Stats.RefreshStatAndCheckLevelup(session);
        }

        public void HandleEvent(FortUsedEvent evt, ISession session)
        {
            session.Stats.TotalExperience += evt.Exp;
            session.Stats.TotalPokestops++;
            session.Stats.RefreshStatAndCheckLevelup(session);
        }

        public void HandleEvent(FortTargetEvent evt, ISession session)
        {
        }

        public void HandleEvent(PokemonActionDoneEvent evt, ISession session)
        {
        }

        public void HandleEvent(PokemonCaptureEvent evt, ISession session)
        {
            if (evt.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
            {
                session.Stats.TotalExperience += evt.Exp;
                session.Stats.TotalPokemons++;
                session.Stats.TotalStardust = evt.Stardust;
                session.Stats.RefreshStatAndCheckLevelup(session);
                session.Stats.RefreshPokeDex(session);
            }
            session.Stats.PokeBalls++;
        }

        public void HandleEvent(NoPokeballEvent evt, ISession session)
        {
        }

        public void HandleEvent(UseBerryEvent evt, ISession session)
        {
        }
        
        public void HandleEvent(DisplayHighestsPokemonEvent evt, ISession session)
        {
        }

        public void HandleEvent(UpdatePositionEvent evt, ISession session)
        {
        }

        public void HandleEvent(NewVersionEvent evt, ISession session)
        {
            
        }

        public void HandleEvent(UpdateEvent evt, ISession session)
        {

        }
        public void HandleEvent(BotCompleteFailureEvent evt, ISession session)
        {

        }
        public void HandleEvent(DebugEvent evt, ISession session)
        {

        }
        public void HandleEvent(EggIncubatorStatusEvent evt, ISession session)
        {

        }
        public void HandleEvent(EggsListEvent evt, ISession session)
        {

        }
        public void HandleEvent(ForceMoveDoneEvent evt, ISession session)
        {

        }
        public void HandleEvent(FortFailedEvent evt, ISession session)
        {

        }
        public void HandleEvent(InvalidKeepAmountEvent evt, ISession session)
        {

        }
        public void HandleEvent(InventoryListEvent evt, ISession session)
        {

        }
        public void HandleEvent(InventoryNewItemsEvent evt, ISession session)
        {

        }

        public void HandleEvent(EggFoundEvent evt, ISession session)
        {
           
        }
        public void HandleEvent(NextRouteEvent evt, ISession session)
        {

        }
        public void HandleEvent(PlayerLevelUpEvent evt, ISession session)
        {

        }
        public void HandleEvent(PlayerStatsEvent evt, ISession session)
        {

        }
        public void HandleEvent(PokemonDisappearEvent evt, ISession session)
        {
            session.Stats.EncountersNow++;
        }
        public void HandleEvent(PokemonEvolveDoneEvent evt, ISession session)
        {
            session.Stats.TotalEvolves++;
            session.Stats.RefreshPokeDex(session);
        }
        public void HandleEvent(PokemonFavoriteEvent evt, ISession session)
        {

        }
        public void HandleEvent(PokemonSettingsEvent evt, ISession session)
        {

        }
        public void HandleEvent(PokemonsFoundEvent evt, ISession session)
        {

        }
        public void HandleEvent(PokemonsWildFoundEvent evt, ISession session)
        {

        }
        public void HandleEvent(PokemonStatsChangedEvent evt, ISession session)
        {

        }

        public void HandleEvent(PokeStopListEvent evt, ISession session)
        {

        }
        public void HandleEvent(SnipeEvent evt, ISession session)
        {

        }
        public void HandleEvent(SnipeModeEvent evt, ISession session)
        {

        }
        public void HandleEvent(UseLuckyEggMinPokemonEvent evt, ISession session)
        {

        }
        public void HandleEvent(PokemonListEvent evt, ISession session)
        {

        }

        public void HandleEvent(GymPokeEvent evt, ISession session)
        {

        }

        public void HandleEvent(PokestopsOptimalPathEvent evt, ISession session)
        {

        }

        public void HandleEvent(TeamSetEvent evt, ISession session)
        {

        }
        public void HandleEvent(ItemLostEvent evt, ISession session)
        {

        }

        public void Listen(IEvent evt, ISession session)
        {
            try
            {
                dynamic eve = evt;
                HandleEvent(eve, session);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }
        }
    }
}