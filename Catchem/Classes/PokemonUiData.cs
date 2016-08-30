using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.PoGoUtils;
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

        //public BitmapSource Image { get; set; }
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

        private int _possibleCp;

        public int PossibleCp
        {
            get { return _possibleCp; }
            set
            {
                _possibleCp = value;
                OnPropertyChanged();
            }
        }

        private int _maxCp;

        public int MaxCp
        {
            get { return _maxCp; }
            set
            {
                _maxCp = value;
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
        private int _candyToEvolve;
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

        public PokemonType Type1
        {
            get { return _type1; }
            set
            {
                _type1 = value;
                OnPropertyChanged();
            }
        }

        public PokemonType Type2
        {
            get { return _type2; }
            set
            {
                _type2 = value;
                OnPropertyChanged();
            }
        }

        private BaseStats _stats;

        private double _level;
        private PokemonMove _move1;
        private PokemonMove _move2;

        private PokemonType _type1;
        private PokemonType _type2;

        public string TypeText => $"{_type1}/{_type2}";

        public BaseStats Stats
        {
            get { return _stats; }
            set
            {
                _stats = value;
                OnPropertyChanged();
            }
        }

        public int Stamina
        {
            get { return _stamina; }
            set
            {
                _stamina = value;
                OnPropertyChanged();
            }
        }

        public int MaxStamina
        {
            get { return _maxStamina; }
            set
            {
                _maxStamina = value;
                OnPropertyChanged();
            }
        }

        public string HpText => $"{_stamina}/{_maxStamina}";
        public string CandyText => $"{Candy} / {(CandyToEvolve > 0 ? CandyToEvolve.ToString() : "-")}";

        public int Atk => Stats.BaseAttack;
        public int Def => Stats.BaseDefense;
        public int Sta => Stats.BaseStamina;

        public int CandyToEvolve
        {
            get { return _candyToEvolve; }
            set
            {
                _candyToEvolve = value;
                OnPropertyChanged();
            }
        }

        private int _stamina;
        private int _maxStamina;


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

        public PokemonUiData(BotWindowData ownerBot, ulong id, PokemonId pokemonid, string name, //BitmapSource img,
            int cp, double iv, PokemonFamilyId family, int candy, ulong stamp, bool fav, bool inGym, double level,
            PokemonMove move1, PokemonMove move2, PokemonType type1, PokemonType type2, int maxCp, BaseStats baseStats,
            int stamina, int maxStamina, int possibleCp, int candyToEvolve)
        {
            OwnerBot = ownerBot;
            Favoured = fav;
            InGym = inGym;
            Id = id;
            PokemonId = pokemonid;
            //Image = img;
            Name = name;
            Cp = cp;
            Iv = iv;
            Candy = candy;
            Family = family;
            Timestamp = stamp;
            Level = level;
            Move1 = move1;
            Move2 = move2;
            Type1 = type1;
            Type2 = type2;
            MaxCp = maxCp;
            Stats = baseStats;
            Stamina = stamina;
            MaxStamina = maxStamina;
            CandyToEvolve = candyToEvolve;
            PossibleCp = possibleCp;
        }
    }
}
