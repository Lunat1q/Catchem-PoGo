using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGo.PokeMobBot.Logic.Utils
{
    internal class PhoneGenerator
    {
        // data from https://conf.skype.com/whitelist26.txt
        private List<IDictionary<string, string>> GetPhoneList()
        {
            List<IDictionary<string, string>> phone_list = new List<IDictionary<string, string>>();


            IDictionary<string, string> phone_item = new Dictionary<string, string>();


            // ******* Samsung ******* //

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Nexus S";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "herring";
            phone_item["model"] = "Nexus S";
            phone_item["product"] = "soju.*";
            phone_item["device"] = "crespo.*";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Galaxy Tab 10.1 (Wifi)";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "samsung";
            phone_item["board"] = "GT-P7510";
            phone_item["model"] = "GT-P7510";
            phone_item["product"] = "GT-P7510";
            phone_item["device"] = "GT-P7510";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Galaxy Nexus";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "google";
            phone_item["board"] = "tuna";
            phone_item["model"] = "Galaxy Nexus";
            phone_item["product"] = "mysid";
            phone_item["device"] = "toro";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Galaxy Nexus";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "google";
            phone_item["board"] = "tuna";
            phone_item["model"] = "Galaxy Nexus";
            phone_item["product"] = "yakju";
            phone_item["device"] = "maguro";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Galaxy S 4G";
            phone_item["mft"] = "Samsung";
            phone_item["board"] = "TMOUS";
            phone_item["board"] = "SGH-T959V";
            phone_item["model"] = "SGH-T959V";
            phone_item["product"] = "SGH-T959V";
            phone_item["device"] = "SGH-T959V";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Galaxy S";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "sprint";
            phone_item["board"] = "GT-I9000.*";
            phone_item["model"] = "GT-I9000.*";
            phone_item["product"] = "GT-I9000.*";
            phone_item["device"] = "GT-I9000.*";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Galaxy S Fascinate";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "verizon";
            phone_item["board"] = "SCH-I500";
            phone_item["model"] = "SCH-I500";
            phone_item["product"] = "SCH-I500";
            phone_item["device"] = "SCH-I500";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Droid Charge";
            phone_item["mft"] = "Samsung";
            phone_item["board"] = "verizon";
            phone_item["board"] = "SCH-I510";
            phone_item["model"] = "SCH-I510";
            phone_item["product"] = "SCH-I510";
            phone_item["device"] = "SCH-I510";


            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Galaxy S II";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "samsung";
            phone_item["board"] = "GT-I9100";
            phone_item["model"] = "GT-I9100";
            phone_item["product"] = "GT-I9100";
            phone_item["device"] = "GT-I9100";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Galaxy S II (Sprint)";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "samsung";
            phone_item["board"] = "SPH-D710";
            phone_item["model"] = "SPH-D710";
            phone_item["product"] = "SPH-D710";
            phone_item["device"] = "SPH-D710";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Galaxy Tab 7 WIFI";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "samsung";
            phone_item["board"] = "GT-P10.*";
            phone_item["model"] = "GT-P10.*";
            phone_item["product"] = "GT-P10.*";
            phone_item["device"] = "GT-P10.*";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Galaxy Tab 7 Verizon";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "verizon";
            phone_item["board"] = "SCH-I800";
            phone_item["model"] = "SCH-I800";
            phone_item["product"] = "SCH-I800";
            phone_item["device"] = "SCH-I800";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Samsung Galaxy Tab 7 Sprint";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "sprint";
            phone_item["board"] = "SPH-P100";
            phone_item["model"] = "SPH-P100";
            phone_item["product"] = "SPH-P100";
            phone_item["device"] = "SPH-P100";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Galaxy Tab 10.1 (T-Mo)";
            phone_item["mft"] = "samsung";
            phone_item["board"] = "samsung";
            phone_item["board"] = "SGH-T859";
            phone_item["model"] = "SGH-T859";
            phone_item["product"] = "SGH-T859";
            phone_item["device"] = "SGH-T859";
            phone_list.Add(phone_item);


            // ******* HTC ******* //

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Nexus One";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "google";
            phone_item["board"] = "mahimahi";
            phone_item["model"] = "Nexus One";
            phone_item["product"] = "passion";
            phone_item["device"] = "passion";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Amaze 4G";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "telus_wwe";
            phone_item["board"] = "ruby";
            phone_item["model"] = "HTC Ruby";
            phone_item["product"] = "htc_ruby";
            phone_item["device"] = "ruby";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Desire";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "bravo";
            phone_item["model"] = "HTC Desire";
            phone_item["product"] = "htc_bravo";
            phone_item["device"] = "bravo";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Desire S";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "saga";
            phone_item["model"] = "HTC Desire S";
            phone_item["product"] = "htc_saga";
            phone_item["device"] = "saga";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Incredible S";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "htc_wwe";
            phone_item["board"] = "vivo";
            phone_item["model"] = "HTC Incredible S";
            phone_item["product"] = "htc_vivo";
            phone_item["device"] = "vivo";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Desire HD";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "htc_wwe";
            phone_item["board"] = "spade";
            phone_item["model"] = "Desire HD";
            phone_item["product"] = "htc_ace";
            phone_item["device"] = "ace";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC EVO 4G";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "sprint";
            phone_item["board"] = "supersonic";
            phone_item["model"] = "PC36100";
            phone_item["product"] = "htc_supersonic";
            phone_item["device"] = "supersonic";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC EVO 3D";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "sprint";
            phone_item["board"] = "shooter.*";
            phone_item["model"] = "PG86100";
            phone_item["product"] = "htc_shooter.*";
            phone_item["device"] = "shooter.*";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Sensation 4G";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "tmous";
            phone_item["board"] = "pyramid";
            phone_item["model"] = "HTC Sensation 4G";
            phone_item["product"] = "htc_pyramid";
            phone_item["device"] = "pyramid";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Thunderbolt";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "verizon_wwe";
            phone_item["board"] = "mecha";
            phone_item["model"] = "ADR6400L";
            phone_item["product"] = "htc_mecha";
            phone_item["device"] = "mecha";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Flyer Wifi HC";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "HTC";
            phone_item["board"] = "flyer";
            phone_item["model"] = "HTC P510e";
            phone_item["product"] = "htc_flyer";
            phone_item["device"] = "flyer";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Flyer Wifi";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "HTC";
            phone_item["board"] = "flyer";
            phone_item["model"] = "HTC P510e";
            phone_item["product"] = "htc_flyer";
            phone_item["device"] = "flyer";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Flyer";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "htc_wwe_wifi";
            phone_item["board"] = "flyer";
            phone_item["model"] = "HTC Flyer P512";
            phone_item["product"] = "htc_flyer";
            phone_item["device"] = "flyer";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "HTC Flyer Wifi 2";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "HTC";
            phone_item["board"] = "flyer";
            phone_item["model"] = "HTC Flyer";
            phone_item["product"] = "htc_flyer";
            phone_item["device"] = "flyer";


            phone_list.Add(phone_item);


            // ******* Lenovo ******* //

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Lenovo IdeaPad K1";
            phone_item["mft"] = "LENOVO";
            phone_item["board"] = "LENOVO";
            phone_item["board"] = "ventana";
            phone_item["model"] = "K1";
            phone_item["product"] = "IdeaPad_Tablet_K1";
            phone_item["device"] = "K1";
            phone_list.Add(phone_item);


            // ******* MOTOROLA ******* //   
            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Droid 4";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "verizon";
            phone_item["board"] = "maserati";
            phone_item["model"] = "DROID4";
            phone_item["product"] = "maserati_vzw";
            phone_item["device"] = "cdma_maserati";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Droid RAZR Verizon";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "verizon";
            phone_item["board"] = "spyder";
            phone_item["model"] = "DROID RAZR";
            phone_item["product"] = "spyder_vzw";
            phone_item["device"] = "cdma_spyder";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Droid RAZR";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "MOTO";
            phone_item["board"] = "spyder";
            phone_item["model"] = "XT910";
            phone_item["product"] = "XT910_O2GB";
            phone_item["device"] = "umts_spyder";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Xoom2";
            phone_item["mft"] = "Motorola";
            phone_item["board"] = "Motorola";
            phone_item["board"] = "ventana";
            phone_item["model"] = "MZ505";
            phone_item["product"] = "MZ505";
            phone_item["device"] = "Graham";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Atrix 2";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "MOTO";
            phone_item["board"] = "p3";
            phone_item["model"] = "MB865";
            phone_item["product"] = "edison_att_us";
            phone_item["device"] = "edison";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Atrix";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "MOTO";
            phone_item["board"] = "olympus";
            phone_item["model"] = "MB860";
            phone_item["product"] = "oly.*";
            phone_item["device"] = "olympus";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Photon";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "sprint";
            phone_item["board"] = "sunfire";
            phone_item["model"] = "MB855";
            phone_item["product"] = "moto_sunfire";
            phone_item["device"] = "sunfire";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Droid 3";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "verizon";
            phone_item["board"] = "solana";
            phone_item["model"] = "DROID3";
            phone_item["product"] = "solana_vzw";
            phone_item["device"] = "cdma_solana";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Bionic";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "verizon";
            phone_item["board"] = "targa";
            phone_item["model"] = "DROID BIONIC";
            phone_item["product"] = "targa_vzw";
            phone_item["device"] = "cdma_targa";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Xoom";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "verizon";
            phone_item["board"] = "unknown";
            phone_item["model"] = "Xoom";
            phone_item["product"] = "trygon";
            phone_item["device"] = "stingray";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Pasteur";
            phone_item["mft"] = "Motorola";
            phone_item["board"] = "verizon";
            phone_item["board"] = "pasteur";
            phone_item["model"] = "MZ617";
            phone_item["product"] = "pasteur";
            phone_item["device"] = "pasteur";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Motorola Fleming";
            phone_item["mft"] = "Motorola";
            phone_item["board"] = "Motorola";
            phone_item["board"] = "fleming";
            phone_item["model"] = "XOOM 2 ME";
            phone_item["product"] = "RTCOREEU";
            phone_item["device"] = "fleming";
            phone_list.Add(phone_item);


            // ******* Sony Ericsson ******* //

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Sony Ericsson Xperia Neo";
            phone_item["mft"] = "Sony Ericsson";
            phone_item["board"] = "SEMC";
            phone_item["board"] = "unknown";
            phone_item["model"] = "MT15[ai]";
            phone_item["product"] = "MT15[ai]_.*";
            phone_item["device"] = "MT15[ai]";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Sony Ericsson Xperia Pro";
            phone_item["mft"] = "Sony Ericsson";
            phone_item["board"] = "SEMC";
            phone_item["board"] = "unknown";
            phone_item["model"] = "MK16[ai]";
            phone_item["product"] = "MK16[ai]_.*";
            phone_item["device"] = "MK16[ai]";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Sony Ericsson Xperia Play ROW";
            phone_item["mft"] = "Sony Ericsson";
            phone_item["board"] = "SEMC";
            phone_item["board"] = "unknown";
            phone_item["model"] = "R800.*";
            phone_item["product"] = "R800.*";
            phone_item["device"] = "R800.*";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Sony Ericsson Xperia Play China";
            phone_item["mft"] = "Sony Ericsson";
            phone_item["board"] = "SEMC";
            phone_item["board"] = "unknown";
            phone_item["model"] = "Z1.*";
            phone_item["product"] = "Z1.*";
            phone_item["device"] = "Z1.*";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Sony Ericsson Xperia Ray";
            phone_item["mft"] = "Sony Ericsson";
            phone_item["board"] = "SEMC";
            phone_item["board"] = "unknown";
            phone_item["model"] = "ST18[ai]";
            phone_item["product"] = "ST18[ai]_.*";
            phone_item["device"] = "ST18[ai]";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Sony Ericsson Xperia Mini Pro2";
            phone_item["mft"] = "Sony Ericsson";
            phone_item["board"] = "SEMC";
            phone_item["board"] = "unknown";
            phone_item["model"] = "SK17[ai]";
            phone_item["product"] = "SK17[ai]_.*";
            phone_item["device"] = "SK17[ai]";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Sony Ericsson Xperia Walkman";
            phone_item["mft"] = "Sony Ericsson";
            phone_item["board"] = "SEMC";
            phone_item["board"] = "unknown";
            phone_item["model"] = "WT19[ai]";
            phone_item["product"] = "WT19[ai]_.*";
            phone_item["device"] = "WT19[ai]";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Sony Ericsson Xperia NeoV";
            phone_item["mft"] = "Sony Ericsson";
            phone_item["board"] = "SEMC";
            phone_item["board"] = "unknown";
            phone_item["model"] = "MT11[ai]";
            phone_item["product"] = "MT11[ai]_.*";
            phone_item["device"] = "MT11[ai]";
            phone_list.Add(phone_item);

            // ******* Acer ******* //

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Acer A5"; phone_item["mft"] = "Acer";
            phone_item["board"] = "acer";
            phone_item["board"] = "jazz";
            phone_item["model"] = "S300";
            phone_item["product"] = "a5_generic.*";
            phone_item["device"] = "a5";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "Acer Iconia Tablet";
            phone_item["mft"] = "Acer";
            phone_item["board"] = "acer";
            phone_item["board"] = "picasso";
            phone_item["model"] = "A500";
            phone_item["product"] = "picasso_comgen.*";
            phone_item["device"] = "picasso";
            phone_list.Add(phone_item);


            // ******* LG ******* //
            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "LG Revolution";
            phone_item["mft"] = "LGE";
            phone_item["board"] = "Verizon";
            phone_item["board"] = "bryce";
            phone_item["model"] = "VS910 4G";
            phone_item["product"] = "bryce";
            phone_item["device"] = "bryce";

            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "LG Optimus Black";
            phone_item["mft"] = "lge";
            phone_item["board"] = "lge";
            phone_item["board"] = "lgp970";
            phone_item["model"] = "LG-P970";
            phone_item["product"] = "lge_bprj";
            phone_item["device"] = "lgp970";


            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "LG Optimus 3D";
            phone_item["mft"] = "LGE";
            phone_item["board"] = "lge";
            phone_item["board"] = "omap4sdp";
            phone_item["model"] = "LG-P920";
            phone_item["product"] = "lge_Cosmopolitan";
            phone_item["device"] = "p920";


            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "LG Optimus 2x";
            phone_item["mft"] = "lge";
            phone_item["board"] = "lge";
            phone_item["board"] = "p990";
            phone_item["model"] = "LG-P990";
            phone_item["product"] = "lge_star";
            phone_item["device"] = "p990";


            phone_list.Add(phone_item);


            // ******* ASUS ******* //
            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "ASUS Transfomer Prime";
            phone_item["mft"] = "asus";
            phone_item["board"] = "asus";
            phone_item["board"] = "EeePad";
            phone_item["model"] = "Transformer Prime TF201";
            phone_item["product"] = "TW_epad";
            phone_item["device"] = "TF201";
            phone_list.Add(phone_item);


            // ******* KDDI ******* //
            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "ISW11M";
            phone_item["mft"] = "motorola";
            phone_item["board"] = "KDDI";
            phone_item["board"] = "sunfire";
            phone_item["model"] = "ISW11M";
            phone_item["product"] = "MOI11";
            phone_item["device"] = "sunfire";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "IS05";
            phone_item["mft"] = "SHARP";
            phone_item["board"] = "KDDI";
            phone_item["board"] = "SHI05";
            phone_item["model"] = "IS05";
            phone_item["product"] = "SHI05";
            phone_item["device"] = "SHI05";
            phone_list.Add(phone_item);

            phone_item = new Dictionary<string, string>();
            phone_item["name"] = "ISW12HT";
            phone_item["mft"] = "HTC";
            phone_item["board"] = "KDDI";
            phone_item["board"] = "shooterk";
            phone_item["model"] = "ISW12HT";
            phone_item["product"] = "HTI12";
            phone_item["device"] = "shooterk";
            phone_list.Add(phone_item);

            return phone_list;

        }

        public PhoneGenerator()
        {
            List<IDictionary<string, string>> phone_list = GetPhoneList();
            Random rnd = new Random();

            int phone_index = rnd.Next(0, phone_list.Count);
            IDictionary<string, string> phone_item = phone_list[phone_index];

        }

    }
}
