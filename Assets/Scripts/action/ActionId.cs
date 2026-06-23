using System;

namespace action
{
    // Todo: ActionId should combines ownerId + actionId; Figure out if there is a way to clarify owner is player or others
    // Todo: Maybe can make some bits as signature sign.. Some bits as uid value.. idk
    public readonly struct ActionId : IEquatable<ActionId>
    {
        public readonly uint Value;

        public ActionId(uint value)
        {
            Value = value;
        }

        public static bool operator ==(ActionId left, ActionId right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(ActionId left, ActionId right)
        {
            return left.Value != right.Value;
        }

        public static bool operator ==(ActionId left, uint right)
        {
            return left.Value == right;
        }

        public static bool operator !=(ActionId left, uint right)
        {
            return left.Value != right;
        }
        
        public bool Equals(ActionId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Value;
        }
    }
}