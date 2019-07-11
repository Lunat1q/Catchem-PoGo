using System;
using System.Collections.Generic;
using System.Linq;
using Catchem.Extensions;
using Catchem.UiTranslation;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using POGOProtos.Enums;

namespace Catchem.Classes
{
    public class PokemonUiData : CatchemNotified
    {
        public ulong Id { get; set; }

        public string Lang => TranslationEngine.CurrentTranslationLanguage;

        public BotWindowData OwnerBot { get; set; }

        private bool _inAction;

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


        public bool _buddy;

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
                OnPropertyChangedByName("CandyData");
                OnPropertyChangedByName("CandyText");
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

        public string LevelText => TranslationEngine.GetDynamicTranslationString("%LEVEL%", "Level") + ": " + Level.ToN1();

        public double Level
        {
            get { return _level; }
            set
            {
                _level = value;
                OnPropertyChanged();
                OnPropertyChangedByName("LevelText");
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

        public string HpText => $"{_stamina}/{MaxStamina}";
        public string CandyData => $"{TranslationEngine.GetDynamicTranslationString("%CANDY%", "Candy")}: {Candy}";
        public string CandyText => $"{CandyData} / {(CandyToEvolve > 0 ? CandyToEvolve.ToString() : "-")}";

        public double Atk => (Stats.BaseAttack + IvAtk)*Cpm;
        //public double MaxStamina => (Stats.BaseStamina + IvSta) * Cpm;
        public double Def => (Stats.BaseDefense + IvDef)*Cpm;

        public int CandyToEvolve
        {
            get { return _candyToEvolve; }
            set
            {
                _candyToEvolve = value;
                OnPropertyChanged();
            }
        }

        public string IvText => $"{TranslationEngine.GetDynamicTranslationString("%IV%", "IV")}: {Iv.ToN1()}% ({IvAtk}/{IvDef}/{IvSta})";

        private int _stamina;
        private int _ivAtk;
        private int _ivDef;
        private int _ivSta;
        private double _cpm;

        private float _weight;


        public PokemonAttackStats Move1Stats => PokemonMoveStatsDictionary.GetMoveData(_move1);
        public double Move1Dps => Move1Stats.Dps*(MatchPokeType(Move1Stats.Type) ? 1.25 : 1);

        public PokemonAttackStats Move2Stats => PokemonMoveStatsDictionary.GetMoveData(_move2);
        public double Move2Dps => Move2Stats.Dps*(MatchPokeType(Move2Stats.Type) ? 1.25 : 1);

        public double SumDps
        {
            get
            {
                if (Move1Stats == null || Move2Stats == null)
                    return 0;
                var energyToFill = Move2Stats.Energy;
                var movesToDo = energyToFill/(double) Move1Stats.Energy;
                if (energyToFill > 50) movesToDo = Math.Ceiling(movesToDo);
                var chargeTimeMs = movesToDo*Move1Stats.DurationMs;
                var chargeDamage = movesToDo*Move1Stats.Damage*(MatchPokeType(Move1Stats.Type) ? 1.25 : 1);
                var attackCycleMs = chargeTimeMs + Move2Stats.DurationMs;
                var attackCycleDamage = chargeDamage + Move2Stats.Damage*(MatchPokeType(Move2Stats.Type) ? 1.25 : 1);
                return attackCycleDamage*(1000/attackCycleMs);
            }
        }

        public float Weight
        {
            get { return _weight; }
            set
            {
                _weight = value;
                OnPropertyChanged();
            }
        }

        public int IvAtk
        {
            get { return _ivAtk; }
            set
            {
                _ivAtk = value;
                OnPropertyChanged();
            }
        }

        public int IvDef
        {
            get { return _ivDef; }
            set
            {
                _ivDef = value;
                OnPropertyChanged();
            }
        }

        public double Cpm
        {
            get { return _cpm; }
            set
            {
                _cpm = value;
                OnPropertyChanged();
            }
        }

        public int IvSta
        {
            get { return _ivSta; }
            set
            {
                _ivSta = value;
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

        private int _maxStamina;

     private bool MatchPokeType(PokemonType type)
        {
            return Type1 == type || Type2 == type;
        }

        public void UpdateTags(LogicSettings ls)
        {
            var tags = new List<string>();
            if (ls.PokemonsToEvolve != null && ls.PokemonsToEvolve.Contains(PokemonId))
                tags.Add("ev");
            if (ls.PokemonToUseMasterball != null && ls.PokemonToUseMasterball.Contains(PokemonId))
                tags.Add("mb");
            if (ls.PokemonsNotToCatch != null && ls.PokemonsNotToCatch.Contains(PokemonId))
                tags.Add("nc");
            if (ls.PokemonsNotToTransfer != null && ls.PokemonsNotToTransfer.Contains(PokemonId))
                tags.Add("nt");
            if (ls.PokemonsTransferFilter != null && ls.PokemonsTransferFilter.Any(x=>x.Id == PokemonId))
                tags.Add("tf");
            Tags = tags.Count > 0 ? tags.Aggregate((x, v) => x + ", " + v) : "";
        }

        public PokemonId[] Evolutions { get; set; }

        public string EvolveCp
        {
            get
            {
                var startText = TranslationEngine.GetDynamicTranslationString("%EVOLVE_CP_AFTER",
                    "CP after Evolution: ");
                if (Evolutions == null || Evolutions.Length == 0) return startText + "-";
                return startText +
                    Evolutions.Select(x => x.PossibleCpAfterEvolution(IvAtk, IvDef, IvSta, Cpm).ToN0())
                        .Aggregate((x, v) => x + " - " + v);
            }
        }

        public bool InAction
        {
            get { return _inAction; }
            set
            {
                _inAction = value;
                OnPropertyChanged();
            }
        }

        public bool Buddy
        {
            get { return _buddy; }
            set
            {
                _buddy = value;
                OnPropertyChanged();
            }
        }

        public PokemonUiData(BotWindowData ownerBot, ulong id, PokemonId pokemonid, string name, //BitmapSource img,
            int cp, double iv, PokemonFamilyId family, int candy, ulong stamp, bool fav, bool inGym, double level,
            PokemonMove move1, PokemonMove move2, PokemonType type1, PokemonType type2, int maxCp, BaseStats baseStats,
            int stamina, int ivSta, int possibleCp, int candyToEvolve, int ivAtk, int ivDef, float cpm,
            float weight, int maxStamina, PokemonId[] evolutions, bool buddy)
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
            IvSta = ivSta;
            CandyToEvolve = candyToEvolve;
            PossibleCp = possibleCp;
            IvAtk = ivAtk;
            IvDef = ivDef;
            Cpm = cpm;
            Weight = weight;
            MaxStamina = maxStamina;
            Evolutions = evolutions;
            Buddy = buddy;
        }



        public PokemonUiData()
        {

        }
    }
}
