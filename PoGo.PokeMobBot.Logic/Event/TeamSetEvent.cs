#region using directives

using POGOProtos.Enums;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class TeamSetEvent : IEvent
    {
        public TeamColor Color;
    }
}