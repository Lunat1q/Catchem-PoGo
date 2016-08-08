#region using directives

using System.Collections.Generic;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic
{
    public class Location
    {
        public Location()
        {
        }

        public Location(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class SnipeSettings
    {
        public SnipeSettings()
        {
        }

        public SnipeSettings(List<Location> locations, List<PokemonId> pokemon)
        {
            Locations = locations;
            Pokemon = pokemon;
        }

        public List<Location> Locations { get; set; }
        public List<PokemonId> Pokemon { get; set; }
    }

    public class TransferFilter
    {
        public TransferFilter()
        {
        }

        public TransferFilter(int keepMinCp, float keepMinIvPercentage, int keepMinDuplicatePokemon)
        {
            KeepMinCp = keepMinCp;
            KeepMinIvPercentage = keepMinIvPercentage;
            KeepMinDuplicatePokemon = keepMinDuplicatePokemon;
        }

        public int KeepMinCp { get; set; }
        public float KeepMinIvPercentage { get; set; }
        public int KeepMinDuplicatePokemon { get; set; }
    }

    public interface ILogicSettings
    {
        //bot start
        bool AutoUpdate { get; }
        bool TransferConfigAndAuthOnUpdate { get; }
        bool DumpPokemonStats { get; }
        int AmountOfPokemonToDisplayOnStart { get; }
        bool StartupWelcomeDelay { get; }
        string TranslationLanguageCode { get; }
        
        //coords and movement
        bool Teleport { get; }
        double WalkingSpeedInKilometerPerHour { get; }
        int MaxTravelDistanceInMeters { get; }
        bool UseGpxPathing { get; }
        string GpxFile { get; }
        
        //delays
        int DelayBetweenPlayerActions { get; }
        int DelayBetweenPokemonCatch { get; }
        int DelayCatchIncensePokemon { get; }
        int DelayCatchLurePokemon { get; }
        int DelayCatchNearbyPokemon { get; }
        int DelayCatchPokemon { get; }
        int DelayDisplayPokemon { get; }
        int DelayEvolvePokemon { get; }
        double DelayEvolveVariation { get; }
        int DelayPokestop { get; }
        int DelayPositionCheckState { get; }
        int DelayRecyleItem { get; }
        int DelaySnipePokemon { get; }
        int DelaySoftbanRetry { get; }
        int DelayTransferPokemon { get; }
        int DelayUseLuckyEgg { get; }        
        
        //incubator
        bool UseEggIncubators { get; } 
        bool AlwaysPrefferLongDistanceEgg { get; }
        
        //rename
        bool RenameOnlyAboveIv { get; }
        bool RenamePokemon { get; }        
        string RenameTemplate { get; }	

        //transfer
        bool TransferDuplicatePokemon { get; }
        bool PrioritizeIvOverCp { get; }
        int KeepMinCp { get; }
        float KeepMinIvPercentage { get; }
        int KeepMinDuplicatePokemon { get; }
        bool KeepPokemonsThatCanEvolve { get; }
        
        //evolve
        bool EvolveAllPokemonAboveIv { get; }
        bool EvolveAllPokemonWithEnoughCandy { get; }        
        float EvolveAboveIvValue { get; } 
        bool UseLuckyEggsWhileEvolving { get; }
        int UseLuckyEggsMinPokemonAmount { get; }
        
        //levelup
        bool AutomaticallyLevelUpPokemon { get; }
        string LevelUpByCPorIv { get; }
        float UpgradePokemonCpMinimum { get; }
        float UpgradePokemonIvMinimum { get; }        
        
        //catch
        bool HumanizeThrows { get; }
        double ThrowAccuracyMax { get; }
        double ThrowAccuracyMin { get; }
        double ThrowSpinFrequency { get; }        
        int MaxPokeballsPerPokemon { get; }        
        
        //pokeballs
        int UseGreatBallAboveIv { get; }
        int UseUltraBallAboveIv { get; }
        double UseGreatBallBelowCatchProbability { get; }
        double UseUltraBallBelowCatchProbability { get; }
        double UseMasterBallBelowCatchProbability { get; }
        bool UsePokemonToNotCatchFilter { get; }
        
        //berries
        int UseBerryMinCp { get;  }
        float UseBerryMinIv { get;  }
        double UseBerryBelowCatchProbability { get; }
         
        //favorite 
         
        //recycle
        int TotalAmountOfPokeballsToKeep { get; }
        int TotalAmountOfPotionsToKeep { get; }
        int TotalAmountOfRevivesToKeep { get; }
        int TotalAmountOfBerriesToKeep { get; }
        double RecycleInventoryAtUsagePercentage { get; }
        
        //snipe
        bool SnipeAtPokestops { get; }
        bool SnipeIgnoreUnknownIv { get; }
        bool UseSnipeLocationServer { get; }
        bool UsePokeSnipersLocationServer { get; }
        bool UseTransferIvForSnipe { get; }
        double SnipingScanOffset { get; }
        int MinDelayBetweenSnipes { get; }
        int MinPokeballsToSnipe { get; }
        int MinPokeballsWhileSnipe { get; }
        int SnipeLocationServerPort { get; }
        string SnipeLocationServer { get; }
        bool UseDiscoveryPathing { get; }
        int SnipeRequestTimeoutSeconds { get; }
        
        //paths
        string GeneralConfigPath { get; }
        string ProfileConfigPath { get; }
        string ProfilePath { get; }

        ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter { get; }

        ICollection<PokemonId> PokemonsToEvolve { get; }

        ICollection<PokemonId> PokemonsNotToTransfer { get; }

        ICollection<PokemonId> PokemonsNotToCatch { get; }

        ICollection<PokemonId> PokemonToUseMasterball { get; }

        Dictionary<PokemonId, TransferFilter> PokemonsTransferFilter { get; }
        SnipeSettings PokemonToSnipe { get; }

    }
}