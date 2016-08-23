#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Tasks;

#endregion

namespace PoGo.PokeMobBot.Logic.State
{
    public class InfoState : IState
    {
        public async Task<IState> Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (session.LogicSettings.AmountOfPokemonToDisplayOnStart > 0)
                await DisplayPokemonStatsTask.Execute(session);
			await session.MapCache.UpdateMapDatas(session);
            Action<IEvent> action = (evt) => session.EventDispatcher.Send(evt);
            await PokemonListTask.Execute(session, action);
            await InventoryListTask.Execute(session, action);

            //return new CheckTosState();
            return new CheckTosState(); 
        }
    }
}