#region using directives

using POGOProtos.Enums;
using POGOProtos.Settings.Master.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class EggHatchedEvent : IEvent
    {
        public int Cp;
        public ulong Id;
        public double Level;
        public int MaxCp;
        public double Perfection;
        public PokemonId PokemonId;
        public PokemonFamilyId Family;
        public int Candy;
        public PokemonMove Move1;
        public PokemonMove Move2;
        public PokemonType Type1;
        public PokemonType Type2;
        public StatsAttributes Stats;
        public int Stamina;
        public int MaxStamina;
        public int PossibleCp;
        public int CandyToEvolve;
    }
}