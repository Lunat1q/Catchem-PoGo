using PoGo.PokeMobBot.Logic.State;

namespace Catchem.Classes
{
    internal class BotRpcMessage
    {
        public string Type;
        public ISession Session;
        public object[] ParamObjects;
        public BotRpcMessage (string type, ISession session, params object[] objData)
        {
            Type = type;
            Session = session;
            ParamObjects = objData;
        }
    }
}
