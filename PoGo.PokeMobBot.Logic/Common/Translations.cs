#region using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#endregion

namespace PoGo.PokeMobBot.Logic.Common
{
    using POGOProtos.Enums;

    public interface ITranslation
    {
        string GetTranslation(TranslationString translationString, params object[] data);

        string GetTranslation(TranslationString translationString);

        string GetPokemonName(PokemonId pkmnId);

        string CurrentCode { get; set; }
    }

    public enum TranslationString
    {
        Pokeball,
        GreatPokeball,
        UltraPokeball,
        MasterPokeball,
        LogLevelDebug,
        LogLevelPokestop,
        WrongAuthType,
        FarmPokestopsOutsideRadius,
        FarmPokestopsNoUsableFound,
        EventFortUsed,
        EventFortFailed,
        EventFortTargeted,
        EventProfileLogin,
        EventLevelUpRewards,
        EventUsedLuckyEgg,
        EventUseLuckyEggMinPokemonCheck,
        EventPokemonEvolvedSuccess,
        EventPokemonEvolvedFailed,
        EventPokemonTransferred,
        EventItemRecycled,
        EventPokemonCapture,
        EventNoPokeballs,
        CatchStatusAttempt,
        CatchStatus,
        Candies,
        UnhandledGpxData,
        DisplayHighestsHeader,
        CommonWordPerfect,
        CommonWordName,
        DisplayHighestsCpHeader,
        DisplayHighestsPerfectHeader,
        WelcomeWarning,
        IncubatorPuttingEgg,
        IncubatorStatusUpdate,
        DisplayHighestsLevelHeader,
        LogEntryError,
        LogEntryAttention,
        LogEntryInfo,
        LogEntryPokestop,
        LogEntryFarming,
        LogEntryRecycling,
        LogEntryPkmn,
        LogEntryTransfered,
        LogEntryEvolved,
        LogEntryBerry,
        LogEntryEgg,
        LogEntryDebug,
        LogEntryUpdate,
        LoggingIn,
        PtcOffline,
        AccessTokenExpired,
        TryingAgainIn,
        AccountNotVerified,
        PtcLoginFailed,
        CommonWordUnknown,
        OpeningGoogleDevicePage,
        CouldntCopyToClipboard,
        CouldntCopyToClipboard2,
        RealisticTravelDetected,
        NotRealisticTravel,
        CoordinatesAreInvalid,
        GotUpToDateVersion,
        AutoUpdaterDisabled,
        DownloadingUpdate,
        FinishedDownloadingRelease,
        FinishedUnpackingFiles,
        FinishedTransferringConfig,
        UpdateFinished,
        LookingForIncensePokemon,
        PokemonSkipped,
        ZeroPokeballInv,
        CurrentPokeballInv,
        CurrentPotionInv,
        CurrentBerryInv,
        CurrentReviveInv,
        CurrentIncenseInv,
        CurrentMiscInv,
        CurrentInvUsage,
        CurrentPokemonUsage,
        CheckingForBallsToRecycle,
        CheckingForPotionsToRecycle,
        CheckingForRevivesToRecycle,
        PokeballsToKeepIncorrect,
        InvFullTransferring,
        InvFullTransferManually,
        InvFullPokestopLooting,
        IncubatorEggHatched,
        EncounterProblem,
        EncounterProblemLurePokemon,
        LookingForPokemon,
        LookingForLurePokemon,
        DesiredDestTooFar,
        PokemonRename,
        PokemonIgnoreFilter,
        CatchStatusError,
        CatchStatusEscape,
        CatchStatusFlee,
        CatchStatusMissed,
        CatchStatusSuccess,
        CatchTypeNormal,
        CatchTypeLure,
        CatchTypeIncense,
        WebSocketFailStart,
        StatsTemplateString,
        StatsXpTemplateString,
        RequireInputText,
        GoogleTwoFactorAuth,
        GoogleTwoFactorAuthExplanation,
        GoogleError,
        MissingCredentialsGoogle,
        MissingCredentialsPtc,
        SnipeScan,
        SnipeScanEx,
        NoPokemonToSnipe,
        NotEnoughPokeballsToSnipe,
        DisplayHighestMove1Header,
        DisplayHighestMove2Header,
        UseBerry,
        NianticServerUnstable,
        OperationCanceled,
        PokemonUpgradeSuccess,
        PokemonUpgradeFailed,
        PokemonUpgradeFailedError,
        PokemonUpgradeUnavailable,
        WebErrorNotFound,
        WebErrorGatewayTimeout,
        WebErrorBadGateway,
        SkipLaggedTimeout,
        SkipLaggedMaintenance,
        CheckingForMaximumInventorySize, //added by Lars
        LogEntryFavorite, //added by Lars
        LogEntryUnFavorite, //added by Lars
        PokemonFavorite, //added by Lars
        PokemonUnFavorite, //added by Lars
        WalkingSpeedRandomized, //added by Lars
        StopBotToAvoidBan,
        BotNotStoppedRiskOfBan,
        EncounterProblemPokemonFlee
    }

