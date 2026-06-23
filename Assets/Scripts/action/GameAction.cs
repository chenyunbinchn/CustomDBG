using System.Collections;
using enums;

namespace action
{
    public abstract class GameAction
    {
        public abstract ActionId Id { get; }
        public abstract EnumActionStatus ActionStatus { get; set; }
        public abstract IEnumerator Execute(); // Todo: Check if action should not have execute function, ActionExecutor handle every execute?
    }
}