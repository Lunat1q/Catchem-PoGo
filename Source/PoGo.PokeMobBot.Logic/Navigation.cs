#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Utils;
using PoGo.PokeMobBot.Logic.Logging;
using PokemonGo.RocketAPI;
using POGOProtos.Networking.Responses;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using PokemonGo.RocketAPI.Extensions;
using RandomExtensions = PoGo.PokeMobBot.Logic.Extensions.RandomExtensions;

#endregion

namespace PoGo.PokeMobBot.Logic
{
    public delegate void UpdatePositionDelegate(double lat, double lng, double alt);

    public class Navigation
    {
        private readonly Client _client;
		private static Random rand = new Random();
		public double FuzzyFactorBearing()
        {
            const double maximum = -8.0f;
            const double minimum = 8.0f;
		    return RandomExtensions.NextInRange(rand, minimum, maximum);
        }

        public Navigation(Client client, UpdatePositionDelegate updatePos)
        {
            _client = client;
            UpdatePositionEvent = updatePos;
        }

        public Navigation(Client client)
        {
            _client = client;
        }

        public async Task<PlayerUpdateResponse> HumanPathWalking(ISession session, GeoCoordinate targetLocation,
            double walkingSpeedInKilometersPerHour, Func<Task<bool>> functionExecutedWhileWalking,
            Func<Task<bool>> functionExecutedWhileWalking2,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            //PlayerUpdateResponse result = null;


            var speedInMetersPerSecond = walkingSpeedInKilometersPerHour/3.6;

            var sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            double distanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);
            Logger.Write($"Distance to target location: {distanceToTarget:0.##} meters. Will take {distanceToTarget/speedInMetersPerSecond:0.##} seconds!", LogLevel.Debug);

            var nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
			nextWaypointBearing += FuzzyFactorBearing();            
			var nextWaypointDistance = speedInMetersPerSecond;
            GeoCoordinate waypoint;
            double altitudeStep;
            double altitude;
            var trueAlt = false;
            var round = rand.Next(5) == 0 ? 6 : 1;
            if (Math.Abs(targetLocation.Altitude) > 0.001)
            {
                trueAlt = true;
                altitudeStep = (_client.Settings.DefaultAltitude - targetLocation.Altitude) / (distanceToTarget / (nextWaypointDistance > 1 ? nextWaypointDistance : 1));
                
                altitude = Math.Round(_client.Settings.DefaultAltitude - altitudeStep, round);
                altitudeStep = Math.Round(altitudeStep, round);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing, altitude);
            }
            else
            {
                var altDiff = _client.Settings.DefaultAltitude -
                              RandomExtensions.NextInRange(_client.Rnd, _client.Settings.DefaultAltitudeMin,
                                  _client.Settings.DefaultAltitudeMax);
                altitudeStep = altDiff / (distanceToTarget / nextWaypointDistance);
                if (Math.Abs(altDiff) < Math.Abs(altitudeStep)) altitudeStep = altDiff;
                altitude = Math.Round(_client.Settings.DefaultAltitude - altitudeStep, round);
                altitudeStep = Math.Round(altitudeStep, round);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing, altitude);
            }

            //Initial walking

            var requestSendDateTime = DateTime.Now;
            var result =
                await
                    _client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude, waypoint.Altitude);

            UpdatePositionEvent?.Invoke(waypoint.Latitude, waypoint.Longitude, waypoint.Altitude);

            long actionQueueTimeStamp = 0;
            do
            {
                var factoredSpeed = speedInMetersPerSecond*session.Settings.MoveSpeedFactor;
                if (session.Runtime.BreakOutOfPathing)
                if (session.Runtime.lastPokeStopCoordinate.Latitude.Equals(targetLocation.Latitude) &&
                    session.Runtime.lastPokeStopCoordinate.Longitude.Equals(targetLocation.Latitude))
                {
                    session.Runtime.BreakOutOfPathing = true;
                    break;
                }
                if (session.ForceMoveJustDone) break;

                cancellationToken.ThrowIfCancellationRequested();

                var millisecondsUntilGetUpdatePlayerLocationResponse =
                    (DateTime.Now - requestSendDateTime).TotalMilliseconds;

                sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                var currentDistanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);

                nextWaypointDistance = Math.Min(currentDistanceToTarget,
                    millisecondsUntilGetUpdatePlayerLocationResponse/1000 * factoredSpeed);
                nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation) + RandomExtensions.NextInRange(_client.Rnd, -11.25, 11.25);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

                requestSendDateTime = DateTime.Now;
                result =
                    await
                        _client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude,
                           altitude);               

                UpdatePositionEvent?.Invoke(waypoint.Latitude, waypoint.Longitude, altitude);
                altitude -= altitudeStep;
                if (!trueAlt && (altitude < _client.Settings.DefaultAltitudeMin || altitude > _client.Settings.DefaultAltitudeMax)) //Keep altitude in range
                {
                    if (altitude < _client.Settings.DefaultAltitudeMin)
                        altitude = _client.Settings.DefaultAltitudeMin + RandomExtensions.NextInRange(_client.Rnd, 0.3, 0.5);
                    else
                        altitude = _client.Settings.DefaultAltitudeMin - RandomExtensions.NextInRange(_client.Rnd, 0.3, 0.5);
                }
                else if (trueAlt) //Keep altitude in range
                {
                    if (altitudeStep < 0 && altitude <= targetLocation.Altitude)
                    {
                        altitudeStep = 0;
                        altitude = targetLocation.Altitude;
                    }
                    else if (altitudeStep > 0 && altitude >= targetLocation.Altitude)
                    {
                        altitudeStep = 0;
                        altitude = targetLocation.Altitude;
                    }
                }
                if (functionExecutedWhileWalking != null)
                    await functionExecutedWhileWalking(); // look for pokemon & hit stops
                if(functionExecutedWhileWalking2 != null)
                    await functionExecutedWhileWalking2();

                if (actionQueueTimeStamp < DateTime.UtcNow.ToUnixTime())
                {
                    actionQueueTimeStamp = DateTime.UtcNow.AddMinutes(2).ToUnixTime();
                    await ActionQueueTask.Execute(session, cancellationToken);
                }
                await Task.Delay(300, cancellationToken);
            } while (LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation) >= 2 && session.Runtime.BreakOutOfPathing == false);
            if (trueAlt) altitude = targetLocation.Altitude;
            _client.Settings.DefaultAltitude = altitude;
            return result;
        }

        public async Task Teleport(GeoCoordinate targetLocation)
        {
            await _client.Player.UpdatePlayerLocation(
                targetLocation.Latitude,
                targetLocation.Longitude,
                _client.Settings.DefaultAltitude);

            UpdatePositionEvent?.Invoke(targetLocation.Latitude, targetLocation.Longitude, targetLocation.Altitude);
        }

        public event UpdatePositionDelegate UpdatePositionEvent;
    }
}