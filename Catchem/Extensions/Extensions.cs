using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using PoGo.PokeMobBot.Logic;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

namespace Catchem.Extensions
{
    public static class Extensions
    {
        public static GlobalSettings Clone(this GlobalSettings gs)
        {
            return gs.CloneJson();
        }

        public static T CloneJson<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null))
            {
                return default(T);
            }

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
        }

        public static void CloneProperties<T>(this T from, T to)
        {
            var objType = from.GetType();
            foreach (var property in objType.GetProperties())
            {
                if (property.PropertyType == objType) continue;
                if (property.PropertyType.IsMyInterface() && property.PropertyType.IsClass)
                {
                    var nextObjFrom = property.GetValue(from);
                    var nextObjTo = property.GetValue(to);
                    CloneProperties(nextObjFrom, nextObjTo);
                    CloneFields(nextObjFrom, nextObjTo);
                    continue;
                }
                if (property.SetMethod != null)
                {
                    var value = property.GetValue(from);
                    property.SetValue(to, value);
                }
            }
        }
        public static void CloneFields<T>(this T from, T to)
        {
            var objType = from.GetType();
            foreach (var property in objType.GetFields())
            {
                if (property.FieldType == objType) continue;
                if (property.FieldType.IsMyInterface() && property.FieldType.IsClass)
                {
                    var nextObjFrom = property.GetValue(from);
                    var nextObjTo = property.GetValue(to);
                    nextObjFrom.CloneProperties(nextObjTo);
                    nextObjFrom.CloneFields(nextObjTo);
                    continue;
                }
                var value = property.GetValue(from);
                property.SetValue(to, value);
            }
        }


        internal static bool IsMyInterface(this Type propertyType)
        {
            return propertyType.Assembly.GetName().Name != "mscorlib";
        }
       
        
        
        public static System.Windows.Controls.Image ToImage(this PokemonId pid)
        {
            var img = new System.Windows.Controls.Image {Source = pid.ToBitmap().LoadBitmap()};
            var tt = new ToolTip { Content = $"{pid} ({DateTime.Now.ToString("HH:mm:ss")})" };
            img.ToolTip = tt;
            return img;
        }

        public static System.Windows.Controls.Image ToInventoryImage(this PokemonId pid)
        {
            var img = new System.Windows.Controls.Image { Source = pid.ToInventoryBitmap().LoadBitmap() };
            var tt = new ToolTip { Content = pid.ToString() };
            img.ToolTip = tt;
            return img;
        }

        public static BitmapSource ToSource(this PokemonId pid)
        {
            return pid.ToBitmap().LoadBitmap();
        }

        public static BitmapSource ToInventorySource(this PokemonId pid)
        {
            return pid.ToInventoryBitmap().LoadBitmap();
        }
        public static BitmapSource ToInventorySource(this PokemonType pid)
        {
            return pid.ToInventoryBitmap().LoadBitmap();
        }
        public static BitmapSource ToInventorySource(this ItemId pid)
        {
            return pid.ToInventoryItem().LoadBitmap();
        }

        public static System.Windows.Controls.Image ToImage(this Bitmap source, string toolTipText = "no_text")
        {
            var img = new System.Windows.Controls.Image {Source = source.LoadBitmap()};
            var tt = new ToolTip {Content = toolTipText};
            img.ToolTip = tt;
            return img;
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource LoadBitmap(this Bitmap source)
        {
            var ip = source.GetHbitmap();
            BitmapSource bs;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }

        public static Bitmap ToInventoryBitmap(this PokemonType pid)
        {
            switch (pid)
            {
                case PokemonType.None:
                    return Properties.Resources.no_name;
                case PokemonType.Normal:
                    return Properties.Resources.Normal_inv;
                case PokemonType.Fighting:
                    return Properties.Resources.Fighting_inv;
                case PokemonType.Flying:
                    return Properties.Resources.Flying_inv;
                case PokemonType.Poison:
                    return Properties.Resources.Poison_inv;
                case PokemonType.Ground:
                    return Properties.Resources.Ground_inv;
                case PokemonType.Rock:
                    return Properties.Resources.Rock_inv;
                case PokemonType.Bug:
                    return Properties.Resources.Bug_inv;
                case PokemonType.Ghost:
                    return Properties.Resources.Ghost_inv;
                case PokemonType.Steel:
                    return Properties.Resources.Steel_inv;
                case PokemonType.Fire:
                    return Properties.Resources.Fire_inv;
                case PokemonType.Water:
                    return Properties.Resources.Water_inv;
                case PokemonType.Grass:
                    return Properties.Resources.Grass_inv;
                case PokemonType.Electric:
                    return Properties.Resources.Electric_inv;
                case PokemonType.Psychic:
                    return Properties.Resources.Psychic_inv;
                case PokemonType.Ice:
                    return Properties.Resources.Ice_inv;
                case PokemonType.Dragon:
                    return Properties.Resources.Dragon_inv;
                case PokemonType.Dark:
                    return Properties.Resources.Dark_inv;
                case PokemonType.Fairy:
                    return Properties.Resources.Fairy_inv;
            }
            return Properties.Resources.no_name;
        }

        public static Bitmap ToBitmap(this PokemonId pid)
        {
            switch (pid)
            {
                case PokemonId.Missingno:
                    return Properties.Resources.no_name;
                case PokemonId.Bulbasaur:
                    return Properties.Resources._1;
                case PokemonId.Ivysaur:
                    return Properties.Resources._2;
                case PokemonId.Venusaur:
                    return Properties.Resources._3;
                case PokemonId.Charmander:
                    return Properties.Resources._4;
                case PokemonId.Charmeleon:
                    return Properties.Resources._5;
                case PokemonId.Charizard:
                    return Properties.Resources._6;
                case PokemonId.Squirtle:
                    return Properties.Resources._7;
                case PokemonId.Wartortle:
                    return Properties.Resources._8;
                case PokemonId.Blastoise:
                    return Properties.Resources._9;
                case PokemonId.Caterpie:
                    return Properties.Resources._10;
                case PokemonId.Metapod:
                    return Properties.Resources._11;
                case PokemonId.Butterfree:
                    return Properties.Resources._12;
                case PokemonId.Weedle:
                    return Properties.Resources._13;
                case PokemonId.Kakuna:
                    return Properties.Resources._14;
                case PokemonId.Beedrill:
                    return Properties.Resources._15;
                case PokemonId.Pidgey:
                    return Properties.Resources._16;
                case PokemonId.Pidgeotto:
                    return Properties.Resources._17;
                case PokemonId.Pidgeot:
                    return Properties.Resources._18;
                case PokemonId.Rattata:
                    return Properties.Resources._19;
                case PokemonId.Raticate:
                    return Properties.Resources._20;
                case PokemonId.Spearow:
                    return Properties.Resources._21;
                case PokemonId.Fearow:
                    return Properties.Resources._22;
                case PokemonId.Ekans:
                    return Properties.Resources._23;
                case PokemonId.Arbok:
                    return Properties.Resources._24;
                case PokemonId.Pikachu:
                    return Properties.Resources._25;
                case PokemonId.Raichu:
                    return Properties.Resources._26;
                case PokemonId.Sandshrew:
                    return Properties.Resources._27;
                case PokemonId.Sandslash:
                    return Properties.Resources._28;
                case PokemonId.NidoranFemale:
                    return Properties.Resources._29;
                case PokemonId.Nidorina:
                    return Properties.Resources._30;
                case PokemonId.Nidoqueen:
                    return Properties.Resources._31;
                case PokemonId.NidoranMale:
                    return Properties.Resources._32;
                case PokemonId.Nidorino:
                    return Properties.Resources._33;
                case PokemonId.Nidoking:
                    return Properties.Resources._34;
                case PokemonId.Clefairy:
                    return Properties.Resources._35;
                case PokemonId.Clefable:
                    return Properties.Resources._36;
                case PokemonId.Vulpix:
                    return Properties.Resources._37;
                case PokemonId.Ninetales:
                    return Properties.Resources._38;
                case PokemonId.Jigglypuff:
                    return Properties.Resources._39;
                case PokemonId.Wigglytuff:
                    return Properties.Resources._40;
                case PokemonId.Zubat:
                    return Properties.Resources._41;
                case PokemonId.Golbat:
                    return Properties.Resources._42;
                case PokemonId.Oddish:
                    return Properties.Resources._43;
                case PokemonId.Gloom:
                    return Properties.Resources._44;
                case PokemonId.Vileplume:
                    return Properties.Resources._45;
                case PokemonId.Paras:
                    return Properties.Resources._46;
                case PokemonId.Parasect:
                    return Properties.Resources._47;
                case PokemonId.Venonat:
                    return Properties.Resources._48;
                case PokemonId.Venomoth:
                    return Properties.Resources._49;
                case PokemonId.Diglett:
                    return Properties.Resources._50;
                case PokemonId.Dugtrio:
                    return Properties.Resources._51;
                case PokemonId.Meowth:
                    return Properties.Resources._52;
                case PokemonId.Persian:
                    return Properties.Resources._53;
                case PokemonId.Psyduck:
                    return Properties.Resources._54;
                case PokemonId.Golduck:
                    return Properties.Resources._55;
                case PokemonId.Mankey:
                    return Properties.Resources._56;
                case PokemonId.Primeape:
                    return Properties.Resources._57;
                case PokemonId.Growlithe:
                    return Properties.Resources._58;
                case PokemonId.Arcanine:
                    return Properties.Resources._59;
                case PokemonId.Poliwag:
                    return Properties.Resources._60;
                case PokemonId.Poliwhirl:
                    return Properties.Resources._61;
                case PokemonId.Poliwrath:
                    return Properties.Resources._62;
                case PokemonId.Abra:
                    return Properties.Resources._63;
                case PokemonId.Kadabra:
                    return Properties.Resources._64;
                case PokemonId.Alakazam:
                    return Properties.Resources._65;
                case PokemonId.Machop:
                    return Properties.Resources._66;
                case PokemonId.Machoke:
                    return Properties.Resources._67;
                case PokemonId.Machamp:
                    return Properties.Resources._68;
                case PokemonId.Bellsprout:
                    return Properties.Resources._69;
                case PokemonId.Weepinbell:
                    return Properties.Resources._70;
                case PokemonId.Victreebel:
                    return Properties.Resources._71;
                case PokemonId.Tentacool:
                    return Properties.Resources._72;
                case PokemonId.Tentacruel:
                    return Properties.Resources._73;
                case PokemonId.Geodude:
                    return Properties.Resources._74;
                case PokemonId.Graveler:
                    return Properties.Resources._75;
                case PokemonId.Golem:
                    return Properties.Resources._76;
                case PokemonId.Ponyta:
                    return Properties.Resources._77;
                case PokemonId.Rapidash:
                    return Properties.Resources._78;
                case PokemonId.Slowpoke:
                    return Properties.Resources._79;
                case PokemonId.Slowbro:
                    return Properties.Resources._80;
                case PokemonId.Magnemite:
                    return Properties.Resources._81;
                case PokemonId.Magneton:
                    return Properties.Resources._82;
                case PokemonId.Farfetchd:
                    return Properties.Resources._83;
                case PokemonId.Doduo:
                    return Properties.Resources._84;
                case PokemonId.Dodrio:
                    return Properties.Resources._85;
                case PokemonId.Seel:
                    return Properties.Resources._86;
                case PokemonId.Dewgong:
                    return Properties.Resources._87;
                case PokemonId.Grimer:
                    return Properties.Resources._88;
                case PokemonId.Muk:
                    return Properties.Resources._89;
                case PokemonId.Shellder:
                    return Properties.Resources._90;
                case PokemonId.Cloyster:
                    return Properties.Resources._91;
                case PokemonId.Gastly:
                    return Properties.Resources._92;
                case PokemonId.Haunter:
                    return Properties.Resources._93;
                case PokemonId.Gengar:
                    return Properties.Resources._94;
                case PokemonId.Onix:
                    return Properties.Resources._95;
                case PokemonId.Drowzee:
                    return Properties.Resources._96;
                case PokemonId.Hypno:
                    return Properties.Resources._97;
                case PokemonId.Krabby:
                    return Properties.Resources._98;
                case PokemonId.Kingler:
                    return Properties.Resources._99;
                case PokemonId.Voltorb:
                    return Properties.Resources._100;
                case PokemonId.Electrode:
                    return Properties.Resources._101;
                case PokemonId.Exeggcute:
                    return Properties.Resources._102;
                case PokemonId.Exeggutor:
                    return Properties.Resources._103;
                case PokemonId.Cubone:
                    return Properties.Resources._104;
                case PokemonId.Marowak:
                    return Properties.Resources._105;
                case PokemonId.Hitmonlee:
                    return Properties.Resources._106;
                case PokemonId.Hitmonchan:
                    return Properties.Resources._107;
                case PokemonId.Lickitung:
                    return Properties.Resources._108;
                case PokemonId.Koffing:
                    return Properties.Resources._109;
                case PokemonId.Weezing:
                    return Properties.Resources._110;
                case PokemonId.Rhyhorn:
                    return Properties.Resources._111;
                case PokemonId.Rhydon:
                    return Properties.Resources._112;
                case PokemonId.Chansey:
                    return Properties.Resources._113;
                case PokemonId.Tangela:
                    return Properties.Resources._114;
                case PokemonId.Kangaskhan:
                    return Properties.Resources._115;
                case PokemonId.Horsea:
                    return Properties.Resources._116;
                case PokemonId.Seadra:
                    return Properties.Resources._117;
                case PokemonId.Goldeen:
                    return Properties.Resources._118;
                case PokemonId.Seaking:
                    return Properties.Resources._119;
                case PokemonId.Staryu:
                    return Properties.Resources._120;
                case PokemonId.Starmie:
                    return Properties.Resources._121;
                case PokemonId.MrMime:
                    return Properties.Resources._122;
                case PokemonId.Scyther:
                    return Properties.Resources._123;
                case PokemonId.Jynx:
                    return Properties.Resources._124;
                case PokemonId.Electabuzz:
                    return Properties.Resources._125;
                case PokemonId.Magmar:
                    return Properties.Resources._126;
                case PokemonId.Pinsir:
                    return Properties.Resources._127;
                case PokemonId.Tauros:
                    return Properties.Resources._128;
                case PokemonId.Magikarp:
                    return Properties.Resources._129;
                case PokemonId.Gyarados:
                    return Properties.Resources._130;
                case PokemonId.Lapras:
                    return Properties.Resources._131;
                case PokemonId.Ditto:
                    return Properties.Resources._132;
                case PokemonId.Eevee:
                    return Properties.Resources._133;
                case PokemonId.Vaporeon:
                    return Properties.Resources._134;
                case PokemonId.Jolteon:
                    return Properties.Resources._135;
                case PokemonId.Flareon:
                    return Properties.Resources._136;
                case PokemonId.Porygon:
                    return Properties.Resources._137;
                case PokemonId.Omanyte:
                    return Properties.Resources._138;
                case PokemonId.Omastar:
                    return Properties.Resources._139;
                case PokemonId.Kabuto:
                    return Properties.Resources._140;
                case PokemonId.Kabutops:
                    return Properties.Resources._141;
                case PokemonId.Aerodactyl:
                    return Properties.Resources._142;
                case PokemonId.Snorlax:
                    return Properties.Resources._143;
                case PokemonId.Articuno:
                    return Properties.Resources._144;
                case PokemonId.Zapdos:
                    return Properties.Resources._145;
                case PokemonId.Moltres:
                    return Properties.Resources._146;
                case PokemonId.Dratini:
                    return Properties.Resources._147;
                case PokemonId.Dragonair:
                    return Properties.Resources._148;
                case PokemonId.Dragonite:
                    return Properties.Resources._149;
                case PokemonId.Mewtwo:
                    return Properties.Resources._150;
                case PokemonId.Mew:
                    return Properties.Resources._151;
            }
            return Properties.Resources.no_name;
        }

        public static Bitmap ToInventoryBitmap(this PokemonId pid)
        {
            switch (pid)
            {
                case PokemonId.Missingno:
                    return Properties.Resources.no_name;
                case PokemonId.Bulbasaur:
                    return Properties.Resources._001_inv;
                case PokemonId.Ivysaur:
                    return Properties.Resources._002_inv;
                case PokemonId.Venusaur:
                    return Properties.Resources._003_inv;
                case PokemonId.Charmander:
                    return Properties.Resources._004_inv;
                case PokemonId.Charmeleon:
                    return Properties.Resources._005_inv;
                case PokemonId.Charizard:
                    return Properties.Resources._006_inv;
                case PokemonId.Squirtle:
                    return Properties.Resources._007_inv;
                case PokemonId.Wartortle:
                    return Properties.Resources._008_inv;
                case PokemonId.Blastoise:
                    return Properties.Resources._009_inv;
                case PokemonId.Caterpie:
                    return Properties.Resources._010_inv;
                case PokemonId.Metapod:
                    return Properties.Resources._011_inv;
                case PokemonId.Butterfree:
                    return Properties.Resources._012_inv;
                case PokemonId.Weedle:
                    return Properties.Resources._013_inv;
                case PokemonId.Kakuna:
                    return Properties.Resources._014_inv;
                case PokemonId.Beedrill:
                    return Properties.Resources._015_inv;
                case PokemonId.Pidgey:
                    return Properties.Resources._016_inv;
                case PokemonId.Pidgeotto:
                    return Properties.Resources._017_inv;
                case PokemonId.Pidgeot:
                    return Properties.Resources._018_inv;
                case PokemonId.Rattata:
                    return Properties.Resources._019_inv;
                case PokemonId.Raticate:
                    return Properties.Resources._020_inv;
                case PokemonId.Spearow:
                    return Properties.Resources._021_inv;
                case PokemonId.Fearow:
                    return Properties.Resources._022_inv;
                case PokemonId.Ekans:
                    return Properties.Resources._023_inv;
                case PokemonId.Arbok:
                    return Properties.Resources._024_inv;
                case PokemonId.Pikachu:
                    return Properties.Resources._025_inv;
                case PokemonId.Raichu:
                    return Properties.Resources._026_inv;
                case PokemonId.Sandshrew:
                    return Properties.Resources._027_inv;
                case PokemonId.Sandslash:
                    return Properties.Resources._028_inv;
                case PokemonId.NidoranFemale:
                    return Properties.Resources._029_inv;
                case PokemonId.Nidorina:
                    return Properties.Resources._030_inv;
                case PokemonId.Nidoqueen:
                    return Properties.Resources._031_inv;
                case PokemonId.NidoranMale:
                    return Properties.Resources._032_inv;
                case PokemonId.Nidorino:
                    return Properties.Resources._033_inv;
                case PokemonId.Nidoking:
                    return Properties.Resources._034_inv;
                case PokemonId.Clefairy:
                    return Properties.Resources._035_inv;
                case PokemonId.Clefable:
                    return Properties.Resources._036_inv;
                case PokemonId.Vulpix:
                    return Properties.Resources._037_inv;
                case PokemonId.Ninetales:
                    return Properties.Resources._038_inv;
                case PokemonId.Jigglypuff:
                    return Properties.Resources._039_inv;
                case PokemonId.Wigglytuff:
                    return Properties.Resources._040_inv;
                case PokemonId.Zubat:
                    return Properties.Resources._041_inv;
                case PokemonId.Golbat:
                    return Properties.Resources._042_inv;
                case PokemonId.Oddish:
                    return Properties.Resources._043_inv;
                case PokemonId.Gloom:
                    return Properties.Resources._044_inv;
                case PokemonId.Vileplume:
                    return Properties.Resources._045_inv;
                case PokemonId.Paras:
                    return Properties.Resources._046_inv;
                case PokemonId.Parasect:
                    return Properties.Resources._047_inv;
                case PokemonId.Venonat:
                    return Properties.Resources._048_inv;
                case PokemonId.Venomoth:
                    return Properties.Resources._049_inv;
                case PokemonId.Diglett:
                    return Properties.Resources._050_inv;
                case PokemonId.Dugtrio:
                    return Properties.Resources._051_inv;
                case PokemonId.Meowth:
                    return Properties.Resources._052_inv;
                case PokemonId.Persian:
                    return Properties.Resources._053_inv;
                case PokemonId.Psyduck:
                    return Properties.Resources._054_inv;
                case PokemonId.Golduck:
                    return Properties.Resources._055_inv;
                case PokemonId.Mankey:
                    return Properties.Resources._056_inv;
                case PokemonId.Primeape:
                    return Properties.Resources._057_inv;
                case PokemonId.Growlithe:
                    return Properties.Resources._058_inv;
                case PokemonId.Arcanine:
                    return Properties.Resources._059_inv;
                case PokemonId.Poliwag:
                    return Properties.Resources._060_inv;
                case PokemonId.Poliwhirl:
                    return Properties.Resources._061_inv;
                case PokemonId.Poliwrath:
                    return Properties.Resources._062_inv;
                case PokemonId.Abra:
                    return Properties.Resources._063_inv;
                case PokemonId.Kadabra:
                    return Properties.Resources._064_inv;
                case PokemonId.Alakazam:
                    return Properties.Resources._065_inv;
                case PokemonId.Machop:
                    return Properties.Resources._066_inv;
                case PokemonId.Machoke:
                    return Properties.Resources._067_inv;
                case PokemonId.Machamp:
                    return Properties.Resources._068_inv;
                case PokemonId.Bellsprout:
                    return Properties.Resources._069_inv;
                case PokemonId.Weepinbell:
                    return Properties.Resources._070_inv;
                case PokemonId.Victreebel:
                    return Properties.Resources._071_inv;
                case PokemonId.Tentacool:
                    return Properties.Resources._072_inv;
                case PokemonId.Tentacruel:
                    return Properties.Resources._073_inv;
                case PokemonId.Geodude:
                    return Properties.Resources._074_inv;
                case PokemonId.Graveler:
                    return Properties.Resources._075_inv;
                case PokemonId.Golem:
                    return Properties.Resources._076_inv;
                case PokemonId.Ponyta:
                    return Properties.Resources._077_inv;
                case PokemonId.Rapidash:
                    return Properties.Resources._078_inv;
                case PokemonId.Slowpoke:
                    return Properties.Resources._079_inv;
                case PokemonId.Slowbro:
                    return Properties.Resources._080_inv;
                case PokemonId.Magnemite:
                    return Properties.Resources._081_inv;
                case PokemonId.Magneton:
                    return Properties.Resources._082_inv;
                case PokemonId.Farfetchd:
                    return Properties.Resources._083_inv;
                case PokemonId.Doduo:
                    return Properties.Resources._084_inv;
                case PokemonId.Dodrio:
                    return Properties.Resources._085_inv;
                case PokemonId.Seel:
                    return Properties.Resources._086_inv;
                case PokemonId.Dewgong:
                    return Properties.Resources._087_inv;
                case PokemonId.Grimer:
                    return Properties.Resources._088_inv;
                case PokemonId.Muk:
                    return Properties.Resources._089_inv;
                case PokemonId.Shellder:
                    return Properties.Resources._090_inv;
                case PokemonId.Cloyster:
                    return Properties.Resources._091_inv;
                case PokemonId.Gastly:
                    return Properties.Resources._092_inv;
                case PokemonId.Haunter:
                    return Properties.Resources._093_inv;
                case PokemonId.Gengar:
                    return Properties.Resources._094_inv;
                case PokemonId.Onix:
                    return Properties.Resources._095_inv;
                case PokemonId.Drowzee:
                    return Properties.Resources._096_inv;
                case PokemonId.Hypno:
                    return Properties.Resources._097_inv;
                case PokemonId.Krabby:
                    return Properties.Resources._098_inv;
                case PokemonId.Kingler:
                    return Properties.Resources._099_inv;
                case PokemonId.Voltorb:
                    return Properties.Resources._100_inv;
                case PokemonId.Electrode:
                    return Properties.Resources._101_inv;
                case PokemonId.Exeggcute:
                    return Properties.Resources._102_inv;
                case PokemonId.Exeggutor:
                    return Properties.Resources._103_inv;
                case PokemonId.Cubone:
                    return Properties.Resources._104_inv;
                case PokemonId.Marowak:
                    return Properties.Resources._105_inv;
                case PokemonId.Hitmonlee:
                    return Properties.Resources._106_inv;
                case PokemonId.Hitmonchan:
                    return Properties.Resources._107_inv;
                case PokemonId.Lickitung:
                    return Properties.Resources._108_inv;
                case PokemonId.Koffing:
                    return Properties.Resources._109_inv;
                case PokemonId.Weezing:
                    return Properties.Resources._110_inv;
                case PokemonId.Rhyhorn:
                    return Properties.Resources._111_inv;
                case PokemonId.Rhydon:
                    return Properties.Resources._112_inv;
                case PokemonId.Chansey:
                    return Properties.Resources._113_inv;
                case PokemonId.Tangela:
                    return Properties.Resources._114_inv;
                case PokemonId.Kangaskhan:
                    return Properties.Resources._115_inv;
                case PokemonId.Horsea:
                    return Properties.Resources._116_inv;
                case PokemonId.Seadra:
                    return Properties.Resources._117_inv;
                case PokemonId.Goldeen:
                    return Properties.Resources._118_inv;
                case PokemonId.Seaking:
                    return Properties.Resources._119_inv;
                case PokemonId.Staryu:
                    return Properties.Resources._120_inv;
                case PokemonId.Starmie:
                    return Properties.Resources._121_inv;
                case PokemonId.MrMime:
                    return Properties.Resources._122_inv;
                case PokemonId.Scyther:
                    return Properties.Resources._123_inv;
                case PokemonId.Jynx:
                    return Properties.Resources._124_inv;
                case PokemonId.Electabuzz:
                    return Properties.Resources._125_inv;
                case PokemonId.Magmar:
                    return Properties.Resources._126_inv;
                case PokemonId.Pinsir:
                    return Properties.Resources._127_inv;
                case PokemonId.Tauros:
                    return Properties.Resources._128_inv;
                case PokemonId.Magikarp:
                    return Properties.Resources._129_inv;
                case PokemonId.Gyarados:
                    return Properties.Resources._130_inv;
                case PokemonId.Lapras:
                    return Properties.Resources._131_inv;
                case PokemonId.Ditto:
                    return Properties.Resources._132_inv;
                case PokemonId.Eevee:
                    return Properties.Resources._133_inv;
                case PokemonId.Vaporeon:
                    return Properties.Resources._134_inv;
                case PokemonId.Jolteon:
                    return Properties.Resources._135_inv;
                case PokemonId.Flareon:
                    return Properties.Resources._136_inv;
                case PokemonId.Porygon:
                    return Properties.Resources._137_inv;
                case PokemonId.Omanyte:
                    return Properties.Resources._138_inv;
                case PokemonId.Omastar:
                    return Properties.Resources._139_inv;
                case PokemonId.Kabuto:
                    return Properties.Resources._140_inv;
                case PokemonId.Kabutops:
                    return Properties.Resources._141_inv;
                case PokemonId.Aerodactyl:
                    return Properties.Resources._142_inv;
                case PokemonId.Snorlax:
                    return Properties.Resources._143_inv;
                case PokemonId.Articuno:
                    return Properties.Resources._144_inv;
                case PokemonId.Zapdos:
                    return Properties.Resources._145_inv;
                case PokemonId.Moltres:
                    return Properties.Resources._146_inv;
                case PokemonId.Dratini:
                    return Properties.Resources._147_inv;
                case PokemonId.Dragonair:
                    return Properties.Resources._148_inv;
                case PokemonId.Dragonite:
                    return Properties.Resources._149_inv;
                case PokemonId.Mewtwo:
                    return Properties.Resources._150_inv;
                case PokemonId.Mew:
                    return Properties.Resources._151_inv;
            }
            return Properties.Resources.no_name;
        }

        private static Bitmap ToInventoryItem(this ItemId pid)
        {
            switch (pid)
            {
                case ItemId.ItemUnknown:
                    return Properties.Resources.no_name;
                case ItemId.ItemPokeBall:
                    return Properties.Resources.pokeball_1;
                case ItemId.ItemGreatBall:
                    return Properties.Resources.pokeball_2;
                case ItemId.ItemUltraBall:
                    return Properties.Resources.pokeball_3;
                case ItemId.ItemMasterBall:
                    return Properties.Resources.pokeball_4;
                case ItemId.ItemPotion:
                    return Properties.Resources.potion_1;
                case ItemId.ItemSuperPotion:
                    return Properties.Resources.potion_2;
                case ItemId.ItemHyperPotion:
                    return Properties.Resources.potion_3;
                case ItemId.ItemMaxPotion:
                    return Properties.Resources.potion_4;
                case ItemId.ItemRevive:
                    return Properties.Resources.revive_1;
                case ItemId.ItemMaxRevive:
                    return Properties.Resources.revive_2;
                case ItemId.ItemLuckyEgg:
                    return Properties.Resources.lucky_egg;
                case ItemId.ItemIncenseOrdinary:
                    return Properties.Resources.ince;
                case ItemId.ItemIncenseSpicy:
                    return Properties.Resources.ince;
                case ItemId.ItemIncenseCool:
                    return Properties.Resources.ince;
                case ItemId.ItemIncenseFloral:
                    return Properties.Resources.ince;
                case ItemId.ItemTroyDisk:
                    return Properties.Resources.lure_mod; //NO
                case ItemId.ItemXAttack:
                    return Properties.Resources.no_name; //NO
                case ItemId.ItemXDefense:
                    return Properties.Resources.no_name; //NO
                case ItemId.ItemXMiracle:
                    return Properties.Resources.no_name; //NO
                case ItemId.ItemRazzBerry:
                    return Properties.Resources.berry_1;
                case ItemId.ItemBlukBerry:
                    return Properties.Resources.berry_2;
                case ItemId.ItemNanabBerry:
                    return Properties.Resources.berry_3;
                case ItemId.ItemWeparBerry:
                    return Properties.Resources.berry_4;
                case ItemId.ItemPinapBerry:
                    return Properties.Resources.berry_5;
                case ItemId.ItemSpecialCamera:
                    return Properties.Resources.camera;
                case ItemId.ItemIncubatorBasicUnlimited:
                    return Properties.Resources.incubator_unlimited;
                case ItemId.ItemIncubatorBasic:
                    return Properties.Resources.incubator;
                case ItemId.ItemPokemonStorageUpgrade:
                    return Properties.Resources.bag_upgrade;
                case ItemId.ItemItemStorageUpgrade:
                    return Properties.Resources.bag_upgrade;
            }
            return Properties.Resources.no_name;
        }

        public static string ToInventoryName(this ItemId pid)
        {
            switch (pid)
            {
                case ItemId.ItemUnknown:
                    return "Unknown";
                case ItemId.ItemPokeBall:
                    return "PokeBall";
                case ItemId.ItemGreatBall:
                    return "GreatBall";
                case ItemId.ItemUltraBall:
                    return "UltraBall";
                case ItemId.ItemMasterBall:
                    return "MasterBall";
                case ItemId.ItemPotion:
                    return "Potion";
                case ItemId.ItemSuperPotion:
                    return "Super Potion";
                case ItemId.ItemHyperPotion:
                    return "Hyper Potion";
                case ItemId.ItemMaxPotion:
                    return "Max Potion";
                case ItemId.ItemRevive:
                    return "Revive";
                case ItemId.ItemMaxRevive:
                    return "Max Revive";
                case ItemId.ItemLuckyEgg:
                    return "Lucky Egg";
                case ItemId.ItemIncenseOrdinary:
                    return "Incense";
                case ItemId.ItemIncenseSpicy:
                    return "Incense Spicy";
                case ItemId.ItemIncenseCool:
                    return "Incense Cool";
                case ItemId.ItemIncenseFloral:
                    return "Incense Floral";
                case ItemId.ItemTroyDisk:
                    return "Lure Module";
                case ItemId.ItemXAttack:
                    return "X Attack";
                case ItemId.ItemXDefense:
                    return "X Defense";
                case ItemId.ItemXMiracle:
                    return "X Miracle";
                case ItemId.ItemRazzBerry:
                    return "Razz Berry";
                case ItemId.ItemBlukBerry:
                    return "Bluk Berry";
                case ItemId.ItemNanabBerry:
                    return "Nana Berry";
                case ItemId.ItemWeparBerry:
                    return "Wepar Berry";
                case ItemId.ItemPinapBerry:
                    return "Pinap Berry";
                case ItemId.ItemSpecialCamera:
                    return "Camera";
                case ItemId.ItemIncubatorBasicUnlimited:
                    return "Incubator Unlim";
                case ItemId.ItemIncubatorBasic:
                    return "Incubator";
                case ItemId.ItemPokemonStorageUpgrade:
                    return "PokeStorage Upgrade";
                case ItemId.ItemItemStorageUpgrade:
                    return "Storage Upgrade";
            }
            return "No Name";
        }
    }
}
