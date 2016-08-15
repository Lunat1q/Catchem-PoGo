using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGo.PokeMobBot.Logic.Event
{
    public class BotCompleteFailureEvent : IEvent
    {
        public bool Shutdown;
        public bool Stop;
    }
}
