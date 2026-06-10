using System;

namespace random
{
    public readonly struct Seed : IEquatable<Seed>
    {
        public readonly uint Value;

        public Seed(uint value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(Seed left, Seed right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(Seed left, Seed right)
        {
            return left.Value != right.Value;
        }

        public static bool operator ==(Seed left, uint right)
        {
            return left.Value == right;
        }

        public static bool operator !=(Seed left, uint right)
        {
            return left.Value != right;
        }

        public override int GetHashCode()
        {
            return (int)Value;
        }

        public bool Equals(Seed other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is Seed other && Equals(other);
        }
    }
}
