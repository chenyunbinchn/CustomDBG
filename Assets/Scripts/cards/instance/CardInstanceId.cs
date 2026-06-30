using System;

namespace cards.instance
{
    public readonly struct CardInstanceId : IEquatable<CardInstanceId>, IComparable<CardInstanceId>
    {
        public readonly uint Value;

        public CardInstanceId(uint value)
        {
            Value = value;
        }

        public static bool operator ==(CardInstanceId left, CardInstanceId right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(CardInstanceId left, CardInstanceId right)
        {
            return left.Value != right.Value;
        }

        public static bool operator ==(CardInstanceId left, uint right)
        {
            return left.Value == right;
        }

        public static bool operator !=(CardInstanceId left, uint right)
        {
            return left.Value != right;
        }
        
        public bool Equals(CardInstanceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CardInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Value;
        }

        // Note: Total order by Value. Enables ShuffleHelper.StableShuffle (sort-then-shuffle).
        public int CompareTo(CardInstanceId other)
        {
            return Value.CompareTo(other.Value);
        }
    }
}