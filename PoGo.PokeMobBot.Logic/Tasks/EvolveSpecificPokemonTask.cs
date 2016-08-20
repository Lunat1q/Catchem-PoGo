#region using directives

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PoGo.PokeMobBot.Logic.Logging;
using PoGo.PokeMobBot.Logic.Common;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class EvolveSpecificPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId)
        {
            var all = await session.Inventory.GetPokemons();
            var pokemons = all.OrderByDescending(x => x.Cp).ThenBy(n => n.StaminaMax);
            var pokemon = pokemons.FirstOrDefault(p => p.Id == pokemonId);

            if (pokemon == null) return;

            if (!string.IsNullOrEmpty(pokemon.DeployedFortId))
            {
                session.EventDispatcher.Send(new WarnEvent()
                {
                    Message = $"Pokemon {(string.IsNullOrEmpty(pokemon.Nickname) ? pokemon.PokemonId.ToString() : pokemon.Nickname)} is signed to defend a GYM!"
                });
                return;
            }

            var evolveResponse = await session.Client.Inventory.EvolvePokemon(pokemon.Id);
            
            session.EventDispatcher.Send(new PokemonEvolveEvent
            {
                Uid = pokemon.Id,
                Id = pokemon.PokemonId,
                Exp = evolveResponse.ExperienceAwarded,
                Result = evolveResponse.Result
            });

            if (evolveResponse.EvolvedPokemonData != null)
            {
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
                var pokemonSettings = (await session.Inventory.GetPokemonSettings()).ToList();
                var setting = pokemonSettings.Single(q => q.PokemonId == evolveResponse.EvolvedPokemonData.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                session.EventDispatcher.Send(new PokemonEvolveDoneEvent
                {
                    Uid = evolveResponse.EvolvedPokemonData.Id,
                    Id = evolveResponse.EvolvedPokemonData.PokemonId,
                    Cp = evolveResponse.EvolvedPokemonData.Cp,
                    Perfection = evolveResponse.EvolvedPokemonData.CalculatePokemonPerfection(),
                    Family = family.FamilyId,
                    Candy = family.Candy_
                });
            }
            await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 2000);
        }
    }
}