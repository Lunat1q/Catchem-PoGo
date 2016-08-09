#region using directives

using POGOProtos.Enums;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokemonEvolveEvent : IEvent
    {
        public ulong Uid;
        public int Exp;
        public PokemonId Id;
        public EvolvePokemonResponse.Types.Result Result;
    }
}