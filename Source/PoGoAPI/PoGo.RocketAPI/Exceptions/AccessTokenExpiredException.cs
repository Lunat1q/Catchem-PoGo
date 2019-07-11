using System;

namespace PokemonGo.RocketAPI.Exceptions
{
    public class AccessTokenExpiredException : Exception
    {
        public AccessTokenExpiredException(string msg = ""): base(msg)
        {
        }
    }
}