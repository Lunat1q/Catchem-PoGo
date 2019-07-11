#region using directives

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Logic;
using PoGo.PokeMobBot.Logic.Event.Player;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.Tasks;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;

#endregion

namespace PoGo.PokeMobBot.Logic.State
{
    public class LoginState : IState
    {
        public async Task<IState> Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            session.EventDispatcher.Send(new NoticeEvent
            {
                Message = session.Translation.GetTranslation(TranslationString.LoggingIn, session.Settings.AuthType)
            });

            await CheckLogin(session, cancellationToken);

            try
            {
                await session.Client.Login.DoLogin();

                if (session.Profile != null && session.Profile.Warn)
                {
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.NianticPlayerWarn)
                    });
                }
            }
            catch (PtcOfflineException)
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.PtcOffline)
                });
                session.EventDispatcher.Send(new NoticeEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.TryingAgainIn, 20)
                });
                await Task.Delay(45000, cancellationToken);
                return this;
            }
            catch (AccessTokenExpiredException)
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.AccessTokenExpired)
                });
                session.EventDispatcher.Send(new NoticeEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.TryingAgainIn, 2)
                });
                await Task.Delay(2000, cancellationToken);
                return this;
            }
            catch (InvalidResponseException ex)
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.NianticServerUnstable)
                });
                if (session.Profile != null && session.Profile.Banned)
                {
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.NianticPlayerBanned)
                    });
                }
                else if (session.Profile != null && session.Profile.Warn)
                {
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.NianticPlayerWarn)
                    });
                }
                session.EventDispatcher.Send(new NoticeEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.TryingAgainIn, 45)
                });
                Logger.Write("[NIANTIC] " + ex.Message, LogLevel.Error);
                await Task.Delay(45000, cancellationToken);
                return this;
            }
            catch (AccountNotVerifiedException)
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.AccountNotVerified)
                });
                await Task.Delay(2000, cancellationToken);
                session.EventDispatcher.Send(new BotCompleteFailureEvent
                {
                    Shutdown = false,
                    Stop = true
                });
            }
            catch (GoogleException e)
            {
                if (e.Message.Contains("NeedsBrowser"))
                {
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.GoogleTwoFactorAuth)
                    });
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.GoogleTwoFactorAuthExplanation)
                    });
                    await Task.Delay(7000, cancellationToken);
                    try
                    {
                        Process.Start("https://security.google.com/settings/security/apppasswords");
                    }
                    catch (Exception)
                    {
                        session.EventDispatcher.Send(new ErrorEvent
                        {
                            Message = "https://security.google.com/settings/security/apppasswords"
                        });
                        throw;
                    }
                }
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.GoogleError)
                });
                await Task.Delay(2000, cancellationToken);
                session.EventDispatcher.Send(new BotCompleteFailureEvent
                {
                    Shutdown = false,
                    Stop = true
                });
            }
            catch (GoogleOfflineException e)
            {
                if (e.Message.Contains("NeedsBrowser"))
                {
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.GoogleTwoFactorAuth)
                    });
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.GoogleTwoFactorAuthExplanation)
                    });
                    await Task.Delay(7000, cancellationToken);
                    try
                    {
                        Process.Start("https://security.google.com/settings/security/apppasswords");
                    }
                    catch (Exception)
                    {
                        session.EventDispatcher.Send(new ErrorEvent
                        {
                            Message = "https://security.google.com/settings/security/apppasswords"
                        });
                    }
                }
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = "Check your google account/password/2 factor auth and internet connection"
                });
                await Task.Delay(2000, cancellationToken);
                session.EventDispatcher.Send(new BotCompleteFailureEvent
                {
                    Shutdown = false,
                    Stop = true
                });
            }
            catch (LoginFailedException)
            {
                session.EventDispatcher.Send(new BotCompleteFailureEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.PtcLoginFailed),
                    Shutdown = false,
                    Stop = true
                });
            }
            catch (InvalidProtocolBufferException ex) when (ex.Message.Contains("SkipLastField"))
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = "IP Banned..."
                });
                await Task.Delay(2000, cancellationToken);
                session.EventDispatcher.Send(new BotCompleteFailureEvent
                {
                    Shutdown = false,
                    Stop = true
                });
            }
            catch (Exception unhandeled)
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = unhandeled.Message
                });
                session.EventDispatcher.Send(new NoticeEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.TryingAgainIn, 45)
                });
                await Task.Delay(45000, cancellationToken);
                if (session.LogicSettings.StopBotToAvoidBanOnUnknownLoginError)
                {
                    session.EventDispatcher.Send(new NoticeEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.StopBotToAvoidBan)
                    });
	                session.EventDispatcher.Send(new BotCompleteFailureEvent
	                {
	                    Shutdown = false,
	                    Stop = true
	                });
                }
                else
                {
                    session.EventDispatcher.Send(new NoticeEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.BotNotStoppedRiskOfBan)
                    });
                    return this;
                }
            }

            await DownloadProfile(session, cancellationToken);

            return new PositionCheckState();
        }

        private static async Task CheckLogin(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (session.Settings.AuthType == AuthType.Google &&
                (session.Settings.GoogleUsername == null || session.Settings.GooglePassword == null))
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.MissingCredentialsGoogle)
                });
                await Task.Delay(2000, cancellationToken);
                session.EventDispatcher.Send(new BotCompleteFailureEvent
                {
                    Shutdown = false,
                    Stop = true
                });
            }
            else if (session.Settings.AuthType == AuthType.Ptc &&
                     (session.Settings.PtcUsername == null || session.Settings.PtcPassword == null))
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.MissingCredentialsPtc)
                });
                await Task.Delay(2000, cancellationToken);
                session.EventDispatcher.Send(new BotCompleteFailureEvent
                {
                    Shutdown = false,
                    Stop = true
                });
            }
        }

        public async Task DownloadProfile(ISession session, CancellationToken cancellationToken)
        {
            try
            {
                session.Profile = await session.Client.Player.GetPlayer();
                await CheckChallengeDoneTask.Execute(session, cancellationToken);
                await CheckChallengeTask.Execute(session, cancellationToken);
                session.EventDispatcher.Send(new ProfileEvent { Profile = session.Profile });
            }
            catch (UriFormatException e)
            {
                session.EventDispatcher.Send(new ErrorEvent { Message = e.ToString() });
            }
            catch (Exception ex)
            {
                session.EventDispatcher.Send(new ErrorEvent { Message = ex.ToString() });
            }
        }
    }
}
