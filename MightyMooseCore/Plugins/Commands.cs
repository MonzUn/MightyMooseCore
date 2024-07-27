using Eco.Core.Utils;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Moose.Data.Constants;
using Eco.Moose.Features;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Message;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;

using static Eco.Moose.Data.Enums;
using static Eco.Moose.Features.Trade;

using StoreOfferList = System.Collections.Generic.IEnumerable<System.Linq.IGrouping<string, System.Tuple<Eco.Gameplay.Components.Store.StoreComponent, Eco.Gameplay.Components.TradeOffer>>>;

namespace Eco.Moose.Plugin
{
    [ChatCommandHandler]
    public class Commands
    {
        #region Commands Base

        private delegate void EcoCommand(User callingUser, params string[] parameters);

        private static void ExecuteCommand<TRet>(EcoCommand command, User callingUser, params string[] parameters)
        {
            // Trim the arguments since they often have a space at the beginning
            for (int i = 0; i < parameters.Length; ++i)
            {
                parameters[i] = parameters[i].Trim();
            }

            string commandName = command.Method.Name;
            try
            {
                Logger.Debug($"{TextUtils.StripTags(callingUser.Name)} invoked command \"/{commandName}\"");
                command(callingUser, parameters);
            }
            catch (Exception e)
            {
                ReportCommandError(callingUser, $"An error occurred while attempting to execute command {commandName}.\nError message: {e}");
                Logger.Exception($"An exception occured while attempting to execute a command.\nCommand name: \"{commandName}\"\nCalling user: \"{TextUtils.StripTags(callingUser.Name)}\"", e);
            }
        }

        [ChatCommand("Commands for the Mighty Moose Core.", "MM", ChatAuthorizationLevel.User)]
#pragma warning disable IDE0079 // Remove unnecessary suppression (This is a false positive case)
#pragma warning disable IDE0060 // Remove unused parameter - callingUser parameter required
        public static void Moose(User callingUser) { }
#pragma warning restore IDE0079
#pragma warning restore IDE0060

        #endregion

        #region User Feedback

        public static void ReportCommandError(User callingUser, string message)
        {
            Message.SendErrorBoxToUser(callingUser, message);
        }

        public static void ReportCommandInfo(User callingUser, string message)
        {
            Message.SendInfoBoxToUser(callingUser, message);
        }

        public static void DisplayCommandData(User callingUser, string panelInstance, string title, string data)
        {
            Message.SendInfoPanelToUser(callingUser, panelInstance, title, data);
        }

        #endregion

        #region Meta

        [ChatSubCommand("Moose", "Displays the installed and latest available plugin version.", ChatAuthorizationLevel.User)]
        public static void Version(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                Version? modIOVersion = MightyMooseCore.Obj.ModIOVersion;
                string modIOVersionDesc = modIOVersion != null ? $"Latest version: {modIOVersion.ToString(3)}" : "Latest version: Unknown";

                Version installedVersion = MightyMooseCore.Obj.InstalledVersion;
                string installedVersionDesc = $"Installed version: {installedVersion.ToString(3)}";

                if (modIOVersion == null)
                    modIOVersionDesc = Text.Color(Color.Red, modIOVersionDesc);

                if (modIOVersion != null && modIOVersion > installedVersion)
                    installedVersionDesc = Text.Color(Color.Red, installedVersionDesc);

                ReportCommandInfo(callingUser, $"{modIOVersionDesc}\n{installedVersionDesc}");
            }, callingUser);
        }

        [ChatSubCommand("Moose", "Opens the documentation web page.", ChatAuthorizationLevel.User)]
        public static void Documentation(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                callingUser.OpenWebpage("https://mod.io/g/eco/m/mightymoosecore");
            }, callingUser);
        }

        #endregion

        #region Messaging
        [ChatSubCommand("Moose", "Announces a message to everyone or a specified user.", "mann", ChatAuthorizationLevel.Admin)]
        public static void Announce(User callingUser, string message, string messageType = "Notification", User recipient = null)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                if (message.IsEmpty())
                {
                    ReportCommandError(callingUser, $"Failed to send message - Message can not be empty");
                    return;
                }

                if (!Enum.TryParse(messageType, ignoreCase: true, out MessageType messageTypeEnum))
                {
                    ReportCommandError(callingUser, $"\"{messageType}\" is not a valid message type. The available message types are: {string.Join(", ", Enum.GetNames(typeof(MessageType)))}");
                    return;
                }

                if (recipient != null && messageTypeEnum != MessageType.NotificationOffline && !recipient.IsOnline)
                {
                    ReportCommandError(callingUser, $"Failed to send message - {recipient.Name} is offline.");
                    return;
                }

                string formattedMessage = messageTypeEnum switch
                {
                    MessageType.Chat => $"{callingUser.Name}: {message}",
                    MessageType.Info => $"{callingUser.Name}: {message}",
                    MessageType.Warning => $"{callingUser.Name}: {message}",
                    MessageType.Error => $"{callingUser.Name}: {message}",
                    MessageType.Notification => $"[{callingUser.Name}]\n\n{message}",
                    MessageType.NotificationOffline => $"[{callingUser.Name}]\n\n{message}",
                    MessageType.Popup => $"[{callingUser.Name}]\n{message}",
                };

                bool result = true;
                switch (messageTypeEnum)
                {
                    case MessageType.Chat:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendChatToUser(null, recipient, formattedMessage);
                            }
                            else
                            {
                                result = Message.SendChatToDefaultChannel(null, formattedMessage);
                            }
                            break;
                        }

                    case MessageType.Info:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendInfoBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendInfoBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }

                    case MessageType.Warning:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendWarningBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendWarningBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case MessageType.Error:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendErrorBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendErrorBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case MessageType.Popup:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendPopupToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendPopupToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case MessageType.Notification:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendNotificationToUser(recipient, message, sendOffline: false);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendNotificationToUser(onlineUser, formattedMessage, sendOffline: false) && result;
                                }
                            }
                            break;
                        }

                    case MessageType.NotificationOffline:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendNotificationToUser(recipient, message, sendOffline: true);
                            }
                            else
                            {
                                foreach (User user in UserManager.Users)
                                {
                                    result = Message.SendNotificationToUser(user, formattedMessage, sendOffline: true) && result;
                                }
                            }
                            break;
                        }
                }

                string sendContext = recipient == null ? "all players" : recipient.Name;
                if (result)
                    ReportCommandInfo(callingUser, $"Message delivered to {sendContext}.");
                else
                    ReportCommandError(callingUser, $"Failed to send message to {sendContext}.");

            }, callingUser);
        }
        #endregion

        #region Features

        [ChatSubCommand("Moose", "Displays available trades by player, tag, item or store.", "Trades", ChatAuthorizationLevel.User)]
        public static void Trades(User callingUser, string searchName)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                if (string.IsNullOrWhiteSpace(searchName))
                {
                    ReportCommandInfo(callingUser, "Please provide the name of a player, tag, item or store to search for.");
                    return;
                }

                string matchedName = Trade.FindOffers(searchName, out TradeTargetType offerType, out StoreOfferList groupedBuyOffers, out StoreOfferList groupedSellOffers);
                if (offerType == TradeTargetType.Invalid)
                {
                    ReportCommandError(callingUser, $"No player, tag, item or store with the name \"{searchName}\" could be found.");
                    return;
                }

                FormatTrades(callingUser, offerType, groupedBuyOffers, groupedSellOffers, out string message);
                DisplayCommandData(callingUser, Constants.GUI_PANEL_TRADES, matchedName, message);

            }, callingUser);
        }

        #endregion

    }
}
