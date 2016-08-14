#region using directives

using System;
using System.Globalization;
using System.Threading;
using PoGo.PokeMobBot.Logic;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using PoGo.PokeMobBot.Logic.Utils;

#endregion

namespace PoGo.PokeMobBot.CLI
{
    internal class Program
    {
        // http://stackoverflow.com/questions/2586612/how-to-keep-a-net-console-app-running Save some CPU Cycles, speed things up
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        private static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            var culture = CultureInfo.CreateSpecificCulture("en-US");

            CultureInfo.DefaultThreadCurrentCulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

            var subPath = "";
            if (args.Length > 0)
                subPath = args[0];

#if DEBUG
            LogLevel logLevel = LogLevel.Debug;
#else
            LogLevel logLevel = LogLevel.Info;
#endif
            Logger.SetLogger(new ConsoleLogger(logLevel), subPath);

            var settings = GlobalSettings.Load(subPath);


            if (settings == null)
            {
                Logger.Write("This is your first start and the bot has generated the default config!", LogLevel.Warning);
                Logger.Write("After pressing a key the config folder will open and this commandline will close", LogLevel.Warning);

                //pauses console until keyinput
                Console.ReadKey();

                // opens explorer with location "config"
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = "config",
                    UseShellExecute = true,
                    Verb = "open"
                });
                Environment.Exit(0);
            }
            var session = new Session(new ClientSettings(settings), new LogicSettings(settings));
            session.Client.ApiFailure = new ApiFailureStrategy(session);


            /*SimpleSession session = new SimpleSession
            {
                _client = new PokemonGo.RocketAPI.Client(new ClientSettings(settings)),
                _dispatcher = new EventDispatcher(),
                _localizer = new Localizer()
            };

            BotService service = new BotService
            {
                _session = session,
                _loginTask = new Login(session)
            };

            service.Run();
            */

            var machine = new StateMachine();
            var stats = new Statistics();
            stats.DirtyEvent +=
                () =>
                    Console.Title =
                        stats.GetTemplatedStats(
                            session.Translation.GetTranslation(TranslationString.StatsTemplateString),
                            session.Translation.GetTranslation(TranslationString.StatsXpTemplateString));

            var aggregator = new StatisticsAggregator(stats);
            var listener = new ConsoleEventListener();
            var websocket = new WebSocketInterface(settings.StartUpSettings.WebSocketPort, session);

            session.EventDispatcher.EventReceived += evt => listener.Listen(evt, session);
            session.EventDispatcher.EventReceived += evt => aggregator.Listen(evt, session);
            session.EventDispatcher.EventReceived += evt => websocket.Listen(evt, session);

            machine.SetFailureState(new LoginState());

            Logger.SetLoggerContext(session);
            
            session.Navigation.UpdatePositionEvent +=
                (lat, lng, alt) => session.EventDispatcher.Send(new UpdatePositionEvent {Latitude = lat, Longitude = lng, Altitude = alt});

#if DEBUG
            machine.AsyncStart(new LoginState(), session);
#else
            machine.AsyncStart(new VersionCheckState(), session);
#endif
            if (session.LogicSettings.UseSnipeLocationServer)
                SnipePokemonTask.AsyncStart(session);

            _quitEvent.WaitOne();
        }
    }
}