using PoGo.PokeMobBot.Logic.State;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class TransferPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId)
        {
            var id = pokemonId;
            
            var all = await session.Inventory.GetPokemons();
            var pokemon = all.FirstOrDefault(p => p.Id == id);

            if (pokemon == null || pokemon.Favorite == 1)
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }

            if (pokemonId == session.Profile.PlayerData.BuddyPokemon.Id)
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = $"Pokemon {(string.IsNullOrEmpty(pokemon.Nickname) ? pokemon.PokemonId.ToString() : pokemon.Nickname)} is set as Buddy!"
                });
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

            if (!await CheckBotStateTask.Execute(session, default(CancellationToken)))
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }
            var prevState = session.State;
            session.State = BotState.Transfer;

            var pokemonSettings = await session.Inventory.GetPokemonSettings();
            var pokemonFamilies = await session.Inventory.GetPokemonFamilies();

            await session.Client.Inventory.TransferPokemon(id);
            await session.Inventory.DeletePokemonFromInvById(id);

            var bestPokemonOfType = (session.LogicSettings.PrioritizeIvOverCp
                ? await session.Inventory.GetHighestPokemonOfTypeByIv(pokemon)
                : await session.Inventory.GetHighestPokemonOfTypeByCp(pokemon)) ?? pokemon;

            var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
            var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);

            family.Candy_++;

            // Broadcast event as everyone would benefit
            session.EventDispatcher.Send(new TransferPokemonEvent
            {
                Uid = pokemon.Id,
                Id = pokemon.PokemonId,
                Perfection = PoGoUtils.PokemonInfo.CalculatePokemonPerfection(pokemon),
                Cp = pokemon.Cp,
                BestCp = bestPokemonOfType.Cp,
                BestPerfection = PoGoUtils.PokemonInfo.CalculatePokemonPerfection(bestPokemonOfType),
                FamilyCandies = family.Candy_,
                Family = family.FamilyId
            });

            await Task.Delay(session.LogicSettings.DelayTransferPokemon);
            session.State = prevState;
        }
    }
}
