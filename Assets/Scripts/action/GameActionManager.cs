using System.Collections.Generic;
using tools.assert;

namespace action
{
    // Todo: Rename to BattleActionManger??
    public class GameActionManager
    {
        public List<GameAction> ActionQueue = new List<GameAction>();
        private uint _index = 0;
        
        public ActionId NextId()
        {
            return new ActionId(_index++);
        }
        
        public GameAction Pop()
        {
            MyAssert.Assert(ActionQueue.Count > 0, "No action in action list!");
            GameAction result = ActionQueue[0];
            ActionQueue.RemoveAt(0);
            return result;
        }

        public void Add(GameAction action)
        {
            ActionQueue.Add(action);
        }
    }
}