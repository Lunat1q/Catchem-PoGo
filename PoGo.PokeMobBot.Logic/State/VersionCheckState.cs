#region using directives

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;

#endregion

namespace PoGo.PokeMobBot.Logic.State
{
    public class VersionCheckState : IState
    {
        public const string VersionUri = 
            "https://raw.githubusercontent.com/Lunat1q/Catchem-PoGo/master/Catchem/Properties/AssemblyInfo.cs";

        public const string LatestReleaseApi =
            "https://api.github.com/repos/Lunat1q/Catchem-PoGo/releases/latest";

        private const string LatestRelease =
            "https://github.com/Lunat1q/Catchem-PoGo/releases";

        public static Version RemoteVersion;

        public async Task<IState> Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await CleanupOldFiles(session);
            var autoUpdate = session.LogicSettings.AutoUpdate;
            var needupdate = !IsLatest(session);
            if (!needupdate || !autoUpdate)
            {
                if (!needupdate)
                {
                    session.EventDispatcher.Send(new UpdateEvent
                    {
                        Message =
                            session.Translation.GetTranslation(TranslationString.GotUpToDateVersion, RemoteVersion)
                    });
                    return new LoginState();
                }
                session.EventDispatcher.Send(new UpdateEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.AutoUpdaterDisabled, LatestRelease)
                });

                return new LoginState();
            }
            session.EventDispatcher.Send(new UpdateEvent
            {
                Message = session.Translation.GetTranslation(TranslationString.DownloadingUpdate)
            });
            var remoteReleaseUrl =
                $"https://github.com/Lunat1q/Catchem-PoGo/releases/download/v{RemoteVersion}/";
            string zipName = $"Catchem_{RemoteVersion}.7z";
            var downloadLink = remoteReleaseUrl + zipName;
            var baseDir = Directory.GetCurrentDirectory();
            var downloadFilePath = Path.Combine(baseDir, zipName);
            var tempPath = Path.Combine(baseDir, "tmp");
            var extractedDir = Path.Combine(tempPath, "Release");
            var destinationDir = baseDir + Path.DirectorySeparatorChar;

            session.EventDispatcher.Send(new NoticeEvent() { Message = downloadLink });

            if (!DownloadFile(session, downloadLink, downloadFilePath))
                return new LoginState();

            session.EventDispatcher.Send(new UpdateEvent
            {
                Message = session.Translation.GetTranslation(TranslationString.FinishedDownloadingRelease)
            });

            if (!UnpackFile(downloadFilePath, tempPath))
                return new LoginState();

            session.EventDispatcher.Send(new UpdateEvent
            {
                Message = session.Translation.GetTranslation(TranslationString.FinishedUnpackingFiles)
            });

            if (!MoveAllFiles(extractedDir, destinationDir))
                return new LoginState();

            session.EventDispatcher.Send(new UpdateEvent
            {
                Message = session.Translation.GetTranslation(TranslationString.UpdateFinished)
            });

            if (TransferConfig(baseDir, session))
                session.EventDispatcher.Send(new UpdateEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.FinishedTransferringConfig)
                });

            await Task.Delay(2000, cancellationToken);

            Process.Start(Assembly.GetEntryAssembly().Location);
            Environment.Exit(-1);
            return null;
        }

        public static async Task CleanupOldFiles(ISession session)
        {
            var tmpDir = Path.Combine(Directory.GetCurrentDirectory(), "tmp");

            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }

            var di = new DirectoryInfo(Directory.GetCurrentDirectory());
            var files = di.GetFiles("*.old", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    if (file.Name.Contains("vshost"))
                        continue;
                    File.Delete(file.FullName);
                }
                catch (Exception e)
                {
                    session.EventDispatcher.Send(new ErrorEvent()
                    {
                        Message = e.ToString()
                    });
                }
            }
            await Task.Delay(200);
        }

        public bool DownloadFile(ISession session, string url, string dest)
        {
            using (var client = new WebClient() { Proxy = session.Proxy })
            {
                try
                {
                    client.DownloadFile(url, dest);
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = dest
                    });
                }
                catch
                {
                    // ignored
                }
                return true;
            }
        }

        private static string DownloadServerVersion(ISession session)
        {
            using (var wC = new WebClient() { Proxy = session.Proxy })
            {
                return wC.DownloadString(VersionUri);
            }
        }

        private JObject GetJObject(string filePath)
        {
            return JObject.Parse(File.ReadAllText(filePath));
        }

        public bool IsLatest(ISession session)
        {
            try
            {
                var regex = new Regex(@"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]");
                var match = regex.Match(DownloadServerVersion(session));

                if (!match.Success)
                    return false;

                var gitVersion = new Version($"{match.Groups[1]}.{match.Groups[2]}.{match.Groups[3]}.{match.Groups[4]}");
                RemoteVersion = gitVersion;
                if (gitVersion > Assembly.GetEntryAssembly().GetName().Version)
                {
                    session.EventDispatcher.Send(new NewVersionEvent
                    {
                        v = gitVersion
                    });
                    return false;
                }
                return true;              
            }
            catch (Exception)
            {
                return true; //better than just doing nothing when git server down
            }
        }

        public bool MoveAllFiles(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);

            var oldfiles = Directory.GetFiles(destFolder);
            foreach (var old in oldfiles)
            {
                if (old.Contains("vshost")) continue;
                File.Move(old, old + ".old");
            }

            try
            {
                var files = Directory.GetFiles(sourceFolder);
                foreach (var file in files)
                {
                    if (file.Contains("vshost")) continue;
                    var name = Path.GetFileName(file);
                    var dest = Path.Combine(destFolder, name);
                    File.Copy(file, dest, true);
                }

                var folders = Directory.GetDirectories(sourceFolder);

                foreach (var folder in folders)
                {
                    var name = Path.GetFileName(folder);
                    if (name == null) continue;
                    var dest = Path.Combine(destFolder, name);
                    MoveAllFiles(folder, dest);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private bool TransferConfig(string baseDir, ISession session)
        {
            if (!session.LogicSettings.TransferConfigAndAuthOnUpdate)
                return false;

            var configDir = Path.Combine(baseDir, "Config");
            if (!Directory.Exists(configDir))
                return false;

            var oldConf = GetJObject(Path.Combine(configDir, "config.json.old"));
            var oldAuth = GetJObject(Path.Combine(configDir, "auth.json.old"));

            GlobalSettings.Load("");

            var newConf = GetJObject(Path.Combine(configDir, "config.json"));
            var newAuth = GetJObject(Path.Combine(configDir, "auth.json"));

            TransferJson(oldConf, newConf);
            TransferJson(oldAuth, newAuth);

            File.WriteAllText(Path.Combine(configDir, "config.json"), newConf.ToString());
            File.WriteAllText(Path.Combine(configDir, "auth.json"), newAuth.ToString());

            return true;
        }

        private void TransferJson(JObject oldFile, JObject newFile)
        {
            try
            {
                foreach (var newProperty in newFile.Properties())
                    foreach (var oldProperty in oldFile.Properties())
                        if (newProperty.Name.Equals(oldProperty.Name))
                        {
                            newFile[newProperty.Name] = oldProperty.Value;
                            break;
                        }
            }
            catch
            {
                // ignored
            }
        }

        public bool UnpackFile(string sourceTarget, string destPath)
        {
            var source = sourceTarget;
            var dest = destPath;
            try
            {
                ZipFile.ExtractToDirectory(source, dest);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}