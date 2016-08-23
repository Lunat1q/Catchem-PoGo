#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Utils;
using PoGo.PokeMobBot.Logic.Logging;
using PokemonGo.RocketAPI;
using POGOProtos.Networking.Responses;
using PoGo.PokeMobBot.Logic.Extensions;
using PoGo.PokeMobBot.Logic.State;

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
		    return rand.NextInRange(minimum, maximum);
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
                altitudeStep = (_client.Settings.DefaultAltitude - _client.rnd.NextInRange(_client.Settings.DefaultAltitudeMin, _client.Settings.DefaultAltitudeMax)) / (distanceToTarget / nextWaypointDistance);
                altitude = Math.Round(_client.Settings.DefaultAltitude - altitudeStep, round);
                altitudeStep = Math.Round(altitudeStep, round);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing, _client.Settings.DefaultAltitude);
            }

            //Initial walking

            var requestSendDateTime = DateTime.Now;
            var result =
                await
                    _client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude, waypoint.Altitude);

            UpdatePositionEvent?.Invoke(waypoint.Latitude, waypoint.Longitude, waypoint.Altitude);
            
            do
            {
                if(session.Runtime.BreakOutOfPathing)
                if (session.Runtime.lastPokeStopCoordinate.Latitude.Equals(targetLocation.Latitude) &&
                    session.Runtime.lastPokeStopCoordinate.Longitude.Equals(targetLocation.Latitude))
                {
                    session.Runtime.BreakOutOfPathing = true;
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var millisecondsUntilGetUpdatePlayerLocationResponse =
                    (DateTime.Now - requestSendDateTime).TotalMilliseconds;

                sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                var currentDistanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);

                //if (currentDistanceToTarget < 40)
                //{
                //    if (speedInMetersPerSecond > SpeedDownTo)
                //    {
                //        //Logger.Write("We are within 40 meters of the target. Speeding down to 10 km/h to not pass the target.", LogLevel.Info);
                //        speedInMetersPerSecond = SpeedDownTo;
                //    }
                //}

                nextWaypointDistance = Math.Min(currentDistanceToTarget,
                    millisecondsUntilGetUpdatePlayerLocationResponse/1000*speedInMetersPerSecond);
                nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation) + _client.rnd.NextInRange(-11.25, 11.25);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

                requestSendDateTime = DateTime.Now;
                result =
                    await
                        _client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude,
                           altitude);               

                UpdatePositionEvent?.Invoke(waypoint.Latitude, waypoint.Longitude, altitude);
                altitude -= altitudeStep;
                if (!trueAlt && (altitude < _client.Settings.DefaultAltitudeMin && altitude > _client.Settings.DefaultAltitudeMax)) //Keep altitude in range
                {
                    if (altitude < _client.Settings.DefaultAltitudeMin)
                        altitude = _client.Settings.DefaultAltitudeMin + _client.rnd.NextInRange(0.3, 0.5);
                    else
                        altitude = _client.Settings.DefaultAltitudeMin - _client.rnd.NextInRange(0.3, 0.5);
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
        /*
        public async Task<PlayerUpdateResponse> HumanLikeWalking(GeoCoordinate targetLocation,
            double walkingSpeedInKilometersPerHour, Func<Task<bool>> functionExecutedWhileWalking,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var speedInMetersPerSecond = walkingSpeedInKilometersPerHour / 3.6;

            var sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);
            // Logger.Write($"Distance to target location: {distanceToTarget:0.##} meters. Will take {distanceToTarget/speedInMetersPerSecond:0.##} seconds!", LogLevel.Info);

            var nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
            var nextWaypointDistance = speedInMetersPerSecond;
            var waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

            //Initial walking
            var requestSendDateTime = DateTime.Now;
            var result =
                await
                    _client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude,
                        _client.Settings.DefaultAltitude);

            UpdatePositionEvent?.Invoke(waypoint.Latitude, waypoint.Longitude);

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var millisecondsUntilGetUpdatePlayerLocationResponse =
                    (DateTime.Now - requestSendDateTime).TotalMilliseconds;

                sourceLocation = new GeoCoordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                var currentDistanceToTarget = LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation);

                if (currentDistanceToTarget < 40)
                {
                    if (speedInMetersPerSecond > SpeedDownTo)
                    {
                        //Logger.Write("We are within 40 meters of the target. Speeding down to 10 km/h to not pass the target.", LogLevel.Info);
                        speedInMetersPerSecond = SpeedDownTo;
                    }
                }

                nextWaypointDistance = Math.Min(currentDistanceToTarget,
                    millisecondsUntilGetUpdatePlayerLocationResponse / 1000 * speedInMetersPerSecond);
                nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, targetLocation);
                waypoint = LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing);

                requestSendDateTime = DateTime.Now;
                result =
                    await
                        _client.Player.UpdatePlayerLocation(waypoint.Latitude, waypoint.Longitude,
                            _client.Settings.DefaultAltitude);

                UpdatePositionEvent?.Invoke(waypoint.Latitude, waypoint.Longitude);


                if (functionExecutedWhileWalking != null)
                    await functionExecutedWhileWalking(); // look for pokemon
                await Task.Delay(500, cancellationToken);
            } while (LocationUtils.CalculateDistanceInMeters(sourceLocation, targetLocation) >= 30);

            return result;        
        }
        */


        //BACKUP OF HUMAN WALKING
    }
}