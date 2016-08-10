#region using directives

using System;
using System.Collections.Generic;
using POGOProtos.Data;
using POGOProtos.Enums;
using PoGo.PokeMobBot.Logic.PoGoUtils;


#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class DisplayHighestsPokemonEvent : IEvent
    {
        //PokemonData | CP | IV | Level | MOVE1 | MOVE2 | AverageRankVsTypes
        public List<PokemonAnalysis> PokemonList;
        public string SortedBy;
        public bool DisplayPokemonMaxPoweredCp;
        public bool DisplayPokemonMovesetRank;

    }
}