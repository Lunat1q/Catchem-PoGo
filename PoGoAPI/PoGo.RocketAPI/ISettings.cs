using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;

namespace PokemonGo.RocketAPI
{
    public interface ISettings
    {
        AuthType AuthType { get; set; }
        double DefaultLatitude { get; set; }
        double DefaultLongitude { get; set; }
        double DefaultAltitudeMin { get; set; }
        double DefaultAltitudeMax { get; set; }
        double DefaultAltitude { get; set; }
        string GoogleRefreshToken { get; set; }
        string PtcPassword { get; set; }
        string PtcUsername { get; set; }
        string GoogleUsername { get; set; }
        string GooglePassword { get; set; }
		string DeviceId { get; set; }
        string AndroidBoardName { get; set; }
        string AndroidBootloader { get; set; }
        string DeviceBrand { get; set; }
        string DeviceModel { get; set; }
        string DeviceModelIdentifier { get; set; }
        string DeviceModelBoot { get; set; }
        string HardwareManufacturer { get; set; }
        string HardwareModel { get; set; }
        string FirmwareBrand { get; set; }
        string FirmwareTags { get; set; }
        string FirmwareType { get; set; }
        string FirmwareFingerprint { get; set; }
        double MoveSpeedFactor { get; set; }
        ByteString SessionHash { get; set; }


        bool UseProxy { get; set; }
        string ProxyUri { get; set; }
        string ProxyLogin { get; set; }
        string ProxyPass { get; set; }
    }
}