using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Catchem.UiTranslation;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic.DataStorage;
using PoGo.PokeMobBot.Logic.Utils;

namespace Catchem.Helpers
{
    internal static class HeatMapHelper
    {

        public static async Task<Dictionary<int, GMapMarker>> GuildPokemonSeenHeatMap(List<PokemonSeen> seenList, int searchRad, Dispatcher dispatcher, CancellationToken token)
        {
            var heatMap = new Dictionary<int, GMapMarker>();
            while (seenList.Any())
            {
                List<PokemonSeen> group;
                try
                {
                    token.ThrowIfCancellationRequested();
                    group = GetBestPokeGroup2(seenList, searchRad, token);

                    if (group.Count == 0) break;
                }
                catch (Exception)
                {
                    return null;
                }

                var firstPokeToGroup = group.First();

                double circleSize = group.Count * 2 + 2;

                if (circleSize > searchRad)
                    circleSize = searchRad;


                await dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Brush ellipseBrush = new SolidColorBrush(Color.FromArgb(122, 32, 193, 8));

                    if (group.Count > 15)
                        ellipseBrush = new RadialGradientBrush(Color.FromArgb(122, 193, 59, 8), Color.FromArgb(122, 193, 166, 8));
                    else if (group.Count > 5)
                        ellipseBrush = new RadialGradientBrush(Color.FromArgb(122, 193, 166, 8), Color.FromArgb(122, 32, 193, 8));

                    var tooltipText =
                        string.Format(
                            TranslationEngine.GetDynamicTranslationString("%HEATMAP_SEEN%", "Seen {0} poke here"),
                            group.Count);

                    var ellipse = new Ellipse
                    {
                        Fill = ellipseBrush,
                        StrokeThickness = 0,
                        Width = circleSize,
                        Height = circleSize,
                        ToolTip = new ToolTip {Content = tooltipText }
                    };

                    var marker =
                        new GMapMarker(GetMidPoint(group.Select(x => (new PointLatLng(x.Latitude, x.Longitude)))))
                        {
                            Shape = ellipse,
                            Offset = new Point(-circleSize/2, -circleSize/2),
                            ZIndex = 2
                        };
                    if (!heatMap.ContainsKey(firstPokeToGroup.Id))
                        heatMap.Add(firstPokeToGroup.Id, marker);
                }));
                
                foreach (var c in group)
                    seenList.Remove(c);
                await Task.Delay(1);
            }
            
            return heatMap;
        }


        private static List<PokemonSeen> GetBestPokeGroup(List<PokemonSeen> pokeList, int radiusToSearch)
        {
            var bestGroup = new List<PokemonSeen>();
            foreach (var p in pokeList)
            {
                var closePoke =
                    pokeList.Where(
                        x =>
                            LocationUtils.CalculateDistanceInMeters(x.Latitude, x.Longitude, p.Latitude,
                                p.Longitude) < radiusToSearch).ToList();
                if (closePoke.Count > bestGroup.Count)
                    bestGroup = closePoke;
            }
            return bestGroup;
        }

        private static List<PokemonSeen> GetBestPokeGroup2(List<PokemonSeen> pokeList, int radiusToSearch, CancellationToken token)
        {
            var bestGroup = new List<PokemonSeen>();
            var firstPoke = pokeList.FirstOrDefault();
            if (firstPoke == null) return bestGroup;

            bestGroup =
                   pokeList.Where(
                       x =>
                           LocationUtils.CalculateDistanceInMeters(x.Latitude, x.Longitude, firstPoke.Latitude,
                               firstPoke.Longitude) < radiusToSearch).ToList();
            var foundBetter = true;
            while (foundBetter)
            {
                token.ThrowIfCancellationRequested();
                foundBetter = false;
                foreach (var p in bestGroup)
                {
                    var closePokeLoop =
                        pokeList.Where(
                            x =>
                                LocationUtils.CalculateDistanceInMeters(x.Latitude, x.Longitude, p.Latitude,
                                    p.Longitude) < radiusToSearch);
                    if (closePokeLoop.Count() > bestGroup.Count)
                    {
                        bestGroup = new List<PokemonSeen>(closePokeLoop);
                        foundBetter = true;
                        break;
                    }
                }
            }
            return bestGroup;
        }

        private static PointLatLng GetMidPoint(IEnumerable<PointLatLng> points)
        {
            var curWeing = 1;
            var pointLatLngs = points as PointLatLng[] ?? points.ToArray();
            var mid = pointLatLngs.First();
            foreach (var p in pointLatLngs.Skip(1))
            {
                var midLat = mid.Lat + (p.Lat - mid.Lat) / (curWeing + 1);
                var midLon = mid.Lng + (p.Lng - mid.Lng) / (curWeing + 1);
                mid = new PointLatLng(midLat, midLon);
                curWeing++;
            }

            return mid;
        }

        public static PointLatLng GetMidPointAndRadius(IEnumerable<PointLatLng> points, out double radius)
        {
            var mid = GetMidPoint(points);
            double dist = 500;
            foreach (var p in points)
            {
                var curDist = LocationUtils.CalculateDistanceInMeters(p.Lat, p.Lng, mid.Lat, mid.Lng);
                if (curDist > dist)
                    dist = curDist;
            }
            if (dist > 500)
                dist += 500;
            radius = dist;
            return mid;
        }
    }
}
