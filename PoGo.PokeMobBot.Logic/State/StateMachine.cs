#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Exceptions;

#endregion

namespace PoGo.PokeMobBot.Logic.State
{
    public class StateMachine
    {
        private IState _initialState;

        public Task AsyncStart(IState initialState, Session session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => Start(initialState, session, cancellationToken), cancellationToken);
        }

        public void SetFailureState(IState state)
        {
            _initialState = state;
        }

        public async Task Start(IState initialState, Session session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = initialState;
            do
            {
                try
                {
                    state = await state.Execute(session, cancellationToken);
                }
                catch (InvalidResponseException ex)
                {
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.NianticServerUnstable)
                    });
                    Logger.Write("[NIANTIC] " + ex.Message, LogLevel.Error);
                    state = _initialState;
                    await DelayingUtils.Delay(15000, 10000);
                }
                catch (OperationCanceledException)
                {
                    session.EventDispatcher.Send(new WarnEvent {Message = session.Translation.GetTranslation(TranslationString.OperationCanceled) });
                    break;
                }
                catch (Exception ex)
                {
                    session.EventDispatcher.Send(new ErrorEvent {Message = ex.ToString()});
                    state = _initialState;
                    await DelayingUtils.Delay(15000, 10000);
                }
                
            } while (state != null);
        }
    }
}