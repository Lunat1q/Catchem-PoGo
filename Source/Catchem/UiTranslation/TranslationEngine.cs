using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Catchem.Extensions;
using PoGo.PokeMobBot.Logic.Utils;

namespace Catchem.UiTranslation
{
    public static class TranslationEngine
    {
        private static byte[] _entropy;
        private const string TranslationsFolder = "Config\\UILanguages";
        private static string Folder => Path.Combine(Directory.GetCurrentDirectory(), TranslationsFolder);
        public static readonly List<string> LangList = new List<string>();
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };
        private static string _curLanguage;

        public static void RaiseStaticPropertyChanged([CallerMemberName] string propName = null)
        {
            StaticPropertyChanged(null, new PropertyChangedEventArgs(propName));
        }

        public static void RaiseStaticPropertyChangedByName(string propName = null)
        {
            StaticPropertyChanged(null, new PropertyChangedEventArgs(propName));
        }

        public static string CurrentTranslationLanguage
        {
            get { return _curLanguage; }
            set
            {
                if (_curLanguage == value) return;
                _curLanguage = value;
                RaiseStaticPropertyChanged();
            }
        }
        private static UiTranslation _alterTranslation;

        public static void Initialize()
        {
            CurrentTranslationLanguage = "English";
            _entropy = Encoding.UTF8.GetBytes("CatchemTranslationEngine-v1.0");
            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
            }
            
            foreach (var item in Directory.GetFiles(Folder))
            {
                if (!CheckForUnprotectedTranslation(item) || !item.Contains(".catchemLang")) continue;
                var fi = new FileInfo(item);
                LangList.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
            }
            if (!LangList.Contains("English"))
            {
                var englishUi = new UiTranslation
                {
                    LanguageName = "English",
                    Translation = GetTranslationTags(MainWindow.BotWindow.SettingsView, MainWindow.BotWindow.MenuGrid, MainWindow.BotWindow.batchInput, MainWindow.BotWindow.InputBox,
                    MainWindow.BotWindow.GlobalMapView, MainWindow.BotWindow.RouteCreatorView, MainWindow.BotWindow.TelegramView)
                };
                englishUi.CryptData(Path.Combine(Folder, "English.catchemLang"), _entropy);
#if DEBUG
                englishUi.SerializeDataJson(Path.Combine(Directory.GetCurrentDirectory(), TranslationsFolder,
                    "English.json"));
#endif
                LangList.Add("English");
            }

#if DEBUG
            CurrentTranslationLanguage = "EMPTY";
#endif

        }

        private static bool CheckForUnprotectedTranslation(string path)
        {
            var fi = new FileInfo(path);
            if (fi.Extension.ToLower() != ".json") return true;
            var uiLang = SerializeUtils.DeserializeDataJson<UiTranslation>(path);
            if (uiLang == null) return true;
            var langPath = path.Substring(0, path.Length - fi.Extension.Length) + ".catchemLang";
            if (!File.Exists(langPath))
            {
#if DEBUG
                uiLang.CryptData(langPath, _entropy);
                LangList.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
#else
                File.Delete(path);
#endif
            }
            return true;
        }

        private static void SaveCurrent()
        {
            var langPath = Path.Combine(Directory.GetCurrentDirectory(), TranslationsFolder,
                CurrentTranslationLanguage + ".json");
            _alterTranslation.SerializeDataJson(langPath);
        }

        public static string GetDynamicTranslationString(string tag, string defaultText)
        {
            if (_alterTranslation == null) return defaultText;
            if (_alterTranslation.Translation.ContainsKey(tag))
            {
                return _alterTranslation.Translation[tag];
            }
#if DEBUG
            _alterTranslation.Translation.Add(tag, defaultText);
            SaveCurrent();
#endif
            return defaultText;
        }

        public static void SetLanguage(string languageName)
        {
            if (languageName == CurrentTranslationLanguage || languageName == null) return;
            if (!LangList.Contains(languageName)) return;
            var lng =
                TranslationSerializer.DecryptData<UiTranslation>(
                    Path.Combine(Folder, languageName + ".catchemLang"), _entropy);
            if (lng == null) return;
            _alterTranslation = lng;
            CurrentTranslationLanguage = languageName;
            RaiseStaticPropertyChangedByName("CurrentTranslationLanguage");
        }
    
        public static void ApplyLanguage(UIElement page)
        {
            if (_alterTranslation == null) return;
            foreach (var uiElem in page.GetLogicalChildCollection<CheckBox>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Content = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<Label>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Content = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<TabItem>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Header = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<GroupBox>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Header = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<Button>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Content = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<TextBlock>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Text = _alterTranslation.Translation[s];
            }

            foreach (var uiElem in page.GetLogicalChildCollection<TextBox>())
            {
                var s = uiElem.Tag as string;
                if (s == null) continue;
                if (_alterTranslation.Translation.ContainsKey(s))
                    uiElem.Text = _alterTranslation.Translation[s];
            }

#if DEBUG
            var dict = GetTranslationTags(page);
            foreach (var kp in dict)
            {
                if (!_alterTranslation.Translation.ContainsKey(kp.Key))
                    _alterTranslation.Translation.Add(kp.Key, kp.Value);
            }
            SaveCurrent();
#endif
        }

        private static Dictionary<string, string> GetTranslationTags(params UIElement[] pages)
        {
            var tagDict = new Dictionary<string, string>();
            foreach (var page in pages)
            {
                foreach (var uiElem in page.GetLogicalChildCollection<CheckBox>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Content as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<Label>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Content as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<TabItem>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Header as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<Button>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Content as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<GroupBox>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Header as string);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<TextBlock>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Text);
                }

                foreach (var uiElem in page.GetLogicalChildCollection<TextBox>())
                {
                    var s = uiElem.Tag as string;
                    if (s == null) continue;
                    if (!tagDict.ContainsKey(s))
                        tagDict.Add(s, uiElem.Text);
                }
            }
            return tagDict;
        }
    }
}
