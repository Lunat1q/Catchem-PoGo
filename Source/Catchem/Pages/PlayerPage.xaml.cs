using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using Catchem.Classes;
using Catchem.Extensions;
using Catchem.Interfaces;
using Catchem.UiTranslation;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using POGOProtos.Enums;
using static System.String;

namespace Catchem.Pages
{
    /// <summary>
    /// Interaction logic for PlayerPage.xaml
    /// </summary>
    public partial class PlayerPage : IBotPage
    {
        private BotWindowData _bot;
        private bool _inRefreshItems;
        private ISession CurSession => _bot.Session;

        public PlayerPage()
        {
            InitializeComponent();
        }

        public void SetBot(BotWindowData bot)
        {
            _bot = bot;
            UpdatePlayerTab();
            UpdateLists();
            Dispatcher.Invoke(new ThreadStart(UpdateRunTimeData));
        }

        public void UpdatePlayerTeam()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                switch (_bot.Team)
                {
                    case TeamColor.Neutral:
                        TeamImage.Source = Properties.Resources.team_neutral.LoadBitmap();
                        break;
                    case TeamColor.Blue:
                        TeamImage.Source = Properties.Resources.team_mystic.LoadBitmap();
                        break;
                    case TeamColor.Red:
                        TeamImage.Source = Properties.Resources.team_valor.LoadBitmap();
                        break;
                    case TeamColor.Yellow:
                        TeamImage.Source = Properties.Resources.team_instinct.LoadBitmap();
                        break;
                }
            }));
        }

        private void RefreshItems_Click(object sender, RoutedEventArgs e)
        {;
            if (_bot?.Session == null || _inRefreshItems) return;
            RefreshItems(_bot.Session);
        }

        private async void RefreshItems(ISession curSession)
        {
            _inRefreshItems = true;
            Action<IEvent> action = (evt) => curSession.EventDispatcher.Send(evt);
            await InventoryListTask.Execute(curSession, action);
            _inRefreshItems = false;
        }

        public void UpdatePlayerTab()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                CoinsFarmed.Text = _bot.Coins.ToString();
                Playername.Content = _bot.PlayerName;
                UpdatePlayerTeam();
                UpdateBuddyPokemon(_bot);
                InvStatus.Text = $"({_bot.ItemList.Sum(x => x.Amount)}/{_bot.MaxItemStorageSize})";
                AmountStardust.Text = _bot.StartStarDust.ToString();
                _bot.StarDust = _bot.StartStarDust;
            }));
        }
       

        public void UpdateItems()
        {
            if (_bot != null && ItemListBox != null && UsedItemsBox != null)
            {
                UpdateLists();
            }
        }

        public void UpdateLists()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                ItemListBox.ItemsSource = _bot.ItemList;
                UsedItemsBox.ItemsSource = _bot.UsedItemsList;
            }));
        }

        public void UpdateInventoryCount()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                InvStatus.Text = $"({_bot.ItemList.Sum(x => x.Amount)}/{_bot.MaxItemStorageSize})";
            }));
        }

        public void UpdateRunTimeData()
        {
            var farmedDust = _bot.Session?.Stats?.TotalStardust == 0 ? 0 : _bot.Session?.Stats?.TotalStardust - _bot.StartStarDust;
            var dustpH = farmedDust / _bot.Ts.TotalHours;
            if (dustpH != null)
            {
                var farmedDustH = _bot?.Ts.TotalHours < 0.001 ? "~" : ((double)dustpH).ToString("0");
                AmountStarDustFarmed.Text = $"{farmedDust} ({farmedDustH}/h)";
            }
            if (_bot.Session?.Stats?.ExportStats == null) return;
            if (_bot.Session?.Stats.TotalStardust > 0)
                _bot.StarDust = _bot.Session.Stats.TotalStardust;
            XpAmount.Text = _bot.Session?.Stats.ExportStats.CurrentXp.ToString();
            XpAmountFarmed.Text = _bot.Session?.Stats.TotalExperience.ToString();
            PokeFarmed.Text = _bot.Session?.Stats.TotalPokemons.ToString();
            PokeTransfered.Text = _bot.Session?.Stats.TotalPokemonsTransfered.ToString();
            PokestopsFarmed.Text = _bot.Session?.Stats.TotalPokestops.ToString();
            LevelLabel.Content = _bot.Session?.Stats.ExportStats.Level.ToString();
            AllTimeWalked.Text = _bot.Session?.PlayerStats?.KmWalked.ToN1();
            EggsHatched.Text = _bot.Session?.PlayerStats?.EggsHatched.ToString();
            Evolutions.Text = _bot.Session?.PlayerStats?.Evolutions.ToString();
            PokeStopVisits.Text = _bot.Session?.PlayerStats?.PokeStopVisits.ToString();
            PokeballsThrown.Text = _bot.Session?.PlayerStats?.PokeballsThrown.ToString();
            PokemonsCaptured.Text = _bot.Session?.PlayerStats?.PokemonsCaptured.ToString();
            PokemonsEncountered.Text = _bot.Session?.PlayerStats?.PokemonsEncountered.ToString();
            UniquePokedexEntries.Text = _bot.Session?.PlayerStats?.UniquePokedexEntries.ToString();
            EvolutionsNow.Text = _bot.Session?.Stats.TotalEvolves.ToString();
            HatchedNow.Text = _bot.Session?.Stats.HatchedNow.ToString();
            EncountersNow.Text = _bot.Session?.Stats.EncountersNow.ToString();
            PokeballsNow.Text = _bot.Session?.Stats.PokeBalls.ToString();

            if (BuddyImg.Source == null)
            {
                UpdateBuddyPokemon(_bot);
                UpdateBuddyCandies(0);
            }

            var walked = _bot.Session?.PlayerStats?.KmWalked -
                                    _bot.Session?.Profile?.PlayerData?.BuddyPokemon?.StartKmWalked;
            if (walked != null)
                UpdateBuddyWalked((double)walked);

            NextLevelInTextBox.Text =
                $"{_bot.Session?.Stats.ExportStats.HoursUntilLvl.ToString("00")}:{_bot.Session?.Stats.ExportStats.MinutesUntilLevel.ToString("00")} ({_bot.Session?.Stats.ExportStats.CurrentXp}/{_bot.Session?.Stats.ExportStats.LevelupXp})";
            LevelProgressBar.Value = (int)(_bot.Session?.Stats.ExportStats.CurrentXp*100/_bot.Session?.Stats.ExportStats.LevelupXp);
        }

        public void UpdateBuddyPokemon(BotWindowData targetBot)
        {
            if (targetBot?.Session?.BuddyPokemon == null)
            {
                Dispatcher.Invoke(new ThreadStart(delegate
                {
                    BuddyName.Text = "";
                }));
                return;
            }
            var toReset = targetBot.PokemonList.Where(x => x.Buddy);
            foreach (var p in toReset)
                p.Buddy = false;
            var id = targetBot.Session?.BuddyPokemon.Id;
            var targetMon = targetBot.PokemonList.FirstOrDefault(x => x.Id == id);
            if (targetMon != null)
                targetMon.Buddy = true;
            

            var nick = targetBot.Session?.BuddyPokemon?.Nickname;
            if (IsNullOrEmpty(targetBot.Session?.BuddyPokemon?.Nickname))
                nick = targetBot.Session?.Translation.GetPokemonName(targetBot.Session.BuddyPokemon.PokemonId);
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                BuddyName.Text = nick;
                BuddyImg.Source = targetBot.Session?.BuddyPokemon?.PokemonId.ToSource();
            }));
        }

        public void ClearData()
        {
            if (ItemListBox != null)
                ItemListBox.ItemsSource = null;
            if (UsedItemsBox != null)
                UsedItemsBox.ItemsSource = null;
        }

       private async void SelectTeam(TeamColor clr)
        {
            await SetPlayerTeamTask.Execute(CurSession, clr);
        }

        private void team_image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_bot == null || !_bot.Started || _bot.Team != TeamColor.Neutral || _bot.Level < 5) return;
            var inputDialog = new SupportForms.InputDialog(TranslationEngine.GetDynamicTranslationString("%SELECT_TEAM_INPUT%", "Please, select a team:"), null, false, 0, new List<object>{TeamColor.Blue, TeamColor.Yellow, TeamColor.Red});
            if (inputDialog.ShowDialog() != true || inputDialog.ObjectAnswer == null) return;
            var team = (TeamColor)inputDialog.ObjectAnswer;
            SelectTeam(team);
        }

        private void ManualMaintenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_bot == null || !_bot.Started) return;
            _bot.Session.Runtime.StopsHit = 999;
        }


        public void UpdateBuddyWalked(double walked)
        {
            if (_bot == null) return;
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                BuddyDistance.Text = walked >= 0 ? walked.ToN1() : "???";
            }));
        }

        public void UpdateBuddyCandies(int candyEarnedCount)
        {
            if (_bot == null) return;
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                int? prevCandy = BuddyCandies.Tag as int? ?? 0;
                BuddyCandies.Text = candyEarnedCount >= 0 ? (candyEarnedCount + prevCandy).ToString() : "???";
                BuddyCandies.Tag = candyEarnedCount + prevCandy;
            }));
        }
    }
}
