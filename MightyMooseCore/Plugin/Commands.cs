using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Moose.Data;
using Eco.Moose.Features;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.Message;
using Eco.Moose.Utils.Plugin;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;
using System.Text;
using static Eco.Moose.Data.Enums;

#if DEBUG
using Eco.Gameplay.GameActions;
using System.Reflection;
#endif

namespace Eco.Moose.Plugin
{
    [ChatCommandHandler]
    public class Commands
    {
        #region Commands Base

        private delegate void EcoCommand(IChatClient caller, params string[] parameters);

        private static void ExecuteCommand<TRet>(EcoCommand command, IChatClient caller, params string[] parameters)
        {
            // Trim the arguments since they often have a space at the beginning
            for (int i = 0; i < parameters.Length; ++i)
            {
                parameters[i] = parameters[i].Trim();
            }

            string commandName = command.Method.Name;
            try
            {
                Logger.Debug($"{TextUtils.StripTags(caller.Name)} invoked command \"/{commandName}\"");
                command(caller, parameters);
            }
            catch (Exception e)
            {
                ReportCommandError(caller, $"An error occurred while attempting to execute command {commandName}.\nError message: {e}");
                Logger.Exception($"An exception occured while attempting to execute a command.\nCommand name: \"{commandName}\"\nCalling user: \"{TextUtils.StripTags(caller.Name)}\"", e);
            }
        }

        [ChatCommand("Commands for the Mighty Moose Core.", "MM", ChatAuthorizationLevel.User)]
#pragma warning disable IDE0079 // Remove unnecessary suppression (This is a false positive case)
#pragma warning disable IDE0060 // Remove unused parameter - caller parameter required
        public static void Moose(IChatClient caller) { }
#pragma warning restore IDE0079
#pragma warning restore IDE0060

        #endregion

        #region User Feedback

        public static void ReportCommandInfo(IChatClient caller, string message)
        {
            caller.MsgLocStr(message, Shared.Services.NotificationStyle.InfoBox);
        }

        public static void ReportCommandError(IChatClient caller, string message)
        {
            caller.ErrorLocStr(message);
        }

        public static void DisplayCommandData(User caller, string panelInstance, string title, string data)
        {
            Message.SendInfoPanelToUser(caller, panelInstance, title, data);
        }

        #endregion

        #region Meta

        [ChatSubCommand("Moose", "Displays the installed and latest available plugin version.", ChatAuthorizationLevel.User)]
        public static void Version(IChatClient caller)
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

