using PoGo.PokeMobBot.Logic.Enums;
using PoGo.PokeMobBot.Logic.State;

namespace Catchem.Classes
{
    internal class BotRpcMessage
    {
        public MainRpc Type;
        public ISession Session;
        public object[] ParamObjects;
        public BotRpcMessage (MainRpc type, ISession session, params object[] objData)
        {
            Type = type;
            Session = session;
            ParamObjects = objData;
        }
    }
}
