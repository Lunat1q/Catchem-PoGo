using POGOProtos.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Catchem
{
    public static class Extensions
    {
        public static List<T> GetLogicalChildCollection<T>(this UIElement parent) where T : DependencyObject
        {
            List<T> logicalCollection = new List<T>();
            GetLogicalChildCollection(parent as DependencyObject, logicalCollection);
            return logicalCollection;
        }

        private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    DependencyObject depChild = child as DependencyObject;
                    if (child is T)
                    {
                        logicalCollection.Add(child as T);
                    }
                    GetLogicalChildCollection(depChild, logicalCollection);
                }
            }
        }

        public static void AppendText(this RichTextBox box, string text, System.Windows.Media.Color color)
        {
            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(color));
            }
            catch (FormatException) { }
        }

        public static void AppendParagraph(this RichTextBox box, string text, System.Windows.Media.Color color)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(text);
            paragraph.Foreground = new SolidColorBrush(color);
            box.Document.Blocks.Add(paragraph);

            box.ScrollToEnd();
        }

        public static System.Windows.Controls.Image ToImage(this PokemonId pid)
        {
            System.Windows.Controls.Image img = new System.Windows.Controls.Image();
            img.Source = pid.ToBitmap().loadBitmap();
            var tt = new ToolTip();
            tt.Content = pid.ToString();
            img.ToolTip = tt;
            return img;
        }

        public static System.Windows.Controls.Image ToImage(this System.Drawing.Bitmap source, string toolTipText = "no_text")
        {
            System.Windows.Controls.Image img = new System.Windows.Controls.Image();
            img.Source = source.loadBitmap();
            var tt = new ToolTip();
            tt.Content = toolTipText;
            img.ToolTip = tt;
            return img;
        }

        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        public static BitmapSource loadBitmap(this System.Drawing.Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try
            {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
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
                    return Properties.Resources._5
                ;
                case PokemonId.Charizard:
                    return Properties.Resources._6
                ;
                case PokemonId.Squirtle:
                    return Properties.Resources._7
                ;
                case PokemonId.Wartortle:
                    return Properties.Resources._8
                ;
                case PokemonId.Blastoise:
                    return Properties.Resources._9
                ;
                case PokemonId.Caterpie:
                    return Properties.Resources._10
                ;
                case PokemonId.Metapod:
                    return Properties.Resources._11
                ;
                case PokemonId.Butterfree:
                    return Properties.Resources._12
                ;
                case PokemonId.Weedle:
                    return Properties.Resources._13
                ;
                case PokemonId.Kakuna:
                    return Properties.Resources._14
                ;
                case PokemonId.Beedrill:
                    return Properties.Resources._15
                ;
                case PokemonId.Pidgey:
                    return Properties.Resources._16
                ;
                case PokemonId.Pidgeotto:
                    return Properties.Resources._17
                ;
                case PokemonId.Pidgeot:
                    return Properties.Resources._18
                ;
                case PokemonId.Rattata:
                    return Properties.Resources._19
                ;
                case PokemonId.Raticate:
                    return Properties.Resources._20
                ;
                case PokemonId.Spearow:
                    return Properties.Resources._21
                ;
                case PokemonId.Fearow:
                    return Properties.Resources._22
                ;
                case PokemonId.Ekans:
                    return Properties.Resources._23
                ;
                case PokemonId.Arbok:
                    return Properties.Resources._24
                ;
                case PokemonId.Pikachu:
                    return Properties.Resources._25
                ;
                case PokemonId.Raichu:
                    return Properties.Resources._26
                ;
                case PokemonId.Sandshrew:
                    return Properties.Resources._27
                ;
                case PokemonId.Sandslash:
                    return Properties.Resources._28
                ;
                case PokemonId.NidoranFemale:
                    return Properties.Resources._29
                ;
                case PokemonId.Nidorina:
                    return Properties.Resources._30
                ;
                case PokemonId.Nidoqueen:
                    return Properties.Resources._31
                ;
                case PokemonId.NidoranMale:
                    return Properties.Resources._32
                ;
                case PokemonId.Nidorino:
                    return Properties.Resources._33
                ;
                case PokemonId.Nidoking:
                    return Properties.Resources._34
                ;
                case PokemonId.Clefairy:
                    return Properties.Resources._35
                ;
                case PokemonId.Clefable:
                    return Properties.Resources._36
                ;
                case PokemonId.Vulpix:
                    return Properties.Resources._37
                ;
                case PokemonId.Ninetales:
                    return Properties.Resources._38
                ;
                case PokemonId.Jigglypuff:
                    return Properties.Resources._39
                ;
                case PokemonId.Wigglytuff:
                    return Properties.Resources._40
                ;
                case PokemonId.Zubat:
                    return Properties.Resources._41
                ;
                case PokemonId.Golbat:
                    return Properties.Resources._42
                ;
                case PokemonId.Oddish:
                    return Properties.Resources._43
                ;
                case PokemonId.Gloom:
                    return Properties.Resources._44
                ;
                case PokemonId.Vileplume:
                    return Properties.Resources._45
                ;
                case PokemonId.Paras:
                    return Properties.Resources._46
                ;
                case PokemonId.Parasect:
                    return Properties.Resources._47
                ;
                case PokemonId.Venonat:
                    return Properties.Resources._48
                ;
                case PokemonId.Venomoth:
                    return Properties.Resources._49
                ;
                case PokemonId.Diglett:
                    return Properties.Resources._50
                ;
                case PokemonId.Dugtrio:
                    return Properties.Resources._51
                ;
                case PokemonId.Meowth:
                    return Properties.Resources._52
                ;
                case PokemonId.Persian:
                    return Properties.Resources._53
                ;
                case PokemonId.Psyduck:
                    return Properties.Resources._54
                ;
                case PokemonId.Golduck:
                    return Properties.Resources._55
                ;
                case PokemonId.Mankey:
                    return Properties.Resources._56
                ;
                case PokemonId.Primeape:
                    return Properties.Resources._57
                ;
                case PokemonId.Growlithe:
                    return Properties.Resources._58
                ;
                case PokemonId.Arcanine:
                    return Properties.Resources._59
                ;
                case PokemonId.Poliwag:
                    return Properties.Resources._60
                ;
                case PokemonId.Poliwhirl:
                    return Properties.Resources._61
                ;
                case PokemonId.Poliwrath:
                    return Properties.Resources._62
                ;
                case PokemonId.Abra:
                    return Properties.Resources._63
                ;
                case PokemonId.Kadabra:
                    return Properties.Resources._64
                ;
                case PokemonId.Alakazam:
                    return Properties.Resources._65
                ;
                case PokemonId.Machop:
                    return Properties.Resources._66
                ;
                case PokemonId.Machoke:
                    return Properties.Resources._67
                ;
                case PokemonId.Machamp:
                    return Properties.Resources._68
                ;
                case PokemonId.Bellsprout:
                    return Properties.Resources._69
                ;
                case PokemonId.Weepinbell:
                    return Properties.Resources._70
                ;
                case PokemonId.Victreebel:
                    return Properties.Resources._71
                ;
                case PokemonId.Tentacool:
                    return Properties.Resources._72
                ;
                case PokemonId.Tentacruel:
                    return Properties.Resources._73
                ;
                case PokemonId.Geodude:
                    return Properties.Resources._74
                ;
                case PokemonId.Graveler:
                    return Properties.Resources._75
                ;
                case PokemonId.Golem:
                    return Properties.Resources._76
                ;
                case PokemonId.Ponyta:
                    return Properties.Resources._77
                ;
                case PokemonId.Rapidash:
                    return Properties.Resources._78
                ;
                case PokemonId.Slowpoke:
                    return Properties.Resources._79
                ;
                case PokemonId.Slowbro:
                    return Properties.Resources._80
                ;
                case PokemonId.Magnemite:
                    return Properties.Resources._81
                ;
                case PokemonId.Magneton:
                    return Properties.Resources._82
                ;
                case PokemonId.Farfetchd:
                    return Properties.Resources._83
                ;
                case PokemonId.Doduo:
                    return Properties.Resources._84
                ;
                case PokemonId.Dodrio:
                    return Properties.Resources._85
                ;
                case PokemonId.Seel:
                    return Properties.Resources._86
                ;
                case PokemonId.Dewgong:
                    return Properties.Resources._87
                ;
                case PokemonId.Grimer:
                    return Properties.Resources._88
                ;
                case PokemonId.Muk:
                    return Properties.Resources._89
                ;
                case PokemonId.Shellder:
                    return Properties.Resources._90
                ;
                case PokemonId.Cloyster:
                    return Properties.Resources._91
                ;
                case PokemonId.Gastly:
                    return Properties.Resources._92
                ;
                case PokemonId.Haunter:
                    return Properties.Resources._93
                ;
                case PokemonId.Gengar:
                    return Properties.Resources._94
                ;
                case PokemonId.Onix:
                    return Properties.Resources._95
                ;
                case PokemonId.Drowzee:
                    return Properties.Resources._96
                ;
                case PokemonId.Hypno:
                    return Properties.Resources._97
                ;
                case PokemonId.Krabby:
                    return Properties.Resources._98
                ;
                case PokemonId.Kingler:
                    return Properties.Resources._99
                ;
                case PokemonId.Voltorb:
                    return Properties.Resources._100
                ;
                case PokemonId.Electrode:
                    return Properties.Resources._101
                ;
                case PokemonId.Exeggcute:
                    return Properties.Resources._102
                ;
                case PokemonId.Exeggutor:
                    return Properties.Resources._103
                ;
                case PokemonId.Cubone:
                    return Properties.Resources._104
                ;
                case PokemonId.Marowak:
                    return Properties.Resources._105
                ;
                case PokemonId.Hitmonlee:
                    return Properties.Resources._106
                ;
                case PokemonId.Hitmonchan:
                    return Properties.Resources._107
                ;
                case PokemonId.Lickitung:
                    return Properties.Resources._108
                ;
                case PokemonId.Koffing:
                    return Properties.Resources._109
                ;
                case PokemonId.Weezing:
                    return Properties.Resources._110
                ;
                case PokemonId.Rhyhorn:
                    return Properties.Resources._111
                ;
                case PokemonId.Rhydon:
                    return Properties.Resources._112
                ;
                case PokemonId.Chansey:
                    return Properties.Resources._113
                ;
                case PokemonId.Tangela:
                    return Properties.Resources._114
                ;
                case PokemonId.Kangaskhan:
                    return Properties.Resources._115
                ;
                case PokemonId.Horsea:
                    return Properties.Resources._116
                ;
                case PokemonId.Seadra:
                    return Properties.Resources._117
                ;
                case PokemonId.Goldeen:
                    return Properties.Resources._118
                ;
                case PokemonId.Seaking:
                    return Properties.Resources._119
                ;
                case PokemonId.Staryu:
                    return Properties.Resources._120
                ;
                case PokemonId.Starmie:
                    return Properties.Resources._121
                ;
                case PokemonId.MrMime:
                    return Properties.Resources._122
                ;
                case PokemonId.Scyther:
                    return Properties.Resources._123
                ;
                case PokemonId.Jynx:
                    return Properties.Resources._124
                ;
                case PokemonId.Electabuzz:
                    return Properties.Resources._125
                ;
                case PokemonId.Magmar:
                    return Properties.Resources._126
                ;
                case PokemonId.Pinsir:
                    return Properties.Resources._127
                ;
                case PokemonId.Tauros:
                    return Properties.Resources._128
                ;
                case PokemonId.Magikarp:
                    return Properties.Resources._129
                ;
                case PokemonId.Gyarados:
                    return Properties.Resources._130
                ;
                case PokemonId.Lapras:
                    return Properties.Resources._131
                ;
                case PokemonId.Ditto:
                    return Properties.Resources._132
                ;
                case PokemonId.Eevee:
                    return Properties.Resources._133
                ;
                case PokemonId.Vaporeon:
                    return Properties.Resources._134
                ;
                case PokemonId.Jolteon:
                    return Properties.Resources._135
                ;
                case PokemonId.Flareon:
                    return Properties.Resources._136
                ;
                case PokemonId.Porygon:
                    return Properties.Resources._137
                ;
                case PokemonId.Omanyte:
                    return Properties.Resources._138
                ;
                case PokemonId.Omastar:
                    return Properties.Resources._139
                ;
                case PokemonId.Kabuto:
                    return Properties.Resources._140
                ;
                case PokemonId.Kabutops:
                    return Properties.Resources._141
                ;
                case PokemonId.Aerodactyl:
                    return Properties.Resources._142
                ;
                case PokemonId.Snorlax:
                    return Properties.Resources._143
                ;
                case PokemonId.Articuno:
                    return Properties.Resources._144
                ;
                case PokemonId.Zapdos:
                    return Properties.Resources._145
                ;
                case PokemonId.Moltres:
                    return Properties.Resources._146
                ;
                case PokemonId.Dratini:
                    return Properties.Resources._147
                ;
                case PokemonId.Dragonair:
                    return Properties.Resources._148
                ;
                case PokemonId.Dragonite:
                    return Properties.Resources._149
                ;
                case PokemonId.Mewtwo:
                    return Properties.Resources._150
                ;
                case PokemonId.Mew:
                    return Properties.Resources._151;
            }
            return Properties.Resources.no_name;
        }
    }
}
