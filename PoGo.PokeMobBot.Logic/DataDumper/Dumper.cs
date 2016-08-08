#region using directives

using System;
using System.IO;
using PoGo.PokeMobBot.Logic.State;

#endregion

namespace PoGo.PokeMobBot.Logic.DataDumper
{
    public static class Dumper
    {
        /// <summary>
        ///     Clears the specified dumpfile.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="filename" />
        /// <param name="extension">Extension to be used for naming the file.</param>
        /// File to clear/param>
        public static void ClearDumpFile(ISession session, string filename, string extension = "txt")
        {
            var path = Path.Combine(session.LogicSettings.ProfilePath, "Dumps");
            var file = Path.Combine(path,
                $"PokeMobBot-{filename}-{DateTime.Today.ToString("yyyy-MM-dd")}-{DateTime.Now.ToString("HH")}.{extension}");
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                // Clears all contents of a file first if overwrite is true
                File.WriteAllText(file, string.Empty);
            }
            catch(IOException) { }
        }

        /// <summary>
        ///     Dumps data to a file
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data">Dumps the string data to the file</param>
        /// <param name="filename">Filename to be used for naming the file.</param>
        /// <param name="extension">Extension to be used for naming the file.</param>
        public static void Dump(ISession session, string data, string filename, string extension = "txt")
        {
            string uniqueFileName = $"{filename}";

            try
            {
                DumpToFile(session, data, uniqueFileName, extension);
            }
            catch(IOException) { }
        }

        /// <summary>
        ///     This is used for dumping contents to a file stored in the Logs folder.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="data">Dumps the string data to the file</param>
        /// <param name="filename">Filename to be used for naming the file.</param>
        /// <param name="extension">Extension to be used for naming the file.</param>
        private static void DumpToFile(ISession session, string data, string filename, string extension = "txt")
        {
            var path = Path.Combine(session.LogicSettings.ProfilePath, "Dumps",
                $"PokeMobBot-{filename}-{DateTime.Today.ToString("yyyy-MM-dd")}-{DateTime.Now.ToString("HH")}.{extension}");

            try
            {
                using (
                    var dumpFile =
                        File.AppendText(path)
                    )
                {
                    dumpFile.WriteLine(data);
                    dumpFile.Flush();
                }
            }
            catch (IOException) { }
        }

        /// <summary>
        ///     Set the dumper.
        /// </summary>
        /// <param name="dumper"></param>
        /// <param name="subPath"></param>
        public static void SetDumper(IDumper dumper, string subPath = "")
        {
        }
    }
}
