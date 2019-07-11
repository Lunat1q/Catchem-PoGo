#region using directives

using PoGo.PokeMobBot.Logic.Common;

#endregion

namespace PoGo.PokeMobBot.Logic.Localization
{
    public interface ILocalizer
    {
        string GetFormat(TranslationString key);
        string GetFormat(TranslationString key, params object[] data);
    }

    public class Localizer : ILocalizer
    {
        public string GetFormat(TranslationString key)
        {
            return "";
        }

        public string GetFormat(TranslationString key, params object[] data)
        {
            return "";
        }
    }
}