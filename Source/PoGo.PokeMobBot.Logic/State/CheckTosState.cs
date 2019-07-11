#region using directives

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Event.Global;
using PoGo.PokeMobBot.Logic.Utils;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.State
{
    public class CheckTosState : IState
    {
        public async Task<IState> Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var player = session.Profile?.PlayerData;
            if (player == null) return new LoginState();
            if (session.LogicSettings.AutoCompleteTutorial)
            {
                var tutState = session.Profile.PlayerData.TutorialState;
                if (!tutState.Contains(TutorialState.LegalScreen))
                {
                    await
                        session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                            TutorialState.LegalScreen
                        });
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = session.Translation.GetTranslation(TranslationString.ReadTos)
                    });
                    await DelayingUtils.Delay(9000, 2000);
                }
                if (!tutState.Contains(TutorialState.AvatarSelection))
                {
                    var gen = session.Client.Rnd.Next(2) == 1 ? Gender.Male : Gender.Female;
                    var avatarRes = await session.Client.Player.SetAvatar(new PlayerAvatar()
                    {
                        Backpack = 0,
                        Eyes = 0,
                        Gender = gen,
                        Hair = 0,
                        Hat = 0,
                        Pants = 0,
                        Shirt = 0,
                        Shoes = 0,
                        Skin = 0
                    });
                    if (avatarRes.Status == SetAvatarResponse.Types.Status.AvatarAlreadySet ||
                        avatarRes.Status == SetAvatarResponse.Types.Status.Success)
                    {
                        await session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                            TutorialState.AvatarSelection
                        });
                        session.EventDispatcher.Send(new NoticeEvent()
                        {
                            Message = session.Translation.GetTranslation(TranslationString.GenderSelect, gen)
                        });
                    }
                }
                if (!tutState.Contains(TutorialState.PokemonCapture))
                {
                    await CatchFirstPokemon(session);
                }
                if (!tutState.Contains(TutorialState.NameSelection))
                {
                    await SelectNicnname(session);
                }
                if (!tutState.Contains(TutorialState.FirstTimeExperienceComplete))
                {
                    await
                        session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                            TutorialState.FirstTimeExperienceComplete
                        });
                    session.EventDispatcher.Send(new NoticeEvent()
                    {
                        Message = session.Translation.GetTranslation(TranslationString.TutorialPokestop)
                    });
                    await DelayingUtils.Delay(3000, 2000);
                }
            }
            return new FarmState();
        }

        public async Task<bool> CatchFirstPokemon(ISession session)
        {
            var firstPokeList = new List<PokemonId>
            {
                PokemonId.Bulbasaur,
                PokemonId.Charmander,
                PokemonId.Squirtle
            };

            var firstpokeRnd = session.Client.Rnd.Next(0, 2);
            var firstPoke = firstPokeList[firstpokeRnd];

            var res = await session.Client.Encounter.EncounterTutorialComplete(firstPoke);
            await DelayingUtils.Delay(7000, 2000);
            if (res.Result != EncounterTutorialCompleteResponse.Types.Result.Success) return false;
            session.EventDispatcher.Send(new NoticeEvent()
            {
                Message = session.Translation.GetTranslation(TranslationString.TutorialPoke, session.Translation.GetPokemonName(firstPoke))
            });
            return true;
        }

        public async Task<bool> SelectNicnname(ISession session)
        {
            if (string.IsNullOrEmpty(session.LogicSettings.DesiredNickname))
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.TutorialNameNotPicked)
                });
                return false;
            }

            if (session.LogicSettings.DesiredNickname.Length > 15)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.TutorialNameTooLong)
                });
                return false;
            }
			
            var res = await session.Client.Misc.ClaimCodename(session.LogicSettings.DesiredNickname);
            if (res.Status == ClaimCodenameResponse.Types.Status.SUCCESS)
            {
                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.TutorialNameDone, res.Codename)
                });
                await session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                            TutorialState.NameSelection
                        });
            }
            else if (res.Status == ClaimCodenameResponse.Types.Status.CODENAME_CHANGE_NOT_ALLOWED || res.Status == ClaimCodenameResponse.Types.Status.CURRENT_OWNER)
            {
                await session.Client.Misc.MarkTutorialComplete(new RepeatedField<TutorialState>()
                        {
                            TutorialState.NameSelection
                        });
            }
            else
            {
                var errorText = "Niantic error";
                switch (res.Status)
                {
                    case ClaimCodenameResponse.Types.Status.UNSET:
                        errorText = session.Translation.GetTranslation(TranslationString.TutorialNameErrorUnset);
                        break;
                    case ClaimCodenameResponse.Types.Status.SUCCESS:
                        errorText = session.Translation.GetTranslation(TranslationString.TutorialNameErrorChanged);
                        break;
                    case ClaimCodenameResponse.Types.Status.CODENAME_NOT_AVAILABLE:
                        errorText = session.Translation.GetTranslation(TranslationString.TutorialNameErrorNotAvail);
                        break;
                    case ClaimCodenameResponse.Types.Status.CODENAME_NOT_VALID:
                        errorText = session.Translation.GetTranslation(TranslationString.TutorialNameErrorNotValid);
                        break;
                    case ClaimCodenameResponse.Types.Status.CURRENT_OWNER:
                        errorText = session.Translation.GetTranslation(TranslationString.TutorialNameErrorOwner);
                        break;
                    case ClaimCodenameResponse.Types.Status.CODENAME_CHANGE_NOT_ALLOWED:
                        errorText = session.Translation.GetTranslation(TranslationString.TutorialNameErrorNoMore);
                        break;
                }

                session.EventDispatcher.Send(new NoticeEvent()
                {
                    Message = session.Translation.GetTranslation(TranslationString.TutorialNameFailed, errorText)
                });
            }
            await DelayingUtils.Delay(3000, 2000);
            return res.Status == ClaimCodenameResponse.Types.Status.SUCCESS;
        }
    }
}