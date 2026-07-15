using System;
using enums;

namespace combat
{
    // Note: Stable, value-type identity for a combat actor (Player or Enemy). Store this on actions
    //       instead of an object reference — it serializes / records cleanly and stays valid when the
    //       target is removed (Resolve simply fails to find it). See EntityApi.Resolve.
    public readonly struct ActionEntityId : IEquatable<ActionEntityId>
    {
        public readonly EnumEntityType Type;
        public readonly uint Id;

        public ActionEntityId(EnumEntityType type, uint id)
        {
            Type = type;
            Id = id;
        }

        public static bool operator ==(ActionEntityId left, ActionEntityId right)
        {
            return left.Type == right.Type && left.Id == right.Id;
        }

        public static bool operator !=(ActionEntityId left, ActionEntityId right)
        {
            return !(left == right);
        }

        public bool Equals(ActionEntityId other)
        {
            return Type == other.Type && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionEntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Type, Id);
        }
    }
}
