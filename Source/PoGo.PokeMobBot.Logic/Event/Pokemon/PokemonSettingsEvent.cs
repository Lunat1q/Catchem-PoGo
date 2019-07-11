using System.Collections.Generic;
using POGOProtos.Settings.Master;

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class PokemonSettingsEvent : IEvent
    {
        public List<PokemonSettings> Data;
    }
}
