#region using directives

using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokemonEvolveDoneEvent : IEvent
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
        public int MaxStamina;
        public int PossibleCp;
        public int CandyToEvolve;
    }
}