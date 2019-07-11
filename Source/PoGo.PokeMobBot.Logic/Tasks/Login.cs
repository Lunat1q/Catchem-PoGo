#region using directives

using System;
using PoGo.PokeMobBot.Logic.State;

#endregion

namespace PoGo.PokeMobBot.Logic.Tasks
{
    public interface ILogin
    {
        void DoLogin();
    }

    public class Login : ILogin
    {
        private readonly ISession _session;

        public Login(ISession session)
        {
            _session = session;
        }

        public void DoLogin()
        {
            try
            {
                _session.Client.Login.DoLogin().Wait();
            }
            catch (AggregateException ae)
            {
                throw ae.Flatten().InnerException;
            }
        }
    }
}