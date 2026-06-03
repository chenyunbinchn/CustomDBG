using System;

namespace cards.definition
{
    public readonly struct CardDefinitionId : IEquatable<CardDefinitionId>
    {
        public readonly string Name;

        public bool Equals(CardDefinitionId other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is CardDefinitionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }
    }
}