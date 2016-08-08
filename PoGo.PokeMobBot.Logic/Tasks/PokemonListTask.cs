#region using directives

using System;
using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class PokemonListTask
    {
        public static async Task Execute(ISession session)
        {
            // Refresh inventory so that the player stats are fresh
            await session.Inventory.RefreshCachedInventory();

            var allPokemonInBag = await session.Inventory.GetHighestsCp(1000);
            var pkmWithIv = allPokemonInBag.Select(p => Tuple.Create(p, PokemonInfo.CalculatePokemonPerfection(p)));
            session.EventDispatcher.Send(
                new PokemonListEvent
                {
                    PokemonList = pkmWithIv.ToList()
                });
            if(session.LogicSettings.Teleport)
                await Task.Delay(session.LogicSettings.DelayDisplayPokemon);
            else
                await Task.Delay(500);
        }
    }
}