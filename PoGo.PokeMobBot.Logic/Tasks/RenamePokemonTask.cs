#region using directives

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class RenamePokemonTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pokemons = await session.Inventory.GetPokemons();
            var prevState = session.State;
            session.State = BotState.Renaming;
            foreach (var pokemon in pokemons)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var perfection = Math.Round(pokemon.CalculatePokemonPerfection());
                var pokemonName = session.Translation.GetPokemonName(pokemon.PokemonId);
                // iv number + templating part + pokemonName <= 12
                var nameLength = 12 -
                                 (perfection.ToString(CultureInfo.InvariantCulture).Length +
                                  session.LogicSettings.RenameTemplate.Length - 6);
                if (pokemonName.Length > nameLength)
                {
                    pokemonName = pokemonName.Substring(0, nameLength);
                }
                var newNickname = string.Format(session.LogicSettings.RenameTemplate, pokemonName, perfection);
                var oldNickname = pokemon.Nickname.Length != 0 ? pokemon.Nickname : session.Translation.GetPokemonName(pokemon.PokemonId);

                // If "RenameOnlyAboveIv" = true only rename pokemon with IV over "KeepMinIvPercentage"
                // Favorites will be skipped
                if ((!session.LogicSettings.RenameOnlyAboveIv || perfection >= session.LogicSettings.KeepMinIvPercentage) &&
                    newNickname != oldNickname && pokemon.Favorite == 0)
                {
                    await session.Client.Inventory.NicknamePokemon(pokemon.Id, newNickname);
                    await DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 2000);
                    session.EventDispatcher.Send(new NoticeEvent
                    {
                        Message =
                            session.Translation.GetTranslation(TranslationString.PokemonRename, session.Translation.GetPokemonName(pokemon.PokemonId),
                                pokemon.Id, oldNickname, newNickname)
                    });
                }
            }
            session.State = prevState;
        }
    }
}