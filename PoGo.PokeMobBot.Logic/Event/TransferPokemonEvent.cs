#region using directives

using POGOProtos.Enums;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class TransferPokemonEvent : IEvent
    {
        public ulong Uid;
        public int BestCp;
        public double BestPerfection;
        public int Cp;
        public PokemonFamilyId Family;
        public int FamilyCandies;
        public PokemonId Id;
        public double Perfection;
    }
}