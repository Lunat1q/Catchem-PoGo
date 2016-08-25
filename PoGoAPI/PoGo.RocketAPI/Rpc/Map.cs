using System;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Helpers;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;

namespace PokemonGo.RocketAPI.Rpc
{
    public class Map : BaseRpc
    {
        public Map(Client client) : base(client)
        {
        }

		public async Task<GetMapObjectsResponse> GetMapObjects()
        {
            #region Messages

            var getMapObjectsMessage = new GetMapObjectsMessage
            {
                CellId = { S2Helper.GetNearbyCellIds(_client.CurrentLongitude, _client.CurrentLatitude) },
                SinceTimestampMs = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Latitude = _client.CurrentLatitude,
                Longitude = _client.CurrentLongitude
            };
            var getHatchedEggsMessage = new GetHatchedEggsMessage();
            var getInventoryMessage = new GetInventoryMessage
            {
                LastTimestampMs = DateTime.UtcNow.ToUnixTime()
            };
            var checkAwardedBadgesMessage = new CheckAwardedBadgesMessage();
            var downloadSettingsMessage = new DownloadSettingsMessage
            {
                Hash = "54b359c97e46900f87211ef6e6dd0b7f2a3ea1f5"
            };

            #endregion

            var request = RequestBuilder.GetRequestEnvelope(
                new Request
                {
                    RequestType = RequestType.GetMapObjects,
                    RequestMessage = getMapObjectsMessage.ToByteString()
                },
                new Request
                {
                    RequestType = RequestType.GetHatchedEggs,
                    RequestMessage = getHatchedEggsMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.GetInventory,
                    RequestMessage = getInventoryMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.CheckAwardedBadges,
                    RequestMessage = checkAwardedBadgesMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.DownloadSettings,
                    RequestMessage = downloadSettingsMessage.ToByteString()
                });

            return await PostProtoPayload<Request, GetMapObjectsResponse>(request);
        }

        public async Task<Tuple<GetMapObjectsResponse, GetHatchedEggsResponse, GetInventoryResponse, CheckAwardedBadgesResponse, DownloadSettingsResponse>> GetMapObjectsTuple()
        {
            #region Messages

            var getMapObjectsMessage = new GetMapObjectsMessage
            {
                CellId = { S2Helper.GetNearbyCellIds(_client.CurrentLongitude, _client.CurrentLatitude) },
                SinceTimestampMs = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Latitude = _client.CurrentLatitude,
                Longitude = _client.CurrentLongitude
            };
            var getHatchedEggsMessage = new GetHatchedEggsMessage();
            var getInventoryMessage = new GetInventoryMessage
            {
                LastTimestampMs = DateTime.UtcNow.ToUnixTime()
            };
            var checkAwardedBadgesMessage = new CheckAwardedBadgesMessage();
            var downloadSettingsMessage = new DownloadSettingsMessage
            {
                Hash = "54b359c97e46900f87211ef6e6dd0b7f2a3ea1f5"//"05daf51635c82611d1aac95c0b051d3ec088a930"
            };

            #endregion

            var request = RequestBuilder.GetRequestEnvelope(
                new Request
                {
                    RequestType = RequestType.GetMapObjects,
                    RequestMessage = getMapObjectsMessage.ToByteString()
                },
                new Request
                {
                    RequestType = RequestType.GetHatchedEggs,
                    RequestMessage = getHatchedEggsMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.GetInventory,
                    RequestMessage = getInventoryMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.CheckAwardedBadges,
                    RequestMessage = checkAwardedBadgesMessage.ToByteString()
                }, new Request
                {
                    RequestType = RequestType.DownloadSettings,
                    RequestMessage = downloadSettingsMessage.ToByteString()
                });
            return await PostProtoPayload<Request, GetMapObjectsResponse, GetHatchedEggsResponse, GetInventoryResponse, CheckAwardedBadgesResponse, DownloadSettingsResponse>(request);
        }

        public async Task<GetIncensePokemonResponse> GetIncensePokemons()
        {
            var message = new GetIncensePokemonMessage()
            {
                PlayerLatitude = _client.CurrentLatitude,
                PlayerLongitude = _client.CurrentLongitude
            };

            return await PostProtoPayload<Request, GetIncensePokemonResponse>(RequestType.GetIncensePokemon, message);
        }
    }
}
