﻿namespace PoGo.PokeMobBot.Logic.Event.GUI
{
    public class UpdateEvent : IEvent
    {
        public string Message = "";

        public override string ToString()
        {
            return Message;
        }
    }
}