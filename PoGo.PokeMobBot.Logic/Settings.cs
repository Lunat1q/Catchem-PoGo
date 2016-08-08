#region using directives

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoGo.PokeMobBot.Logic.Logging;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using PoGo.PokeMobBot.Logic.Utils;

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
        // device data
        [DefaultValue("random")]
        public string DevicePackageName;
        [DefaultValue("8525f5d8201f78b5")]
        public string DeviceId;
        [DefaultValue("msm8996")]
        public string AndroidBoardName;
        [DefaultValue("1.0.0.0000")]
        public string AndroidBootloader;
        [DefaultValue("HTC")]
        public string DeviceBrand;
        [DefaultValue("HTC 10")]
        public string DeviceModel;
        [DefaultValue("pmewl_00531")]
        public string DeviceModelIdentifier;
        [DefaultValue("qcom")]
        public string DeviceModelBoot;
        [DefaultValue("HTC")]
        public string HardwareManufacturer;
        [DefaultValue("HTC 10")]
        public string HardwareModel;
        [DefaultValue("pmewl_00531")]
        public string FirmwareBrand;
        [DefaultValue("release-keys")]
        public string FirmwareTags;
        [DefaultValue("user")]
        public string FirmwareType;
        [DefaultValue("htc/pmewl_00531/htc_pmewl:6.0.1/MMB29M/770927.1:user/release-keys")]
        public string FirmwareFingerprint;

        public AuthSettings()
        {
            InitializePropertyDefaultValues(this);
        }

        public void InitializePropertyDefaultValues(object obj)
        {
            FieldInfo[] fields = obj.GetType().GetFields();

            foreach (FieldInfo field in fields)
            {
                var d = field.GetCustomAttribute<DefaultValueAttribute>();

                if (d != null)
                    field.SetValue(obj, d.Value);
            }
        }

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
                
                if (this.DevicePackageName.Equals("random", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Random is set, so pick a random device package and set it up - it will get saved to disk below and re-used in subsequent sessions
                    Random rnd = new Random();
                    int rndIdx = rnd.Next(0, DeviceInfoHelper.DeviceInfoSets.Keys.Count + 1);
                    SetDevInfoByKey(DeviceInfoHelper.DeviceInfoSets.Keys.ToArray()[rndIdx]);
                }
                if (string.IsNullOrEmpty(this.DeviceId) || this.DeviceId == "8525f5d8201f78b5")
                    this.DeviceId = RandomString(16, "0123456789abcdef"); // changed to random hex as full alphabet letters could have been flagged

                // Jurann: Note that some device IDs I saw when adding devices had smaller numbers, only 12 or 14 chars instead of 16 - probably not important but noted here anyway

                Save(_filePath);
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

        private void SetDevInfoByKey(string devKey)
        {
            this.DevicePackageName = devKey;
            if (DeviceInfoHelper.DeviceInfoSets.ContainsKey(this.DevicePackageName))
            {
                this.AndroidBoardName = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["AndroidBoardName"];
                this.AndroidBootloader = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["AndroidBootloader"];
                this.DeviceBrand = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["DeviceBrand"];
                this.DeviceId = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["DeviceId"];
                this.DeviceModel = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["DeviceModel"];
                this.DeviceModelBoot = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["DeviceModelBoot"];
                this.DeviceModelIdentifier = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["DeviceModelIdentifier"];
                this.FirmwareBrand = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["FirmwareBrand"];
                this.FirmwareFingerprint = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["FirmwareFingerprint"];
                this.FirmwareTags = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["FirmwareTags"];
                this.FirmwareType = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["FirmwareType"];
                this.HardwareManufacturer = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["HardwareManufacturer"];
                this.HardwareModel = DeviceInfoHelper.DeviceInfoSets[this.DevicePackageName]["HardwareModel"];
            }
            else
            {
                throw new ArgumentException("Invalid device info package! Check your auth.config file and make sure a valid DevicePackageName is set or simply set it to 'random'...");
            }
        }
    }

    public class GlobalSettings
    {
        [JsonIgnore]
        public AuthSettings Auth = new AuthSettings();
        [JsonIgnore]
        public string GeneralConfigPath;
        [JsonIgnore]
        public string ProfilePath;
        [JsonIgnore]
        public string ProfileConfigPath;

        //bot start
        public bool AutoUpdate = true;
        public bool TransferConfigAndAuthOnUpdate = true;
        public bool DumpPokemonStats = false;
        public int AmountOfPokemonToDisplayOnStart = 10;
        public bool StartupWelcomeDelay = false;
        public string TranslationLanguageCode = "en";
        public int WebSocketPort = 14251;

        //coords and movement
        public bool Teleport = false;
        public double DefaultLatitude = 40.785091;
        public double DefaultLongitude = -73.968285;
        public double DefaultAltitude = 10;
        public double WalkingSpeedInKilometerPerHour = 15.0;
        public int MaxSpawnLocationOffset = 10;
        public int MaxTravelDistanceInMeters = 1000;
        public bool UseGpxPathing = false;
        public string GpxFile = "GPXPath.GPX";
        public bool UseDiscoveryPathing = true;

        //delays
        public int DelayBetweenPlayerActions = 5000;
        public int DelayPositionCheckState = 1000;
        public int DelayPokestop = 1000;
        public int DelayCatchPokemon = 1000;
        public int DelayBetweenPokemonCatch = 2000;
        public int DelayCatchNearbyPokemon = 1000;
        public int DelayCatchLurePokemon = 1000;
        public int DelayCatchIncensePokemon = 1000;
        public int DelayEvolvePokemon = 1000;
        public double DelayEvolveVariation = 0.3;
        public int DelayTransferPokemon = 1000;
        public int DelayDisplayPokemon = 1000;
        public int DelayUseLuckyEgg = 1000;
        public int DelaySoftbanRetry = 1000;
        public int DelayRecyleItem = 1000;
        public int DelaySnipePokemon = 1000;
        public int MinDelayBetweenSnipes = 60000;
        public double SnipingScanOffset = 0.003;

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
        public int UseLuckyEggsMinPokemonAmount = 30;

        //proxy
        public bool UseProxy = false;
        public string ProxyUri = "proxy.com:1234";
        public string ProxyLogin = "pLogin";
        public string ProxyPass = "pPass";

        //levelup
        public bool AutomaticallyLevelUpPokemon = false;
        public string LevelUpByCPorIv = "iv";
        public float UpgradePokemonCpMinimum = 1000;
        public float UpgradePokemonIvMinimum = 95;

        //catch
        public bool HumanizeThrows = false;
        public double ThrowAccuracyMin = 0.50;
        public double ThrowAccuracyMax = 1.00;
        public double ThrowSpinFrequency = 0.75;
        public int MaxPokeballsPerPokemon = 6;
        public int UseGreatBallAboveIv = 80;
        public int UseUltraBallAboveIv = 90;
        public double UseGreatBallBelowCatchProbability = 0.5;
        public double UseUltraBallBelowCatchProbability = 0.25;
        public double UseMasterBallBelowCatchProbability = 0.05;
        public bool UsePokemonToNotCatchFilter = false;

        //berries
        public int UseBerryMinCp = 450;
        public float UseBerryMinIv = 95;
        public double UseBerryBelowCatchProbability = 0.2;

        //favorite
        public bool AutoFavoritePokemon = false;
        public float FavoriteMinIvPercentage = 95;

        //recycle
        public int TotalAmountOfPokeballsToKeep = 100;
        public int TotalAmountOfPotionsToKeep = 80;
        public int TotalAmountOfRevivesToKeep = 60;
        public int TotalAmountOfBerriesToKeep = 80;
        public double RecycleInventoryAtUsagePercentage = 0.90;

        //snipe
        public bool SnipeAtPokestops = false;
        public bool SnipeIgnoreUnknownIv = false;
        public bool UseTransferIvForSnipe = false;
        public int MinPokeballsToSnipe = 20;
        public int MinPokeballsWhileSnipe = 0;
        public bool UseSnipeLocationServer = false;
        public bool UsePokeSnipersLocationServer = false;
        public string SnipeLocationServer = "localhost";
        public int SnipeLocationServerPort = 16969;
        public int SnipeRequestTimeoutSeconds = 5;

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
            PokemonId.Venusaur,
            PokemonId.Charizard,
            PokemonId.Blastoise,
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
            PokemonId.Muk,
            //PokemonId.Exeggutor,
            //PokemonId.Lickitung,
            PokemonId.Chansey,
            //PokemonId.Kangaskhan,
            //PokemonId.MrMime,
            //PokemonId.Tauros,
            PokemonId.Gyarados,
            //PokemonId.Lapras,
            PokemonId.Ditto,
            //PokemonId.Vaporeon,
            //PokemonId.Jolteon,
            //PokemonId.Flareon,
            //PokemonId.Porygon,
            PokemonId.Snorlax,
            PokemonId.Articuno,
            PokemonId.Zapdos,
            PokemonId.Moltres,
            PokemonId.Dragonite,
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
            PokemonId.Spearow,
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
            //criteria: based on NY Central Park and Tokyo variety + sniping optimization v3
            {PokemonId.Venusaur, new TransferFilter(1500, 40, 1)},
            {PokemonId.Charizard, new TransferFilter(1500, 20, 1)},
            {PokemonId.Blastoise, new TransferFilter(1500, 20, 1)},
            {PokemonId.Nidoqueen, new TransferFilter(1750, 80, 1)},
            {PokemonId.Nidoking, new TransferFilter(1750, 80, 1)},
            {PokemonId.Clefable, new TransferFilter(1500, 60, 1)},
            {PokemonId.Vileplume, new TransferFilter(1750, 80, 1)},
            {PokemonId.Golduck, new TransferFilter(1750, 80, 1)},
            {PokemonId.Arcanine, new TransferFilter(2000, 90, 1)},
            {PokemonId.Poliwrath, new TransferFilter(1500, 80, 1)},
            {PokemonId.Machamp, new TransferFilter(1250, 80, 1)},
            {PokemonId.Victreebel, new TransferFilter(1250, 60, 1)},
            {PokemonId.Golem, new TransferFilter(1500, 80, 1)},
            {PokemonId.Slowbro, new TransferFilter(1750, 80, 1)},
            {PokemonId.Farfetchd, new TransferFilter(1000, 90, 1)},
            {PokemonId.Muk, new TransferFilter(2000, 80, 1)},
            {PokemonId.Exeggutor, new TransferFilter(2250, 80, 1)},
            {PokemonId.Lickitung, new TransferFilter(1500, 80, 1)},
            {PokemonId.Chansey, new TransferFilter(1500, 95, 1)},
            {PokemonId.Kangaskhan, new TransferFilter(1500, 60, 1)},
            {PokemonId.MrMime, new TransferFilter(250, 40, 1)},
            {PokemonId.Scyther, new TransferFilter(1750, 90, 1)},
            {PokemonId.Jynx, new TransferFilter(1250, 90, 1)},
            {PokemonId.Electabuzz, new TransferFilter(1500, 80, 1)},
            {PokemonId.Magmar, new TransferFilter(1750, 80, 1)},
            {PokemonId.Pinsir, new TransferFilter(1750, 98, 1)},
            {PokemonId.Tauros, new TransferFilter(500, 90, 1)},
            {PokemonId.Gyarados, new TransferFilter(1750, 90, 1)},
            {PokemonId.Lapras, new TransferFilter(2000, 90, 1)},
            {PokemonId.Eevee, new TransferFilter(1500, 98, 1)},
            {PokemonId.Vaporeon, new TransferFilter(2000, 98, 1)},
            {PokemonId.Jolteon, new TransferFilter(2000, 95, 1)},
            {PokemonId.Flareon, new TransferFilter(2000, 95, 1)},
            {PokemonId.Porygon, new TransferFilter(1500, 95, 1)},
            {PokemonId.Aerodactyl, new TransferFilter(1750, 95, 1)},
            {PokemonId.Snorlax, new TransferFilter(2500, 96, 1)},
            {PokemonId.Dragonite, new TransferFilter(2500, 90, 1)}
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
                PokemonId.Venusaur,
                PokemonId.Charizard,
                PokemonId.Blastoise,
                PokemonId.Beedrill,
                PokemonId.Raichu,
                PokemonId.Sandslash,
                PokemonId.Nidoking,
                PokemonId.Nidoqueen,
                PokemonId.Clefable,
                PokemonId.Ninetales,
                PokemonId.Golbat,
                PokemonId.Vileplume,
                PokemonId.Golduck,
                PokemonId.Primeape,
                PokemonId.Arcanine,
                PokemonId.Poliwrath,
                PokemonId.Alakazam,
                PokemonId.Machamp,
                PokemonId.Golem,
                PokemonId.Rapidash,
                PokemonId.Slowbro,
                PokemonId.Farfetchd,
                PokemonId.Muk,
                PokemonId.Cloyster,
                PokemonId.Gengar,
                PokemonId.Exeggutor,
                PokemonId.Marowak,
                PokemonId.Hitmonchan,
                PokemonId.Lickitung,
                PokemonId.Rhydon,
                PokemonId.Chansey,
                PokemonId.Kangaskhan,
                PokemonId.Starmie,
                PokemonId.MrMime,
                PokemonId.Scyther,
                PokemonId.Magmar,
                PokemonId.Electabuzz,
                PokemonId.Magmar,
                PokemonId.Jynx,
                PokemonId.Gyarados,
                PokemonId.Lapras,
                PokemonId.Ditto,
                PokemonId.Vaporeon,
                PokemonId.Jolteon,
                PokemonId.Flareon,
                PokemonId.Porygon,
                PokemonId.Kabutops,
                PokemonId.Aerodactyl,
                PokemonId.Snorlax,
                PokemonId.Articuno,
                PokemonId.Zapdos,
                PokemonId.Moltres,
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
            }
            else
            {
                settings = new GlobalSettings();
            }

            if (settings.WebSocketPort == 0)
            {
                settings.WebSocketPort = 14251;
            }

            if (settings.PokemonToSnipe == null)
            {
                settings.PokemonToSnipe = Default.PokemonToSnipe;
            }

            if (settings.RenameTemplate == null)
            {
                settings.RenameTemplate = Default.RenameTemplate;
            }

            if (settings.SnipeLocationServer == null)
            {
                settings.SnipeLocationServer = Default.SnipeLocationServer;
            }

            settings.GeneralConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "config");
            settings.ProfilePath = profilePath;
            settings.ProfileConfigPath = profileConfigPath;

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
                return _settings.DefaultLatitude + _rand.NextDouble() * ((double)_settings.MaxSpawnLocationOffset / 111111);
            }

            set { _settings.DefaultLatitude = value; }
        }

        double ISettings.DefaultLongitude
        {
            get
            {
                return _settings.DefaultLongitude +
                       _rand.NextDouble() *
                       ((double)_settings.MaxSpawnLocationOffset / 111111 / Math.Cos(_settings.DefaultLatitude));
            }

            set { _settings.DefaultLongitude = value; }
        }

        double ISettings.DefaultAltitude
        {
            get { return _settings.DefaultAltitude; }

            set { _settings.DefaultAltitude = value; }
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

        bool ISettings.UseProxy
        {
            get
            {
                return _settings.UseProxy;
            }

            set
            {
                _settings.UseProxy = value;
            }
        }
        string ISettings.ProxyUri
        {
            get
            {
                return _settings.ProxyUri;
            }

            set
            {
                _settings.ProxyUri = value;
            }
        }
        string ISettings.ProxyLogin
        {
            get
            {
                return _settings.ProxyLogin;
            }

            set
            {
                _settings.ProxyLogin = value;
            }
        }
        string ISettings.ProxyPass
        {
            get
            {
                return _settings.ProxyPass;
            }

            set
            {
                _settings.ProxyPass = value;
            }
        }
#region Device Config Values

        string DevicePackageName
        {
            get { return _settings.Auth.DevicePackageName; }
            set { _settings.Auth.DevicePackageName = value; }
        }
        string ISettings.DeviceId
        {
            get { return _settings.Auth.DeviceId; }
            set { _settings.Auth.DeviceId = value; }
        }
        string ISettings.AndroidBoardName
        {
            get { return _settings.Auth.AndroidBoardName; }
            set { _settings.Auth.AndroidBoardName = value; }
        }
        string ISettings.AndroidBootloader
        {
            get { return _settings.Auth.AndroidBootloader; }
            set { _settings.Auth.AndroidBootloader = value; }
        }
        string ISettings.DeviceBrand
        {
            get { return _settings.Auth.DeviceBrand; }
            set { _settings.Auth.DeviceBrand = value; }
        }
        string ISettings.DeviceModel
        {
            get { return _settings.Auth.DeviceModel; }
            set { _settings.Auth.DeviceModel = value; }
        }
        string ISettings.DeviceModelIdentifier
        {
            get { return _settings.Auth.DeviceModelIdentifier; }
            set { _settings.Auth.DeviceModelIdentifier = value; }
        }
        string ISettings.DeviceModelBoot
        {
            get { return _settings.Auth.DeviceModelBoot; }
            set { _settings.Auth.DeviceModelBoot = value; }
        }
        string ISettings.HardwareManufacturer
        {
            get { return _settings.Auth.HardwareManufacturer; }
            set { _settings.Auth.HardwareManufacturer = value; }
        }
        string ISettings.HardwareModel
        {
            get { return _settings.Auth.HardwareModel; }
            set { _settings.Auth.HardwareModel = value; }
        }
        string ISettings.FirmwareBrand
        {
            get { return _settings.Auth.FirmwareBrand; }
            set { _settings.Auth.FirmwareBrand = value; }
        }
        string ISettings.FirmwareTags
        {
            get { return _settings.Auth.FirmwareTags; }
            set { _settings.Auth.FirmwareTags = value; }
        }
        string ISettings.FirmwareType
        {
            get { return _settings.Auth.FirmwareType; }
            set { _settings.Auth.FirmwareType = value; }
        }
        string ISettings.FirmwareFingerprint
        {
            get { return _settings.Auth.FirmwareFingerprint; }
            set { _settings.Auth.FirmwareFingerprint = value; }
        }

        #endregion Device Config Values

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
        public int SnipeRequestTimeoutSeconds => _settings.SnipeRequestTimeoutSeconds*1000;
        public string GeneralConfigPath => _settings.GeneralConfigPath;
        public bool AutoUpdate => _settings.AutoUpdate;
        public bool TransferConfigAndAuthOnUpdate => _settings.TransferConfigAndAuthOnUpdate;
        public float KeepMinIvPercentage => _settings.KeepMinIvPercentage;
        public int KeepMinCp => _settings.KeepMinCp;
        public bool AutomaticallyLevelUpPokemon => _settings.AutomaticallyLevelUpPokemon;
        public string LevelUpByCPorIv => _settings.LevelUpByCPorIv;
        public float UpgradePokemonIvMinimum => _settings.UpgradePokemonIvMinimum;
        public float UpgradePokemonCpMinimum => _settings.UpgradePokemonCpMinimum;
        public double WalkingSpeedInKilometerPerHour => _settings.WalkingSpeedInKilometerPerHour;
        public bool EvolveAllPokemonWithEnoughCandy => _settings.EvolveAllPokemonWithEnoughCandy;
        public bool KeepPokemonsThatCanEvolve => _settings.KeepPokemonsThatCanEvolve;
        public bool TransferDuplicatePokemon => _settings.TransferDuplicatePokemon;
        public bool UseEggIncubators => _settings.UseEggIncubators;
        public bool AlwaysPrefferLongDistanceEgg => _settings.AlwaysPrefferLongDistanceEgg;
        public int UseGreatBallAboveIv => _settings.UseGreatBallAboveIv;
        public int UseUltraBallAboveIv => _settings.UseUltraBallAboveIv;
        public double UseMasterBallBelowCatchProbability => _settings.UseMasterBallBelowCatchProbability;
        public double UseUltraBallBelowCatchProbability => _settings.UseUltraBallBelowCatchProbability;
        public double UseGreatBallBelowCatchProbability => _settings.UseGreatBallBelowCatchProbability;
        public int DelayBetweenPokemonCatch => _settings.DelayBetweenPokemonCatch;
        public int DelayBetweenPlayerActions => _settings.DelayBetweenPlayerActions;
        public bool UsePokemonToNotCatchFilter => _settings.UsePokemonToNotCatchFilter;
        public int KeepMinDuplicatePokemon => _settings.KeepMinDuplicatePokemon;
        public bool PrioritizeIvOverCp => _settings.PrioritizeIvOverCp;
        public int MaxTravelDistanceInMeters => _settings.MaxTravelDistanceInMeters;
        public string GpxFile => _settings.GpxFile;
        public bool UseGpxPathing => _settings.UseGpxPathing;
        public bool UseLuckyEggsWhileEvolving => _settings.UseLuckyEggsWhileEvolving;
        public int UseLuckyEggsMinPokemonAmount => _settings.UseLuckyEggsMinPokemonAmount;
        public bool EvolveAllPokemonAboveIv => _settings.EvolveAllPokemonAboveIv;
        public float EvolveAboveIvValue => _settings.EvolveAboveIvValue;
        public bool RenamePokemon => _settings.RenamePokemon;
        public bool RenameOnlyAboveIv => _settings.RenameOnlyAboveIv;
        public float FavoriteMinIvPercentage => _settings.FavoriteMinIvPercentage;
        public bool AutoFavoritePokemon => _settings.AutoFavoritePokemon;
        public string RenameTemplate => _settings.RenameTemplate;
        public int AmountOfPokemonToDisplayOnStart => _settings.AmountOfPokemonToDisplayOnStart;
        public bool DumpPokemonStats => _settings.DumpPokemonStats;
        public string TranslationLanguageCode => _settings.TranslationLanguageCode;
        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter => _settings.ItemRecycleFilter;
        public ICollection<PokemonId> PokemonsToEvolve => _settings.PokemonsToEvolve;
        public ICollection<PokemonId> PokemonsNotToTransfer => _settings.PokemonsNotToTransfer;
        public ICollection<PokemonId> PokemonsNotToCatch => _settings.PokemonsToIgnore;
        public ICollection<PokemonId> PokemonToUseMasterball => _settings.PokemonToUseMasterball;
        public Dictionary<PokemonId, TransferFilter> PokemonsTransferFilter => _settings.PokemonsTransferFilter;
        public bool StartupWelcomeDelay => _settings.StartupWelcomeDelay;
        public bool SnipeAtPokestops => _settings.SnipeAtPokestops;
        public int MinPokeballsToSnipe => _settings.MinPokeballsToSnipe;
        public int MinPokeballsWhileSnipe => _settings.MinPokeballsWhileSnipe;
        public int MaxPokeballsPerPokemon => _settings.MaxPokeballsPerPokemon;
        public SnipeSettings PokemonToSnipe => _settings.PokemonToSnipe;
        public string SnipeLocationServer => _settings.SnipeLocationServer;
        public int SnipeLocationServerPort => _settings.SnipeLocationServerPort;
        public bool UseSnipeLocationServer => _settings.UseSnipeLocationServer;
        public bool UsePokeSnipersLocationServer => _settings.UsePokeSnipersLocationServer;
        public bool UseTransferIvForSnipe => _settings.UseTransferIvForSnipe;
        public bool SnipeIgnoreUnknownIv => _settings.SnipeIgnoreUnknownIv;
        public int MinDelayBetweenSnipes => _settings.MinDelayBetweenSnipes;
        public double SnipingScanOffset => _settings.SnipingScanOffset;
        public int TotalAmountOfPokeballsToKeep => _settings.TotalAmountOfPokeballsToKeep;
        public int TotalAmountOfBerriesToKeep => _settings.TotalAmountOfBerriesToKeep;
        public int TotalAmountOfPotionsToKeep => _settings.TotalAmountOfPotionsToKeep;
        public int TotalAmountOfRevivesToKeep => _settings.TotalAmountOfRevivesToKeep;
        public bool Teleport => _settings.Teleport;
        public int DelayCatchIncensePokemon => _settings.DelayCatchIncensePokemon;
        public int DelayCatchNearbyPokemon => _settings.DelayCatchNearbyPokemon;
        public int DelayPositionCheckState => _settings.DelayPositionCheckState;
        public int DelayCatchLurePokemon => _settings.DelayCatchLurePokemon;
        public int DelayCatchPokemon => _settings.DelayCatchPokemon;
        public int DelayDisplayPokemon => _settings.DelayDisplayPokemon;
        public int DelayUseLuckyEgg => _settings.DelayUseLuckyEgg;
        public int DelaySoftbanRetry => _settings.DelaySoftbanRetry;
        public int DelayPokestop => _settings.DelayPokestop;
        public int DelayRecyleItem => _settings.DelayRecyleItem;
        public int DelaySnipePokemon => _settings.DelaySnipePokemon;
        public int DelayTransferPokemon => _settings.DelayTransferPokemon;

        public bool UseDiscoveryPathing => _settings.UseDiscoveryPathing;
        public int DelayEvolvePokemon => _settings.DelayEvolvePokemon;
        public double DelayEvolveVariation => _settings.DelayEvolveVariation;
        public double RecycleInventoryAtUsagePercentage => _settings.RecycleInventoryAtUsagePercentage;
        public bool HumanizeThrows => _settings.HumanizeThrows;
        public double ThrowAccuracyMin => _settings.ThrowAccuracyMin;
        public double ThrowAccuracyMax => _settings.ThrowAccuracyMax;
        public double ThrowSpinFrequency => _settings.ThrowSpinFrequency;
        public int UseBerryMinCp => _settings.UseBerryMinCp;
        public float UseBerryMinIv => _settings.UseBerryMinIv;
        public double UseBerryBelowCatchProbability => _settings.UseBerryBelowCatchProbability;

    }
}
