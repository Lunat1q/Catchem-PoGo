﻿#region using directives

using POGOProtos.Enums;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class EggHatchedEvent : IEvent
    {
        public int Cp;
        public ulong Id;
        public double Level;
        public int MaxCp;
        public double Perfection;
        public PokemonId PokemonId;
        public PokemonFamilyId Family;
        public int Candy;
        public PokemonMove Move1;
        public PokemonMove Move2;
    }
}