using System.Collections.Generic;
using System.Linq;
using tools.assert;

namespace action
{
    public class ActionQueue
    {
        public List<GameAction> ActionList = new List<GameAction>();

        public GameAction Pop()
        {
            MyAssert.Assert(ActionList.Count > 0, "No action in action list!");
            GameAction result = ActionList[0];
            ActionList.RemoveAt(0);
            return result;
        }

        public void Add(GameAction action)
        {
            ActionList.Add(action);
        }
    }
}