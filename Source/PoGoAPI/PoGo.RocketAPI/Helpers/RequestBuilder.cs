using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;
using POGOProtos.Networking;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using PoGoLibrary.Providers;
using PokemonGo.RocketAPI.Extensions;
// ReSharper disable RedundantAssignment

namespace PokemonGo.RocketAPI.Helpers
{
    public class RequestBuilder
    {
        private readonly string _authToken;
        private readonly AuthType _authType;
        private readonly double _latitude;
        private readonly double _longitude;
        private readonly double _altitude;
        private readonly AuthTicket _authTicket;
        private readonly Stopwatch _internalWatch = new Stopwatch();
        private readonly ISettings _settings;

        private ByteString SessionHash
        {
            get { return _settings.SessionHash; }
            set { _settings.SessionHash = value; }
        }

        public void GenerateNewHash()
        {
            var hashBytes = new byte[16];

            RandomDevice.NextBytes(hashBytes);

            SessionHash = ByteString.CopyFrom(hashBytes);
        }

        public RequestBuilder(string authToken, AuthType authType, double latitude, double longitude, double altitude, ISettings settings, AuthTicket authTicket = null)
        {
            _authToken = authToken;
            _authType = authType;
            _latitude = latitude;
            _longitude = longitude;
            _altitude = altitude;
            _settings = settings;
            _authTicket = authTicket;
            if (!_internalWatch.IsRunning)
                _internalWatch.Start();

            if (SessionHash == null)
            {
                GenerateNewHash();
            }

            //if (_encryptNative != null) return;
            //if (IntPtr.Size == 4)
            //{
            //    _encryptNativeInit = (EncryptInitDelegate)
            //        FunctionLoader.LoadFunction<EncryptInitDelegate>(
            //            @"Resources\encrypt32.dll", "MobBot");

            //    _encryptNative = (EncryptDelegate)
            //        FunctionLoader.LoadFunction<EncryptDelegate>(
            //            @"Resources\encrypt32.dll", "encryptMobBot");
            //}
            //else
            //{
            //    _encryptNativeInit = (EncryptInitDelegate)
            //        FunctionLoader.LoadFunction<EncryptInitDelegate>(
            //            @"Resources\encrypt64.dll", "MobBot");

            //    _encryptNative = (EncryptDelegate)
            //        FunctionLoader.LoadFunction<EncryptDelegate>(
            //            @"Resources\encrypt64.dll", "encryptMobBot");
            //}
        }

