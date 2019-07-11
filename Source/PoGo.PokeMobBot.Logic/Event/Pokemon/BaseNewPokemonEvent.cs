#region using directives

using POGOProtos.Enums;
using POGOProtos.Settings.Master.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class BaseNewPokemonEvent : IEvent
    {
        public int Cp;
        public ulong Uid;
        public PokemonId Id;
        public double Perfection;
        public PokemonFamilyId Family;
        public int Candy;
        public double Level;
        public PokemonMove Move1;
        public PokemonMove Move2;
        public PokemonType Type1;
        public PokemonType Type2;
        public StatsAttributes Stats;
        public int MaxCp;
        public int Stamina;
        public int IvSta;
        public int PossibleCp;
        public int CandyToEvolve;
        public int IvAtk;
        public int IvDef;
        public float Cpm;
        public float Weight;
        public int StaminaMax;
        public PokemonId[] Evolutions;
        public double Latitude;
        public double Longitude;
    }
}