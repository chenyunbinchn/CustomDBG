using System;
using ids;

namespace cards.instance
{
    public struct CardInstanceId
    {
        private Int64 _value;

        public void GenerateId()
        {
            _value = IdGenerator.Generate();
        }

        public Int64 GetId()
        {
            return _value;
        }
    }
}