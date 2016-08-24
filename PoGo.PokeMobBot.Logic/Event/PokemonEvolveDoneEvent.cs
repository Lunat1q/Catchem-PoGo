#region using directives

using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;

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
    }
}