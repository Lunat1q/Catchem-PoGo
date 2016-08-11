using POGOProtos.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Catchem
{
    public class PokemonUiData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ulong Id { get; set; }
        public BitmapSource Image { get; set; }
        public string Name { get; set; }
        public int Cp { get; set; }
        public double Iv { get; set; }
        public PokemonId PokemonId { get; set; }
        public PokemonFamilyId Family { get; set; }
        private int _candy;
        public ulong Timestamp { get; set; }
        public int Candy
        {
            get { return _candy; }
            set
            {
                _candy = value;
                OnPropertyChanged();
            }
        }

        public PokemonUiData(ulong id, PokemonId pokemonid, BitmapSource img, string name, int cp, double iv, PokemonFamilyId family, int candy, ulong stamp)
        {
            Id = id;
            PokemonId = pokemonid;
            Image = img;
            Name = name;
            Cp = cp;
            Iv = iv;
            Candy = candy;
            Family = family;
            Timestamp = stamp;
        }
    }
}
