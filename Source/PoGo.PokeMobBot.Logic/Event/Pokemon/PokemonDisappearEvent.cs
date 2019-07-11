#region using directives



#endregion

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class PokemonDisappearEvent : IEvent
    {
        public ulong EncounterId;
    }
}