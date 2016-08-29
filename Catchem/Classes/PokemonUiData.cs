﻿using System.Collections.Generic;
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

        public BotWindowData OwnerBot { get; set; }

        public BitmapSource Image { get; set; }
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

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

        private bool _inGym;

        public bool InGym
        {
            get { return _inGym; }
            set
            {
                _inGym = value;
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

        private bool _favoured;

        public bool Favoured
        {
            get { return _favoured; }
            set
            {
                _favoured = value;
                OnPropertyChanged();
            }
        }

        public double Level
        {
            get { return _level; }
            set
            {
                _level = value;
                OnPropertyChanged();
            }
        }

        public PokemonMove Move1
        {
            get { return _move1; }

            set
            {
                _move1 = value;
                OnPropertyChanged();
            }
        }

        public PokemonMove Move2
        {
            get { return _move2; }

            set
            {
                _move2 = value;
                OnPropertyChanged();
            }
        }

        private double _level;
        private PokemonMove _move1;
        private PokemonMove _move2;

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

        public PokemonUiData(BotWindowData ownerBot, ulong id, PokemonId pokemonid, BitmapSource img, string name,
            int cp, double iv, PokemonFamilyId family, int candy, ulong stamp, bool fav, bool inGym, double level, PokemonMove move1, PokemonMove move2)
        {
            OwnerBot = ownerBot;
            Favoured = fav;
            InGym = inGym;
            Id = id;
            PokemonId = pokemonid;
            Image = img;
            Name = name;
            Cp = cp;
            Iv = iv;
            Candy = candy;
            Family = family;
            Timestamp = stamp;
            Level = level;
            Move1 = move1;
            Move2 = move2;
        }
    }
}
