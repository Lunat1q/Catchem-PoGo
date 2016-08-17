using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoGo.PokeMobBot.Logic.Utils
{
    public static class SerializeUtils
    {
        public static bool SerializeDataJson<T>(this T data, string path)
        {
            try
            {
                var p = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(p))
                    Directory.CreateDirectory(p);
                var js = new JsonSerializer();
                js.Converters.Add(new JavaScriptDateTimeConverter());
                js.NullValueHandling = NullValueHandling.Ignore;
                js.Formatting = Formatting.Indented;
                js.TypeNameHandling = TypeNameHandling.Objects;

                using (StreamWriter sw = new StreamWriter(path))
                using (JsonWriter writer = new JsonTextWriter(sw))
                    js.Serialize(writer, data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static T DeserializeDataJson<T>(string path)
        {
            if (!File.Exists(path))
                return default(T);
            try
            {
                using (var file = File.OpenText(path))
                {
                    var serializer = new JsonSerializer {TypeNameHandling = TypeNameHandling.Objects};
                    serializer.Converters.Add(new JavaScriptDateTimeConverter());
                    var data = (T)serializer.Deserialize(file, typeof(T));
                    return data;
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
