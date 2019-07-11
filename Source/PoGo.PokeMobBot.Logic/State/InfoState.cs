#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Event.Global;
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
                await DisplayPokemonStatsTask.Execute(session, cancellationToken);
			await session.MapCache.UpdateMapDatas(session);
            Action<IEvent> action = (evt) => session.EventDispatcher.Send(evt);
            await PokemonListTask.Execute(session, action);
            await InventoryListTask.Execute(session, action);
            await session.Inventory.UpdatePokeDex();

            session.EventDispatcher.Send(new NoticeEvent
            {
                Message = session.Translation.GetTranslation(TranslationString.UpdatesAt) + " https://github.com/Lunat1q/Catchem-PoGo "+ session.Translation.GetTranslation(TranslationString.DiscordLink) + " https://discord.me/Catchem"
            });

            //return new CheckTosState();
            return new CheckTosState(); 
        }
    }
}