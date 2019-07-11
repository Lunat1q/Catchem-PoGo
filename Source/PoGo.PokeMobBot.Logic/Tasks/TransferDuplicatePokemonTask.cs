#region using directives

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class TransferDuplicatePokemonTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Refresh inventory so that the player stats are fresh
            // await session.Inventory.RefreshCachedInventory();

            if (!await CheckBotStateTask.Execute(session, cancellationToken)) return;
            var prevState = session.State;
            session.State = BotState.Transfer;

            var duplicatePokemons =
                await
                    session.Inventory.GetDuplicatePokemonToTransfer(session.LogicSettings.KeepPokemonsThatCanEvolve,
                        session.LogicSettings.PrioritizeIvOverCp,
                        session.LogicSettings.PokemonsNotToTransfer);

            if (session.Profile.PlayerData.BuddyPokemon != null)
                duplicatePokemons = duplicatePokemons.Where(x => x.Id != session.Profile.PlayerData.BuddyPokemon.Id);

            var currentPokemonCount = await session.Inventory.GetPokemonsCount();
            var maxPokemonCount = session.Profile.PlayerData.MaxPokemonStorage;

            session.EventDispatcher.Send(new NoticeEvent
            {
                Message = session.Translation.GetTranslation(TranslationString.CurrentPokemonUsage,
                    currentPokemonCount, maxPokemonCount)
            });

            var pokemonSettings = await session.Inventory.GetPokemonSettings();
            var pokemonFamilies = await session.Inventory.GetPokemonFamilies();

            if (duplicatePokemons != null)
            foreach (var duplicatePokemon in duplicatePokemons)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrEmpty(duplicatePokemon.DeployedFortId)) continue;

                await session.Client.Inventory.TransferPokemon(duplicatePokemon.Id);
                await session.Inventory.DeletePokemonFromInvById(duplicatePokemon.Id);

                var bestPokemonOfType = (session.LogicSettings.PrioritizeIvOverCp
                    ? await session.Inventory.GetHighestPokemonOfTypeByIv(duplicatePokemon)
                    : await session.Inventory.GetHighestPokemonOfTypeByCp(duplicatePokemon)) ?? duplicatePokemon;

                var setting = pokemonSettings?.Single(q => q.PokemonId == duplicatePokemon.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting?.FamilyId);

                family.Candy_++;

                session.EventDispatcher.Send(new TransferPokemonEvent
                {
                    Uid = duplicatePokemon.Id,
                    Id = duplicatePokemon.PokemonId,
                    Perfection = duplicatePokemon.CalculatePokemonPerfection(),
                    Cp = duplicatePokemon.Cp,
                    BestCp = bestPokemonOfType.Cp,
                    BestPerfection = bestPokemonOfType.CalculatePokemonPerfection(),
                    FamilyCandies = family.Candy_,
                    Family = family.FamilyId
                });
                    await Task.Delay(session.LogicSettings.DelayTransferPokemon, cancellationToken);
            }
            session.State = prevState;
        }
    }
}