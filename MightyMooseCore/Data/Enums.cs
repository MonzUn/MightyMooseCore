namespace Eco.Moose.Data
{
    public static class Enums
    {
        public enum MessageTypes
        {
            Chat,                   // Regular chat message that is sent to the default chat channel
            Info,                   // Short on-screen message that times out quickly (White)
            Warning,                // Short on-screen message that times out quickly (Yellow)
            Error,                  // Short on-screen message that times out quickly (Red)
            Popup,                  // OKBox that takes focus and has to be actively dismissed
            Notification,           // Persistent notification that ends up in the user's inbox while also being automatically opened
            NotificationOffline,    // Persistent notification that ends up in the user's inbox. Also sent to offline users
        }

        public enum LookupResultTypes
        {
            NoMatch,
            SingleMatch,
            MultiMatch
        }

        #pragma warning disable format
        [Flags]
        public enum LookupTypes
        {
            None    = 0,
            Item    = 1 << 1,
            Tag     = 1 << 2,
            User    = 1 << 3,
            Store   = 1 << 4,
        }
        #pragma warning restore format
    }
}
