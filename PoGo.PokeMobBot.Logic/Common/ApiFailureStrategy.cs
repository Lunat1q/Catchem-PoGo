#region using directives

using System;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.State;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Networking.Envelopes;

#endregion

namespace PoGo.PokeMobBot.Logic.Common
{
    public class ApiFailureStrategy : IApiFailureStrategy
    {
        private readonly ISession _session;
        private int _retryCount;

        public ApiFailureStrategy(ISession session)
        {
            _session = session;
        }

        private async void DoLogin()
        {
            try
            {
                await _session.Client.Login.DoLogin();
            }
            catch (AggregateException ae)
            {
                _session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = "APIFailureStrategy: " + ae.Flatten().InnerException.Message
                });
            }
            catch (Exception ex)
            {
                _session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = "APIFailureStrategy (Exception): " + ex.Message
                });
            }
        }

        public async Task<ApiOperation> HandleApiFailure(RequestEnvelope request, ResponseEnvelope response)
        {
            if (_retryCount == 11)
                return ApiOperation.Abort;

            await Task.Delay(500);
            _retryCount++;

            if (_retryCount % 5 == 0)
            {
                try
                {
                    DoLogin();
                }
                catch (PtcOfflineException)
                {
                    _session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = _session.Translation.GetTranslation(TranslationString.PtcOffline)
                    });
                    _session.EventDispatcher.Send(new NoticeEvent
                    {
                        Message = _session.Translation.GetTranslation(TranslationString.TryingAgainIn, 20)
                    });
                    await Task.Delay(20000);
                }
                catch (AccessTokenExpiredException)
                {
                    _session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = _session.Translation.GetTranslation(TranslationString.AccessTokenExpired)
                    });
                    _session.EventDispatcher.Send(new NoticeEvent
                    {
                        Message = _session.Translation.GetTranslation(TranslationString.TryingAgainIn, 2)
                    });
                    await Task.Delay(2000);
                }
                catch (Exception ex) when (ex is InvalidResponseException || ex is TaskCanceledException)
                {
                    _session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = _session.Translation.GetTranslation(TranslationString.NianticServerUnstable)
                    });
                    Logger.Write("[NIANTIC] " + ex.Message, LogLevel.Error);
                    await Task.Delay(5000);
                }
            }

            return ApiOperation.Retry;
        }

        public void HandleApiSuccess(RequestEnvelope request, ResponseEnvelope response)
        {
            _retryCount = 0;
        }
    }
}
