﻿using System.Threading.Tasks;
using Google.Protobuf.Collections;
using POGOProtos.Enums;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;

namespace PokemonGo.RocketAPI.Rpc
{
    public class Misc : BaseRpc
    {
        public Misc(Client client) : base(client)
        {
        }


        public async Task<ClaimCodenameResponse> ClaimCodename(string codename)
        {
            return
                await
                    PostProtoPayload<Request, ClaimCodenameResponse>(RequestType.ClaimCodename,
                        new ClaimCodenameMessage()
                        {
                            Codename = codename
                        });
        }

        public async Task<CheckCodenameAvailableResponse> CheckCodenameAvailable(string codename)
        {
            return
                await
                    PostProtoPayload<Request, CheckCodenameAvailableResponse>(RequestType.CheckCodenameAvailable,
                        new CheckCodenameAvailableMessage()
                        {
                            Codename = codename
                        });
        }

        public async Task<GetSuggestedCodenamesResponse> GetSuggestedCodenames()
        {
            return await PostProtoPayload<Request, GetSuggestedCodenamesResponse>(RequestType.GetSuggestedCodenames, new GetSuggestedCodenamesMessage());
        }

        public async Task<EchoResponse> SendEcho()
        {
            return await PostProtoPayload<Request, EchoResponse>(RequestType.Echo, new EchoMessage());
        }

        public async Task<EncounterTutorialCompleteResponse> MarkTutorialComplete(RepeatedField<TutorialState> toComplete )
        {
            return await PostProtoPayload<Request, EncounterTutorialCompleteResponse>(RequestType.MarkTutorialComplete, new MarkTutorialCompleteMessage()
            {
                SendMarketingEmails = false,
                SendPushNotifications = false,
                TutorialsCompleted = { toComplete }
            });
        }

        public async Task<CheckChallengeResponse> CheckChallenge()
        {
            var message = new CheckChallengeMessage();
            return await PostProtoPayload<Request, CheckChallengeResponse>(RequestType.CheckChallenge, message);
        }

        public async Task<VerifyChallengeResponse> VerifyChallenge(string token)
        {
            var message = new VerifyChallengeMessage()
            {
                Token = token
            };
            return await PostProtoPayload<Request, VerifyChallengeResponse>(RequestType.VerifyChallenge, message);
        }
    }
}