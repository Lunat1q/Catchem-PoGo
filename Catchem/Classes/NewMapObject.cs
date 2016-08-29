namespace Catchem.Classes
{
    public class NewMapObject
    {
        public string OType;
        public string OName;
        public double Lat;
        public double Lng;
        internal string Uid;

        public NewMapObject(string oType, string oName, double lat, double lng, string uid)
        {
            OType = oType;
            OName = oName;
            Lat = lat;
            Lng = lng;
            Uid = uid;
        }

        public NewMapObject()
        {
            
        }
    }
}