    public class Translation : ITranslation
    {
        [JsonProperty("TranslationStrings",
            ItemTypeNameHandling = TypeNameHandling.Arrays,
            ItemConverterType = typeof(KeyValuePairConverter),
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            DefaultValueHandling = DefaultValueHandling.Populate)]
        //Default Translations (ENGLISH)        
        private readonly List<KeyValuePair<TranslationString, string>> _translationStrings = new List
            <KeyValuePair<TranslationString, string>>
        {
            new KeyValuePair<TranslationString, string>(TranslationString.Pokeball, "PokeBall"),
            new KeyValuePair<TranslationString, string>(TranslationString.GreatPokeball, "GreatBall"),
            new KeyValuePair<TranslationString, string>(TranslationString.UltraPokeball, "UltraBall"),
            new KeyValuePair<TranslationString, string>(TranslationString.MasterPokeball, "MasterBall"),
            new KeyValuePair<TranslationString, string>(TranslationString.WrongAuthType,
                "Unknown AuthType in config.json"),
            new KeyValuePair<TranslationString, string>(TranslationString.FarmPokestopsOutsideRadius,
                "You're outside of your defined radius! Walking to start ({0}m away) in 5 seconds. Is your Coords.ini file correct?"),
            new KeyValuePair<TranslationString, string>(TranslationString.FarmPokestopsNoUsableFound,
                "No usable PokeStops found in your area. Is your maximum distance too small?"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventFortUsed,
                "Name: {0} XP: {1}, Gems: {2}, Items: {3}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventFortFailed,
                "Name: {0} INFO: Looting failed, possible softban. Unban in: {1}/{2}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventFortTargeted,
                "Travelling to Pokestop: {0} ({1}m Away)"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventProfileLogin, "Playing as {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventLevelUpRewards,
                "Leveled Up: {0} | Items: {1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventUsedLuckyEgg,
                "Used Lucky Egg, remaining: {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventUseLuckyEggMinPokemonCheck,
                "Not enough Pokemon to trigger a lucky egg. Waiting for {0} more. ({1}/{2})"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventPokemonEvolvedSuccess,
                "{0} successfully for {1}xp"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventPokemonEvolvedFailed,
                "Failed {0}. Result was {1}, stopping evolving {2}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventPokemonTransferred,
                "{0,-12} - CP: {1,4}  IV: {2,6}%  [Best CP: {3,4}  IV: {4,6}%] (Candies: {5})"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventItemRecycled, "{0}x {1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventPokemonCapture,
                "({0}) | {2}, Lvl: {3} | CP: ({4}/{5}) | IV: {6}% | Type: {1} | Chance: {7}% | Dist: {8}m | Used: {9} ({10} left) | XP: {11} | {12}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EventNoPokeballs,
                "No Pokeballs - We missed a {0} with CP {1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchStatusAttempt, "{0} Attempt #{1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchStatus, "{0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.Candies, "Candies: {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.UnhandledGpxData,
                "Unhandled data in GPX file, attempting to skip."),
            new KeyValuePair<TranslationString, string>(TranslationString.DisplayHighestsHeader, "Pokemons"),
            new KeyValuePair<TranslationString, string>(TranslationString.CommonWordPerfect, "perfect"),
            new KeyValuePair<TranslationString, string>(TranslationString.CommonWordName, "name"),
            new KeyValuePair<TranslationString, string>(TranslationString.CommonWordUnknown, "Unknown"),
            new KeyValuePair<TranslationString, string>(TranslationString.DisplayHighestsCpHeader, "DisplayHighestsCP"),
            new KeyValuePair<TranslationString, string>(TranslationString.DisplayHighestsPerfectHeader,
                "DisplayHighestsPerfect"),
            new KeyValuePair<TranslationString, string>(TranslationString.DisplayHighestsLevelHeader,
                "DisplayHighestsLevel"),
            new KeyValuePair<TranslationString, string>(TranslationString.WelcomeWarning,
                "Make sure Lat & Lng are right. Exit Program if not! Lat: {0} Lng: {1} Alt: {2}"),
            new KeyValuePair<TranslationString, string>(TranslationString.IncubatorPuttingEgg,
                "Putting egg in incubator: {0:0.00}km left"),
            new KeyValuePair<TranslationString, string>(TranslationString.IncubatorStatusUpdate,
                "Incubator status update: {0:0.00}km left"),
            new KeyValuePair<TranslationString, string>(TranslationString.IncubatorEggHatched,
                "Incubated egg has hatched: {0} | Lvl: {1} CP: ({2}/{3}) IV: {4}%"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryError, "ERROR"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryAttention, "ATTENTION"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryInfo, "INFO"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryPokestop, "POKESTOP"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryFarming, "FARMING"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryRecycling, "RECYCLING"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryPkmn, "CATCH"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryTransfered, "TRANSFER"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryEvolved, "EVOLVED"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryBerry, "BERRY"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryEgg, "EGG"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryDebug, "DEBUG"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryUpdate, "UPDATE"),
            new KeyValuePair<TranslationString, string>(TranslationString.LoggingIn, "Logging in using account {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.PtcOffline,
                "PTC Servers are probably down OR your credentials are wrong. Try google"),
            new KeyValuePair<TranslationString, string>(TranslationString.AccessTokenExpired,
                "Access token expired. Relogin to get a new token."),
            new KeyValuePair<TranslationString, string>(TranslationString.TryingAgainIn,
                "Trying again in {0} seconds..."),
            new KeyValuePair<TranslationString, string>(TranslationString.AccountNotVerified,
                "Account not verified! Exiting..."),
            new KeyValuePair<TranslationString, string>(TranslationString.PtcLoginFailed,
                "PTC login failed. Make sure you have entered the right Email & Password. If you continue to get this, check your phone or Nox for a ban. Exiting..."),
            new KeyValuePair<TranslationString, string>(TranslationString.OpeningGoogleDevicePage,
                "Opening Google Device page. Please paste the code using CTRL+V"),
            new KeyValuePair<TranslationString, string>(TranslationString.CouldntCopyToClipboard,
                "Couldnt copy to clipboard, do it manually"),
            new KeyValuePair<TranslationString, string>(TranslationString.CouldntCopyToClipboard2,
                "Goto: {0} & enter {1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.RealisticTravelDetected,
                "Detected realistic Traveling , using UserSettings.settings"),
            new KeyValuePair<TranslationString, string>(TranslationString.NotRealisticTravel,
                "Not realistic Traveling at {0}, using last saved Coords.ini"),
            new KeyValuePair<TranslationString, string>(TranslationString.CoordinatesAreInvalid,
                "Coordinates in \"Coords.ini\" file are invalid, using the default coordinates"),
            new KeyValuePair<TranslationString, string>(TranslationString.GotUpToDateVersion,
                "Perfect! You already have the newest Version {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.AutoUpdaterDisabled,
                "AutoUpdater is disabled. Get the latest release from: {0}\n "),
            new KeyValuePair<TranslationString, string>(TranslationString.DownloadingUpdate,
                "Downloading and apply Update..."),
            new KeyValuePair<TranslationString, string>(TranslationString.FinishedDownloadingRelease,
                "Finished downloading newest Release..."),
            new KeyValuePair<TranslationString, string>(TranslationString.FinishedUnpackingFiles,
                "Finished unpacking files..."),
            new KeyValuePair<TranslationString, string>(TranslationString.FinishedTransferringConfig,
                "Finished transferring your config to the new version..."),
            new KeyValuePair<TranslationString, string>(TranslationString.UpdateFinished,
                "Update finished, you can close this window now."),
            new KeyValuePair<TranslationString, string>(TranslationString.LookingForIncensePokemon,
                "Looking for incense Pokemon..."),
            new KeyValuePair<TranslationString, string>(TranslationString.LookingForPokemon, "Looking for Pokemon..."),
            new KeyValuePair<TranslationString, string>(TranslationString.LookingForLurePokemon,
                "Looking for lure Pokemon..."),
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonSkipped, "Skipped {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.ZeroPokeballInv,
                "You have no pokeballs in your inventory, no more Pokemon can be caught!"),
            new KeyValuePair<TranslationString, string>(TranslationString.CurrentPokeballInv,
                "[Inventory] Pokeballs: {0} | Greatballs: {1} | Ultraballs: {2} | Masterballs: {3} | Total: {4}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CurrentPotionInv,
                "[Inventory] Potions: {0} | Super Potions: {1} | Hyper Potions: {2} | Max Potions: {3} | Total: {4}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CurrentBerryInv,
                "[Inventory] Razz Berries: {0} | Bluk Berries: {1} | Nanab Berries: {2} | Pinap Berries: {3} | Wepar Berries: {4} | Total: {5}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CurrentReviveInv,
                "[Inventory] Revives: {0} | Max Revives: {1} | Total: {2}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CurrentIncenseInv,
                "[Inventory] Incense: {0} | Cool Incense: {1} | Floral Incense: {2} | Spicy Incense: {3} | Total {4}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CurrentMiscInv,
                "[Inventory] Lure Modules: {0} | Lucky Eggs: {1} | Incubators: {2} | Total: {3}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CurrentInvUsage,
                "[Inventory] Inventory Usage: {0}/{1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CurrentPokemonUsage,
                "[Inventory] Pokemon Stored: {0}/{1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CheckingForBallsToRecycle,
                "Checking for balls to recycle, keeping {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CheckingForPotionsToRecycle,
                "Checking for potions to recycle, keeping {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.CheckingForRevivesToRecycle,
                "Checking for revives to recycle, keeping {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.PokeballsToKeepIncorrect,
                "TotalAmountOfPokeballsToKeep is configured incorrectly. The number is smaller than 1."),
            new KeyValuePair<TranslationString, string>(TranslationString.InvFullTransferring,
                "Pokemon Inventory is full, transferring Pokemon..."),
            new KeyValuePair<TranslationString, string>(TranslationString.InvFullTransferManually,
                "Pokemon Inventory is full! Please transfer Pokemon manually or set TransferDuplicatePokemon to true in settings..."),
            new KeyValuePair<TranslationString, string>(TranslationString.InvFullPokestopLooting,
                "Inventory is full, no items looted!"),
            new KeyValuePair<TranslationString, string>(TranslationString.EncounterProblem, "Encounter problem: {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EncounterProblemLurePokemon,
                "Encounter problem: Lure Pokemon {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.EncounterProblemPokemonFlee, "Encounter Pokemon Fled {0}!"),
            new KeyValuePair<TranslationString, string>(TranslationString.DesiredDestTooFar,
                "Your desired destination of {0}, {1} is too far from your current position of {2}, {3}"),
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonRename,
                "Pokemon {0} ({1}) renamed from {2} to {3}."),
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonIgnoreFilter,
                "[Pokemon ignore filter] - Ignoring {0} as defined in settings"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchStatusAttempt, "Attempt"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchStatusError, "Error"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchStatusEscape, "Escape"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchStatusFlee, "Flee"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchStatusMissed, "Missed"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchStatusSuccess, "Success"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchTypeNormal, "Normal"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchTypeLure, "Lure"),
            new KeyValuePair<TranslationString, string>(TranslationString.CatchTypeIncense, "Incense"),
            new KeyValuePair<TranslationString, string>(TranslationString.WebSocketFailStart,
                "Failed to start WebSocketServer on port : {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.StatsTemplateString,
                "{0} - Runtime {1} - Lvl: {2} | EXP/H: {3:n0} | P/H: {4:n0} | Stardust: {5:n0} | Transfered: {6:n0} | Recycled: {7:n0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.StatsXpTemplateString,
                "{0} (Advance in {1}h {2}m | {3:n0}/{4:n0} XP)"),
            new KeyValuePair<TranslationString, string>(TranslationString.RequireInputText,
                "Program will continue after the key press..."),
            new KeyValuePair<TranslationString, string>(TranslationString.GoogleTwoFactorAuth,
                "As you have Google Two Factor Auth enabled, you will need to insert an App Specific Password into the auth.json"),
            new KeyValuePair<TranslationString, string>(TranslationString.GoogleTwoFactorAuthExplanation,
                "Opening Google App-Passwords. Please make a new App Password (use Other as Device)"),
            new KeyValuePair<TranslationString, string>(TranslationString.GoogleError,
                "Make sure you have entered the right Email & Password."),
            new KeyValuePair<TranslationString, string>(TranslationString.MissingCredentialsGoogle,
                "You need to fill out GoogleUsername and GooglePassword in auth.json!"),
            new KeyValuePair<TranslationString, string>(TranslationString.MissingCredentialsPtc,
                "You need to fill out PtcUsername and PtcPassword in auth.json!"),
            new KeyValuePair<TranslationString, string>(TranslationString.SnipeScan,
                "[Sniper] Scanning for Snipeable Pokemon at {0}..."),
            new KeyValuePair<TranslationString, string>(TranslationString.SnipeScanEx,
                "[Sniper] Sniping a {0} with {1} IV at {2}..."),
            new KeyValuePair<TranslationString, string>(TranslationString.NoPokemonToSnipe,
                "[Sniper] No Pokemon found to snipe!"),
            new KeyValuePair<TranslationString, string>(TranslationString.NotEnoughPokeballsToSnipe,
                "Not enough Pokeballs to start sniping! ({0}/{1})"),
            new KeyValuePair<TranslationString, string>(TranslationString.DisplayHighestMove1Header, "MOVE1"),
            new KeyValuePair<TranslationString, string>(TranslationString.DisplayHighestMove2Header, "MOVE2"),
            new KeyValuePair<TranslationString, string>(TranslationString.UseBerry,
                "Using Razzberry. Berries left: {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.NianticServerUnstable, 
                "Niantic Servers unstable, throttling API Calls."),
            new KeyValuePair<TranslationString, string>(TranslationString.OperationCanceled, 
                "Current Operation was canceled. Bot stopped/paused."),
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonUpgradeSuccess,
                "Pokemon upgraded: {0}:{1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonUpgradeFailed,
                "Pokemon upgrade failed, not enough resources, probably not enough stardust"),
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonUpgradeUnavailable,
                "Pokemon upgrade unavailable for: {0}:{1}/{2}"),
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonUpgradeFailedError,
                "Pokemon upgrade failed duo to an unknown error, pokemon could be max level for your level. The Pokemon that caused issue was: {0}"),
            new KeyValuePair<TranslationString, string>(TranslationString.WebErrorBadGateway,
                "502 Bad Gateway: Server is under heavy load!"),
            new KeyValuePair<TranslationString, string>(TranslationString.WebErrorGatewayTimeout,
                "504 Gateway Time-out: The server didn't respond in time."),
            new KeyValuePair<TranslationString, string>(TranslationString.WebErrorNotFound,
                "404 Not Found: Not able to retrieve file from server!"),
            new KeyValuePair<TranslationString, string>(TranslationString.SkipLaggedTimeout,
                "SkipLagged is down or SnipeRequestTimeoutSeconds is too small!"),
            new KeyValuePair<TranslationString, string>(TranslationString.SkipLaggedMaintenance,
                "SkipLagged servers are down for maintenance."),
            new KeyValuePair<TranslationString, string>(TranslationString.CheckingForMaximumInventorySize, // added by Lars
                "[Inventory] Items to Keep exceeds maximum inventory size! {0}/{1}"),
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryFavorite, "FAVORITE"), // added by Lars
            new KeyValuePair<TranslationString, string>(TranslationString.LogEntryUnFavorite, "UNFAVORITE"), // added by Lars
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonFavorite, "{0}"), //pre-formatted - added by Lars
            new KeyValuePair<TranslationString, string>(TranslationString.PokemonUnFavorite, "{0}"), //pre-formatted - added by Lars
            new KeyValuePair<TranslationString, string>(TranslationString.WalkingSpeedRandomized, "{0}"), //pre-formatted - added by Lars
            new KeyValuePair<TranslationString, string>(TranslationString.StopBotToAvoidBan, "The bot was stopped to avoid ban!"), // added by Lars
            new KeyValuePair<TranslationString, string>(TranslationString.BotNotStoppedRiskOfBan, "Somethng happened that shouldn't have and bot hasn't stopped. Higher possibility of ban. (suggested action: stop the bot \"Control+C\").") // added by Lars
        };

        [JsonIgnore]
        public string CurrentCode { get; set; }

        [JsonProperty("Pokemon",
            ItemTypeNameHandling = TypeNameHandling.Arrays,
            ItemConverterType = typeof(KeyValuePairConverter),
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            DefaultValueHandling = DefaultValueHandling.Populate)]
        //Default Translations (ENGLISH)        
        private readonly List<KeyValuePair<PokemonId, string>> _pokemons = new List<KeyValuePair<PokemonId, string>>
        {
            new KeyValuePair<PokemonId, string>(PokemonId.Missingno, "Missingno"),
            new KeyValuePair<PokemonId, string>(PokemonId.Bulbasaur, "Bulbasaur"),
            new KeyValuePair<PokemonId, string>(PokemonId.Ivysaur, "Ivysaur"),
            new KeyValuePair<PokemonId, string>(PokemonId.Venusaur, "Venusaur"),
            new KeyValuePair<PokemonId, string>(PokemonId.Charmander, "Charmander"),
            new KeyValuePair<PokemonId, string>(PokemonId.Charmeleon, "Charmeleon"),
            new KeyValuePair<PokemonId, string>(PokemonId.Charizard, "Charizard"),
            new KeyValuePair<PokemonId, string>(PokemonId.Squirtle, "Squirtle"),
            new KeyValuePair<PokemonId, string>(PokemonId.Wartortle, "Wartortle"),
            new KeyValuePair<PokemonId, string>(PokemonId.Blastoise, "Blastoise"),
            new KeyValuePair<PokemonId, string>(PokemonId.Caterpie, "Caterpie"),
            new KeyValuePair<PokemonId, string>(PokemonId.Metapod, "Metapod"),
            new KeyValuePair<PokemonId, string>(PokemonId.Butterfree, "Butterfree"),
            new KeyValuePair<PokemonId, string>(PokemonId.Weedle, "Weedle"),
            new KeyValuePair<PokemonId, string>(PokemonId.Kakuna, "Kakuna"),
            new KeyValuePair<PokemonId, string>(PokemonId.Beedrill, "Beedrill"),
            new KeyValuePair<PokemonId, string>(PokemonId.Pidgey, "Pidgey"),
            new KeyValuePair<PokemonId, string>(PokemonId.Pidgeotto, "Pidgeotto"),
            new KeyValuePair<PokemonId, string>(PokemonId.Pidgeot, "Pidgeot"),
            new KeyValuePair<PokemonId, string>(PokemonId.Rattata, "Rattata"),
            new KeyValuePair<PokemonId, string>(PokemonId.Raticate, "Raticate"),
            new KeyValuePair<PokemonId, string>(PokemonId.Spearow, "Spearow"),
            new KeyValuePair<PokemonId, string>(PokemonId.Fearow, "Fearow"),
            new KeyValuePair<PokemonId, string>(PokemonId.Ekans, "Ekans"),
            new KeyValuePair<PokemonId, string>(PokemonId.Arbok, "Arbok"),
            new KeyValuePair<PokemonId, string>(PokemonId.Pikachu, "Pikachu"),
            new KeyValuePair<PokemonId, string>(PokemonId.Raichu, "Raichu"),
            new KeyValuePair<PokemonId, string>(PokemonId.Sandshrew, "Sandshrew"),
            new KeyValuePair<PokemonId, string>(PokemonId.Sandslash, "Sandslash"),
            new KeyValuePair<PokemonId, string>(PokemonId.NidoranFemale, "NidoranF"),
            new KeyValuePair<PokemonId, string>(PokemonId.Nidorina, "Nidorina"),
            new KeyValuePair<PokemonId, string>(PokemonId.Nidoqueen, "Nidoqueen"),
            new KeyValuePair<PokemonId, string>(PokemonId.NidoranMale, "NidoranM"),
            new KeyValuePair<PokemonId, string>(PokemonId.Nidorino, "Nidorino"),
            new KeyValuePair<PokemonId, string>(PokemonId.Nidoking, "Nidoking"),
            new KeyValuePair<PokemonId, string>(PokemonId.Clefairy, "Clefairy"),
            new KeyValuePair<PokemonId, string>(PokemonId.Clefable, "Clefable"),
            new KeyValuePair<PokemonId, string>(PokemonId.Vulpix, "Vulpix"),
            new KeyValuePair<PokemonId, string>(PokemonId.Ninetales, "Ninetales"),
            new KeyValuePair<PokemonId, string>(PokemonId.Jigglypuff, "Jigglypuff"),
            new KeyValuePair<PokemonId, string>(PokemonId.Wigglytuff, "Wigglytuff"),
            new KeyValuePair<PokemonId, string>(PokemonId.Zubat, "Zubat"),
            new KeyValuePair<PokemonId, string>(PokemonId.Golbat, "Golbat"),
            new KeyValuePair<PokemonId, string>(PokemonId.Oddish, "Oddish"),
            new KeyValuePair<PokemonId, string>(PokemonId.Gloom, "Gloom"),
            new KeyValuePair<PokemonId, string>(PokemonId.Vileplume, "Vileplume"),
            new KeyValuePair<PokemonId, string>(PokemonId.Paras, "Paras"),
            new KeyValuePair<PokemonId, string>(PokemonId.Parasect, "Parasect"),
            new KeyValuePair<PokemonId, string>(PokemonId.Venonat, "Venonat"),
            new KeyValuePair<PokemonId, string>(PokemonId.Venomoth, "Venomoth"),
            new KeyValuePair<PokemonId, string>(PokemonId.Diglett, "Diglett"),
            new KeyValuePair<PokemonId, string>(PokemonId.Dugtrio, "Dugtrio"),
            new KeyValuePair<PokemonId, string>(PokemonId.Meowth, "Meowth"),
            new KeyValuePair<PokemonId, string>(PokemonId.Persian, "Persian"),
            new KeyValuePair<PokemonId, string>(PokemonId.Psyduck, "Psyduck"),
            new KeyValuePair<PokemonId, string>(PokemonId.Golduck, "Golduck"),
            new KeyValuePair<PokemonId, string>(PokemonId.Mankey, "Mankey"),
            new KeyValuePair<PokemonId, string>(PokemonId.Primeape, "Primeape"),
            new KeyValuePair<PokemonId, string>(PokemonId.Growlithe, "Growlithe"),
            new KeyValuePair<PokemonId, string>(PokemonId.Arcanine, "Arcanine"),
            new KeyValuePair<PokemonId, string>(PokemonId.Poliwag, "Poliwag"),
            new KeyValuePair<PokemonId, string>(PokemonId.Poliwhirl, "Poliwhirl"),
            new KeyValuePair<PokemonId, string>(PokemonId.Poliwrath, "Poliwrath"),
            new KeyValuePair<PokemonId, string>(PokemonId.Abra, "Abra"),
            new KeyValuePair<PokemonId, string>(PokemonId.Kadabra, "Kadabra"),
            new KeyValuePair<PokemonId, string>(PokemonId.Alakazam, "Alakazam"),
            new KeyValuePair<PokemonId, string>(PokemonId.Machop, "Machop"),
            new KeyValuePair<PokemonId, string>(PokemonId.Machoke, "Machoke"),
            new KeyValuePair<PokemonId, string>(PokemonId.Machamp, "Machamp"),
            new KeyValuePair<PokemonId, string>(PokemonId.Bellsprout, "Bellsprout"),
            new KeyValuePair<PokemonId, string>(PokemonId.Weepinbell, "Weepinbell"),
            new KeyValuePair<PokemonId, string>(PokemonId.Victreebel, "Victreebel"),
            new KeyValuePair<PokemonId, string>(PokemonId.Tentacool, "Tentacool"),
            new KeyValuePair<PokemonId, string>(PokemonId.Tentacruel, "Tentacruel"),
            new KeyValuePair<PokemonId, string>(PokemonId.Geodude, "Geodude"),
            new KeyValuePair<PokemonId, string>(PokemonId.Graveler, "Graveler"),
            new KeyValuePair<PokemonId, string>(PokemonId.Golem, "Golem"),
            new KeyValuePair<PokemonId, string>(PokemonId.Ponyta, "Ponyta"),
            new KeyValuePair<PokemonId, string>(PokemonId.Rapidash, "Rapidash"),
            new KeyValuePair<PokemonId, string>(PokemonId.Slowpoke, "Slowpoke"),
            new KeyValuePair<PokemonId, string>(PokemonId.Slowbro, "Slowbro"),
            new KeyValuePair<PokemonId, string>(PokemonId.Magnemite, "Magnemite"),
            new KeyValuePair<PokemonId, string>(PokemonId.Magneton, "Magneton"),
            new KeyValuePair<PokemonId, string>(PokemonId.Farfetchd, "Farfetch'd"),
            new KeyValuePair<PokemonId, string>(PokemonId.Doduo, "Doduo"),
            new KeyValuePair<PokemonId, string>(PokemonId.Dodrio, "Dodrio"),
            new KeyValuePair<PokemonId, string>(PokemonId.Seel, "Seel"),
            new KeyValuePair<PokemonId, string>(PokemonId.Dewgong, "Dewgong"),
            new KeyValuePair<PokemonId, string>(PokemonId.Grimer, "Grimer"),
            new KeyValuePair<PokemonId, string>(PokemonId.Muk, "Muk"),
            new KeyValuePair<PokemonId, string>(PokemonId.Shellder, "Shellder"),
            new KeyValuePair<PokemonId, string>(PokemonId.Cloyster, "Cloyster"),
            new KeyValuePair<PokemonId, string>(PokemonId.Gastly, "Gastly"),
            new KeyValuePair<PokemonId, string>(PokemonId.Haunter, "Haunter"),
            new KeyValuePair<PokemonId, string>(PokemonId.Gengar, "Gengar"),
            new KeyValuePair<PokemonId, string>(PokemonId.Onix, "Onix"),
            new KeyValuePair<PokemonId, string>(PokemonId.Drowzee, "Drowzee"),
            new KeyValuePair<PokemonId, string>(PokemonId.Hypno, "Hypno"),
            new KeyValuePair<PokemonId, string>(PokemonId.Krabby, "Krabby"),
            new KeyValuePair<PokemonId, string>(PokemonId.Kingler, "Kingler"),
            new KeyValuePair<PokemonId, string>(PokemonId.Voltorb, "Voltorb"),
            new KeyValuePair<PokemonId, string>(PokemonId.Electrode, "Electrode"),
            new KeyValuePair<PokemonId, string>(PokemonId.Exeggcute, "Exeggcute"),
            new KeyValuePair<PokemonId, string>(PokemonId.Exeggutor, "Exeggutor"),
            new KeyValuePair<PokemonId, string>(PokemonId.Cubone, "Cubone"),
            new KeyValuePair<PokemonId, string>(PokemonId.Marowak, "Marowak"),
            new KeyValuePair<PokemonId, string>(PokemonId.Hitmonlee, "Hitmonlee"),
            new KeyValuePair<PokemonId, string>(PokemonId.Hitmonchan, "Hitmonchan"),
            new KeyValuePair<PokemonId, string>(PokemonId.Lickitung, "Lickitung"),
            new KeyValuePair<PokemonId, string>(PokemonId.Koffing, "Koffing"),
            new KeyValuePair<PokemonId, string>(PokemonId.Weezing, "Weezing"),
            new KeyValuePair<PokemonId, string>(PokemonId.Rhyhorn, "Rhyhorn"),
            new KeyValuePair<PokemonId, string>(PokemonId.Rhydon, "Rhydon"),
            new KeyValuePair<PokemonId, string>(PokemonId.Chansey, "Chansey"),
            new KeyValuePair<PokemonId, string>(PokemonId.Tangela, "Tangela"),
            new KeyValuePair<PokemonId, string>(PokemonId.Kangaskhan, "Kangaskhan"),
            new KeyValuePair<PokemonId, string>(PokemonId.Horsea, "Horsea"),
            new KeyValuePair<PokemonId, string>(PokemonId.Seadra, "Seadra"),
            new KeyValuePair<PokemonId, string>(PokemonId.Goldeen, "Goldeen"),
            new KeyValuePair<PokemonId, string>(PokemonId.Seaking, "Seaking"),
            new KeyValuePair<PokemonId, string>(PokemonId.Staryu, "Staryu"),
            new KeyValuePair<PokemonId, string>(PokemonId.Starmie, "Starmie"),
            new KeyValuePair<PokemonId, string>(PokemonId.MrMime, "Mr. Mime"),
            new KeyValuePair<PokemonId, string>(PokemonId.Scyther, "Scyther"),
            new KeyValuePair<PokemonId, string>(PokemonId.Jynx, "Jynx"),
            new KeyValuePair<PokemonId, string>(PokemonId.Electabuzz, "Electabuzz"),
            new KeyValuePair<PokemonId, string>(PokemonId.Magmar, "Magmar"),
            new KeyValuePair<PokemonId, string>(PokemonId.Pinsir, "Pinsir"),
            new KeyValuePair<PokemonId, string>(PokemonId.Tauros, "Tauros"),
            new KeyValuePair<PokemonId, string>(PokemonId.Magikarp, "Magikarp"),
            new KeyValuePair<PokemonId, string>(PokemonId.Gyarados, "Gyarados"),
            new KeyValuePair<PokemonId, string>(PokemonId.Lapras, "Lapras"),
            new KeyValuePair<PokemonId, string>(PokemonId.Ditto, "Ditto"),
            new KeyValuePair<PokemonId, string>(PokemonId.Eevee, "Eevee"),
            new KeyValuePair<PokemonId, string>(PokemonId.Vaporeon, "Vaporeon"),
            new KeyValuePair<PokemonId, string>(PokemonId.Jolteon, "Jolteon"),
            new KeyValuePair<PokemonId, string>(PokemonId.Flareon, "Flareon"),
            new KeyValuePair<PokemonId, string>(PokemonId.Porygon, "Porygon"),
            new KeyValuePair<PokemonId, string>(PokemonId.Omanyte, "Omanyte"),
            new KeyValuePair<PokemonId, string>(PokemonId.Omastar, "Omastar"),
            new KeyValuePair<PokemonId, string>(PokemonId.Kabuto, "Kabuto"),
            new KeyValuePair<PokemonId, string>(PokemonId.Kabutops, "Kabutops"),
            new KeyValuePair<PokemonId, string>(PokemonId.Aerodactyl, "Aerodactyl"),
            new KeyValuePair<PokemonId, string>(PokemonId.Snorlax, "Snorlax"),
            new KeyValuePair<PokemonId, string>(PokemonId.Articuno, "Articuno"),
            new KeyValuePair<PokemonId, string>(PokemonId.Zapdos, "Zapdos"),
            new KeyValuePair<PokemonId, string>(PokemonId.Moltres, "Moltres"),
            new KeyValuePair<PokemonId, string>(PokemonId.Dratini, "Dratini"),
            new KeyValuePair<PokemonId, string>(PokemonId.Dragonair, "Dragonair"),
            new KeyValuePair<PokemonId, string>(PokemonId.Dragonite, "Dragonite"),
            new KeyValuePair<PokemonId, string>(PokemonId.Mewtwo, "Mewtwo"),
            new KeyValuePair<PokemonId, string>(PokemonId.Mew, "Mew")
        };

        public string GetTranslation(TranslationString translationString, params object[] data)
        {
            try
            {
                var translation = _translationStrings.FirstOrDefault(t => t.Key.Equals(translationString)).Value;
                return translation != default(string)
                    ? string.Format(translation, data)
                    : $"Translation for {translationString} is missing";
            }
            catch (Exception)
            {
                return $"Translation for {translationString} failed";
            }
        }

        public string GetTranslation(TranslationString translationString)
        {
            var translation = _translationStrings.FirstOrDefault(t => t.Key.Equals(translationString)).Value;
            return translation != default(string) ? translation : $"Translation for {translationString} is missing";
        }

        public string GetPokemonName(PokemonId pkmnId)
        {
            var name = _pokemons.FirstOrDefault(p => p.Key == pkmnId).Value;

            return name != default(string)
                ? name
                : $"Translation for pokemon name {pkmnId} is missing";
        }

        public static Translation Load(ILogicSettings logicSettings)
        {
            var translationsLanguageCode = logicSettings.TranslationLanguageCode;
            var translationPath = Path.Combine(logicSettings.GeneralConfigPath, "translations");
            var fullPath = Path.Combine(translationPath, "translation." + translationsLanguageCode + ".json");

            Translation translations;
            if (File.Exists(fullPath))
            {
                var input = File.ReadAllText(fullPath);

                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                try
                {
                    translations = JsonConvert.DeserializeObject<Translation>(input, jsonSettings);
                    //TODO make json to fill default values as it won't do it now

                    var defaultTranslation = new Translation();

                    defaultTranslation._translationStrings.Where(
                        item => translations._translationStrings.All(a => a.Key != item.Key))
                        .ToList()
                        .ForEach(translations._translationStrings.Add);

                    defaultTranslation._pokemons.Where(
                        item => translations._pokemons.All(a => a.Key != item.Key))
                        .ToList()
                        .ForEach(translations._pokemons.Add);
                    translations.CurrentCode = translationsLanguageCode;
                }
                catch (Exception)
                {
                    translations = new Translation {CurrentCode = "en"};
                    translations.Save(Path.Combine(translationPath, "translation.en.json"));
                }
            }
            else
            {
                translations = new Translation {CurrentCode = "en"};
                translations.Save(Path.Combine(translationPath, "translation.en.json"));
            }
            return translations;
        }

        public void Save(string fullPath)
        {
            var output = JsonConvert.SerializeObject(this, Formatting.Indented,
                new StringEnumConverter { CamelCaseText = true });

            var folder = Path.GetDirectoryName(fullPath);
            if (folder != null && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(fullPath, output);
        }
    }
}
