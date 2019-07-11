#region using directives

using POGOProtos.Enums;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Player
{
    public class TeamSetEvent : IEvent
    {
        public TeamColor Color;
    }
}