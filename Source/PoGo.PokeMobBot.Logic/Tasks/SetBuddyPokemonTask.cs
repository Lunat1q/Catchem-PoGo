using System.Linq;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.State;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class SetBuddyPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId)
        {
            var all = await session.Inventory.GetPokemons();
            var pokemon = all.FirstOrDefault(p => p.Id == pokemonId);

            if (pokemon == null || pokemonId == session.Profile?.PlayerData?.BuddyPokemon?.Id)
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }

            if (!string.IsNullOrEmpty(pokemon.DeployedFortId))
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = $"Pokemon {(string.IsNullOrEmpty(pokemon.Nickname) ? pokemon.PokemonId.ToString() : pokemon.Nickname)} is signed to defend a GYM!"
                });
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }

            var resp = await session.Client.Inventory.SetBuddyPokemon(pokemonId);
            
            session.EventDispatcher.Send(new BuddySetEvent
            {
               UpdatedBuddy = resp.UpdatedBuddy
            });

            session.EventDispatcher.Send(new PokemonActionDoneEvent
            {
                Uid = pokemonId
            });
        }
    }
}
