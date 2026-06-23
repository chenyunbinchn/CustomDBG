namespace gameStates.transient
{
    // Note: Per-battle bundle of mutable state that GameAction.Execute needs to reach at run time.
    public class BattleContext
    {
        public BattleState BattleState;
        public BattleCardPileState CardPileState;
    }
}