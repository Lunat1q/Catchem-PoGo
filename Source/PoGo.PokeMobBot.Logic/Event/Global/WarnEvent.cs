﻿namespace PoGo.PokeMobBot.Logic.Event.Global
{
    public class WarnEvent : IEvent
    {
        public string Message = "";

        /// <summary>
        ///     This event requires handler to perform input
        /// </summary>
        public bool RequireInput;

        public override string ToString()
        {
            return Message;
        }
    }
}