        private Unknown6 GenerateSignature(IEnumerable<IMessage> requests)
        {
            var accelNextZ = RandomDevice.NextInRange(5.8125, 10.125); //9,80665
            var accelNextX = RandomDevice.NextInRange(-0.513123, 0.61231567); //Considering we handle phone only in 2 directions
            var accelNextY = Math.Sqrt(96.16744225D - accelNextZ * accelNextZ) * ((accelNextZ > 9.8) ? -1 : 1);

            var sig = new Signature
            {
                TimestampSinceStart = (ulong)_internalWatch.ElapsedMilliseconds,
                Timestamp = (ulong)DateTime.UtcNow.ToUnixTime(),
                DeviceInfo = new Signature.Types.DeviceInfo()
                {
                    DeviceId = _settings.DeviceId,
                    AndroidBoardName = _settings.AndroidBoardName,
                    AndroidBootloader = _settings.AndroidBootloader,
                    DeviceBrand = _settings.DeviceBrand,
                    DeviceModel = _settings.DeviceModel,
                    DeviceModelIdentifier = _settings.DeviceModelIdentifier,
                    DeviceModelBoot = _settings.DeviceModelBoot,
                    HardwareManufacturer = _settings.HardwareManufacturer,
                    HardwareModel = _settings.HardwareModel,
                    FirmwareBrand = _settings.FirmwareBrand,
                    FirmwareTags = _settings.FirmwareTags,
                    FirmwareType = _settings.FirmwareType,
                    FirmwareFingerprint = _settings.FirmwareFingerprint
                }
            };
            sig.SensorInfo.Add(new Signature.Types.SensorInfo
            {
                GravityZ = accelNextZ,
                GravityX = accelNextX,
                GravityY = accelNextY,
                TimestampSnapshot = (ulong)_internalWatch.ElapsedMilliseconds - 230,
                MagneticFieldX = accelNextX * 10,
                MagneticFieldY = -20 + -20 * accelNextY / 9.8065,
                MagneticFieldZ = -40 * accelNextZ / 9.8065,
                AttitudePitch = Math.Acos(accelNextX / 9.8065),
                AttitudeRoll = Math.Acos(accelNextY / 9.8065),
                AttitudeYaw = Math.Acos(accelNextZ / 9.8065),
                LinearAccelerationX = RandomDevice.NextInRange(-0.005, 0.005),
                LinearAccelerationY = RandomDevice.NextInRange(0.5, 1),
                LinearAccelerationZ = RandomDevice.NextInRange(-0.05, 0.05),
                RotationRateX = RandomDevice.NextInRange(-0.0001, 0.0001),
                RotationRateY = RandomDevice.NextInRange(-0.0005, 0.0005),
                RotationRateZ = RandomDevice.NextInRange(-0.003, 0.003),
                MagneticFieldAccuracy = RandomDevice.Next(10),
                Status = 3
            });

            sig.LocationFix.Add(new Signature.Types.LocationFix()
            {
                Provider = "fused",

                //Unk4 = 120,
                Latitude = (float)_latitude,
                Longitude = (float)_longitude,
                Altitude = (float)_altitude,
                Speed = -1,
                Course = -1,
                //TimestampSinceStart = (ulong)InternalWatch.ElapsedMilliseconds - 200,
                TimestampSnapshot = (ulong)_internalWatch.ElapsedMilliseconds - 200,
                Floor = 3,
                HorizontalAccuracy = (float)Math.Round(RandomDevice.NextInRange(4, 10), 6), //10
                VerticalAccuracy = RandomDevice.Next(3, 7),
                ProviderStatus = 3,
                LocationType = 1
            });

            //Compute 10
            
            byte[] serializedTicket = _authTicket.ToByteArray();

            uint firstHash = HashBuilder.Hash32(serializedTicket);

            var locationBytes = BitConverter.GetBytes(_latitude).Reverse()
                .Concat(BitConverter.GetBytes(_longitude).Reverse())
                .Concat(BitConverter.GetBytes(_altitude).Reverse()).ToArray();

           sig.LocationHash1 = HashBuilder.Hash32Salt(locationBytes, firstHash);

          //Compute 20
            sig.LocationHash2 = HashBuilder.Hash32(locationBytes);

            //Compute 24
            ulong seed = HashBuilder.Hash64(_authTicket.ToByteArray());
            foreach (var req in requests)
                
                sig.RequestHash.Add(HashBuilder.Hash64Salt64(req.ToByteArray(), seed));

            //static for now
            //sig.Unk22 = ByteString.CopyFrom(0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F);
            sig.SessionHash = SessionHash;
            sig.Unknown25 = 16892874496697272497; //16892874496697272497; //7363665268261373700; //-8537042734809897855;



            var val = new Unknown6
            {
                RequestType = 6,
                Unknown2 = new Unknown6.Types.Unknown2 { Unknown1 = ByteString.CopyFrom(PCrypt.encrypt(sig.ToByteArray(), (uint)sig.TimestampSinceStart)) }
            };
            return val;
        }

        public RequestEnvelope GetRequestEnvelope(params Request[] customRequests)
        {
            if (_authTicket == null) return new RequestEnvelope();
            var e = new RequestEnvelope
            {
                StatusCode = 2, //1

                RequestId = (ulong)DateTime.UtcNow.ToUnixTime() + (ulong)(RandomDevice.NextDouble() * 1000000 - 0.000001), //3
                Requests = { customRequests }, //4

                //Unknown6 = , //6
                Latitude = _latitude, //7
                Longitude = _longitude, //8
                Altitude = _altitude, //9
                AuthTicket = _authTicket, //11
                MsSinceLastLocationfix = RandomDevice.Next(700, 999) //12
            };
            e.Unknown6 = new Unknown6();
            e.Unknown6.MergeFrom(GenerateSignature(customRequests));
            return e;
        }
        


        public RequestEnvelope GetInitialRequestEnvelope(params Request[] customRequests)
        {
            var e = new RequestEnvelope
            {
                StatusCode = 2, //1

                RequestId = (ulong)DateTime.UtcNow.ToUnixTime() * 1000000 + (ulong)(RandomDevice.NextDouble() * 1000000 - 0.000001),//1469378659230941192, //3 
                Requests = { customRequests }, //4

                //Unknown6 = , //6
                Latitude = _latitude, //7
                Longitude = _longitude, //8
                Altitude = _altitude, //9
                AuthInfo = new RequestEnvelope.Types.AuthInfo
                {
                    Provider = _authType == AuthType.Google ? "google" : "ptc",
                    Token = new RequestEnvelope.Types.AuthInfo.Types.JWT
                    {
                        Contents = _authToken,
                        Unknown2 = 14
                    }
                }, //10
                MsSinceLastLocationfix = RandomDevice.Next(700, 999) //12
            };
            return e;
        }



        public RequestEnvelope GetRequestEnvelope(RequestType type, IMessage message)
        {
            return GetRequestEnvelope(new Request()
            {
                RequestType = type,
                RequestMessage = message.ToByteString()
            });

        }

        private static readonly Random RandomDevice = new Random();

        public static double GenRandom(double num)
        {
            var randomFactor = 0.3f;
            var randomMin = (num * (1 - randomFactor));
            var randomMax = (num * (1 + randomFactor));
            var randomizedDelay = RandomDevice.NextDouble() * (randomMax - randomMin) + randomMin;
            return randomizedDelay;
        }
    }
}
