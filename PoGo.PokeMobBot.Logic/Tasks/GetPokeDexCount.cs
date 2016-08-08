using PoGo.PokeMobBot.Logic.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.Common;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    class GetPokeDexCount
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            var PokeDex = await session.Inventory.GetPokeDexItems();
            var _totalUniqueEncounters = PokeDex.Select(i => new { Pokemon = i.InventoryItemData.PokedexEntry.PokemonId, Captures = i.InventoryItemData.PokedexEntry.TimesCaptured });
            var _totalCaptures = _totalUniqueEncounters.Count(i => i.Captures > 0);
            var _totalData = PokeDex.Count();
            
            Logger.Write($"Seen: {_totalData}, Captured: {_totalCaptures}");
        }
    }
}