using System;
using System.Collections.Generic;

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokestopsOptimalPathEvent : IEvent
    {
        public List<Tuple<double, double>> Coords = new List<Tuple<double, double>>();
    }
}