using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoGo.PokeMobBot.Logic.Extensions
{
    public static class RandomExtensions
    {
        /// <summary>
        /// Extension method to Random to return a value in a certain range.
        /// </summary>
        /// <param name="rng"><c>Random</c> object.</param>
        /// <param name="min">Minimum value, inclusive.</param>
        /// <param name="max">Maximum value, inclusive.</param>
        /// <returns>A value between <c>min</c> and <c>max</c>, inclusive.</returns>
        public static double NextInRange(this Random rng, double min, double max)
        {
            return rng.NextDouble() * (max - min) + min;
        }
    }
}
