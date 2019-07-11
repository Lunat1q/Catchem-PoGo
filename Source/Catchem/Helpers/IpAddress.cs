using System.Collections.Generic;
using System.Linq;
// ReSharper disable PossibleMultipleEnumeration

namespace Catchem.Helpers
{
    public class IpAddress
    {
        public byte Main;
        public byte Net;
        public byte Subnet;
        public byte Client;

        public IpAddress GenerateNewAddress(IEnumerable<IpAddress> used)
        {
            if (used == null) return new IpAddress();
            var ip = new IpAddress(used.FirstOrDefault());
            foreach (var ipU in used)
            {
                if (Equal(ip, ipU))
                {
                    ip.Next();
                }
            }
            return ip;
        }

        public void Next()
        {
            Client++;
            if (Client <= 254) return;
            Client = 1;
            Subnet++;
            if (Subnet <= 254) return;
            Subnet = 0;
            Net++;
            if (Net <= 254) return;
            Main++;
            Net = 0;
            if (Main > 254)
            {
                Main = 1;
            }
        }

        public static bool Equal(IpAddress ip1, IpAddress ip2)
        {
            return ip1.Main == ip2.Main &&
                   ip1.Net == ip2.Net &&
                   ip1.Subnet == ip2.Subnet &&
                   ip1.Client == ip2.Client;
        }

        public IpAddress()
        {
            Main = 0;
            Net = 0;
            Subnet = 0;
            Client = 0;
        }

        public IpAddress(IpAddress addr)
        {
            Main = addr.Main;
            Net = addr.Net;
            Subnet = addr.Subnet;
            Client = 1;
        }
        public IpAddress(string ip)
        {
            var ipSplit = ip.Split('.');
            if (ipSplit.Length < 4)
            {
                Main = 0;
                Net = 0;
                Subnet = 0;
                Client = 0;
            }
            else
            {
                Main = byte.Parse(ipSplit[0]);
                Net = byte.Parse(ipSplit[1]);
                Subnet = byte.Parse(ipSplit[2]);
                Client = byte.Parse(ipSplit[3]);
            }
        }
        public override string ToString()
        {
            return $"{Main}.{Net}.{Subnet}.{Client}";
        }
    }
}
