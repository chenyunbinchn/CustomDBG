using System;
using cards.definition;
using tools.assert;

namespace items.definition
{
    public readonly struct ItemDefinitionId : IEquatable<ItemDefinitionId>
    {
        public readonly string Name;

        public ItemDefinitionId(string name)
        {
            MyAssert.Assert(name != "", "CardDefinitionId create failed, empty card name!");
            Name = name;
        }

        public static bool operator ==(ItemDefinitionId left, ItemDefinitionId right)
        {
            return left.Name == right.Name;
        }

        public static bool operator !=(ItemDefinitionId left, ItemDefinitionId right)
        {
            return left.Name != right.Name;
        }

        public static bool operator ==(ItemDefinitionId left, string right)
        {
            return left.Name == right;
        }

        public static bool operator !=(ItemDefinitionId left, string right)
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

        public bool Equals(ItemDefinitionId other)
        {
            return Name == other.Name;
        }
    }
}