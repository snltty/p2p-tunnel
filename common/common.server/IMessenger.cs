using System;

namespace common.server
{
    public interface IMessenger
    {
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class MessengerIdRangeAttribute : Attribute
    {
        public int Min { get; set; }
        public int Max { get; set; }

        public MessengerIdRangeAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MessengerIdAttribute : Attribute
    {
        public int Id { get; set; }

        public MessengerIdAttribute(int id)
        {
            Id = id;
        }
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class MessengerIdEnumAttribute : Attribute
    {
    }
}