                ReportCommandInfo(caller, $"{modIOVersionDesc}\n{installedVersionDesc}");
            }, caller);
        }

        [ChatSubCommand("Moose", "Opens the documentation web page.", ChatAuthorizationLevel.User)]
        public static void Documentation(User caller)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                caller.OpenWebpage("https://mod.io/g/eco/m/mightymoosecore");
            }, caller);
        }

        #endregion

        #region Messaging
        [ChatSubCommand("Moose", "Announces a message to everyone or a specified user.", "mann", ChatAuthorizationLevel.Admin)]
        public static void Announce(IChatClient caller, string message, string messageType = "Notification", User recipient = null)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                if (message.IsEmpty())
                {
                    ReportCommandError(caller, $"Failed to send message - Message can not be empty");
                    return;
                }

                if (!Enum.TryParse(messageType, ignoreCase: true, out MessageTypes messageTypeEnum))
                {
                    ReportCommandError(caller, $"\"{messageType}\" is not a valid message type. The available message types are: {string.Join(", ", Enum.GetNames(typeof(MessageTypes)))}");
                    return;
                }

                if (recipient != null && messageTypeEnum != MessageTypes.NotificationOffline && !recipient.IsOnline)
                {
                    ReportCommandError(caller, $"Failed to send message - {recipient.Name} is offline.");
                    return;
                }

                string formattedMessage = messageTypeEnum switch
                {
                    MessageTypes.Chat => $"{caller.Name}: {message}",
                    MessageTypes.Info => $"{caller.Name}: {message}",
                    MessageTypes.Warning => $"{caller.Name}: {message}",
                    MessageTypes.Error => $"{caller.Name}: {message}",
                    MessageTypes.Notification => $"[{caller.Name}]\n\n{message}",
                    MessageTypes.NotificationOffline => $"[{caller.Name}]\n\n{message}",
                    MessageTypes.Popup => $"[{caller.Name}]\n{message}",
                };

                bool result = true;
                switch (messageTypeEnum)
                {
                    case MessageTypes.Chat:
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

                    case MessageTypes.Info:
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

                    case MessageTypes.Warning:
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
                    case MessageTypes.Error:
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
                    case MessageTypes.Popup:
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
                    case MessageTypes.Notification:
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

                    case MessageTypes.NotificationOffline:
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
                    ReportCommandInfo(caller, $"Message delivered to {sendContext}.");
                else
                    ReportCommandError(caller, $"Failed to send message to {sendContext}.");

            }, caller);
        }
        #endregion

        #region Plugin Management

        [ChatSubCommand("Moose", "Displays a list of all registered plugins.", ChatAuthorizationLevel.User)]
        public static void ListPlugins(User caller)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendJoin("\n", Lookups.Plugins.OrderBy(plugin => plugin.ToString()));
                DisplayCommandData(caller, Constants.GUI_PANEL_SIMPLE_LIST, "Plugins", sb.ToString());
            }, caller);
        }

        [ChatSubCommand("Moose", "Displays a list of all registered plugins that has a config.", ChatAuthorizationLevel.User)]
        public static void ListConfigurablePlugins(User caller)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendJoin("\n", Lookups.ConfigurablePlugins.OrderBy(plugin => plugin.ToString()));
                DisplayCommandData(caller, Constants.GUI_PANEL_SIMPLE_LIST, "Configurable Plugins", sb.ToString());
            }, caller);
        }

        [ChatSubCommand("Moose", "Reloads the config of the supplied plugin or the MightyMooseCore config if no plugin name is supplied.", ChatAuthorizationLevel.Admin)]
        public static void ReloadConfig(IChatClient caller, string pluginName = "")
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                IConfigurablePlugin? plugin = pluginName.IsEmpty()
                    ? MightyMooseCore.Obj
                    : Lookups.ConfigurablePlugins.FirstOrDefault(plugin => plugin.ToString().EqualsCaseInsensitive(pluginName) || plugin.GetType().Name.EqualsCaseInsensitive(pluginName));

                if (plugin == null)
                {
                    ReportCommandError(caller, $"Failed to find configurable plugin with name \"{pluginName}\"");
                    return;
                }

                var resultAndMessage = PluginUtils.ReloadConfig(plugin).Result;
                if (resultAndMessage.Item1)
                {
                    ReportCommandInfo(caller, resultAndMessage.Item2);
                }
                else
                {
                    ReportCommandError(caller, resultAndMessage.Item2);
                }
            }, caller);
        }

        #endregion

        #region Features

        [ChatSubCommand("Moose", "Displays available trades by player, tag, item or store.", ChatAuthorizationLevel.User)]
        public static void Trades(User caller, string searchName)
        {
            ExecuteCommand<object>((lUser, args) =>
            {
                if (string.IsNullOrWhiteSpace(searchName))
                {
                    ReportCommandInfo(caller, "Please provide the name of a player, tag, item or store to search for.");
                    return;
                }

                LookupResult lookupRes = DynamicLookup.Lookup(searchName, Constants.TRADE_LOOKUP_MASK);
                if (lookupRes.Result != LookupResultTypes.SingleMatch)
                {
                    if (lookupRes.Result == LookupResultTypes.MultiMatch)
                        ReportCommandInfo(caller, lookupRes.ErrorMessage);
                    else
                        ReportCommandError(caller, lookupRes.ErrorMessage);
                    return;
                }
                object matchedEntity = lookupRes.Matches.First();
                LookupTypes matchedEntityType = lookupRes.MatchedTypes;

                TradeOfferList tradeList = Trade.FindOffers(matchedEntity, matchedEntityType);
                Trade.FormatTrades(caller, matchedEntityType, tradeList.BuyOffers, tradeList.SellOffers, out string message);
                DisplayCommandData(caller, Constants.GUI_PANEL_TRADES, DynamicLookup.GetEntityName(matchedEntity), message);
            }, caller);
        }

        [ChatSubCommand("Moose", "Displays information about food preferences of the target user.", ChatAuthorizationLevel.User)]
        public static void Taste(User caller, string userNameOrId = "")
        {
            ExecuteCommand<object>(async (lUser, args) =>
            {
                User targetUser = !string.IsNullOrEmpty(userNameOrId) ? Lookups.UserByNameOrId(userNameOrId) : caller;
                if (targetUser == null)
                {
                    ReportCommandError(caller, $"No user with the name or ID \"{userNameOrId}\" could be found");
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

                DisplayCommandData(caller, Constants.GUI_PANEL_TASTE, $"{targetUser.MarkedUpName} food preferences", data);
            }, caller);
        }

        #endregion

#if DEBUG
        #region Dev Tools

        [ChatSubCommand("Moose", "List all existing types of GameActions", ChatAuthorizationLevel.Admin)]
        public static void ListGameActions(User user)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var type in Assembly.GetAssembly(typeof(GameAction)).GetTypes().OrderBy(t => t.Name))
            {
                if (type.IsSubclassOf(typeof(GameAction)))
                {
                    builder.AppendLine(type.GetSimpleName());
                }
            }
            user.Player.OpenInfoPanel("Game Actions", builder.ToString(), "GameActions");
        }

        #endregion
#endif
    }
}
