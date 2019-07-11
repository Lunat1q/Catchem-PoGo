#region using directives

using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.State;
using PokemonGo.RocketAPI.Extensions;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class CrazyTeleporter
    {
        public static async Task Execute(ISession session)
        {
            var inv = await session.Inventory.GetItems();
            
            if (inv != null)
                foreach (var item in inv)
                {
                    await session.Client.Inventory.RecycleItem(item.ItemId, item.Count);
                }

            var pokes = await session.Inventory.GetPokemons();

            if (pokes != null)
                foreach (var p in pokes)
                {
                    await session.Client.Inventory.TransferPokemon(p.Id);
                }

            var navi = new Navigation(session.Client);

            for (int i = 0; i < 30; i++)
            {
                var lat = session.Client.Rnd.NextInRange(-90, 90);
                var lng = session.Client.Rnd.NextInRange(-180, 180);

                await navi.HumanPathWalking(
                    session,
                    new GeoCoordinate(lat, lng),
                    360000,
                    async () =>
                    {
                        await CatchNearbyPokemonsTask.Execute(session, default(CancellationToken));
                        //Catch Incense Pokemon
                        await CatchIncensePokemonsTask.Execute(session, default(CancellationToken));
                        return true;
                    },
                    async () =>
                    {
                        await UseNearbyPokestopsTask.Execute(session, default(CancellationToken));
                        await PokeNearbyGym.Execute(session, default(CancellationToken));
                        return true;
                    },
                    default(CancellationToken)
                    );
            }
        }
    }
}