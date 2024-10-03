using Eco.Gameplay.Civics.Demographics;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Channels;
using Eco.Gameplay.Systems.Messaging.Mail;
using Eco.Gameplay.Utils;
using Eco.Moose.Tools.Logger;
using Eco.Shared.Localization;
using Eco.Shared.Networking;
using Eco.Shared.Utils;
using System.Reflection;
using Constants = Eco.Moose.Data.Constants;

namespace Eco.Moose.Utils.Message
{
    public static class Message
    {
        public static bool ChatChannelExists(string channelName)
        {
            return ChannelManager.Obj.Registrar.GetByName(channelName) != null;
        }

        public static Channel CreateChatChannel(string channelName)
        {
            Channel newChannel = new Channel();
            newChannel.Managers.Add(DemographicManager.Obj.Get(SpecialDemographics.Admins));
            newChannel.Users.Add(DemographicManager.Obj.Get(SpecialDemographics.Everyone));
            newChannel.Name = channelName;
            ChannelManager.Obj.Registrar.Insert(newChannel);

            var channelUsers = newChannel.ChatRecipients;
            foreach (User user in channelUsers)
            {
                var tabSettings = GlobalData.Obj.ChatSettings(user).ChatTabSettings;
                var generalChannel = ChannelManager.Obj.Get(SpecialChannel.General);
                var chatTab = tabSettings.OfType<ChatTabSettingsCommon>().FirstOrDefault(tabSetting => tabSetting.Channels.Contains(generalChannel));
                if (chatTab != null)
                {
                    chatTab.Channels.Add(newChannel);
                }
                else
                {
                    Logger.Warning($"Failed to find chat tab when creating channel \"{channelName}\" for user \"{user.Name}\"", Assembly.GetCallingAssembly());
                }
            }

            Logger.Info($"Created chat channel \"{newChannel.Name}\"", Assembly.GetCallingAssembly());
            return newChannel;
        }

        public static bool SendChatRaw(User sender, string targetAndMessage) // NOTE: Does not trigger ChatMessageSent GameAction
        {
            var to = ChatParsingUtils.ResolveReceiver(targetAndMessage, out var messageContent);
            if (to.Failed || to.Val == null)
            {
                Logger.Error($"Failed to resolve receiver of message: \"{targetAndMessage}\"", Assembly.GetCallingAssembly());
                return false;
            }
            IChatReceiver receiver = to.Val;

            // Clean the message
            messageContent = messageContent.Replace("<br>", "");
            ProfanityUtils.ReplaceIfNotClear(ref messageContent, "<Message blocked - Contained profanity>", null);

            if (string.IsNullOrEmpty(messageContent))
            {
                Logger.Warning($"Attempted to send empty message: \"{targetAndMessage}\"", Assembly.GetCallingAssembly());
                return false;
            }

            // TODO: Handle muted users
            // TODO: Handle access to channels
            // TODO: Handle tab opening for DMs
            // TODO: Handle tab opening for channels

            ChatMessage chatMessage = new ChatMessage(sender, receiver, messageContent);
            IEnumerable<User> receivers = (sender != null ? chatMessage.Receiver.ChatRecipients.Append(sender).Distinct() : chatMessage.Receiver.ChatRecipients);
            foreach (INetClient client in receivers.Select(u => u.Player?.Client).NonNull())
                ChatManager.Obj.RPC("DisplayChatMessage", client, chatMessage.ToBson(client));

            // Add to chatlog so that offline users can see the message when they come online
            ChatManager.Obj.AddToChatLog(chatMessage);

            ChatManager.MessageSent.Invoke(chatMessage);
            return true;
        }

        public static bool SendChatToChannel(User sender, string channel, string message)
        {
            return SendChatRaw(sender, $"#{channel} {message}");
        }

        public static bool SendChatToDefaultChannel(User? sender, string message)
        {
            return SendChatRaw(sender, $"#{Constants.DEFAULT_CHAT_CHANNEL_NAME} {message}");
        }

        public static bool SendChatToUser(User? sender, User recipient, string message)
        {
            return SendChatRaw(sender, $"@{recipient.Name} {message}");
        }

        public static bool SendInfoBoxToUser(User recipient, string message)
        {
            if (recipient == null || recipient.Player == null)
                return false;

            recipient.Msg(Localizer.DoStr(message), Shared.Services.NotificationStyle.InfoBox);
            return true;
        }

        public static bool SendWarningBoxToUser(User recipient, string message)
        {
            if (recipient == null || recipient.Player == null)
                return false;

            recipient.Msg(Localizer.DoStr(message), Shared.Services.NotificationStyle.Warning);
            return true;
        }

        public static bool SendErrorBoxToUser(User recipient, string message)
        {
            if (recipient == null || recipient.Player == null)
                return false;

            recipient.Error(Localizer.DoStr(message));
            return true;
        }

        public static bool SendPopupToUser(User recipient, string message)
        {
            if (recipient == null || recipient.Player == null)
                return false;

            recipient.Player.OkBoxLoc($"{message}");
            return true;
        }

        public static bool SendNotificationToUser(User recipient, string message, bool sendOffline)
        {
            if (recipient == null || recipient.Player == null && !sendOffline)
                return false;


            recipient.Mailbox.Add(new MailMessage(message, "Notifications"), false);
            return true;
        }

        public static bool SendInfoPanelToUser(User recipient, string instance, string title, string message)
        {
            if (recipient == null || recipient.Player == null)
                return false;

            recipient.Player.OpenInfoPanel(title, message, instance);
            return true;
        }
    }
}
