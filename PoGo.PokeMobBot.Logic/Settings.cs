#region using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic
{
    public class AuthSettings
    {
        [JsonIgnore]
        private string _filePath;
        public AuthType AuthType;
        public string GoogleRefreshToken = "";
        public string GoogleUsername;
        public string GooglePassword;
        public string PtcUsername;
        public string PtcPassword;
        public bool UseProxy;
        public string ProxyLogin;
        public string ProxyPass;
        public string ProxyUri;

        public void Load(string path)
        {
            try
            {
                _filePath = path;

                if (File.Exists(_filePath))
                {
                    //if the file exists, load the settings
                    var input = File.ReadAllText(_filePath);

                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new StringEnumConverter { CamelCaseText = true });

                    JsonConvert.PopulateObject(input, this, settings);
                }
                else
                {
                    Save(_filePath);
                }
			}
            catch (JsonReaderException exception)
            {
                if (exception.Message.Contains("Unexpected character") && exception.Message.Contains("PtcUsername"))
                    Logger.Write("JSON Exception: You need to properly configure your PtcUsername using quotations.",
                        LogLevel.Error);
                else if (exception.Message.Contains("Unexpected character") && exception.Message.Contains("PtcPassword"))
                    Logger.Write(
                        "JSON Exception: You need to properly configure your PtcPassword using quotations.",
                        LogLevel.Error);
                else if (exception.Message.Contains("Unexpected character") &&
                         exception.Message.Contains("GoogleUsername"))
                    Logger.Write(
                        "JSON Exception: You need to properly configure your GoogleUsername using quotations.",
                        LogLevel.Error);
                else if (exception.Message.Contains("Unexpected character") &&
                         exception.Message.Contains("GooglePassword"))
                    Logger.Write(
                        "JSON Exception: You need to properly configure your GooglePassword using quotations.",
                        LogLevel.Error);
                else
                    Logger.Write("JSON Exception: " + exception.Message, LogLevel.Error);
            }
        }

        public void Save(string path)
        {
            var output = JsonConvert.SerializeObject(this, Formatting.Indented,
                new StringEnumConverter { CamelCaseText = true });

            var folder = Path.GetDirectoryName(path);
            if (folder != null && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(path, output);
        }

        public void Save()
        {
            if (!string.IsNullOrEmpty(_filePath))
            {
                Save(_filePath);
            }
        }
    }

    public static class RuntimeSettings
    {

        public static DateTime StartTime = DateTime.Now;
        public static bool DelayingScan;
        public static int PokemonScanDelay = 5000;// in ms

        public static void CheckScan()
        {
            if (DelayingScan)
            {
                if((DateTime.Now.Subtract(StartTime).TotalMilliseconds > PokemonScanDelay) && DelayingScan)
                {
                    DelayingScan = false;
                }
            }
        }
    }

    public class DeviceSettings
    {
		public string DevicePackageName = "random";
        public string DeviceId = "GWK74"; // "ro.build.id";
        public string AndroidBoardName = "thunderc"; // "ro.product.board";
        public string AndroidBootLoader = "unknown"; //"ro.product.bootloader; //I think
        public string DeviceBrand = "LGE";// "product.brand";
        public string DeviceModel = "thunderc"; //"product.device";
        public string DeviceModelIdentifier = "GWK74 10282011";// "build.display.id";
        public string DeviceModelBoot = "thunderc"; //"boot.hardware";
        public string HardwareManufacturer = "LGE"; //"product.manufacturer";
        public string HardWareModel = "LG-VS660"; //"product.model";
        public string FirmwareBrand = "thunderc"; //"product.name"; //iOS is "iPhone OS"
        public string FirmwareTags = "test-keys"; //"build.tags";
        public string FirmwareType = "eng"; //"build.type"; //iOS is "iOS version"
        public string FirmwareFingerprint = "lge/lge_gelato/VM701:2.3.4/GRJ22/ZV4.19cd75186d:user/release-keys"; //"build.fingerprint";
		
		public static string RandomString(int length, string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789")
        {
            var outOfRange = Byte.MaxValue + 1 - (Byte.MaxValue + 1) % alphabet.Length;

            return string.Concat(
                Enumerable
                    .Repeat(0, Int32.MaxValue)
                    .Select(e => RandomByte())
                    .Where(randomByte => randomByte < outOfRange)
                    .Take(length)
                    .Select(randomByte => alphabet[randomByte % alphabet.Length])
            );
        }

        private static byte RandomByte()
        {
            using (var randomizationProvider = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[1];
                randomizationProvider.GetBytes(randomBytes);
                return randomBytes.Single();
            }
        }

        public void SetDevInfoByKey(string devKey)
        {
            DevicePackageName = devKey;
            if (DeviceInfoHelper.DeviceInfoSets.ContainsKey(DevicePackageName))
            {
                AndroidBoardName = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["AndroidBoardName"];
                AndroidBootLoader = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["AndroidBootloader"];
                DeviceBrand = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["DeviceBrand"];
                DeviceId = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["DeviceId"];
                DeviceModel = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["DeviceModel"];
                DeviceModelBoot = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["DeviceModelBoot"];
                DeviceModelIdentifier = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["DeviceModelIdentifier"];
                FirmwareBrand = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["FirmwareBrand"];
                FirmwareFingerprint = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["FirmwareFingerprint"];
                FirmwareTags = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["FirmwareTags"];
                FirmwareType = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["FirmwareType"];
                HardwareManufacturer = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["HardwareManufacturer"];
                HardWareModel = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["HardwareModel"];
            }
            else
            {
                throw new ArgumentException("Invalid device info package! Check your auth.config file and make sure a valid DevicePackageName is set or simply set it to 'random'...");
            }
        }    
	} 
	
	public class DelaySettings
    {//delays
        public int DelayBetweenPlayerActions = 1000;
        public int DelayPositionCheckState = 200;
        public int DelayPokestop = 2000;
        public int DelayCatchPokemon = 2000;
        public int DelayBetweenPokemonCatch = 2000;
        public int DelayCatchNearbyPokemon = 2000;
        public int DelayCatchLurePokemon = 3000;
        public int DelayCatchIncensePokemon = 3000;
        public int DelayEvolvePokemon = 5000;
        public double DelayEvolveVariation = 0.3;
        public int DelayTransferPokemon = 1000;
        public int DelayDisplayPokemon = 5;
        public int DelayUseLuckyEgg = 1500;
        public int DelaySoftbanRetry = 250;
        public int DelayRecyleItem = 2500;
        public int DelaySnipePokemon = 250;
        public int MinDelayBetweenSnipes = 10000;
        public double SnipingScanOffset = 0.003;
    }

    public class StartUpSettings
    {
        //bot start
        public bool AutoUpdate = false;
        public bool TransferConfigAndAuthOnUpdate = true;
        public bool DumpPokemonStats = false;
        public int AmountOfPokemonToDisplayOnStart = 10;
        public bool StartupWelcomeDelay = false;
        public string TranslationLanguageCode = "en";
        public int WebSocketPort = 14251;
        //display
        public bool DisplayPokemonMaxPoweredCp = true;
        public bool DisplayPokemonMovesetRank = true;
    }

    public class PokemonConfig
    {
        //incubator
        public bool UseEggIncubators = true;
		public bool AlwaysPrefferLongDistanceEgg = false;
        //rename
        public bool RenamePokemon = false;
        public bool RenameOnlyAboveIv = true;
        public string RenameTemplate = "{1}_{0}";

        //transfer
        public bool TransferDuplicatePokemon = true;
        public bool PrioritizeIvOverCp = true;
        public int KeepMinCp = 1250;
        public float KeepMinIvPercentage = 95;
        public int KeepMinDuplicatePokemon = 1;
        public bool KeepPokemonsThatCanEvolve = false;

        //evolve
        public bool EvolveAllPokemonWithEnoughCandy = false;
        public bool EvolveAllPokemonAboveIv = false;
        public float EvolveAboveIvValue = 95;
        public bool UseLuckyEggsWhileEvolving = false;
        public int UseLuckyEggsMinPokemonAmount = 50;

        //levelup
        public bool AutomaticallyLevelUpPokemon = false;
        public string LevelUpByCPorIv = "iv";
        public float UpgradePokemonCpMinimum = 1000;
        public float UpgradePokemonIvMinimum = 95;

        //favorite
        public bool AutoFavoritePokemon = false;
        public float FavoriteMinIvPercentage = 95;
    }

    public class LocationSettings
    {
        //coords and movement
        public bool Teleport = false;
        public double DefaultLatitude = -33.8688;
        public double DefaultLongitude = 151.2093;
        public double DefaultAltitude = 10;
        public double WalkingSpeedInKilometerPerHour = 50.0;
        public int MaxSpawnLocationOffset = 10;
        public int MaxTravelDistanceInMeters = 1000;
        public bool UseGpxPathing = false;
        public string GpxFile = "GPXPath.GPX";
		public bool UseDiscoveryPathing = true;
        [JsonIgnore]
        public double MoveSpeedFactor = 1;
    }

    public class CatchSettings
    {
        public bool CatchWildPokemon = true;

        //catch
        public bool HumanizeThrows = false;
        public double ThrowAccuracyMin = 0.80;
        public double ThrowAccuracyMax = 1.00;
        public double ThrowSpinFrequency = 0.80;
        public int MaxPokeballsPerPokemon = 6;
        public int UseGreatBallAboveIv = 80;
        public int UseUltraBallAboveIv = 90;
        public double UseGreatBallBelowCatchProbability = 0.35;
        public double UseUltraBallBelowCatchProbability = 0.2;
        public double UseMasterBallBelowCatchProbability = 0.05;
        public bool UsePokemonToNotCatchFilter = false;

        //berries
        public int UseBerryMinCp = 450;
        public float UseBerryMinIv = 95;
        public double UseBerryBelowCatchProbability = 0.25;
    }

    public class RecycleSettings
    {
        //recycle
        public bool AutomaticInventoryManagement = false;
        public int AutomaticMaxAllPokeballs = 100;
        public int AutomaticMaxAllPotions = 60;
        public int AutomaticMaxAllRevives = 80;
        public int AutomaticMaxAllBerries = 50;
        public int TotalAmountOfPokeballsToKeep = 100;
        //public int TotalAmountOfGreatballsToKeep = 40;
        //public int TotalAmountOfUltraballsToKeep = 60;
        //public int TotalAmountOfMasterballsToKeep = 100;
        public int TotalAmountOfPotionsToKeep = 60;
        //public int TotalAmountOfSuperPotionsToKeep = 0;
        //public int TotalAmountOfHyperPotionsToKeep = 20;
        //public int TotalAmountOfMaxPotionsToKeep = 40;
        public int TotalAmountOfRevivesToKeep = 20;
        //public int TotalAmountOfMaxRevivesToKeep = 60;
        public int TotalAmountOfRazzToKeep = 50;
        //public int TotalAmountOfBlukToKeep = 50;
        //public int TotalAmountOfNanabToKeep = 50;
        //public int TotalAmountOfPinapToKeep = 50;
        //public int TotalAmountOfWeparToKeep = 50;
        public double RecycleInventoryAtUsagePercentage = 0.90;
    }

    public class SnipeConfig
    {
        //snipe
        public bool SnipeAtPokestops = false;
        public bool SnipeIgnoreUnknownIv = false;
        public bool UseTransferIvForSnipe = false;
        public int MinPokeballsToSnipe = 20;
        public int MinPokeballsWhileSnipe = 5;
        public bool UseSnipeLocationServer = false;
        public bool UsePokeSnipersLocationServer = false;
        public string SnipeLocationServer = "localhost";
        public int SnipeLocationServerPort = 16969;
        public int SnipeRequestTimeoutSeconds = 5;
    }

    public class GlobalSettings
    {
        [JsonIgnore] public AuthSettings Auth = new AuthSettings();
        [JsonIgnore] public string GeneralConfigPath;
        [JsonIgnore] public string ProfilePath;
        [JsonIgnore] public string ProfileConfigPath;

        public DeviceSettings Device = new DeviceSettings();

        public StartUpSettings StartUpSettings = new StartUpSettings();

        public LocationSettings LocationSettings = new LocationSettings();

        public DelaySettings DelaySettings = new DelaySettings();

        public PokemonConfig PokemonSettings = new PokemonConfig();

        public CatchSettings CatchSettings = new CatchSettings();

        public RecycleSettings RecycleSettings = new RecycleSettings();

        public SnipeConfig SnipeSettings = new SnipeConfig();



        #region Lists of pokemon and item settings
        public List<KeyValuePair<ItemId, int>> ItemRecycleFilter = new List<KeyValuePair<ItemId, int>>
        {
            new KeyValuePair<ItemId, int>(ItemId.ItemUnknown, 0),
            new KeyValuePair<ItemId, int>(ItemId.ItemLuckyEgg, 200),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncenseOrdinary, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncenseSpicy, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncenseCool, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncenseFloral, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemTroyDisk, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemXAttack, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemXDefense, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemXMiracle, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemSpecialCamera, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncubatorBasicUnlimited, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemIncubatorBasic, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemPokemonStorageUpgrade, 100),
            new KeyValuePair<ItemId, int>(ItemId.ItemItemStorageUpgrade, 100)
        };

        public List<PokemonId> PokemonsNotToTransfer = new List<PokemonId>
        {
            //criteria: from SS Tier to A Tier + Regional Exclusive
            //PokemonId.Venusaur,
            //PokemonId.Charizard,
            //PokemonId.Blastoise,
            //PokemonId.Nidoqueen,
            //PokemonId.Nidoking,
            //PokemonId.Clefable,
            //PokemonId.Vileplume,
            //PokemonId.Golduck,
            //PokemonId.Arcanine,
            //PokemonId.Poliwrath,
            //PokemonId.Machamp,
            //PokemonId.Victreebel,
            //PokemonId.Golem,
            //PokemonId.Slowbro,
            //PokemonId.Farfetchd,
            //PokemonId.Muk,
            //PokemonId.Exeggutor,
            //PokemonId.Lickitung,
            //PokemonId.Chansey,
            //PokemonId.Kangaskhan,
            //PokemonId.MrMime,
            //PokemonId.Tauros,
            //PokemonId.Gyarados,
            //PokemonId.Lapras,
            PokemonId.Ditto,
            //PokemonId.Vaporeon,
            //PokemonId.Jolteon,
            //PokemonId.Flareon,
            //PokemonId.Porygon,
            //PokemonId.Snorlax,
            PokemonId.Articuno,
            PokemonId.Zapdos,
            PokemonId.Moltres,
            //PokemonId.Dragonite,
            PokemonId.Mewtwo,
            PokemonId.Mew
        };

        public List<PokemonId> PokemonsToEvolve = new List<PokemonId>
        {
            /*NOTE: keep all the end-of-line commas exept for the last one or an exception will be thrown!
            criteria: 12 candies*/
            PokemonId.Caterpie,
            PokemonId.Weedle,
            PokemonId.Pidgey,
            /*criteria: 25 candies*/
            //PokemonId.Bulbasaur,
            //PokemonId.Charmander,
            //PokemonId.Squirtle,
            PokemonId.Rattata,
            //PokemonId.NidoranFemale,
            //PokemonId.NidoranMale,
            //PokemonId.Oddish,
            //PokemonId.Poliwag,
            //PokemonId.Abra,
            //PokemonId.Machop,
            //PokemonId.Bellsprout,
            //PokemonId.Geodude,
            //PokemonId.Gastly,
            //PokemonId.Eevee,
            //PokemonId.Dratini,
            /*criteria: 50 candies commons*/
            //PokemonId.Spearow,
            //PokemonId.Ekans,
            PokemonId.Zubat,
            //PokemonId.Paras,
            //PokemonId.Venonat,
            //PokemonId.Psyduck,
            //PokemonId.Slowpoke,
            PokemonId.Doduo
            //PokemonId.Drowzee,
            //PokemonId.Krabby,
            //PokemonId.Horsea,
            //PokemonId.Goldeen,
            //PokemonId.Staryu
        };

        public List<PokemonId> PokemonsToIgnore = new List<PokemonId>
        {
            //criteria: most common
            PokemonId.Caterpie,
            PokemonId.Weedle,
            PokemonId.Pidgey,
            PokemonId.Rattata,
            PokemonId.Spearow,
            PokemonId.Zubat,
            PokemonId.Doduo
        };

        public Dictionary<PokemonId, TransferFilter> PokemonsTransferFilter = new Dictionary<PokemonId, TransferFilter>
        {
            //criteria: based on NY Central Park and Tokyo variety + sniping optimization v4.1
            {PokemonId.Venusaur, new TransferFilter(1750, 80, 1)},
            {PokemonId.Charizard, new TransferFilter(1750, 20, 1)},
            {PokemonId.Blastoise, new TransferFilter(1750, 50, 1)},
            {PokemonId.Nidoqueen, new TransferFilter(1750, 80, 1)},
            {PokemonId.Nidoking, new TransferFilter(1750, 80, 1)},
            {PokemonId.Clefable, new TransferFilter(1500, 60, 1)},
            {PokemonId.Vileplume, new TransferFilter(1750, 80, 1)},
            {PokemonId.Golduck, new TransferFilter(1750, 80, 1)},
            {PokemonId.Arcanine, new TransferFilter(2250, 90, 1)},
            {PokemonId.Poliwrath, new TransferFilter(1750, 80, 1)},
            {PokemonId.Machamp, new TransferFilter(1250, 80, 1)},
            {PokemonId.Victreebel, new TransferFilter(1250, 60, 1)},
            {PokemonId.Golem, new TransferFilter(1500, 80, 1)},
            {PokemonId.Slowbro, new TransferFilter(1750, 90, 1)},
            {PokemonId.Farfetchd, new TransferFilter(1250, 90, 1)},
            {PokemonId.Muk, new TransferFilter(2000, 80, 1)},
            {PokemonId.Exeggutor, new TransferFilter(2250, 80, 1)},
            {PokemonId.Lickitung, new TransferFilter(1500, 80, 1)},
            {PokemonId.Chansey, new TransferFilter(1500, 95, 1)},
            {PokemonId.Kangaskhan, new TransferFilter(1500, 60, 1)},
            {PokemonId.MrMime, new TransferFilter(1250, 80, 1)},
            {PokemonId.Scyther, new TransferFilter(1750, 90, 1)},
            {PokemonId.Jynx, new TransferFilter(1250, 90, 1)},
            {PokemonId.Electabuzz, new TransferFilter(1500, 80, 1)},
            {PokemonId.Magmar, new TransferFilter(1750, 90, 1)},
            {PokemonId.Pinsir, new TransferFilter(2000, 98, 1)},
            {PokemonId.Tauros, new TransferFilter(500, 90, 1)},
            {PokemonId.Gyarados, new TransferFilter(2000, 90, 1)},
            {PokemonId.Lapras, new TransferFilter(2250, 90, 1)},
            {PokemonId.Eevee, new TransferFilter(1500, 98, 1)},
            {PokemonId.Vaporeon, new TransferFilter(2250, 98, 1)},
            {PokemonId.Jolteon, new TransferFilter(2250, 95, 1)},
            {PokemonId.Flareon, new TransferFilter(2250, 95, 1)},
            {PokemonId.Porygon, new TransferFilter(1500, 95, 1)},
            {PokemonId.Aerodactyl, new TransferFilter(2000, 95, 1)},
            {PokemonId.Snorlax, new TransferFilter(2750, 96, 1)},
            {PokemonId.Dragonite, new TransferFilter(2750, 90, 1)}
        };

        public SnipeSettings PokemonToSnipe = new SnipeSettings
        {
            Locations = new List<Location>
            {
                new Location(38.55680748646112, -121.2383794784546), //Dratini Spot
                new Location(-33.85901900, 151.21309800), //Magikarp Spot
                new Location(47.5014969, -122.0959568), //Eevee Spot
                new Location(51.5025343, -0.2055027) //Charmender Spot
            },
            Pokemon = new List<PokemonId>
            {
                PokemonId.Bulbasaur,
                PokemonId.Ivysaur,
                PokemonId.Venusaur,
                PokemonId.Charmander,
                PokemonId.Charmeleon,
                PokemonId.Charizard,
                PokemonId.Squirtle,
                PokemonId.Wartortle,
                PokemonId.Blastoise,
                PokemonId.Butterfree,
                PokemonId.Beedrill,
                PokemonId.Pidgeot,
                PokemonId.Raticate,
                PokemonId.Fearow,
                PokemonId.Arbok,
                PokemonId.Pikachu,
                PokemonId.Raichu,
                PokemonId.Sandslash,
                PokemonId.Nidoqueen,
                PokemonId.Nidoking,
                PokemonId.Clefable,
                PokemonId.Ninetales,
                PokemonId.Wigglytuff,
                PokemonId.Golbat,
                PokemonId.Vileplume,
                PokemonId.Parasect,
                PokemonId.Venomoth,
                PokemonId.Dugtrio,
                PokemonId.Persian,
                PokemonId.Golduck,
                PokemonId.Primeape,
                PokemonId.Growlithe,
                PokemonId.Arcanine,
                PokemonId.Poliwag,
                PokemonId.Poliwhirl,
                PokemonId.Poliwrath,
                PokemonId.Abra,
                PokemonId.Kadabra,
                PokemonId.Alakazam,
                PokemonId.Machop,
                PokemonId.Machoke,
                PokemonId.Machamp,
                PokemonId.Victreebel,
                PokemonId.Tentacruel,
                PokemonId.Golem,
                PokemonId.Rapidash,
                PokemonId.Slowbro,
                PokemonId.Magneton,
                PokemonId.Farfetchd,
                PokemonId.Dodrio,
                PokemonId.Dewgong,
                PokemonId.Grimer,
                PokemonId.Muk,
                PokemonId.Cloyster,
                PokemonId.Gastly,
                PokemonId.Haunter,
                PokemonId.Gengar,
                PokemonId.Onix,
                PokemonId.Hypno,
                PokemonId.Kingler,
                PokemonId.Electrode,
                PokemonId.Exeggutor,
                PokemonId.Marowak,
                PokemonId.Hitmonlee,
                PokemonId.Hitmonchan,
                PokemonId.Lickitung,
                PokemonId.Koffing,
                PokemonId.Weezing,
                PokemonId.Rhyhorn,
                PokemonId.Rhydon,
                PokemonId.Chansey,
                PokemonId.Tangela,
                PokemonId.Kangaskhan,
                PokemonId.Seadra,
                PokemonId.Seaking,
                PokemonId.Starmie,
                PokemonId.MrMime,
                PokemonId.Scyther,
                PokemonId.Jynx,
                PokemonId.Electabuzz,
                PokemonId.Magmar,
                PokemonId.Pinsir,
                PokemonId.Tauros,
                PokemonId.Magikarp,
                PokemonId.Gyarados,
                PokemonId.Lapras,
                PokemonId.Ditto,
                PokemonId.Eevee,
                PokemonId.Vaporeon,
                PokemonId.Jolteon,
                PokemonId.Flareon,
                PokemonId.Porygon,
                PokemonId.Omanyte,
                PokemonId.Omastar,
                PokemonId.Kabuto,
                PokemonId.Kabutops,
                PokemonId.Aerodactyl,
                PokemonId.Snorlax,
                PokemonId.Articuno,
                PokemonId.Zapdos,
                PokemonId.Moltres,
                PokemonId.Dratini,
                PokemonId.Dragonair,
                PokemonId.Dragonite,
                PokemonId.Mewtwo,
                PokemonId.Mew
            }
        };

        public List<PokemonId> PokemonToUseMasterball = new List<PokemonId>
        {
            PokemonId.Articuno,
            PokemonId.Zapdos,
            PokemonId.Moltres,
            PokemonId.Mew,
            PokemonId.Mewtwo
        };
        #endregion

        public static GlobalSettings Default => new GlobalSettings();

        public static GlobalSettings Load(string path)
        {
            GlobalSettings settings;
            var profilePath = Path.Combine(Directory.GetCurrentDirectory(), path);
            var profileConfigPath = Path.Combine(profilePath, "config");
            var configFile = Path.Combine(profileConfigPath, "config.json");

            if (File.Exists(configFile))
            {
                try
                {
                    //if the file exists, load the settings
                    var input = File.ReadAllText(configFile);

                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                    jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;

                    settings = JsonConvert.DeserializeObject<GlobalSettings>(input, jsonSettings);
                }
                catch (JsonReaderException exception)
                {
                    Logger.Write("JSON Exception: " + exception.Message, LogLevel.Error);
                    return null;
                }
				if (settings.Device.DevicePackageName.Equals("random", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Random is set, so pick a random device package and set it up - it will get saved to disk below and re-used in subsequent sessions
                    var rnd = new Random();
                    var rndIdx = rnd.Next(0, DeviceInfoHelper.DeviceInfoSets.Keys.Count + 1);
                    settings.Device.SetDevInfoByKey(DeviceInfoHelper.DeviceInfoSets.Keys.ToArray()[rndIdx]);
                }
                if (string.IsNullOrEmpty(settings.Device.DeviceId) || settings.Device.DeviceId == "8525f5d8201f78b5")
                    settings.Device.DeviceId = DeviceSettings.RandomString(16, "0123456789abcdef"); // changed to random hex as full alphabet letters could have been flagged
            }
            else
            {
                settings = new GlobalSettings();
            }

            if (settings.StartUpSettings.WebSocketPort == 0)
            {
                settings.StartUpSettings.WebSocketPort = 14251;
            }

            if (settings.PokemonToSnipe == null)
            {
                settings.PokemonToSnipe = Default.PokemonToSnipe;
            }

            if (settings.PokemonSettings.RenameTemplate == null)
            {
                settings.PokemonSettings.RenameTemplate = Default.PokemonSettings.RenameTemplate;
            }

            if (settings.SnipeSettings.SnipeLocationServer == null)
            {
                settings.SnipeSettings.SnipeLocationServer = Default.SnipeSettings.SnipeLocationServer;
            }

            settings.ProfilePath = profilePath;
            settings.ProfileConfigPath = profileConfigPath;
            settings.GeneralConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "config");

            var firstRun = !File.Exists(configFile);

            settings.Save(configFile);
            settings.Auth.Load(Path.Combine(profileConfigPath, "auth.json"));

            if (firstRun)
            {
                return null;
            }

            return settings;
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
        public void StoreData(string path)
        {
            var profilePath = Path.Combine(Directory.GetCurrentDirectory(), path);
            var profileConfigPath = Path.Combine(profilePath, "config");
            var configFile = Path.Combine(profileConfigPath, "config.json");
            Save(configFile);
            Auth.Save(Path.Combine(profileConfigPath, "auth.json"));
        }
    }

    public class ClientSettings : ISettings
    {
        // Never spawn at the same position.
        private readonly Random _rand = new Random();
        private readonly GlobalSettings _settings;

        public ClientSettings(GlobalSettings settings)
        {
            _settings = settings;
        }


        public string GoogleUsername => _settings.Auth.GoogleUsername;
        public string GooglePassword => _settings.Auth.GooglePassword;

        public string GoogleRefreshToken
        {
            get { return null; }
            set
            {
                if (value == null && GoogleRefreshToken == null) return;
                GoogleRefreshToken = null;
            }
        }

        AuthType ISettings.AuthType
        {
            get { return _settings.Auth.AuthType; }

            set { _settings.Auth.AuthType = value; }
        }

        double ISettings.DefaultLatitude
        {
            get
            {
                return _settings.LocationSettings.DefaultLatitude + _rand.NextDouble() * ((double)_settings.LocationSettings.MaxSpawnLocationOffset / 111111);
            }

            set { _settings.LocationSettings.DefaultLatitude = value; }
        }

        double ISettings.DefaultLongitude
        {
            get
            {
                return _settings.LocationSettings.DefaultLongitude +
                       _rand.NextDouble() *
                       ((double)_settings.LocationSettings.MaxSpawnLocationOffset / 111111 / Math.Cos(_settings.LocationSettings.DefaultLatitude));
            }

            set { _settings.LocationSettings.DefaultLongitude = value; }
        }

        double ISettings.DefaultAltitude
        {
            get { return _settings.LocationSettings.DefaultAltitude; }

            set { _settings.LocationSettings.DefaultAltitude = value; }
        }

        string ISettings.PtcPassword
        {
            get { return _settings.Auth.PtcPassword; }

            set { _settings.Auth.PtcPassword = value; }
        }

        string ISettings.PtcUsername
        {
            get { return _settings.Auth.PtcUsername; }

            set { _settings.Auth.PtcUsername = value; }
        }

        string ISettings.GoogleUsername
        {
            get { return _settings.Auth.GoogleUsername; }

            set { _settings.Auth.GoogleUsername = value; }
        }

        string ISettings.GooglePassword
        {
            get { return _settings.Auth.GooglePassword; }

            set { _settings.Auth.GooglePassword = value; }
        }

        //string DevicePackageName
        //{
        //    get { return _settings.de; }
        //    set { _settings.DevicePackageName = value; }
        //}
        string ISettings.DeviceId
        {
            get { return _settings.Device.DeviceId; }
            set { _settings.Device.DeviceId = value; }
        }
        string ISettings.AndroidBoardName
        {
            get { return _settings.Device.AndroidBoardName; }
            set { _settings.Device.AndroidBoardName = value; }
        }
        string ISettings.AndroidBootloader
        {
            get { return _settings.Device.AndroidBootLoader; }
            set { _settings.Device.AndroidBootLoader = value; }
        }
        string ISettings.DeviceBrand
        {
            get { return _settings.Device.DeviceBrand; }
            set { _settings.Device.DeviceBrand = value; }
        }
        string ISettings.DeviceModel
        {
            get { return _settings.Device.DeviceModel; }
            set { _settings.Device.DeviceModel = value; }
        }
        string ISettings.DeviceModelIdentifier
        {
            get { return _settings.Device.DeviceModelIdentifier; }
            set { _settings.Device.DeviceModelIdentifier = value; }
        }
        string ISettings.DeviceModelBoot
        {
            get { return _settings.Device.DeviceModelBoot; }
            set { _settings.Device.DeviceModelBoot = value; }
        }
        string ISettings.HardwareManufacturer
        {
            get { return _settings.Device.HardwareManufacturer; }
            set { _settings.Device.HardwareManufacturer = value; }
        }
        string ISettings.HardwareModel
        {
            get { return _settings.Device.HardWareModel; }
            set { _settings.Device.HardWareModel = value; }
        }
        string ISettings.FirmwareBrand
        {
            get { return _settings.Device.FirmwareBrand; }
            set { _settings.Device.FirmwareBrand = value; }
        }
        string ISettings.FirmwareTags
        {
            get { return _settings.Device.FirmwareTags; }
            set { _settings.Device.FirmwareTags = value; }
        }
        string ISettings.FirmwareType
        {
            get { return _settings.Device.FirmwareType; }
            set { _settings.Device.FirmwareType = value; }
        }
        string ISettings.FirmwareFingerprint
        {
            get { return _settings.Device.FirmwareFingerprint; }
            set { _settings.Device.FirmwareFingerprint = value; }
        }

        bool ISettings.UseProxy
        {
            get { return _settings.Auth.UseProxy; }
            set { _settings.Auth.UseProxy = value; }
        }
        string ISettings.ProxyLogin
        {
            get { return _settings.Auth.ProxyLogin; }
            set { _settings.Auth.ProxyLogin = value; }
        }
        string ISettings.ProxyPass
        {
            get { return _settings.Auth.ProxyPass; }
            set { _settings.Auth.ProxyPass = value; }
        }
        string ISettings.ProxyUri
        {
            get { return _settings.Auth.ProxyUri; }
            set { _settings.Auth.ProxyUri = value; }
        }
        double ISettings.MoveSpeedFactor
        {
            get { return _settings.LocationSettings.MoveSpeedFactor; }
            set { _settings.LocationSettings.MoveSpeedFactor = value; }
        }
    }

    public class LogicSettings : ILogicSettings
    {
        private readonly GlobalSettings _settings;

        public LogicSettings(GlobalSettings settings)
        {
            _settings = settings;
        }

        public string ProfilePath => _settings.ProfilePath;
        public string ProfileConfigPath => _settings.ProfileConfigPath;
        public int SnipeRequestTimeoutSeconds => _settings.SnipeSettings.SnipeRequestTimeoutSeconds * 1000;
        public string GeneralConfigPath => _settings.GeneralConfigPath;
        public bool AutoUpdate => _settings.StartUpSettings.AutoUpdate;
        public bool TransferConfigAndAuthOnUpdate => _settings.StartUpSettings.TransferConfigAndAuthOnUpdate;
        public float KeepMinIvPercentage => _settings.PokemonSettings.KeepMinIvPercentage;
        public int KeepMinCp => _settings.PokemonSettings.KeepMinCp;
        public bool AutomaticallyLevelUpPokemon => _settings.PokemonSettings.AutomaticallyLevelUpPokemon;
        public string LevelUpByCPorIv => _settings.PokemonSettings.LevelUpByCPorIv;
        public float UpgradePokemonIvMinimum => _settings.PokemonSettings.UpgradePokemonIvMinimum;
        public float UpgradePokemonCpMinimum => _settings.PokemonSettings.UpgradePokemonCpMinimum;
        public double WalkingSpeedInKilometerPerHour => _settings.LocationSettings.WalkingSpeedInKilometerPerHour;
        public bool EvolveAllPokemonWithEnoughCandy => _settings.PokemonSettings.EvolveAllPokemonWithEnoughCandy;
        public bool KeepPokemonsThatCanEvolve => _settings.PokemonSettings.KeepPokemonsThatCanEvolve;
        public bool TransferDuplicatePokemon => _settings.PokemonSettings.TransferDuplicatePokemon;
        public bool UseEggIncubators => _settings.PokemonSettings.UseEggIncubators;
        public int UseGreatBallAboveIv => _settings.CatchSettings.UseGreatBallAboveIv;
        public int UseUltraBallAboveIv => _settings.CatchSettings.UseUltraBallAboveIv;
        public double UseUltraBallBelowCatchProbability => _settings.CatchSettings.UseUltraBallBelowCatchProbability;
        public double UseGreatBallBelowCatchProbability => _settings.CatchSettings.UseGreatBallBelowCatchProbability;
        public int DelayBetweenPokemonCatch => _settings.DelaySettings.DelayBetweenPokemonCatch;
        public int DelayBetweenPlayerActions => _settings.DelaySettings.DelayBetweenPlayerActions;
        public bool UsePokemonToNotCatchFilter => _settings.CatchSettings.UsePokemonToNotCatchFilter;
        public int KeepMinDuplicatePokemon => _settings.PokemonSettings.KeepMinDuplicatePokemon;
        public bool PrioritizeIvOverCp => _settings.PokemonSettings.PrioritizeIvOverCp;
        public int MaxTravelDistanceInMeters => _settings.LocationSettings.MaxTravelDistanceInMeters;
        public string GpxFile => _settings.LocationSettings.GpxFile;
        public bool UseGpxPathing => _settings.LocationSettings.UseGpxPathing;
        public bool UseLuckyEggsWhileEvolving => _settings.PokemonSettings.UseLuckyEggsWhileEvolving;
        public int UseLuckyEggsMinPokemonAmount => _settings.PokemonSettings.UseLuckyEggsMinPokemonAmount;
        public bool EvolveAllPokemonAboveIv => _settings.PokemonSettings.EvolveAllPokemonAboveIv;
        public float EvolveAboveIvValue => _settings.PokemonSettings.EvolveAboveIvValue;
        public bool RenamePokemon => _settings.PokemonSettings.RenamePokemon;
        public bool RenameOnlyAboveIv => _settings.PokemonSettings.RenameOnlyAboveIv;
        public float FavoriteMinIvPercentage => _settings.PokemonSettings.FavoriteMinIvPercentage;
        public bool AutoFavoritePokemon => _settings.PokemonSettings.AutoFavoritePokemon;
        public string RenameTemplate => _settings.PokemonSettings.RenameTemplate;
        public int AmountOfPokemonToDisplayOnStart => _settings.StartUpSettings.AmountOfPokemonToDisplayOnStart;
        public bool DisplayPokemonMaxPoweredCp => _settings.StartUpSettings.DisplayPokemonMaxPoweredCp;
        public bool DisplayPokemonMovesetRank => _settings.StartUpSettings.DisplayPokemonMovesetRank;
        public bool DumpPokemonStats => _settings.StartUpSettings.DumpPokemonStats;
        public string TranslationLanguageCode => _settings.StartUpSettings.TranslationLanguageCode;
        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter => _settings.ItemRecycleFilter;
        public ICollection<PokemonId> PokemonsToEvolve => _settings.PokemonsToEvolve;
        public ICollection<PokemonId> PokemonsNotToTransfer => _settings.PokemonsNotToTransfer;
        public ICollection<PokemonId> PokemonsNotToCatch => _settings.PokemonsToIgnore;
        public ICollection<PokemonId> PokemonToUseMasterball => _settings.PokemonToUseMasterball;
        public Dictionary<PokemonId, TransferFilter> PokemonsTransferFilter => _settings.PokemonsTransferFilter;
        public bool StartupWelcomeDelay => _settings.StartUpSettings.StartupWelcomeDelay;
        public bool SnipeAtPokestops => _settings.SnipeSettings.SnipeAtPokestops;
        public int MinPokeballsToSnipe => _settings.SnipeSettings.MinPokeballsToSnipe;
        public int MinPokeballsWhileSnipe => _settings.SnipeSettings.MinPokeballsWhileSnipe;
        public int MaxPokeballsPerPokemon => _settings.CatchSettings.MaxPokeballsPerPokemon;
        public SnipeSettings PokemonToSnipe => _settings.PokemonToSnipe;
        public string SnipeLocationServer => _settings.SnipeSettings.SnipeLocationServer;
        public int SnipeLocationServerPort => _settings.SnipeSettings.SnipeLocationServerPort;
        public bool UseSnipeLocationServer => _settings.SnipeSettings.UseSnipeLocationServer;
        public bool UsePokeSnipersLocationServer => _settings.SnipeSettings.UsePokeSnipersLocationServer;
        public bool UseTransferIvForSnipe => _settings.SnipeSettings.UseTransferIvForSnipe;
        public bool SnipeIgnoreUnknownIv => _settings.SnipeSettings.SnipeIgnoreUnknownIv;
        public int MinDelayBetweenSnipes => _settings.DelaySettings.MinDelayBetweenSnipes;
        public double SnipingScanOffset => _settings.DelaySettings.SnipingScanOffset;
        public bool AutomaticInventoryManagement => _settings.RecycleSettings.AutomaticInventoryManagement;
        public int AutomaticMaxAllPokeballs => _settings.RecycleSettings.AutomaticMaxAllPokeballs;
        public int AutomaticMaxAllPotions => _settings.RecycleSettings.AutomaticMaxAllPotions;
        public int AutomaticMaxAllRevives => _settings.RecycleSettings.AutomaticMaxAllRevives;
        public int AutomaticMaxAllBerries => _settings.RecycleSettings.AutomaticMaxAllBerries;
        public int TotalAmountOfPokeballsToKeep => _settings.RecycleSettings.TotalAmountOfPokeballsToKeep;
        //public int TotalAmountOfGreatballsToKeep => _settings.RecycleSettings.TotalAmountOfGreatballsToKeep;
        //public int TotalAmountOfUltraballsToKeep => _settings.RecycleSettings.TotalAmountOfUltraballsToKeep;
        //public int TotalAmountOfMasterballsToKeep => _settings.RecycleSettings.TotalAmountOfMasterballsToKeep;
        public int TotalAmountOfRazzToKeep => _settings.RecycleSettings.TotalAmountOfRazzToKeep;
        //public int TotalAmountOfBlukToKeep => _settings.RecycleSettings.TotalAmountOfBlukToKeep;
        //public int TotalAmountOfNanabToKeep => _settings.RecycleSettings.TotalAmountOfNanabToKeep;
        //public int TotalAmountOfPinapToKeep => _settings.RecycleSettings.TotalAmountOfPinapToKeep;
        //public int TotalAmountOfWeparToKeep => _settings.RecycleSettings.TotalAmountOfWeparToKeep;
        public int TotalAmountOfPotionsToKeep => _settings.RecycleSettings.TotalAmountOfPotionsToKeep;
        //public int TotalAmountOfSuperPotionsToKeep => _settings.RecycleSettings.TotalAmountOfSuperPotionsToKeep;
        //public int TotalAmountOfHyperPotionsToKeep => _settings.RecycleSettings.TotalAmountOfHyperPotionsToKeep;
        //public int TotalAmountOfMaxPotionsToKeep => _settings.RecycleSettings.TotalAmountOfMaxPotionsToKeep;
        public int TotalAmountOfRevivesToKeep => _settings.RecycleSettings.TotalAmountOfRevivesToKeep;
        public int TotalAmountOfMaxRevivesToKeep => _settings.RecycleSettings.TotalAmountOfRevivesToKeep;
        public bool Teleport => _settings.LocationSettings.Teleport;
        public int DelayCatchIncensePokemon => _settings.DelaySettings.DelayCatchIncensePokemon;
        public int DelayCatchNearbyPokemon => _settings.DelaySettings.DelayCatchNearbyPokemon;
        public int DelayPositionCheckState => _settings.DelaySettings.DelayPositionCheckState;
        public int DelayCatchLurePokemon => _settings.DelaySettings.DelayCatchLurePokemon;
        public int DelayCatchPokemon => _settings.DelaySettings.DelayCatchPokemon;
        public int DelayDisplayPokemon => _settings.DelaySettings.DelayDisplayPokemon;
        public int DelayUseLuckyEgg => _settings.DelaySettings.DelayUseLuckyEgg;
        public int DelaySoftbanRetry => _settings.DelaySettings.DelaySoftbanRetry;
        public int DelayPokestop => _settings.DelaySettings.DelayPokestop;
        public int DelayRecyleItem => _settings.DelaySettings.DelayRecyleItem;
        public int DelaySnipePokemon => _settings.DelaySettings.DelaySnipePokemon;
        public int DelayTransferPokemon => _settings.DelaySettings.DelayTransferPokemon;
        public int DelayEvolvePokemon => _settings.DelaySettings.DelayEvolvePokemon;
        public double DelayEvolveVariation => _settings.DelaySettings.DelayEvolveVariation;
        public double RecycleInventoryAtUsagePercentage => _settings.RecycleSettings.RecycleInventoryAtUsagePercentage;
        public bool HumanizeThrows => _settings.CatchSettings.HumanizeThrows;
        public double ThrowAccuracyMin => _settings.CatchSettings.ThrowAccuracyMin;
        public double ThrowAccuracyMax => _settings.CatchSettings.ThrowAccuracyMax;
        public double ThrowSpinFrequency => _settings.CatchSettings.ThrowSpinFrequency;
        public int UseBerryMinCp => _settings.CatchSettings.UseBerryMinCp;
        public float UseBerryMinIv => _settings.CatchSettings.UseBerryMinIv;
        public double UseBerryBelowCatchProbability => _settings.CatchSettings.UseBerryBelowCatchProbability;
        public bool AlwaysPrefferLongDistanceEgg => _settings.PokemonSettings.AlwaysPrefferLongDistanceEgg;
        public bool UseDiscoveryPathing => _settings.LocationSettings.UseDiscoveryPathing;
        public double UseMasterBallBelowCatchProbability => _settings.CatchSettings.UseMasterBallBelowCatchProbability;
        public bool CatchWildPokemon => _settings.CatchSettings.CatchWildPokemon;
    }
}
