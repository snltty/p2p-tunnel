using System;

namespace common.server
{
    public interface IMessenger
    {
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class MessengerIdRangeAttribute : Attribute
    {
        public ushort Min { get; set; }
        public ushort Max { get; set; }

        public MessengerIdRangeAttribute(ushort min, ushort max)
        {
            Min = min;
            Max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class MessengerIdAttribute : Attribute
    {
        public ushort Id { get; set; }

        public MessengerIdAttribute(ushort id)
        {
            Id = id;
        }
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public class MessengerIdEnumAttribute : Attribute
    {
    }
}
