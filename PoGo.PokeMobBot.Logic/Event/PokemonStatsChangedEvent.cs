#region using directives

using POGOProtos.Enums;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
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
    }
}