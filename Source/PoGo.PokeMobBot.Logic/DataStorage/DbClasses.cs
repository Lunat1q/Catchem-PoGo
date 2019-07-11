using System;
using POGOProtos.Enums;

namespace PoGo.PokeMobBot.Logic.DataStorage
{
    public class PokeStop
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
    }

    public class PokemonSeen
    {
        public int Id { get; set; }
        public PokemonId PokemonId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime SeenTime { get; set; }
        public string SpawnpointId { get; set; }
    }
    
}
