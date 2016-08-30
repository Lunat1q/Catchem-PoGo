#region using directives

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.Tasks;
using PokemonGo.RocketAPI.Exceptions;

#endregion

namespace PoGo.PokeMobBot.Logic.State
{
    public class FarmState : IState
    {
        public async Task<IState> Execute(ISession session, CancellationToken cancellationToken)
        {
            try
            {
                if (session.LogicSettings.EvolveAllPokemonAboveIv || session.LogicSettings.EvolveAllPokemonWithEnoughCandy)
                {
                    await EvolvePokemonTask.Execute(session, cancellationToken);
                }
                if (session.LogicSettings.BeLikeRobot)
                {
                    if (session.LogicSettings.AutoFavoritePokemon)
                    {
                        await FavoritePokemonTask.Execute(session, cancellationToken);
                    }
                    if (session.LogicSettings.TransferDuplicatePokemon)
                    {
                        await TransferDuplicatePokemonTask.Execute(session, cancellationToken);
                    }
                    if (session.LogicSettings.AutomaticallyLevelUpPokemon)
                    {
                        await LevelUpPokemonTask.Execute(session, cancellationToken);
                    }
                    if (session.LogicSettings.RenamePokemon)
                    {
                        await RenamePokemonTask.Execute(session, cancellationToken);
                    }

                    await RecycleItemsTask.Execute(session, cancellationToken);
                }
                if (session.LogicSettings.UseEggIncubators)
                {
                    await UseIncubatorsTask.Execute(session, cancellationToken);
                }

                if (session.LogicSettings.UseCustomRoute)
                {
                    await FarmPokestopsCustomRouteTask.Execute(session, cancellationToken);
                }
                else
                {
                    if (session.LogicSettings.UseDiscoveryPathing)
                    	await FarmPokeStopsDiscoveryTask.Execute(session, cancellationToken);
                	else
                    	await FarmPokestopsTask.Execute(session, cancellationToken);
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
                await Task.Delay(20000, cancellationToken);
                return new LoginState();
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
                return new LoginState();
            }
            catch (InvalidResponseException ex)
            {
                session.EventDispatcher.Send(new ErrorEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.NianticServerUnstable)
                });
                Logger.Write("[NIANTIC] " + ex.Message, LogLevel.Error);
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

            return this;
        }
    }
}