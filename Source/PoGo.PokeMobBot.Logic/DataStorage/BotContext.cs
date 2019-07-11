using System;
using System.Data.Entity;
using PoGo.PokeMobBot.Logic.API;
using PoGo.PokeMobBot.Logic.Migrations;

namespace PoGo.PokeMobBot.Logic.DataStorage
{
    internal class BotContext : DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<BotContext, Configuration>());
        }

        public void CheckCreated()
        {
            Database.CreateIfNotExists();
        }


        public BotContext(): base("BotContext")
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", System.IO.Directory.GetCurrentDirectory());
        }

        public DbSet<PokeStop> PokeStops { get; set; }
        public DbSet<GeoLatLonAlt> MapzenAlt { get; set; }
        public DbSet<PokemonSeen> PokemonSeen { get; set; }
    }
}
