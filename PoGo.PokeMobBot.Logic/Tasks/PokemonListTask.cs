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
        public static async Task Execute(ISession session, Action<IEvent> action)
        {
            // Refresh inventory so that the player stats are fresh
            //await session.Inventory.RefreshCachedInventory();

            var myPokemonSettings = await session.Inventory.GetPokemonSettings();
            var pokemonSettings = myPokemonSettings.ToList();

            var myPokemonFamilies = await session.Inventory.GetPokemonFamilies();
            var pokemonFamilies = myPokemonFamilies.ToArray();

            var allPokemonInBag = await session.Inventory.GetHighestsCp(1000);

            var pkmWithIv = allPokemonInBag?.Select(p => {
                var settings = pokemonSettings.Single(x => x.PokemonId == p.PokemonId);
                return Tuple.Create(
                    p,
                    p.CalculatePokemonPerfection(),
                    pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId).Candy_
                );
            });

            action(new PokemonListEvent
            {
                PokemonList = pkmWithIv?.ToList()
            });

            await Task.Delay(session.LogicSettings.DelayBetweenPlayerActions);
        }
    }
}