using PoGo.PokeMobBot.Logic.Event;
using POGOProtos.Enums;

namespace Catchem.Events
{
    public class TelegramPokemonCaughtEvent : IEvent
    {
        public PokemonId PokemonId;
        public int Cp;
        public double Iv;
        public string ProfileName;
        public string BotNicnname;
        public double Level;
        public PokemonMove? Move2;
        public PokemonMove? Move1;
    }
}
