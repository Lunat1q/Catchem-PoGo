using System;
using System.Windows.Media;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;

namespace Catchem.Classes
{
    public class WpfLogger : ILogger
    {
        private readonly LogLevel _maxLogLevel;
        private ISession _session;

        private static string _strError = "ERROR";
        private static string _strAttention = "ATTENTION";
        private static string _strInfo = "INFO";
        private static string _strPokestop = "POKESTOP";
        private static string _strFarming = "FARMING";
        private static string _strRecycling = "RECYCLING";
        private static string _strPkmn = "PKMN";
        private static string _strTransfered = "TRANSFERED";
        private static string _strEvolved = "EVOLVED";
        private static string _strBerry = "BERRY";
        private static string _strEgg = "EGG";
        private static string _strDebug = "DEBUG";
        private static string _strUpdate = "UPDATE";
        private static string _strNone = "NONE";
        private static string _strEscape = "ESCAPE";
        private static string _strFlee = "FLEE";
        private static string _strGym = "GYM";
        private static string _strFavourite = "FAVOURITE";
        private static string _strUnFavourite = "UNFAVOURITE";
        private static string _strTelegram = "TLGRM";

        public void SetSession(ISession session)
        {
            _session = session;

            if (_session != null)
            {
                _strError = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryError);
                _strAttention = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryAttention);
                _strInfo = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryInfo);
                _strPokestop = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryPokestop);
                _strFarming = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryFarming);
                _strRecycling = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryRecycling);
                _strPkmn = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryPkmn);
                _strTransfered = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryTransfered);
                _strEvolved = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryEvolved);
                _strBerry = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryBerry);
                _strEgg = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryEgg);
                _strDebug = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryDebug);
                _strUpdate = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryUpdate);
                _strFavourite = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryFavorite);
                _strUnFavourite = _session.Translation.GetTranslation(PoGo.PokeMobBot.Logic.Common.TranslationString.LogEntryUnFavorite);
            }
        }

        /// <summary>
        ///     To create a ConsoleLogger, we must define a maximum log level.
        ///     All levels above won't be logged.
        /// </summary>
        /// <param name="maxLogLevel"></param>
        public WpfLogger(LogLevel maxLogLevel)
        {
            _maxLogLevel = maxLogLevel;
        }

        public void Write(string message, LogLevel level = LogLevel.Info, ConsoleColor color = ConsoleColor.Black, ISession session = null)
        {
            //Remember to change to a font that supports your language, otherwise it'll still show as ???
            if (level >= _maxLogLevel)
                return;

            switch (level)
            {
                case LogLevel.Error:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strError}) {message}", Color.FromRgb(255, 0, 0));
                    break;
                case LogLevel.Warning:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strAttention}) {message}", Color.FromRgb(254, 229, 5));
                    break;
                case LogLevel.Info:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strInfo}) {message}", Color.FromRgb(239, 239, 239));
                    break;
                case LogLevel.Pokestop:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strPokestop}) {message}", Color.FromRgb(0, 190, 255));
                    break;
                case LogLevel.Farming:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strFarming}) {message}", Color.FromRgb(157, 255, 0));
                    break;
                case LogLevel.Recycling:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strRecycling}) {message}", Color.FromRgb(255, 106, 240));
                    break;
                case LogLevel.Caught:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strPkmn}) {message}", Color.FromRgb(8, 206, 8));
                    break;
                case LogLevel.Transfer:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strTransfered}) {message}", Color.FromRgb(0, 255, 214));
                    break;
                case LogLevel.Evolve:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strEvolved}) {message}", Color.FromRgb(255, 230, 0));
                    break;
                case LogLevel.Berry:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strBerry}) {message}", Color.FromRgb(255, 0, 194));
                    break;
                case LogLevel.Egg:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strEgg}) {message}", Color.FromRgb(167, 249, 255));
                    break;
                case LogLevel.Debug:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strDebug}) {message}", Colors.White);
                    break;
                case LogLevel.Update:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strUpdate}) {message}", Color.FromRgb(145, 255, 0));
                    break;
                case LogLevel.None:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strNone}) {message}", Colors.White);
                    break;
                case LogLevel.Escape:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strEscape}) {message}", Color.FromRgb(255, 177, 0));
                    break;
                case LogLevel.Flee:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strFlee}) {message}", Color.FromRgb(158, 255, 255));
                    break;
                case LogLevel.Favorite:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strFavourite}) {message}", Color.FromRgb(255, 0, 159));
                    break;
                case LogLevel.UnFavorite:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strUnFavourite}) {message}", Color.FromRgb(255, 0, 159));
                    break;
                case LogLevel.Gym:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strGym}) {message}", Color.FromRgb(192, 0, 255));
                    break;
				case LogLevel.Telegram:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strTelegram}) {message}", Color.FromRgb(60, 197, 255));
                    break;
                default:
                    SendWindowMsg("log", session, $"[{DateTime.Now.ToString("HH:mm:ss")}] ({_strError}) {message}", Color.FromRgb(255, 255, 255));
                    break;
            }
        }

        public void SendWindowMsg(string msgType, ISession session, params object[] objData)
        {
            MainWindow.BotWindow.ReceiveMsg(msgType, session, objData);
        }
    }
}
