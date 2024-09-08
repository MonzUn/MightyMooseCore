using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Moose.Data.Constants;
using Eco.Moose.Features;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.Message;
using Eco.Moose.Utils.Plugin;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;
using System.Text;
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
                Version? modIOVersion = MightyMooseCore.Obj.ModIoVersion;
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

        #region Plugin Management

        [ChatSubCommand("Moose", "Displays a list of all registered plugins.", ChatAuthorizationLevel.User)]
        public static void ListPlugins(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendJoin("\n", Lookups.Plugins.OrderBy(plugin => plugin.ToString()));
                DisplayCommandData(callingUser, Constants.GUI_PANEL_SIMPLE_LIST, "Plugins", sb.ToString());
            }, callingUser);
        }

        [ChatSubCommand("Moose", "Displays a list of all registered plugins that has a config.", ChatAuthorizationLevel.User)]
        public static void ListConfigurablePlugins(User callingUser)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendJoin("\n", Lookups.ConfigurablePlugins.OrderBy(plugin => plugin.ToString()));
                DisplayCommandData(callingUser, Constants.GUI_PANEL_SIMPLE_LIST, "Configurable Plugins", sb.ToString());
            }, callingUser);
        }

        [ChatSubCommand("Moose", "Reloads the config of the supplied plugin or the MightyMooseCore config if no plugin name is supplied.", ChatAuthorizationLevel.Admin)]
        public static void ReloadConfig(User callingUser, string pluginName = "")
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                IConfigurablePlugin? plugin = pluginName.IsEmpty()
                    ? MightyMooseCore.Obj
                    : Lookups.ConfigurablePlugins.FirstOrDefault(plugin => plugin.ToString().EqualsCaseInsensitive(pluginName) || plugin.GetType().Name.EqualsCaseInsensitive(pluginName));

                if (plugin == null)
                {
                    ReportCommandError(callingUser, $"Failed to find configurable plugin with name \"{pluginName}\"");
                    return;
                }

                var resultAndMessage = PluginUtils.ReloadConfig(plugin).Result;
                if (resultAndMessage.Item1)
                {
                    ReportCommandInfo(callingUser, resultAndMessage.Item2);
                }
                else
                {
                    ReportCommandError(callingUser, resultAndMessage.Item2);
                }
            }, callingUser);
        }

        #endregion

        #region Features

        [ChatSubCommand("Moose", "Displays available trades by player, tag, item or store.", ChatAuthorizationLevel.User)]
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

        [ChatSubCommand("Moose", "Displays information about food preferences of the target user.", ChatAuthorizationLevel.User)]
        public static void Taste(User callingUser, string userNameOrId = "")
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                User targetUser = !string.IsNullOrEmpty(userNameOrId) ? Lookups.UserByNameOrId(userNameOrId) : callingUser;
                if (targetUser == null)
                {
                    ReportCommandError(callingUser, $"No user with the name or ID \"{userNameOrId}\" could be found");
                    return;
                }

                TasteBuds targetTaste = targetUser.Stomach.TasteBuds;
                string[] tastePrefereneNames = Enum.GetNames(typeof(ItemTaste.TastePreference));
                Color[] tastePrefereneColors = { Color.Red, Color.Red, Color.LightRed, Color.Grey, Color.LightGreen, Color.Green, Color.Green };
                List<FoodItem>[] tasteToFood = new List<FoodItem>[tastePrefereneNames.Count()];
                for (int i = 0; i < tasteToFood.Length; ++i)
                {
                    tasteToFood[i] = new List<FoodItem>();
                }

                string data = string.Empty;
                foreach (var tasteEntry in targetTaste.FoodToTaste)
                {
                    FoodItem food = Lookups.FoodItems.FirstOrDefault(x => x.Type == tasteEntry.Key);
                    if (food == null)
                        continue;

                    tasteToFood[(int)tasteEntry.Value.Preference].Add(food);
                }

                // Add favorite/worst
                string favoriteDesc = targetTaste.FavoriteDiscovered ? tasteToFood[(int)ItemTaste.TastePreference.Favorite].First().MarkedUpName : "Unknown";
                string worstDesc = targetTaste.WorstDiscovered ? tasteToFood[(int)ItemTaste.TastePreference.Worst].First().MarkedUpName : "Unknown";
                data += $"{Text.Header(Text.Color(Color.Green, "Favorite"))}:   {favoriteDesc}\n{Text.Header(Text.Color(Color.Red, "Worst"))}:   {worstDesc}\n\n";

                // Loop backwards over the remaining taste categories to get tastier food at the top and skip favorite and worst
                for (int i = tasteToFood.Length - 2; i >= 1; --i)
                {
                    data += $"--- {Text.Header(Text.Color(tastePrefereneColors[i], tastePrefereneNames[i]))} ---\n";
                    foreach (FoodItem food in tasteToFood[i])
                    {
                        data += $"{food.MarkedUpName}\n";
                    }
                    data += "\n";
                }

                DisplayCommandData(callingUser, Constants.GUI_PANEL_TASTE, $"{targetUser.MarkedUpName} food preferences", data);
            }, callingUser);
        }

        #endregion
    }
}
