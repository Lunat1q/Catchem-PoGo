#region using directives

using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class ProfileEvent : IEvent
    {
        public GetPlayerResponse Profile;
    }
}