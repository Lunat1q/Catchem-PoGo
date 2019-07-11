#region using directives

using System;
using System.Linq;
using System.Threading.Tasks;
using POGOProtos.Networking.Responses;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.Event.Item;
using PoGo.PokeMobBot.Logic.Event.Player;

#endregion

namespace PoGo.PokeMobBot.Logic.Utils
{
    public delegate void StatisticsDirtyDelegate();

    public class Statistics
    {
        private readonly DateTime _initSessionDateTime = DateTime.Now;

        public StatsExport ExportStats;
		private StatsExport _currentStats;
        private string _playerName;
        public int TotalExperience;
        public int TotalItemsRemoved;
        public int TotalPokemons;
        public int TotalPokemonsTransfered;
        public int TotalStardust;
        public int TotalPokestops;
        public int TotalEvolves;
        public int PokeBalls;
        public int HatchedNow;
        public int EncountersNow;


        public async Task Dirty(Inventory inventory)
        {
            if (ExportStats != null)
                _currentStats = ExportStats;

            ExportStats = await GetCurrentInfo(inventory);
            
            DirtyEvent?.Invoke();
        }

        public async void RefreshStatAndCheckLevelup(ISession session)
        {
            if (session.BuddyPokemon == null || session.BuddyPokemon.Id != session.Profile.PlayerData.BuddyPokemon.Id)
            {
                session.BuddyPokemon = await session.Inventory.GetBuddyPokemon(session.Profile.PlayerData.BuddyPokemon.Id);
            }
            await Dirty(session.Inventory);
            await CheckLevelUp(session);
        }

        public async void RefreshPokeDex(ISession session)
        {
            await session.Inventory.UpdatePokeDex();
        }


        public async Task GetLevelUpRewards(ISession session, int level)
        {
            var resp = await session.Inventory.GetLevelUpRewards(level);
            session.Runtime.CurrentLevel = ExportStats.Level;
            if (resp.Result == LevelUpRewardsResponse.Types.Result.Success)
            {
                if (resp.ItemsAwarded.Any())
                {
                    session.EventDispatcher.Send(new PlayerLevelUpEvent
                    {
                        Items = StringUtils.GetSummedFriendlyNameOfItemAwardList(resp.ItemsAwarded)
                    });
                    session.EventDispatcher.Send(new InventoryNewItemsEvent()
                    {
                        Items = resp.ItemsAwarded.ToItemList()
                    });
                }
            }
        }

        public async Task CheckLevelUp(ISession session)
        {
            if (_currentStats == null)
            {
                if (ExportStats != null)
                {
                    await GetLevelUpRewards(session, ExportStats.Level);
                }
                return;
            }
            if (_currentStats.Level < ExportStats.Level)
            {
                await GetLevelUpRewards(session, ExportStats.Level);
            }
            else if (session.Runtime.CurrentLevel == 0)
            {
                session.Runtime.CurrentLevel = ExportStats.Level;
            }

            _currentStats = null;
        }

        public event StatisticsDirtyDelegate DirtyEvent;

        private string FormatRuntime()
        {
            return (DateTime.Now - _initSessionDateTime).ToString(@"dd\.hh\:mm\:ss");
        }

        public async Task<StatsExport> GetCurrentInfo(Inventory inventory)
        {
            var stats = await inventory.RefreshPlayerStats();
            if (stats == null) return null;
            var ep = stats.NextLevelXp - stats.PrevLevelXp - (stats.Experience - stats.PrevLevelXp);
            var time = Math.Round(ep/(TotalExperience/GetRuntime()), 2);
            var hours = 0.00;
            var minutes = 0.00;
            if (double.IsInfinity(time) == false && time > 0)
            {
                hours = Math.Truncate(TimeSpan.FromHours(time).TotalHours);
                minutes = TimeSpan.FromHours(time).Minutes;
            }

            var output = new StatsExport
            {
                Level = stats.Level,
                HoursUntilLvl = hours,
                MinutesUntilLevel = minutes,
                CurrentXp = stats.Experience - stats.PrevLevelXp - GetXpDiff(stats.Level),
                LevelupXp = stats.NextLevelXp - stats.PrevLevelXp - GetXpDiff(stats.Level)
            };
            return output;
        }

        public double GetRuntime()
        {
            return (DateTime.Now - _initSessionDateTime).TotalSeconds/3600;
        }

        public string GetTemplatedStats(string template, string xpTemplate)
        {
            var xpStats = string.Format(xpTemplate, ExportStats.Level, ExportStats.HoursUntilLvl,
                ExportStats.MinutesUntilLevel, ExportStats.CurrentXp, ExportStats.LevelupXp);
            return string.Format(template, _playerName, FormatRuntime(), xpStats, TotalExperience/GetRuntime(),
                TotalPokemons/GetRuntime(),
                TotalStardust, TotalPokemonsTransfered, TotalItemsRemoved);
        }

        public static int GetXpDiff(int level)
        {
            if (level <= 0 || level > 40) return 0;
            int[] xpTable =
            {
                0, 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
                10000, 10000, 10000, 10000, 15000, 20000, 20000, 20000, 25000, 25000,
                50000, 75000, 100000, 125000, 150000, 190000, 200000, 250000, 300000, 350000,
                500000, 500000, 750000, 1000000, 1250000, 1500000, 2000000, 2500000, 3000000, 5000000
            };
            return xpTable[level - 1];
        }

        public void SetUsername(GetPlayerResponse profile)
        {
            _playerName = profile.PlayerData.Username ?? "";
        }
    }

    public class StatsExport
    {
        public long CurrentXp;
        public double HoursUntilLvl;
        public int Level;
        public long LevelupXp;
        public double MinutesUntilLevel;
    }
}
