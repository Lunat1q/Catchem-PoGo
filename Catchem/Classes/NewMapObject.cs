namespace Catchem.Classes
{
    public class NewMapObject
    {
        public string OType;
        public string OName;
        public double Lat;
        public double Lng;
        internal string Uid;
        public object[] ExtraData;

        public NewMapObject(string oType, string oName, double lat, double lng, string uid, params object[] extraData)
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
}
