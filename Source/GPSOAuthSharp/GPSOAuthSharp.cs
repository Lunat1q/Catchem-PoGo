using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable once CheckNamespace
namespace DankMemes.GPSOAuthSharp
{
    // gpsoauth:__init__.py
    // URL: https://github.com/simon-weber/gpsoauth/blob/master/gpsoauth/__init__.py
    // ReSharper disable once InconsistentNaming
    public class GPSOAuthClient
    {
        private const string B64Key = "AAAAgMom/1a/v0lblO2Ubrt60J2gcuXSljGFQXgcyZWveWLEwo6prwgi3" + "iJIZdodyhKZQrNWp5nKJ3srRXcUW+F1BD3baEVGcmEgqaLZUNBjm057pK" + "RI16kB0YppeGx5qIQ5QjKzsR8ETQbKLNWgRY0QRNVz34kMJR3P/LgHax/" + "6rmf5AAAAAwEAAQ==";

        private static readonly RSAParameters AndroidKey = GoogleKeyUtils.KeyFromB64(B64Key);

        private const string Version = "0.0.5";
        private const string AuthUrl = "https://android.clients.google.com/auth";
        private const string UserAgent = "GPSOAuthSharp/" + Version;

        private readonly string _email;
        private readonly string _password;
        private readonly IWebProxy _proxy;

        public GPSOAuthClient(string email, string password, IWebProxy proxy = null)
        {
            _email = email;
            _password = password;
            _proxy = proxy;
        }

        // _perform_auth_request
        private Dictionary<string, string> PerformAuthRequest(Dictionary<string, string> data)
        {
            var nvc = new NameValueCollection();
            if (data == null) return new Dictionary<string, string> {{"Error", "Data looks like null, are u sure u used Application specific password?"}};
            foreach (var kvp in data)
            {
                nvc.Add(kvp.Key, kvp.Value);
            }
            using (var client = new WebClient() { Proxy = _proxy })
            {
                client.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);
                var result = "";
                try
                {
                    var response = client.UploadValues(AuthUrl, nvc);
                    result = Encoding.UTF8.GetString(response);
                }
                catch (WebException e)
                {
                    var resp = e.Response?.GetResponseStream();
                    if (resp != null)
                        result = new StreamReader(resp).ReadToEnd();
                }
                return GoogleKeyUtils.ParseAuthResponse(result);
            }
        }

        // perform_master_login
        public Dictionary<string, string> PerformMasterLogin(string service = "ac2dm",
            string deviceCountry = "us", string operatorCountry = "us", string lang = "en", int sdkVersion = 21)
        {
            var signature = GoogleKeyUtils.CreateSignature(_email, _password, AndroidKey);
            var dict = new Dictionary<string, string> {
                { "accountType", "HOSTED_OR_GOOGLE" },
                { "Email", _email },
                { "has_permission", 1.ToString() },
                { "add_account", 1.ToString() },
                { "EncryptedPasswd",  signature},
                { "service", service },
                { "source", "android" },
                { "device_country", deviceCountry },
                { "operatorCountry", operatorCountry },
                { "lang", lang },
                { "sdk_version", sdkVersion.ToString() }
            };
            return PerformAuthRequest(dict);
        }

        // perform_oauth
        public Dictionary<string, string> PerformOAuth(string masterToken, string service, string app, string clientSig,
            string deviceCountry = "us", string operatorCountry = "us", string lang = "en", int sdkVersion = 21)
        {
            var dict = new Dictionary<string, string> {
                { "accountType", "HOSTED_OR_GOOGLE" },
                { "Email", _email },
                { "has_permission", 1.ToString() },
                { "EncryptedPasswd",  masterToken},
                { "service", service },
                { "source", "android" },
                { "app", app },
                { "client_sig", clientSig },
                { "device_country", deviceCountry },
                { "operatorCountry", operatorCountry },
                { "lang", lang },
                { "sdk_version", sdkVersion.ToString() }
            };
            return PerformAuthRequest(dict);
        }
    }

    // gpsoauth:google.py
    // URL: https://github.com/simon-weber/gpsoauth/blob/master/gpsoauth/google.py
    class GoogleKeyUtils
    {
        // key_from_b64
        // BitConverter has different endianness, hence the Reverse()
        public static RSAParameters KeyFromB64(string b64Key)
        {
            var decoded = Convert.FromBase64String(b64Key);
            var modLength = BitConverter.ToInt32(decoded.Take(4).Reverse().ToArray(), 0);
            var mod = decoded.Skip(4).Take(modLength).ToArray();
            var expLength = BitConverter.ToInt32(decoded.Skip(modLength + 4).Take(4).Reverse().ToArray(), 0);
            var exponent = decoded.Skip(modLength + 8).Take(expLength).ToArray();
            var rsaKeyInfo = new RSAParameters
            {
                Modulus = mod,
                Exponent = exponent
            };
            return rsaKeyInfo;
        }

        // key_to_struct
        // Python version returns a string, but we use byte[] to get the same results
        public static byte[] KeyToStruct(RSAParameters key)
        {
            byte[] modLength = { 0x00, 0x00, 0x00, 0x80 };
            var mod = key.Modulus;
            byte[] expLength = { 0x00, 0x00, 0x00, 0x03 };
            var exponent = key.Exponent;
            return DataTypeUtils.CombineBytes(modLength, mod, expLength, exponent);
        }

        // parse_auth_response
        public static Dictionary<string, string> ParseAuthResponse(string text)
        {
            var responseData = new Dictionary<string, string>();
            foreach (var line in text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('=');
                responseData.Add(parts[0], parts[1]);
            }
            return responseData;
        }

        // signature
        public static string CreateSignature(string email, string password, RSAParameters key)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(key);
            var sha1 = SHA1.Create();
            byte[] prefix = { 0x00 };
            var hash = sha1.ComputeHash(KeyToStruct(key)).Take(4).ToArray();
            var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(email + "\x00" + password), true);
            return DataTypeUtils.UrlSafeBase64(DataTypeUtils.CombineBytes(prefix, hash, encrypted));
        }
    }

    class DataTypeUtils
    {
        public static string UrlSafeBase64(byte[] byteArray)
        {
            return Convert.ToBase64String(byteArray).Replace('+', '-').Replace('/', '_');
        }

        public static byte[] CombineBytes(params byte[][] arrays)
        {
            var rv = new byte[arrays.Sum(a => a.Length)];
            var offset = 0;
            foreach (var array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}