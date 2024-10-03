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
    }
}
