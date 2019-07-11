using POGOProtos.Enums;

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class BuddyWalkedEvent : IEvent
    {
        public int CandyEarnedCount;
        public PokemonFamilyId FamilyCandyId;
        public bool Success;
    }
}
