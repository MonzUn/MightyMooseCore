namespace Eco.Moose.Events
{
    public static class EventConstants
    {
        public static readonly long INTERNAL_EVENT_DIVIDER = 1L << 32; // Events over this should trigger the a public callback
    }

    public enum EventType : System.UInt64
    {
        WorldReset = 1L << 0,
        Trade = 1L << 1,

        // Matched with other plugins
        AccumulatedTrade = 1L << 62,
    }

    public class MooseEventArgs : EventArgs
    {
        public MooseEventArgs(EventType eventType, object[] data)
        {
            EventType = eventType;
            Data = data;
        }

        public EventType EventType { get; set; }
        public object[] Data { get; set; }
    }
}
