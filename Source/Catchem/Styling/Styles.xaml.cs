using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Catchem.Classes;
using Catchem.UiTranslation;
using GMap.NET.WindowsPresentation;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using POGOProtos.Enums;

namespace Catchem.Styling
{
    public partial class Styles
    {
        private static bool _debugMode = false;

        public Styles()
        {
#if DEBUG
            _debugMode = true;
#endif
        }

        private void btn_botStart_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var bot = btn?.DataContext as BotWindowData;
            if (bot == null) return;
            if (bot.Started)
            {
                bot.Stop();
                MainWindow.BotWindow.ClearPokemonData(bot);
            }
            else
            {
                bot.Start();
                MainWindow.BotWindow.SetPokemonData(bot);
            }
        }

        private void btn_removeFromList_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.DataContext == null) return;
            var pokemonId = (PokemonId) btn.DataContext;
            var parentList = btn.Tag as ListBox;
            var source = parentList?.ItemsSource as ObservableCollection<PokemonId>;
            if (source != null && source.Contains(pokemonId))
                source.Remove(pokemonId);
        }

        private void btn_removeFromListGeneric_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.DataContext == null) return;
            var oType = btn.DataContext.GetType();
            var obj = Convert.ChangeType(btn.DataContext, oType);
            var parentList = btn.Tag as ListBox;
            var source = (IList) parentList?.ItemsSource;
            if (source != null && source.Contains(obj))
                source.Remove(obj);
        }

        private void mi_favouritePokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToLevel = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToLevel == null) return;
            FavouritePokemon(pokemonToLevel);
        }

        private static void FavouritePokemon(Queue<PokemonUiData> pokemonQueue)
        {
            if (pokemonQueue == null) return;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon == null) continue;
                if (pokemon.OwnerBot != null && pokemon.OwnerBot.Started || _debugMode)
                {
                    pokemon.OwnerBot?.Session.AddActionToQueue(
                        async () =>
                        {
                            await
                                FavoriteSpecificPokemonTask.Execute(pokemon.OwnerBot.Session,
                                    pokemon.OwnerBot.CancellationToken,
                                    pokemon.Id);
                            return true;
                        },
                        $"{TranslationEngine.GetDynamicTranslationString("%FAVORITE%", "Favorite")} {pokemon.Name}", pokemon.Id);
                    pokemon.InAction = true;
                }
            }
        }

        private void mi_evolvePokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToEvolve = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToEvolve == null) return;
            EvolvePokemon(pokemonToEvolve);
        }

        private void mi_transferPokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToTransfer = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToTransfer == null) return;
            TransferPokemon(pokemonToTransfer);
        }

        private void mi_levelupPokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToLevel = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToLevel == null) return;
            LevelUpPokemon(pokemonToLevel);
        }


        private void mi_renamePokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToRename = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToRename == null) return;
            var inputDialog = new SupportForms.InputDialog(TranslationEngine.GetDynamicTranslationString("%POKE_NAME_INPUT%","Please Enter a Name to Rename Pokemon:"), "", false, 12);
            if (inputDialog.ShowDialog() != true) return;
            var customName = inputDialog.Answer;
            if (customName.Length > 12) return;
            RenamePokemon(pokemonToRename, customName);
        }

        private void mi_renametoDefaultPokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToRename = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToRename == null) return;
            RenamePokemon(pokemonToRename, "", true);
        }

        private void mi_maxlevelupPokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToLevel = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToLevel == null) return;
            LevelUpPokemon(pokemonToLevel, true);
        }

        private static void EvolvePokemon(Queue<PokemonUiData> pokemonQueue)
        {
            if (pokemonQueue == null) return;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon == null) continue;
                if (pokemon.OwnerBot != null && pokemon.OwnerBot.Started && pokemon.Evolutions.Any()|| _debugMode)
                {
                    pokemon.OwnerBot.Session.AddActionToQueue(
                        async () =>
                        {
                            await
                                EvolveSpecificPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id,
                                    pokemon.OwnerBot.CancellationToken);
                            return true;
                        }, $"{TranslationEngine.GetDynamicTranslationString("%EVOLVE%", "Evolve")} {pokemon.Name}", pokemon.Id);
                    pokemon.InAction = true;
                }
            }
        }

        private static void LevelUpPokemon(Queue<PokemonUiData> pokemonQueue, bool toMax = false)
        {
            if (pokemonQueue == null) return;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon == null) continue;
                if (pokemon.OwnerBot != null && pokemon.OwnerBot.Started || _debugMode)
                {
                    pokemon.OwnerBot.Session.AddActionToQueue(
                        async () =>
                        {
                            await
                                LevelUpSpecificPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id,
                                    pokemon.OwnerBot.CancellationToken, toMax);
                            return true;
                        }, $"{TranslationEngine.GetDynamicTranslationString("%LEVEL_UP%", "Level up")}: {pokemon.Name}", pokemon.Id);
                    pokemon.InAction = true;
                }
            }
        }

        private static void RenamePokemon(Queue<PokemonUiData> pokemonQueue, string name = null, bool toDefault = false)
        {
            if (pokemonQueue == null) return;
            if (name == null) return;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon == null) continue;
                if (pokemon.OwnerBot != null && pokemon.OwnerBot.Started || _debugMode)
                {
                    pokemon.OwnerBot.Session.AddActionToQueue(
                        async () =>
                        {
                            await
                                RenameSpecificPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id,
                                    pokemon.OwnerBot.CancellationToken, name, toDefault);
                            return true;
                        }, $"{TranslationEngine.GetDynamicTranslationString("%RENAME_POKE%", "Rename poke")}: {pokemon.Name}", pokemon.Id); 
                    pokemon.InAction = true;
                }
            }
        }

        private static void TransferPokemon(Queue<PokemonUiData> pokemonQueue)
        {
            if (pokemonQueue == null) return;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon == null) continue;
                if (pokemon.OwnerBot != null && pokemon.OwnerBot.Started || _debugMode)
                {
                    pokemon.OwnerBot.Session.AddActionToQueue(
                        async () =>
                        {
                            await TransferPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id);
                            return true;
                        }, $"{TranslationEngine.GetDynamicTranslationString("%TRANSFER%", "Transfer")}: {pokemon.Name}", pokemon.Id);
                    pokemon.InAction = true;
                }
            }
        }

        private static Queue<PokemonUiData> GetMultipleSelectedPokemon(ListBox pokeListBox)
        {
            var pokemonQueue = new Queue<PokemonUiData>();
            if (pokeListBox == null) return pokemonQueue;
            foreach (var selectedItem in pokeListBox.SelectedItems)
            {
                var selectedMon = selectedItem as PokemonUiData;
                if (selectedMon != null && !selectedMon.InAction)
                    pokemonQueue.Enqueue(selectedMon);

            }
            return pokemonQueue;
        }

        private static void SetPokemonAsBuddy(Queue<PokemonUiData> pokemonQueue)
        {
            if (pokemonQueue == null) return;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon == null) continue;
                if (pokemon.OwnerBot != null && pokemon.OwnerBot.Started || _debugMode)
                {
                    pokemon.OwnerBot.Session.AddActionToQueue(
                        async () =>
                        {
                            await
                                SetBuddyPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id);
                            return true;
                        }, $"{TranslationEngine.GetDynamicTranslationString("%SET_BUDDY%", "Set Buddy")}: {pokemon.Name}", pokemon.Id);
                    pokemon.InAction = true;
                }
            }
        }

        private void mi_useLure_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var marker = mi?.DataContext as GMapMarker;
            var bot = marker?.Tag as BotWindowData;
            if (bot == null) return;
            try
            {
                if (!bot.MapMarkers.ContainsValue(marker)) return;
                var psUid = bot.MapMarkers.FirstOrDefault(x => x.Value == marker).Key;
                UseLureModuleFromUi(bot, psUid);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async void UseLureModuleFromUi(BotWindowData bot, string fortId)
        {
            await UseLureModule.Execute(bot.Session, bot.CancellationToken, fortId);
        }

        private void mi_recycleItem_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var itemListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (itemListBox == null || itemListBox.SelectedItems.Count != 1) return;
            var item = (ItemUiData)itemListBox.SelectedItem;
            if (item?.OwnerBot == null) return;
            var curSession = item.OwnerBot.Session;
            int amount;
            var inputDialog = new SupportForms.InputDialog(TranslationEngine.GetDynamicTranslationString("%RECYCLE_INPUT%", "Please, enter amout to recycle:"), "1", true);
            if (inputDialog.ShowDialog() != true) return;
            if (int.TryParse(inputDialog.Answer, out amount))
            {
                RecycleItem(curSession, item, amount, item.OwnerBot.CancellationToken);
            }
        }

        private static void RecycleItem(ISession session, ItemUiData item, int amount, CancellationToken cts)
        {
            long uidLong = -1;
            var uid = (ulong)uidLong;
            session.AddActionToQueue(async () =>
            {
                await RecycleSpecificItemTask.Execute(session, item.Id, amount, cts);
                return true;
            }, $"{TranslationEngine.GetDynamicTranslationString("%RECYCLE_ITEM%", "Recycle item")}: {item.Id}x{amount}", uid);
        }

        private void mi_useItem_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var cm = (mi?.Parent as ContextMenu);
            var itemListBox = cm?.Tag as ListBox;
            //var itemListBox = cmGrid?.Tag as ListBox;            
            if (itemListBox == null || itemListBox.SelectedItems.Count != 1) return;
            var item = (ItemUiData)itemListBox.SelectedItem;
            if (item?.OwnerBot == null) return;
            UseItem(item);
        }

        private static void UseItem(ItemUiData item)
        {
            if (item.OwnerBot.Started || _debugMode)
            {
                item.OwnerBot.Session.AddActionToQueue(
                    async () =>
                    {
                        await UseItemTask.Execute(item.OwnerBot.Session, item.Id, item.OwnerBot.CancellationToken);
                        return true;
                    }, $"{TranslationEngine.GetDynamicTranslationString("%USING%", "Using")}: {item.Name}", 0);
            }
        }

        private void mi_makePokemonBuddy_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokeToSetBuddy = GetMultipleSelectedPokemon(pokeListBox);
            if (pokeToSetBuddy.Count > 1 || pokeToSetBuddy.Count <= 0) return;
            SetPokemonAsBuddy(pokeToSetBuddy);
        }
    }
}
