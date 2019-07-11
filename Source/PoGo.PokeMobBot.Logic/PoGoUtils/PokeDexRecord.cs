using PoGo.PokeMobBot.Logic.Extensions;
using POGOProtos.Enums;

namespace PoGo.PokeMobBot.Logic.PoGoUtils
{
    public class PokeDexRecord : PropertyNotification
    {
        private PokemonId _id;
        private string _pokemonName;
        private int _seen;
        private int _captured;

        public int Rare => _id.HowRare();

        public int Num => (int) _id;

        public PokemonId Id
        {
            get { return _id; }
            set
            {
                _id = value; 
                OnPropertyChanged();
            }
        }

        public string PokemonName
        {
            get { return _pokemonName; }
            set
            {
                _pokemonName = value;
                OnPropertyChanged(); 
            }
        }

        public int CapturedTimes
        {
            get { return _captured; }
            set
            {
                _captured = value;
                OnPropertyChanged();
                OnPropertyChangedByName("CapturedTimes");
            }
        }

        public int SeenTimes
        {
            get { return _seen; }
            set
            {
                _seen = value;
                OnPropertyChanged();
                OnPropertyChangedByName("SeenTimes");
            }
        }

        public bool Seen => _seen > 0;
        public bool Captured => _captured > 0;
    }
}
