using enums;

namespace action
{
    public abstract class GameAction
    {
        public abstract ActionId Id { get; }
        public abstract EnumActionStatus ActionStatus { get; set; }

        public virtual GameAction Create()
        {
            ActionId id = new ActionId();
            
        }
        public abstract void Execute();
    }
}