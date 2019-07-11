namespace Catchem.Classes
{
    public class NewMapObject
    {
        public MapPbjectType OType;
        public string OName;
        public double Lat;
        public double Lng;
        internal string Uid;
        public object[] ExtraData;

        public NewMapObject(MapPbjectType oType, string oName, double lat, double lng, string uid, params object[] extraData)
        {
            OType = oType;
            OName = oName;
            Lat = lat;
            Lng = lng;
            Uid = uid;
            ExtraData = extraData;
        }

        public NewMapObject()
        {
            
        }
    }

    public enum MapPbjectType
    {
        Pokestop,
        PokestopLured,
        Pokemon,
        ForceMove,
        ForceMoveDone,
        PokemonRemove,
        PokestopRemove,
        PokestopUpdate,
        SetLured,
        SetUnLured,
        Gym
    }
}
