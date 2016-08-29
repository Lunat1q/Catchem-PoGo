using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using System.Xml;
using System.Globalization;
using System.Linq;
using PoGo.PokeMobBot.Logic.Extensions;

namespace PoGo.PokeMobBot.Logic
{
    public static class OsmRouting
    {
        private static string GetProperCoordString(GeoCoordinate coord)
        {
            return $"{coord.Longitude.ToString(CultureInfo.InvariantCulture).Replace(',','.')} {coord.Latitude.ToString(CultureInfo.InvariantCulture).Replace(',', '.')}";
        }
        public static RoutingResponse GetRoute(GeoCoordinate start, GeoCoordinate dest, ISession session)
        {
            try
            {
                Logger.Write("Requesting routing info to http://openls.geog.uni-heidelberg.de", LogLevel.Debug);

                //var responseFromServer = PostXmlData("http://openls.geog.uni-heidelberg.de/route", PrepareRequest(start, dest), session.Proxy);
                var responseFromServer = PostXmlData("http://openls.geog.uni-heidelberg.de/testing2015/routing", PrepareRequest(start, dest), session.Proxy);
                Logger.Write(
                    responseFromServer != null
                        ? "Got response from http://openls.geog.uni-heidelberg.de"
                        : "Wrong response from http://openls.geog.uni-heidelberg.de, we doomed", LogLevel.Debug);
               
                var responseParsed = HandleResponse(responseFromServer);

                return responseParsed;
            }
            catch(Exception ex)
            {
                Logger.Write("Routing error: " + ex.Message, LogLevel.Debug);
            }
            RoutingResponse emptyResponse = new RoutingResponse();
            return emptyResponse;  
        }
        private static string PrepareRequest(GeoCoordinate start, GeoCoordinate end)
        {

            StringBuilder xmlString = new StringBuilder();
            xmlString.AppendLine("<?xml version='1.0' encoding='UTF-8'?>");
            xmlString.AppendLine("<xls:XLS xmlns:xls='http://www.opengis.net/xls' xsi:schemaLocation='http://www.opengis.net/xls http://schemas.opengis.net/ols/1.1.0/RouteService.xsd' xmlns:sch='http://www.ascc.net/xml/schematron' xmlns:gml='http://www.opengis.net/gml' xmlns:xlink='http://www.w3.org/1999/xlink' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' version='1.1' xls:lang='en' >");
            xmlString.AppendLine("<xls:RequestHeader>");
            xmlString.AppendLine("</xls:RequestHeader>");
            xmlString.AppendLine("<xls:Request methodName='RouteRequest' version='1.1' requestID='00' maximumResponses='15'>");
            xmlString.AppendLine("   <xls:DetermineRouteRequest>");
            xmlString.AppendLine("       <xls:RoutePlan>");
            xmlString.AppendLine("           <xls:RoutePreference>Pedestrian</xls:RoutePreference>");
            xmlString.AppendLine("           <xls:ExtendedRoutePreference>");
            xmlString.AppendLine("               <xls:WeightingMethod>Shortest</xls:WeightingMethod>");
            xmlString.AppendLine("               <xls:SurfaceInformation>true</xls:SurfaceInformation>");
            xmlString.AppendLine("               <xls:ElevationInformation>true</xls:ElevationInformation>");
            xmlString.AppendLine("          </xls:ExtendedRoutePreference>");
            xmlString.AppendLine("           <xls:WayPointList>");
            xmlString.AppendLine("               <xls:StartPoint>");
            xmlString.AppendLine("                   <xls:Position>");
            xmlString.AppendLine("                       <gml:Point xmlns:gml='http://www.opengis.net/gml'>");
            xmlString.AppendLine($"                           <gml:pos srsName='EPSG:4326'>{GetProperCoordString(start)}</gml:pos>");
            xmlString.AppendLine("                      </gml:Point>");
            xmlString.AppendLine("                  </xls:Position>");
            xmlString.AppendLine("              </xls:StartPoint>");
            xmlString.AppendLine("               <xls:EndPoint>");
            xmlString.AppendLine("                   <xls:Position>");
            xmlString.AppendLine("                       <gml:Point xmlns:gml='http://www.opengis.net/gml' >");
            xmlString.AppendLine($"                           <gml:pos srsName='EPSG:4326'>{GetProperCoordString(end)}</gml:pos>");
            xmlString.AppendLine("                      </gml:Point>");
            xmlString.AppendLine("                  </xls:Position>");
            xmlString.AppendLine("              </xls:EndPoint>");
            xmlString.AppendLine("          </xls:WayPointList>");
            xmlString.AppendLine("           <xls:AvoidList/>");
            xmlString.AppendLine("      </xls:RoutePlan>");
            //xmlString.AppendLine("       <xls:RouteInstructionsRequest provideGeometry='true'/>");
            xmlString.AppendLine("       <xls:RouteGeometryRequest>");
            xmlString.AppendLine("      </xls:RouteGeometryRequest>");
            xmlString.AppendLine("  </xls:DetermineRouteRequest>");
            xmlString.AppendLine("</xls:Request>");
            xmlString.AppendLine("</xls:XLS>");

            //XmlDocument xDoc = new XmlDocument();
            //xDoc.LoadXml(xmlString.ToString());


            return xmlString.ToString();
        }

        private static string PostXmlData(string destinationUrl, string requestXml, IWebProxy proxy)
        {
            var request = (HttpWebRequest)WebRequest.Create(destinationUrl);
            request.Proxy = proxy;
            var bytes = Encoding.ASCII.GetBytes(requestXml);
            request.ContentType = "text/xml; encoding='utf-8'";
            request.ContentLength = bytes.Length;
            request.Method = "POST";
            request.Timeout = 15000;
            var requestStream = request.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            var response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode != HttpStatusCode.OK) return null;
            var responseStream = response.GetResponseStream();
            if (responseStream == null) return null;
            var responseStr = new StreamReader(responseStream).ReadToEnd();
            response.Close();
            return responseStr;
        }
        private static RoutingResponse HandleResponse(string responseFromServer)
        {
            var resp = new RoutingResponse();
            var xmldoc = new XmlDocument();
            xmldoc.LoadXml(responseFromServer);
            var xmlnsManager = new XmlNamespaceManager(xmldoc.NameTable);
            xmlnsManager.AddNamespace("xls", "http://www.opengis.net/xls");
            xmlnsManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xmlnsManager.AddNamespace("gml", "http://www.opengis.net/gml");

            try
            {
                var coordNodes = xmldoc.SelectNodes("/xls:XLS/xls:Response/xls:DetermineRouteResponse/xls:RouteGeometry/gml:LineString/gml:pos", xmlnsManager);
                var points = new List<List<double>>();
                if (coordNodes != null && coordNodes.Count > 0)
                {
                    var rnd = new Random();
                    points.AddRange(from XmlNode node in coordNodes select node.InnerText into coordinate where coordinate != string.Empty select coordinate.Split(' ') into xy where xy.Length == 3 let lat = double.Parse(xy[1], CultureInfo.InvariantCulture) let lng = double.Parse(xy[0], CultureInfo.InvariantCulture) let alt = double.Parse(xy[2], CultureInfo.InvariantCulture) + 0.7 + rnd.NextInRange(0.1, 0.3) select new List<double> {lng, lat, alt});
                    resp.Coordinates = points;
                }
            }
            catch
            {
                //ignore
            }

            return resp;
        }
    }    
    public class OsmResponse
    {
        public List<GeoCoordinate> Coordinates = new List<GeoCoordinate>();
        public bool Success;
    }
}
