using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Catchem.UiTranslation;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

namespace Catchem.Extensions
{
    public sealed class MethodToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var methodName = parameter as string;
            if (value == null || methodName == null)
                return value;
            var methodInfo = value.GetType().GetMethod(methodName, new Type[0]);
            if (methodInfo != null) return methodInfo.Invoke(value, new object[0]);
            var assembly = Assembly.GetExecutingAssembly();
                //change this to whatever assembly the extension method is in
            methodInfo = value.GetType().GetExtensionMethod(assembly, methodName, new[] {value.GetType()});
            if (methodInfo != null) return methodInfo.Invoke(value, new object[0]);
            //Shitty hack, need to figure out why extMethod wasn't received
            if (value.GetType() == typeof(PokemonId) && methodName == "ToSource")
                return ((PokemonId) value).ToSource();
            if (value.GetType() == typeof(PokemonId) && methodName == "ToBitmap")
                return ((PokemonId) value).ToSource();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class EvolveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var indx = parameter as string;
            if (value == null || indx == null)
                return value;

            var ids = value as PokemonId[];
            if (ids != null)
            {
                var ev = ids;
                int i;
                if (int.TryParse(indx, out i))
                {
                    if (i < ev.Length)
                    {
                        return ev[i].ToInventorySource();
                    }
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class LoadImageSourceFromResource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var imageName = parameter as string;
            if (value == null || imageName == null)
                return value;
            if (value is bool)
            {
                var bVal = (bool) value;
                return bVal
                    ? (Properties.Resources.ResourceManager.GetObject(imageName, Properties.Resources.Culture) as Bitmap)
                        .LoadBitmap()
                    : null;
            }
            var bm = Properties.Resources.ResourceManager.GetObject(imageName, Properties.Resources.Culture) as Bitmap;
            return bm?.LoadBitmap();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class LoadTranslatedString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = parameter as string;
            if (value == null || s == null)
                return value;
            var tag = s.Split(';');
            return tag.Length > 1 ? TranslationEngine.GetDynamicTranslationString(tag[0], tag[1]) : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
    public sealed class TranslationAutoConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = parameter as string;
            if (value == null || s == null || value.Length < 2)
                return value;
            var tag = value[0] as string;
            var lng = value[1] as string;
            if (tag != null)
            {
                return TranslationEngine.GetDynamicTranslationString(tag, s);
            }
            return value;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public sealed class EggImageSourceConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            if (value is ItemId)
            {
                var bVal = (ItemId) value;
                switch (bVal)
                {
                    case ItemId.ItemIncubatorBasic:
                        return Properties.Resources.incubator.LoadBitmap();
                    case ItemId.ItemIncubatorBasicUnlimited:
                        return Properties.Resources.incubator_unlimited.LoadBitmap();
                    default:
                        return Properties.Resources.egg.LoadBitmap();
                }
            }
            return Properties.Resources.no_name.LoadBitmap();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class LoadImageFromResource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var imageName = parameter as string;
            if (value == null || imageName == null)
                return value;
            var img =
                (Properties.Resources.ResourceManager.GetObject(imageName, Properties.Resources.Culture) as Bitmap)
                    .ToImage(imageName.Substring(0, 1).ToUpper() + imageName.Substring(1));
            img.HorizontalAlignment = HorizontalAlignment.Center;
            img.VerticalAlignment = VerticalAlignment.Center;
            img.Stretch = Stretch.None;
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class LoadImageIconFromResource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var imageName = parameter as string;
            if (value == null || imageName == null)
                return value;
            var img =
                (Properties.Resources.ResourceManager.GetObject(imageName, Properties.Resources.Culture) as Bitmap)
                    .ToImage(imageName.Substring(0, 1).ToUpper() + imageName.Substring(1));
            img.HorizontalAlignment = HorizontalAlignment.Stretch;
            img.VerticalAlignment = VerticalAlignment.Stretch;
            img.Stretch = Stretch.Uniform;
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class PokeToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var poke = value as PokemonId?;
            var img = poke?.ToInventorySource();
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class ItemToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as ItemId?;
            var img = item?.ToInventorySource();
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class PokeTypeToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var poke = value as PokemonType?;
            var img = poke?.ToInventorySource();
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public sealed class ButtonStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var prop = parameter as string;
                var status = (bool?) value;
                if (value == null || prop == null)
                    return value;

                if (prop == "Text")
                {
                    return (bool) status ? TranslationEngine.GetDynamicTranslationString("%STOP%", "STOP") : TranslationEngine.GetDynamicTranslationString("%START%", "START");
                }
                if (prop == "Background")
                {
                    var color1 = (bool) status
                        ? System.Windows.Media.Color.FromArgb(255, 192, 79, 83)
                        : System.Windows.Media.Color.FromArgb(255, 83, 192, 177);
                    var color2 = (bool) status
                        ? System.Windows.Media.Color.FromArgb(255, 238, 178, 156)
                        : System.Windows.Media.Color.FromArgb(255, 176, 238, 156);
                    return new LinearGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop
                            {
                                Color = color1,
                                Offset = 1
                            },
                            new GradientStop
                            {
                                Color = color2,
                                Offset = 0
                            }
                        }
                    };
                }
                return value;
            }
            catch (Exception)
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class CamelCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumString = value.ToString();
            var camelCaseString = Regex.Replace(enumString, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ").ToLower();
            return char.ToUpper(camelCaseString[0]) + camelCaseString.Substring(1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class IsGreaterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return null;
            var val = (int)value;
            var limit = 0;
            if (int.TryParse(parameter.ToString(), out limit))
            {
                return val >= limit;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public static class ReflectionExtensions
    {
        public static IEnumerable<MethodInfo> GetExtensionMethods(this Type type, Assembly extensionsAssembly)
        {
            var query = from t in extensionsAssembly.GetTypes()
                where !t.IsGenericType && !t.IsNested
                from m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                where m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
                where m.GetParameters()[0].ParameterType == type
                select m;

            return query;
        }

        public static MethodInfo GetExtensionMethod(this Type type, Assembly extensionsAssembly, string name)
        {
            return type.GetExtensionMethods(extensionsAssembly).FirstOrDefault(m => m.Name == name);
        }

        public static MethodInfo GetExtensionMethod(this Type type, Assembly extensionsAssembly, string name,
            Type[] types)
        {
            var methods = (from m in type.GetExtensionMethods(extensionsAssembly)
                where m.Name == name
                      && m.GetParameters().Count() == types.Length + 1
                // + 1 because extension method parameter (this)
                select m).ToList();

            if (!methods.Any())
            {
                return default(MethodInfo);
            }

            if (methods.Count() == 1)
            {
                return methods.First();
            }

            foreach (var methodInfo in methods)
            {
                var parameters = methodInfo.GetParameters();

                bool found = true;
                for (byte b = 0; b < types.Length; b++)
                {
                    found = true;
                    if (parameters[b].GetType() != types[b])
                    {
                        found = false;
                    }
                }

                if (found)
                {
                    return methodInfo;
                }
            }

            return default(MethodInfo);
        }
    }
}