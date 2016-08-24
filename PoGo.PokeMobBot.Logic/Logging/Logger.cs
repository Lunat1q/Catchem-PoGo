#region using directives

using System;
using System.IO;
using PoGo.PokeMobBot.Logic.State;
using System.Collections.Generic;
using PoGo.PokeMobBot.Logic.Extensions;

#endregion

namespace PoGo.PokeMobBot.Logic.Logging
{
    public static class Logger
    {
        private static ILogger _logger;
        private static string _path;
        private static readonly Queue<string> LogQueue = new Queue<string>();
        private static bool _writerActive;

        private static void Log(string message)
        {
            LogQueue.Enqueue(message);

            if (!_writerActive) LogWorker();
        }

        private static async void LogWorker()
        {
            _writerActive = true;
            var delay = 10;
            var currentLogPath = Path.Combine(_path,
                $"PokeMobBot-{DateTime.Today.ToString("yyyy-MM-dd")}-{DateTime.Now.ToString("HH")}.txt");
            var prevH = DateTime.Now.Hour;
            var nextHour = false;
            try
            {
                using (var w = File.AppendText(currentLogPath))
                {
                    var loggedLines = 0;
                    while (!nextHour)
                    {
                        if (LogQueue.Count > 0)
                        {
                            var m = LogQueue.Dequeue();
                            await w.WriteLineAsync(m);
                            loggedLines++;
                        }
                        if (loggedLines >= 100)
                        {
                            w.Flush();
                            loggedLines = 0;
                        }

                        if (LogQueue.Count > 100)
                        {
                            delay = 0;
                        }
                        else if (delay == 0 && LogQueue.Count == 0)
                        {
                            delay = 10;
                        }
                        if (DateTime.Now.Hour != prevH)
                        {
                            nextHour = true;
                            LogWorker();
                        }
                        await System.Threading.Tasks.Task.Delay(delay);
                    }
                    w.Flush();
                }
            }
            catch (Exception)
            {
                LogWorker();
            }
        }

        /// <summary>
        ///     Set the logger. All future requests to <see cref="Write(string,LogLevel,ConsoleColor,ISession)" /> will use that logger, any
        ///     old will be
        ///     unset.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="subPath"></param>
        public static void SetLogger(ILogger logger, string subPath = "")
        {
            _logger = logger;
            _path = Path.Combine(Directory.GetCurrentDirectory(), subPath, "Logs");
            Directory.CreateDirectory(_path);
            Log($"Initializing Rocket logger at time {DateTime.Now}...");
        }

        /// <summary>
        ///     Sets Context for the logger
        /// </summary>
        /// <param name="session">Context</param>
        public static void SetLoggerContext(ISession session)
        {
            _logger?.SetSession(session);
        }

        /// <summary>
        ///     Log a specific message to the logger setup by <see cref="SetLogger(ILogger,string)" /> .
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">Optional level to log. Default <see cref="LogLevel.Info" />.</param>
        /// <param name="color">Optional. Default is automatic color.</param>
        /// <param name="session">Bot's session parameter</param>
        public static void Write(string message, LogLevel level = LogLevel.Info, ConsoleColor color = ConsoleColor.Black, ISession session = null)
        {
            if (_logger == null)
                return;
            _logger.Write(message, level, color, session);
            Log(string.Concat($"[{DateTime.Now.ToString("HH:mm:ss")}] ", message));
        }

        public static void PushToUi(string msgType, ISession session, params object[] obj)
        {
            _logger?.SendWindowMsg(msgType, session, obj);
        }
    }

    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Pokestop = 3,
        Farming = 4,
        Recycling = 5,
        Berry = 6,
        Caught = 7,
        Escape = 8,
        Flee = 9,
        Transfer = 10,
        Evolve = 11,
        Egg = 12,
        Update = 13,
        Info = 14,
        Favorite = 15, //added by lars
        UnFavorite = 16, //added by lars
        Gym = 17,
		Telegram = 18,
        Debug = 19, //always have debug as last enum.
    }
}