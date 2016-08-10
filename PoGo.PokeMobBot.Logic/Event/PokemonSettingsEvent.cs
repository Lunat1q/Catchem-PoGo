using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POGOProtos.Settings.Master;

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokemonSettingsEvent : IEvent
    {
        public List<PokemonSettings> Data;
    }
}
