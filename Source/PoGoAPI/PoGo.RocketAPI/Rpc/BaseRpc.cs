﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Google.Protobuf;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Helpers;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;

namespace PokemonGo.RocketAPI.Rpc
{
    public class BaseRpc
    {
        protected static string _downloadHash = "7ad08d8cb05b616c0ecd3cd094669de98c0c4d33"; //"0e773c434a8d950fb98786f3000adaeafe4dbb85"; //54b359c97e46900f87211ef6e6dd0b7f2a3ea1f5
        protected Client _client;
        protected RequestBuilder RequestBuilder => new RequestBuilder(_client.AuthToken, _client.AuthType, _client.CurrentLatitude, _client.CurrentLongitude, _client.CurrentAltitude, _client.Settings, _client.AuthTicket);
        protected string ApiUrl => $"https://{_client.ApiUrl}/rpc";
        protected BaseRpc(Client client)
        {
            _client = client;
        }

        protected async void CheckAuth()
        {
            var haveLegitTicket = _client.AuthTicket?.End != null &&
                              _client.AuthTicket.ExpireTimestampMs > (ulong) (DateTime.UtcNow.ToUnixTime() + 30000) &&
                              _client.AuthTicket.Start != null;

            if (haveLegitTicket) return;
            await _client.UpdateTicket();
            Debug.Write("Auth ticket update");
        }

        protected async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestType type, IMessage message) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            var requestEnvelops = RequestBuilder.GetRequestEnvelope(type, message);

            var response = await _client.PokemonHttpClient.PostProtoPayload<TRequest, TResponsePayload>(ApiUrl, requestEnvelops, _client.ApiFailure);
            CheckAuth();
            return response;
        }

        protected async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            return await _client.PokemonHttpClient.PostProtoPayload<TRequest, TResponsePayload>(ApiUrl, requestEnvelope, _client.ApiFailure);
        }

        protected async Task<Tuple<T1, T2>> PostProtoPayload<TRequest, T1, T2>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
        {
            var responses = await PostProtoPayload<TRequest>(requestEnvelope, typeof (T1), typeof (T2));
            return new Tuple<T1, T2>(responses[0] as T1, responses[1] as T2);
        }

        protected async Task<Tuple<T1, T2, T3>> PostProtoPayload<TRequest, T1, T2, T3>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
        {
            var responses = await PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2), typeof(T3));
            return new Tuple<T1, T2, T3>(responses[0] as T1, responses[1] as T2, responses[2] as T3);
        }

        protected async Task<Tuple<T1, T2, T3, T4>> PostProtoPayload<TRequest, T1, T2, T3, T4>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
            where T4 : class, IMessage<T4>, new()
        {
            var responses = await PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
            return new Tuple<T1, T2, T3, T4>(responses[0] as T1, responses[1] as T2, responses[2] as T3, responses[3] as T4);
        }

        protected async Task<Tuple<T1, T2, T3, T4, T5>> PostProtoPayload<TRequest, T1, T2, T3, T4, T5>(
            RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
            where T4 : class, IMessage<T4>, new()
            where T5 : class, IMessage<T5>, new()
        {
            var responses =
                await
                    PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2), typeof(T3), typeof(T4),
                        typeof(T5));
            return new Tuple<T1, T2, T3, T4, T5>(responses[0] as T1, responses[1] as T2, responses[2] as T3,
                responses[3] as T4, responses[4] as T5);
        }

        protected async Task<Tuple<T1, T2, T3, T4, T5, T6>> PostProtoPayload<TRequest, T1, T2, T3, T4, T5, T6>(
            RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
            where T4 : class, IMessage<T4>, new()
            where T5 : class, IMessage<T5>, new()
            where T6 : class, IMessage<T6>, new()
        {
            var responses =
                await
                    PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2), typeof(T3), typeof(T4),
                        typeof(T5), typeof(T6));
            return new Tuple<T1, T2, T3, T4, T5, T6>(responses[0] as T1, responses[1] as T2, responses[2] as T3,
                responses[3] as T4, responses[4] as T5, responses[5] as T6);
        }

        protected async Task<Tuple<T1, T2, T3, T4, T5, T6, T7>> PostProtoPayload<TRequest, T1, T2, T3, T4, T5, T6, T7>(
            RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
            where T1 : class, IMessage<T1>, new()
            where T2 : class, IMessage<T2>, new()
            where T3 : class, IMessage<T3>, new()
            where T4 : class, IMessage<T4>, new()
            where T5 : class, IMessage<T5>, new()
            where T6 : class, IMessage<T6>, new()
            where T7 : class, IMessage<T7>, new()
        {
            var responses =
                await
                    PostProtoPayload<TRequest>(requestEnvelope, typeof(T1), typeof(T2), typeof(T3), typeof(T4),
                        typeof(T5), typeof(T6), typeof(T7));
            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(responses[0] as T1, responses[1] as T2, responses[2] as T3,
                responses[3] as T4, responses[4] as T5, responses[5] as T6, responses[6] as T7);
        }

        protected async Task<IMessage[]> PostProtoPayload<TRequest>(RequestEnvelope requestEnvelope, params Type[] responseTypes) where TRequest : IMessage<TRequest>
        {
            return await _client.PokemonHttpClient.PostProtoPayload<TRequest>(ApiUrl, requestEnvelope, _client.ApiFailure, responseTypes);
        }

        protected async Task<ResponseEnvelope> PostProto<TRequest>(RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            return await _client.PokemonHttpClient.PostProto<TRequest>(ApiUrl, requestEnvelope);
        }
        protected async Task<ResponseEnvelope> PostProto<TRequest>(string url, RequestEnvelope requestEnvelope) where TRequest : IMessage<TRequest>
        {
            return await _client.PokemonHttpClient.PostProto<TRequest>(url, requestEnvelope);
        }
    }
}
