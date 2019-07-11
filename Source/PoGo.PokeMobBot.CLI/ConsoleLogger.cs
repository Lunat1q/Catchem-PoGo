﻿#region using directives

using System;
using System.Text;
using PoGo.PokeMobBot.CLI.Models;
using PoGo.PokeMobBot.Logic.Enums;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;

#endregion

namespace PoGo.PokeMobBot.CLI
{
    /// <summary>
    ///     The ConsoleLogger is a simple logger which writes all logs to the Console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _maxLogLevel;
        private ISession _session;

        /// <summary>
        /// To create a ConsoleLogger, we must define a maximum log level.
        /// All levels above won't be logged.
        /// </summary>
        /// <param name="maxLogLevel"></param>
        internal ConsoleLogger(LogLevel maxLogLevel)
        {
            _maxLogLevel = maxLogLevel;
        }

        public void SetSession(ISession session)
        {
            _session = session;
            // Create the logging strings here.
            LoggingStrings.SetStrings(_session);
        }

        /// <summary>
        /// Log a specific message by LogLevel. Won't log if the LogLevel is greater than the maxLogLevel set.
        /// </summary>
        /// <param name="message">The message to log. The current time will be prepended.</param>
        /// <param name="level">Optional. Default <see cref="LogLevel.Info" />.</param>
        /// <param name="color">Optional. Default is auotmatic</param>
        /// <param name="session"></param>
        public void Write(string message, LogLevel level = LogLevel.Info, ConsoleColor color = ConsoleColor.Black, ISession session = null)
        {
            // Remember to change to a font that supports your language, otherwise it'll still show as ???.
            Console.OutputEncoding = Encoding.UTF8;
            if (level > _maxLogLevel)
            {
                return;
            }

            switch (level)
            {
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Error}) {message}");
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Attention}) {message}");
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Info}) {message}");
                    break;
                case LogLevel.Pokestop:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Pokestop}) {message}");
                    break;
                case LogLevel.Farming:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Farming}) {message}");
                    break;
                case LogLevel.Recycling:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Recycling}) {message}");
                    break;
                case LogLevel.Caught:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Pkmn}) {message}");
                    break;
                case LogLevel.Escape:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Pkmn}) {message}");
                    break;
                case LogLevel.Flee:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Pkmn}) {message}");
                    break;
                case LogLevel.Transfer:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Transfered}) {message}");
                    break;
                case LogLevel.Evolve:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Evolved}) {message}");
                    break;
                case LogLevel.Berry:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Berry}) {message}");
                    break;
                case LogLevel.Egg:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Egg}) {message}");
                    break;
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Debug}) {message}");
                    break;
                case LogLevel.Update:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Update}) {message}");
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ({LoggingStrings.Error}) {message}");
                    break;
            }
        }
        public void SendWindowMsg(MainRpc str, ISession sess, params object[] obj)
        {
            
        }
    }
}
