using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using PoGo.PokeMobBot.Logic;
using POGOProtos.Enums;

namespace Catchem.Classes
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
        private int _cp;
        public int Cp
        {
            get { return _cp; }
            set
            {
                _cp = value;
                OnPropertyChanged();
            }
        }

        private double _iv;
        public double Iv
        {
            get { return _iv; }
            set
            {
                _iv = value;
                OnPropertyChanged();
            }
        }
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

        private string _tags;

        public string Tags
        {
            get { return _tags; }
            set
            {
                _tags = value;
                OnPropertyChanged();
            }
        }

        public void UpdateTags(LogicSettings ls)
        {
            var tags = new List<string>();
            if (ls.PokemonsToEvolve.Contains(PokemonId))
                tags.Add("ev");
            if (ls.PokemonToUseMasterball.Contains(PokemonId))
                tags.Add("mb");
            if (ls.PokemonsNotToCatch.Contains(PokemonId))
                tags.Add("nc");
            if (ls.PokemonsNotToTransfer.Contains(PokemonId))
                tags.Add("nt");
            if (ls.PokemonsTransferFilter.ContainsKey(PokemonId))
                tags.Add("tf");
            Tags = tags.Count > 0 ? tags.Aggregate((x, v) => x + ", " + v) : "";
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
