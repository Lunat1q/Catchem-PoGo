#region using directives

using POGOProtos.Enums;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Item
{
    public class NoPokeballEvent : IEvent
    {
        public int Cp;
        public PokemonId Id;
    }
}