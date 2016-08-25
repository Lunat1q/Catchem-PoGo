using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;
using POGOProtos.Networking;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonGo.RocketAPI.Extensions;
// ReSharper disable RedundantAssignment
using System.Security.Cryptography;

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
        private static readonly Stopwatch InternalWatch = new Stopwatch();
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
            if (!InternalWatch.IsRunning)
                InternalWatch.Start();

            if (SessionHash == null)
            {
                GenerateNewHash();
            }

            if (_encryptNative != null) return;
            if (IntPtr.Size == 4)
            {
                _encryptNativeInit = (EncryptInitDelegate)
                    FunctionLoader.LoadFunction<EncryptInitDelegate>(
                        @"Resources\encrypt32.dll", "MobBot");

                _encryptNative = (EncryptDelegate)
                    FunctionLoader.LoadFunction<EncryptDelegate>(
                        @"Resources\encrypt32.dll", "encryptMobBot");
            }
            else
            {
                _encryptNativeInit = (EncryptInitDelegate)
                    FunctionLoader.LoadFunction<EncryptInitDelegate>(
                        @"Resources\encrypt64.dll", "MobBot");

                _encryptNative = (EncryptDelegate)
                    FunctionLoader.LoadFunction<EncryptDelegate>(
                        @"Resources\encrypt64.dll", "encryptMobBot");
            }
        }

        private Unknown6 GenerateSignature(IEnumerable<IMessage> requests)
        {
            var accelNextZ = RandomDevice.NextInRange(5.8125, 10.125); //9,80665
            var accelNextX = RandomDevice.NextInRange(-0.513123, 0.61231567); //Considering we handle phone only in 2 directions
            var accelNextY = Math.Sqrt(96.16744225D - accelNextZ * accelNextZ) * ((accelNextZ > 9.8) ? -1 : 1);
            var sig = new Signature
            {
                TimestampSinceStart = (ulong)InternalWatch.ElapsedMilliseconds,
                Timestamp = (ulong)DateTime.UtcNow.ToUnixTime(),
                SensorInfo = new Signature.Types.SensorInfo()
                {
                    AccelNormalizedZ = accelNextZ,
                    AccelNormalizedX = accelNextX,
                    AccelNormalizedY = accelNextY,
                    TimestampSnapshot = (ulong)InternalWatch.ElapsedMilliseconds - 230,
                    MagnetometerX = accelNextX * 10,
                    MagnetometerY = -20 + -20 * accelNextY / 9.8065,
                    MagnetometerZ = -40 * accelNextZ / 9.8065,
                    AngleNormalizedX = Math.Acos(accelNextX / 9.8065),
                    AngleNormalizedY = Math.Acos(accelNextY / 9.8065),
                    AngleNormalizedZ = Math.Acos(accelNextZ / 9.8065),
                    AccelRawX = RandomDevice.NextInRange(-0.005, 0.005),
                    AccelRawY = RandomDevice.NextInRange(0.5, 1),
                    AccelRawZ = RandomDevice.NextInRange(-0.05, 0.05),
                    GyroscopeRawX = RandomDevice.NextInRange(-0.0001, 0.0001),
                    GyroscopeRawY = RandomDevice.NextInRange(-0.0005, 0.0005),
                    GyroscopeRawZ = RandomDevice.NextInRange(-0.003, 0.003),
                    AccelerometerAxes = 3
                },
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
            sig.LocationFix.Add(new Signature.Types.LocationFix()
            {
                Provider = "fused",

                //Unk4 = 120,
                Latitude = (float)_latitude,
                Longitude = (float)_longitude,
                Altitude = (float)_altitude,
                //TimestampSinceStart = (ulong)InternalWatch.ElapsedMilliseconds - 200,
                TimestampSnapshot = (ulong)InternalWatch.ElapsedMilliseconds - 200,
                Floor = 3,
                HorizontalAccuracy = (float)Math.Round(RandomDevice.NextInRange(4, 10), 6),
                VerticalAccuracy = RandomDevice.Next(3, 7),
                ProviderStatus = 3,
                LocationType = 1
            });

            //Compute 10
            var x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            var firstHash = BitConverter.ToUInt32(x.ComputeHash(_authTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(32, firstHash);
            var locationBytes = BitConverter.GetBytes(_latitude).Reverse()
                .Concat(BitConverter.GetBytes(_longitude).Reverse())
                .Concat(BitConverter.GetBytes(_altitude).Reverse()).ToArray();
            sig.LocationHash1 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            //Compute 20
            x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            sig.LocationHash2 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            //Compute 24
            x = new System.Data.HashFunction.xxHash(64, 0x1B845238);
            var seed = BitConverter.ToUInt64(x.ComputeHash(_authTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(64, seed);
            foreach (var req in requests)
                sig.RequestHash.Add(BitConverter.ToUInt64(x.ComputeHash(req.ToByteArray()), 0));

            //static for now
            //sig.Unk22 = ByteString.CopyFrom(0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F);
            sig.SessionHash = SessionHash;
            sig.Unknown25 = -7363665268261373700; //-8537042734809897855;



            var val = new Unknown6
            {
                RequestType = 6,
                Unknown2 = new Unknown6.Types.Unknown2 { Unknown1 = ByteString.CopyFrom(Encrypt(sig.ToByteArray())) }
            };
            return val;
        }

        private static byte[] GetURandom(int size)
        {
            var rng = new RNGCryptoServiceProvider();
            var buffer = new byte[size];
            rng.GetBytes(buffer);
            return buffer;
        }

        private byte[] Encrypt(byte[] bytes)
        {
            var outputLength = 32 + bytes.Length + (256 - (bytes.Length % 256));
            var ptr = Marshal.AllocHGlobal(outputLength);
            var ptrOutput = Marshal.AllocHGlobal(outputLength);
            FillMemory(ptr, (uint)outputLength, 0);
            FillMemory(ptrOutput, (uint)outputLength, 0);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            var iv = GetURandom(32);
            var iv_ptr = Marshal.AllocHGlobal(iv.Length);
            Marshal.Copy(iv, 0, iv_ptr, iv.Length);

            var magic = Encoding.ASCII.GetBytes("We love PokeMobBot, They are the true gangstas, everyone else is just a poser!");
            var magic_ptr = Marshal.AllocHGlobal(magic.Length);
            Marshal.Copy(magic, 0, magic_ptr, magic.Length);

            try
            {
                var outputSize = outputLength;
                //Console.WriteLine("Testing Sign");
                int res = _encryptNativeInit(magic_ptr);
                //Console.WriteLine(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                var outputSize = outputLength;
                //_encryptNative(ptr, bytes.Length, new byte[32], 32, ptrOutput, out outputSize);
                //_encryptNative(ptr, bytes.Length, iv_ptr, iv.Length, ptrOutput, out outputSize);
                //Console.WriteLine("Testing Encrypt");
                int res = _encryptNative(ptr, bytes.Length, iv_ptr, iv.Length, ptrOutput, out outputSize);
                //Console.WriteLine(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            var output = new byte[outputLength];
            Marshal.Copy(ptrOutput, output, 0, outputLength);
            return output;
        }

        private static class FunctionLoader
        {
            [DllImport("Kernel32.dll")]
            private static extern IntPtr LoadLibrary(string path);

            [DllImport("Kernel32.dll")]
            private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            public static Delegate LoadFunction<T>(string dllPath, string functionName)
            {
                var hModule = LoadLibrary(dllPath);
                var functionAddress = GetProcAddress(hModule, functionName);
                return Marshal.GetDelegateForFunctionPointer(functionAddress, typeof(T));
            }
        }
        //private delegate int EncryptDelegate(IntPtr arr, int length, byte[] iv, int ivsize, IntPtr output, out int outputSize);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate int EncryptDelegate(IntPtr arr, int length, IntPtr iv, int ivsize, IntPtr output, out int outputSize);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate int EncryptInitDelegate(IntPtr input);
        //private unsafe delegate int EncryptDelegate(IntPtr arr, int length, byte[] iv, int ivsize, IntPtr output, out int outputSize);

        private static EncryptInitDelegate _encryptNativeInit;
        private static EncryptDelegate _encryptNative;

        //[DllImport("Resources/encrypt.dll", EntryPoint = "encrypt", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        //static extern private void EncryptNative(IntPtr arr, int length, byte[] iv, int ivsize, IntPtr output, out int outputSize);
        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        static extern void FillMemory(IntPtr destination, uint length, byte fill);

        public RequestEnvelope GetRequestEnvelope(params Request[] customRequests)
        {
            if (_authTicket == null) return new RequestEnvelope();
            var e = new RequestEnvelope
            {
                StatusCode = 2, //1

                RequestId = 1469378659230941192, //3
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

                RequestId = 1469378659230941192, //3
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