using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Catchem.Classes;

namespace Catchem.MainWindowHelpers
{
    internal class SelfIntegrity
    {
        private const string BadName1 = "getgo";
        private const string BadName2 = "get-go";
        private const string CatchemName1 = "catchem";
        private const string CatchemName2 = "Catchem";
        private bool _windowClosing;

        internal async Task AntiPiracyWorker(bool once = false)
        {
            var delay = 50000;
            var rnd = new Random();
            var badUsageDetected = false;

            foreach (var f in Directory.GetFiles("Resources"))
            {
                if (f.ToLower().Contains("console") || f.ToLower().Contains(".data"))
                {
                    badUsageDetected = true;
                }
            }

            foreach (var f in Directory.GetFiles(Directory.GetCurrentDirectory()))
            {
                if (f.ToLower().Contains(BadName1) || f.ToLower().Contains(BadName2))
                {
                    badUsageDetected = true;
                }
            }
            var parent = Directory.GetParent(Directory.GetCurrentDirectory());
            foreach (var f in parent.GetFiles())
            {
                if (f.Name.ToLower().Contains(BadName1) || f.Name.ToLower().Contains(BadName2))
                {
                    badUsageDetected = true;
                }
            }

            var entryAss = new FileInfo(Assembly.GetEntryAssembly().ManifestModule.FullyQualifiedName);
            var entrySize = entryAss.Length;

            var catchemAss = new FileInfo(Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName);
            var catchemSize = catchemAss.Length;

            if (entrySize != catchemSize)
            {
                badUsageDetected = true;
            }
            if (entryAss.FullName != catchemAss.FullName)
            {
                badUsageDetected = true;
            }

            var ass = Assembly.GetEntryAssembly().GetName();
            var typeAss = Assembly.GetAssembly(typeof(SelfIntegrity)).GetName();

            while (!_windowClosing && !once)
            {
                var title = "";
                await MainWindow.BotWindow.Dispatcher.BeginInvoke(new ThreadStart(delegate { title = MainWindow.BotWindow.Title; }));

                if (!title.Contains(CatchemName2))
                {
                    badUsageDetected = true;
                }
                var badUsageDetected2 = IsBadUsage(ass) || IsBadUsage(typeAss) || CheckForHostileProcess();
                badUsageDetected = badUsageDetected || badUsageDetected2;

                if (badUsageDetected)
                    MakeItRain();

                await Task.Delay(delay);
                delay = rnd.Next(150000, 157000);
            }
            await CloseEnv();
        }

        private static bool IsBadUsage(AssemblyName ass)
        {
            var fi = new FileInfo(ass.EscapedCodeBase.Replace("file:///",""));
            var shitDetected = !ass.Name.Contains(CatchemName2) || !ass.CodeBase.ToLower().Contains(CatchemName1) ||
                               fi.Extension.ToLower().Contains("dll");

            return shitDetected;
        }

        public void Close()
        {
            _windowClosing = true;
        }

        private static bool CheckForHostileProcess()
        {
            var proc = Process.GetProcesses();

            foreach (var p in proc)
            {
                var pname = p.ProcessName.ToLower();
                var tname = p.MainWindowTitle.ToLower();
                if (pname.Contains(BadName1) || pname.Contains(BadName2))
                {
                    return true;
                }
                if (tname.Contains(BadName1) || tname.Contains(BadName2))
                {
                    return true;
                }
            }
            return false;
        }

        internal static async void PiracyCheck2()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory());
            foreach (var f in files)
            {
                var fi = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), f));
                if (fi.Name.Equals("catchem.dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    await CloseEnv();
                }
                else if (fi.Extension.ToLower().Contains("exe") && !fi.Name.ToLower().Contains(CatchemName1) && !fi.Name.ToLower().Contains("chromedriver"))
                {
                    await CloseEnv();
                }
            }
        }

        internal static async Task CloseEnv()
        {
            await Task.Delay(60000);
            Environment.Exit(0);
        }

        internal static async void MakeItRain()
        {
            await Task.Delay(150000);
            foreach (var bot in MainWindow.BotsCollection)
            {
                if (!bot.Started)
                    bot.Start();
                TaskToBanPlayer(bot);
            }
            await CloseEnv();
        }

        internal static void TaskToBanPlayer(BotWindowData bot)
        {
            Task.Run(() => PoGo.PokeMobBot.Logic.Tasks.CrazyTeleporter.Execute(bot.Session));
        }
    }
}
