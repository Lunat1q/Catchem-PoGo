using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using GeoCoordinatePortable;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using System.Xml;
using System.Globalization;
using PoGo.PokeMobBot.Logic.Extensions;

namespace PoGo.PokeMobBot.Logic
{
    public static class OSMRouting
    {
        private static string GetProperCoordString(GeoCoordinate coord)
        {
            return $"{coord.Longitude.ToString().Replace(',','.')} {coord.Latitude.ToString().Replace(',', '.')}";
        }
        public static OSMResponse GetRoute(GeoCoordinate start, GeoCoordinate dest, ISession session)
        {
            try
            {
                Logger.Write("Requesting routing info to http://openls.geog.uni-heidelberg.de", LogLevel.Debug);
                //var coordsFrom = GetProperCoordString(start.Latitude, start.Longitude);
                //var coordsTo = GetProperCoordString(dest.Latitude, dest.Longitude);                
                //WebRequest request = WebRequest.Create(
                //  $"http://openls.geog.uni-heidelberg.de" + $"/route?start={coordsFrom}&end={coordsTo}&via=&lang=en&distunit=KM&routepref=Pedestrian&weighting=Fastest&SurfaceInformation=true&ElevationInformation=true&instructions=false");
                //request.Credentials = CredentialCache.DefaultCredentials;
                //request.Proxy = session.Proxy;
                //WebResponse response = request.GetResponse();
                var responseFromServer = postXMLData("http://openls.geog.uni-heidelberg.de/testing2015/routing", PrepareRequest(start, dest), session.Proxy);
                if (responseFromServer != null)
                    Logger.Write("Got response from http://openls.geog.uni-heidelberg.de", LogLevel.Debug);
                else
                    Logger.Write("Wrong response from http://openls.geog.uni-heidelberg.de, we doomed", LogLevel.Debug);
                //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                //Stream dataStream = response.GetResponseStream();
                //StreamReader reader = new StreamReader(dataStream);
                //string responseFromServer = reader.ReadToEnd();
                //Console.WriteLine(responseFromServer);
                OSMResponse responseParsed = HandleResponse(responseFromServer);
                //reader.Close();
                //response.Close();

                return responseParsed;
            }
            catch(Exception ex)
            {
                Logger.Write("Routing error: " + ex.Message, LogLevel.Debug);
            }
            OSMResponse emptyResponse = new OSMResponse();
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
            xmlString.AppendLine("               <xls:WeightingMethod>Fastest</xls:WeightingMethod>");
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

        private static string postXMLData(string destinationUrl, string requestXml, IWebProxy proxy)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(destinationUrl);
            request.Proxy = proxy;
            byte[] bytes;
            bytes = System.Text.Encoding.ASCII.GetBytes(requestXml);
            request.ContentType = "text/xml; encoding='utf-8'";
            request.ContentLength = bytes.Length;
            request.Method = "POST";
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            HttpWebResponse response;
            response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream responseStream = response.GetResponseStream();
                string responseStr = new StreamReader(responseStream).ReadToEnd();
                response.Close();
                return responseStr;
            }
            return null;
        }
        private static OSMResponse HandleResponse(string responseFromServer)
        {
            OSMResponse resp = new OSMResponse();
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(responseFromServer);
            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(xmldoc.NameTable);
            xmlnsManager.AddNamespace("xls", "http://www.opengis.net/xls");
            xmlnsManager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xmlnsManager.AddNamespace("gml", "http://www.opengis.net/gml");

            try
            {
                var coordNodes = xmldoc.SelectNodes("/xls:XLS/xls:Response/xls:DetermineRouteResponse/xls:RouteGeometry/gml:LineString/gml:pos", xmlnsManager);
                var points = new List<GeoCoordinate>();
                if (coordNodes != null && coordNodes.Count > 0)
                {
                    Random rnd = new Random();
                    foreach (XmlNode node in coordNodes)
                    {
                        var coordinate = node.InnerText;
                        if (coordinate != string.Empty)
                        {
                            string[] XY = coordinate.Split(' ');
                            if (XY.Length == 3)
                            {
                                double lat = double.Parse(XY[1], CultureInfo.InvariantCulture);
                                double lng = double.Parse(XY[0], CultureInfo.InvariantCulture);
                                double alt = double.Parse(XY[2], CultureInfo.InvariantCulture) + 0.7 + rnd.NextInRange(0.1, 0.3);
                                points.Add(new GeoCoordinate(lat, lng, alt));
                            }
                        }
                    }
                    resp.Success = true;
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
    public class OSMResponse
    {
        public List<GeoCoordinate> Coordinates = new List<GeoCoordinate>();
        public bool Success = false;
    }
}
