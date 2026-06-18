using System.Collections.Generic;

namespace action
{
    public class ActionExecutor
    {
        public void Execute(ActionQueue actionQueue)
        {
            GameAction curAction = actionQueue.Pop();
            
        }
    }
}