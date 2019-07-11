#region using directives

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Event.Pokemon;
using PoGo.PokeMobBot.Logic.PoGoUtils;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public class EvolveSpecificPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId, CancellationToken cancellationToken)
        {
            if (!await CheckBotStateTask.Execute(session, cancellationToken))
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }

           
            var all = await session.Inventory.GetPokemons();
            var pokemons = all.OrderByDescending(x => x.Cp).ThenBy(n => n.StaminaMax);
            var pokemon = pokemons.FirstOrDefault(p => p.Id == pokemonId);


            if (pokemon == null)
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }
            if (!await CheckBotStateTask.Execute(session, cancellationToken))
            {
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }
            var prevState = session.State;
            session.State = BotState.Evolve;
            var pokemonFamilies = await session.Inventory.GetPokemonFamilies();
            var pokemonSettings = (await session.Inventory.GetPokemonSettings()).ToList();
            var setting = pokemonSettings.Single(q => q.PokemonId == pokemon.PokemonId);
            var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);

            if (!string.IsNullOrEmpty(pokemon.DeployedFortId))
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.PokeInGym, string.IsNullOrEmpty(pokemon.Nickname) ? pokemon.PokemonId.ToString() : pokemon.Nickname)
                });
                session.State = prevState;
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }

            if (family.Candy_ < setting.CandyToEvolve)
            {
                session.EventDispatcher.Send(new WarnEvent
                {
                    Message = session.Translation.GetTranslation(TranslationString.CandyMiss, session.Translation.GetPokemonName(pokemon.PokemonId), $"{family.Candy_}/{setting.CandyToEvolve}")
                });
                session.State = prevState;
                session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
                return;
            }

            var isBuddy = pokemonId == session.Profile.PlayerData.BuddyPokemon.Id;

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
                family.Candy_ -= (setting.CandyToEvolve - 1);
                setting = pokemonSettings.Single(q => q.PokemonId == evolveResponse.EvolvedPokemonData.PokemonId);
                family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);
                var evolvedPokemonData = evolveResponse.EvolvedPokemonData;
                session.EventDispatcher.Send(new PokemonEvolveDoneEvent
                {
                    Uid = evolvedPokemonData.Id,
                    Id = evolvedPokemonData.PokemonId,
                    Cp = evolvedPokemonData.Cp,
                    Perfection = evolvedPokemonData.CalculatePokemonPerfection(),
                    Family = family.FamilyId,
                    Level = evolvedPokemonData.GetLevel(),
                    Candy = family.Candy_,
                    Type1 = setting.Type,
                    Type2 = setting.Type2,
                    Stats = setting.Stats,
                    MaxCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(evolvedPokemonData, session.Runtime.CurrentLevel),
                    Stamina = evolvedPokemonData.Stamina,
                    IvSta = evolvedPokemonData.IndividualStamina,
                    Move1 = evolvedPokemonData.Move1,
                    Move2 = evolvedPokemonData.Move2,
                    PossibleCp = (int)PokemonInfo.GetMaxCpAtTrainerLevel(evolvedPokemonData, 40),
                    CandyToEvolve = setting.CandyToEvolve,
                    IvAtk = evolvedPokemonData.IndividualAttack,
                    IvDef = evolvedPokemonData.IndividualDefense,
                    Weight = evolvedPokemonData.WeightKg,
                    Cpm = evolvedPokemonData.CpMultiplier + evolvedPokemonData.AdditionalCpMultiplier,
                    StaminaMax = evolvedPokemonData.StaminaMax,
                    Evolutions = setting.EvolutionIds.ToArray()
                });
                if (isBuddy)
                {
                    session.BuddyPokemon = evolveResponse.EvolvedPokemonData; //TODO: CHECK THAT, Or should resend that poke as buddy
                }
            }
            session.EventDispatcher.Send(new PokemonActionDoneEvent { Uid = pokemonId });
            await DelayingUtils.Delay(session.LogicSettings.DelayEvolvePokemon, 25000);
            session.State = prevState;
        }
    }
}