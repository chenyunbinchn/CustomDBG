namespace action
{
    public class ActionExecutor
    {
        public void Execute(GameActionManager gameActionManager)
        {
            GameAction curAction = gameActionManager.Pop();
            curAction.Execute();
        }
    }
}