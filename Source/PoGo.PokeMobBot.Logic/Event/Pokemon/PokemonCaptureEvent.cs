#region using directives

using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.PokeMobBot.Logic.Event.Pokemon
{
    public class PokemonCaptureEvent : BaseNewPokemonEvent
    {
        public int Attempt;
        public int BallAmount;
        public string CatchType;
        public double Distance;
        public int Exp;
        public int FamilyCandies;
        public ItemId Pokeball;
        public double Probability;
        public int Stardust;
        public CatchPokemonResponse.Types.CatchStatus Status;
    }
}