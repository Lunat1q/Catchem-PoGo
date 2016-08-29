using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Catchem.Classes;
using Catchem.Pages;
using PoGo.PokeMobBot.Logic.Tasks;
using POGOProtos.Enums;

namespace Catchem
{
    public partial class Styles
    {
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
                bot.Start();
        }

        private void btn_removeFromList_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.DataContext == null) return;
            var pokemonId = (PokemonId)btn.DataContext;
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
            var botOwner = Convert.ChangeType(btn.DataContext, oType);
            var parentList = btn.Tag as ListBox;
            var source = (IList)parentList?.ItemsSource;
            if (source != null && source.Contains(botOwner))
                source.Remove(botOwner);
        }

        private void mi_favouritePokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToLevel = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToLevel == null) return;
            FavouritePokemon(pokemonToLevel, pokeListBox);
        }

        private async void FavouritePokemon(Queue<PokemonUiData> pokemonQueue, ListBox pokeListBox)
        {
            if (pokemonQueue == null) return;
            pokeListBox.IsEnabled = false;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon?.OwnerBot != null && pokemon.OwnerBot.Started)
                await FavoriteSpecificPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.OwnerBot.CancellationToken, pokemon.Id);
            }
            pokeListBox.IsEnabled = true;
        }

        private void mi_evolvePokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToEvolve = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToEvolve == null) return;
            EvolvePokemon(pokemonToEvolve, pokeListBox);
        }

        private void mi_transferPokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToTransfer = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToTransfer == null) return;
            TransferPokemon(pokemonToTransfer, pokeListBox);
        }

        private void mi_levelupPokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToLevel = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToLevel == null) return;
            LevelUpPokemon(pokemonToLevel, pokeListBox);
        }


        private void mi_renamePokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToRename = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToRename == null) return;
            var inputDialog = new SupportForms.InputDialog("Please Enter a Name to Rename Pokemon:", "", false, 12);
            if (inputDialog.ShowDialog() != true) return;
            var customName = inputDialog.Answer;
            if (customName.Length > 12) return;
            RenamePokemon(pokemonToRename, pokeListBox, customName);
        }

        private void mi_renametoDefaultPokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToRename = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToRename == null) return;
            RenamePokemon(pokemonToRename, pokeListBox, "", true);
        }

        private void mi_maxlevelupPokemon_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            var pokeListBox = (mi?.Parent as ContextMenu)?.Tag as ListBox;
            if (pokeListBox?.SelectedIndex == -1) return;
            var pokemonToLevel = GetMultipleSelectedPokemon(pokeListBox);
            if (pokemonToLevel == null) return;
            LevelUpPokemon(pokemonToLevel, pokeListBox, true);
        }

        private static async void EvolvePokemon(Queue<PokemonUiData> pokemonQueue, ListBox pokeListBox)
        {
            if (pokemonQueue == null) return;
            pokeListBox.IsEnabled = false;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon?.OwnerBot != null && pokemon.OwnerBot.Started)
                    await EvolveSpecificPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id, pokemon.OwnerBot.CancellationToken);
            }
            pokeListBox.IsEnabled = true;
        }

        private static async void LevelUpPokemon(Queue<PokemonUiData> pokemonQueue, ListBox pokeListBox, bool toMax = false)
        {
            if (pokemonQueue == null) return;
            pokeListBox.IsEnabled = false;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon?.OwnerBot != null && pokemon.OwnerBot.Started)
                    await LevelUpSpecificPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id, pokemon.OwnerBot.CancellationToken, toMax);
            }
            pokeListBox.IsEnabled = true;
        }

        private static async void RenamePokemon(Queue<PokemonUiData> pokemonQueue, ListBox pokeListBox, string name = null, bool toDefault = false)
        {
            if (pokemonQueue == null) return;
            if (name == null) return;
            pokeListBox.IsEnabled = false;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon?.OwnerBot != null && pokemon.OwnerBot.Started)
                    await RenameSpecificPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id, pokemon.OwnerBot.CancellationToken, name, toDefault);
            }
            pokeListBox.IsEnabled = true;
        }

        private static async void TransferPokemon(Queue<PokemonUiData> pokemonQueue, ListBox pokeListBox)
        {
            if (pokemonQueue == null) return;
            pokeListBox.IsEnabled = false;
            while (pokemonQueue.Count > 0)
            {
                var pokemon = pokemonQueue.Dequeue();
                if (pokemon?.OwnerBot != null && pokemon.OwnerBot.Started)
                    await TransferPokemonTask.Execute(pokemon.OwnerBot.Session, pokemon.Id);
            }
            pokeListBox.IsEnabled = true;
        }

        private static Queue<PokemonUiData> GetMultipleSelectedPokemon(ListBox pokeListBox)
        {
            var pokemonQueue = new Queue<PokemonUiData>();
            if (pokeListBox == null) return pokemonQueue;
            foreach (var selectedItem in pokeListBox.SelectedItems)
            {
                var selectedMon = selectedItem as PokemonUiData;
                if (selectedMon != null)
                    pokemonQueue.Enqueue(selectedMon);

            }
            return pokemonQueue;
        }
    }
}
