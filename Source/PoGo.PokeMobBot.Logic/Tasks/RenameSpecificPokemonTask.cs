#region using directives

using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.State;
using System.Linq;
using System.Threading;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class RenameSpecificPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId, CancellationToken cancellationToken,
            string customName = null, bool toDefault = false)
        {
            if (customName == null)
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }
            if (!await CheckBotStateTask.Execute(session, cancellationToken))
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }
            var id = pokemonId;
            var all = await session.Inventory.GetPokemons();
            var pokemons = all.OrderByDescending(x => x.Cp).ThenBy(n => n.StaminaMax);
            var pokemon = pokemons.FirstOrDefault(p => p.Id == id);
            if (pokemon == null)
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }
            var pokemonDefaultName = session.Translation.GetPokemonName(pokemon.PokemonId);
            var currentNickname = pokemon.Nickname.Length != 0
                ? pokemon.Nickname
                : session.Translation.GetPokemonName(pokemon.PokemonId);
            if (toDefault)
                customName = pokemonDefaultName;

            if (currentNickname == customName)
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }
            var resp = await session.Client.Inventory.NicknamePokemon(id, customName);

            var prevState = session.State;
            session.State = BotState.Renaming;

            await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 2000);
            session.EventDispatcher.Send(new NoticeEvent
            {
                Message =
                    session.Translation.GetTranslation(TranslationString.PokemonRename,
                        session.Translation.GetPokemonName(pokemon.PokemonId), pokemon.Id, currentNickname, customName)
            });

            if (resp.Result == NicknamePokemonResponse.Types.Result.Success)
            {
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
                var pokemonSettings = (await session.Inventory.GetPokemonSettings()).ToList();
                var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);

                session.EventDispatcher.Send(new PokemonStatsChangedEvent
                {
                    Name = customName,
                    Uid = pokemonId,
                    Id = pokemon.PokemonId,
                    Family = family.FamilyId,
                    Candy = family.Candy_,
                    Cp = pokemon.Cp,
                    MaxCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(pokemon, session.Runtime.CurrentLevel),
                    Iv = pokemon.CalculatePokemonPerfection(),
                    Favourite = pokemon.Favorite == 1,
                    Weight = pokemon.WeightKg,
                    Cpm = pokemon.CpMultiplier + pokemon.AdditionalCpMultiplier,
                    Level = pokemon.GetLevel(),
                    IvDef = pokemon.IndividualDefense,
                    IvAtk = pokemon.IndividualAttack
                });

                session.EventDispatcher.Send(new PokemonActionDoneEvent
                {
                    Uid = pokemonId
                });
            }
            session.State = prevState;
        }
    }
}