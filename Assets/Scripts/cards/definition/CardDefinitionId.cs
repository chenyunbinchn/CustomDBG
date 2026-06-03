using System;
using tools;

namespace cards.definition
{
    public readonly struct CardDefinitionId : IEquatable<CardDefinitionId>
    {
        public readonly string Name;

        public CardDefinitionId(string name)
        {
            MyAssert.Assert(name != "", "CardDefinitionId create failed, empty card name!");
            Name = name;
        }

        public static bool operator ==(CardDefinitionId left, CardDefinitionId right)
        {
            return left.Name == right.Name;
        }

        public static bool operator !=(CardDefinitionId left, CardDefinitionId right)
        {
            return left.Name != right.Name;
        }

        public static bool operator ==(CardDefinitionId left, string right)
        {
            return left.Name == right;
        }

        public static bool operator !=(CardDefinitionId left, string right)
        {
            return left.Name != right;
        }
        
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