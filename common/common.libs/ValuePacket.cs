namespace common.libs
{
    public class ValuePacket<T> where T : struct
    {
        public T Value { get; set; }
    }
}
