#region using directives

using PoGo.PokeMobBot.Logic.PoGoUtils;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Event
{
    public class PokemonCaptureEvent : IEvent
    {
        public int Attempt;
        public int BallAmount;
        public string CatchType;
        public int Cp;
        public ulong Uid;
        public double Distance;
        public int Exp;
        public int FamilyCandies;
        public PokemonFamilyId Family;
        public PokemonId Id;
        public double Level;
        public int MaxCp;
        public double Perfection;
        public ItemId Pokeball;
        public double Probability;
        public int Stardust;
        public CatchPokemonResponse.Types.CatchStatus Status;
        public double Latitude;
        public double Longitude;
        public PokemonMove Move1;
        public PokemonMove Move2;
        public PokemonType Type1;
        public PokemonType Type2;
        public StatsAttributes Stats;
        public int Stamina;
        public int MaxStamina;
        public int PossibleCp;
        public int CandyToEvolve;
    }
}