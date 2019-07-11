using System.Linq;
using Newtonsoft.Json;

namespace PoGo.PokeMobBot.Logic.Utils
{
    public class ScheduleAction
    {
        public int Hour;
        public int Day;

        [JsonIgnore]
        public bool Done;

        [JsonIgnore]
        public string Args
        {
            get
            {
                if (ActionArgs == null || ActionArgs.Length == 0) return "";
                return ActionArgs.Aggregate((x, v) => x + " | " + v);
            }
        }

        public ScheduleActionType ActionType { get; set; }
        public string[] ActionArgs;
    }

    public enum ScheduleActionType
    {
        ChangeRoute,
        ChangeLocation,
        ChangeSettings
    }
}
