#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
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
                catch (InvalidResponseException)
                {
                    session.EventDispatcher.Send(new ErrorEvent
                    {
                        Message = session.Translation.GetTranslation(TranslationString.NianticServerUnstable)
                    });
                    state = _initialState;
                }
                catch (OperationCanceledException)
                {
                    session.EventDispatcher.Send(new ErrorEvent {Message = session.Translation.GetTranslation(TranslationString.OperationCanceled) });
                    state = _initialState;
                }
                catch (Exception ex)
                {
                    session.EventDispatcher.Send(new ErrorEvent {Message = ex.ToString()});
                    state = _initialState;
                }
                
            } while (state != null);
        }
    }
}