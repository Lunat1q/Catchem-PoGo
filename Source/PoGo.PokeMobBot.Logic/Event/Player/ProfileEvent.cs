#region using directives

using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Player
{
    public class ProfileEvent : IEvent
    {
        public GetPlayerResponse Profile;
    }
}