﻿#region using directives

using System.Collections.Generic;
using POGOProtos.Inventory;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Egg
{
    public class EggsListEvent : IEvent
    {
        public float PlayerKmWalked { get; set; }
        public List<EggIncubator> Incubators { get; set; }
        public object UnusedEggs { get; set; }
    }
}