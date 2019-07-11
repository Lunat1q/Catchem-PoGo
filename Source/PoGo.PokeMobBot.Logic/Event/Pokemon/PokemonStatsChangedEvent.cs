#region using directives

using POGOProtos.Enums;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class PokemonStatsChangedEvent : IEvent
    {
        public string Name;
        public ulong Uid;
        public PokemonFamilyId Family;
        public int Candy;
        public int Cp;
        public double Iv;
        public PokemonId Id;
        public bool Favourite;
        public int MaxCp;
        public int IvAtk;
        public int IvDef;
        public float Cpm;
        public float Weight;
        public double Level;
        public int Stamina;
        public int StaminaMax;
    }
}