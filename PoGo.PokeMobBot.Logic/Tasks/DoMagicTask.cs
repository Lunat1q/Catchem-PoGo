#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.PokeMobBot.Logic.Common;
using PoGo.PokeMobBot.Logic.Event;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public static class DoMagicTask
    {
        public static int TimesZeroXPawarded;

        public static async void Execute(ISession session, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                //while (!cancellationToken.IsCancellationRequested)
                //{
                    await Task.Delay(29*60*1000, cancellationToken);
                //    await session.Client.UpdateTicket();
                //}
            }
            catch (Exception)
            {
                //ignore
            }

        }
    }
}