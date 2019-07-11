using PoGo.PokeMobBot.Logic.State;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Logging;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    class GetPokeDexCount
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            await session.Inventory.UpdatePokeDex();
            var _totalUniqueEncounters = session.PokeDex.Select(i => new { Pokemon = i.Id, Captures = i.CapturedTimes });
            var _totalCaptures = _totalUniqueEncounters.Count(i => i.Captures > 0);
            var _totalData = session.PokeDex.Count();
            
            Logger.Write($"Seen: {_totalData}, Captured: {_totalCaptures}");
        }
    }
}