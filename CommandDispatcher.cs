using System;
using System.Collections.Generic;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using log4net;

namespace RemoteControl
{
    /// <summary>
    /// Maps command strings to FollowBot message IDs and dispatches them.
    /// Also handles BotManager start/stop which don't need FollowBot.
    /// </summary>
    public static class CommandDispatcher
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        // Command string → FollowBot message ID mapping
        private static readonly Dictionary<string, string> CommandToMessageId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "StartFollow",    "RC_start_follow" },
            { "StopFollow",     "RC_stop_follow" },
            { "StartAttack",    "RC_start_attack" },
            { "StopAttack",     "RC_stop_attack" },
            { "StartLoot",      "RC_start_loot" },
            { "StopLoot",       "RC_stop_loot" },
            { "StartPortal",    "RC_start_portal" },
            { "StopPortal",     "RC_stop_portal" },
            { "Teleport",       "RC_teleport" },
            { "OpenPortal",     "RC_open_portal" },
            { "EnterPortal",    "RC_enter_portal" },
            { "Stash",          "RC_stash" },
            { "NewInstance",    "RC_new_instance" },
            { "FollowTownOn",   "RC_follow_town_on" },
            { "FollowTownOff",  "RC_follow_town_off" },
            { "FollowHideoutOn",  "RC_follow_hideout_on" },
            { "FollowHideoutOff", "RC_follow_hideout_off" },
            { "FollowHeistOn",  "RC_follow_heist_on" },
            { "FollowHeistOff", "RC_follow_heist_off" },
            { "AutoDepositOn",  "RC_auto_deposit_on" },
            { "AutoDepositOff", "RC_auto_deposit_off" },
            { "UseGuildStash",  "RC_use_guild_stash" },
            { "UseRegularStash","RC_use_regular_stash" },
            { "UltPortalOn",    "RC_ult_portal_on" },
            { "UltPortalOff",   "RC_ult_portal_off" },
            { "Unloader",       "RC_unloader" },
        };

        /// <summary>
        /// Dispatch a command string. Handles bot lifecycle commands directly,
        /// routes everything else to FollowBot via the DPB Message system.
        /// Supports parameterized commands like "SetUltTimer:15".
        /// </summary>
        public static void Dispatch(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                Log.WarnFormat("[RemoteControl] Empty command received, ignoring.");
                return;
            }

            // Handle bot lifecycle commands directly — these don't go through FollowBot
            if (command.Equals("BotStart", StringComparison.OrdinalIgnoreCase))
            {
                if (!BotManager.IsRunning)
                {
                    var result = BotManager.Start();
                    Log.WarnFormat("[RemoteControl] BotManager.Start() => {0}", result);
                }
                else
                {
                    Log.InfoFormat("[RemoteControl] Bot is already running.");
                }
                return;
            }

            if (command.Equals("BotStop", StringComparison.OrdinalIgnoreCase))
            {
                if (BotManager.IsRunning)
                {
                    var result = BotManager.Stop(false);
                    Log.WarnFormat("[RemoteControl] BotManager.Stop() => {0}", result);
                }
                else
                {
                    Log.InfoFormat("[RemoteControl] Bot is already stopped.");
                }
                return;
            }

            // Handle parameterized commands — format: "SetUltTimer:15"
            if (command.StartsWith("SetUltTimer:", StringComparison.OrdinalIgnoreCase))
            {
                var valuePart = command.Substring("SetUltTimer:".Length);
                int timerValue;
                if (int.TryParse(valuePart, out timerValue))
                {
                    var bot = BotManager.Current;
                    if (bot == null)
                    {
                        Log.WarnFormat("[RemoteControl] No bot selected, cannot set UltTimer.");
                        return;
                    }
                    var msg = new Message("RC_set_ult_timer");
                    msg.AddInput(timerValue, "value");
                    var result = bot.Message(msg);
                    Log.InfoFormat("[RemoteControl] SetUltTimer({0}) => {1}", timerValue, result);
                }
                return;
            }

            // Handle parameterized commands — format: "SetUnloaderDelay:2000"
            if (command.StartsWith("SetUnloaderDelay:", StringComparison.OrdinalIgnoreCase))
            {
                var valuePart = command.Substring("SetUnloaderDelay:".Length);
                int delayValue;
                if (int.TryParse(valuePart, out delayValue))
                {
                    var bot = BotManager.Current;
                    if (bot == null)
                    {
                        Log.WarnFormat("[RemoteControl] No bot selected, cannot set UnloaderDelay.");
                        return;
                    }
                    var msg = new Message("RC_set_unloader_delay");
                    msg.AddInput(delayValue, "value");
                    var result = bot.Message(msg);
                    Log.InfoFormat("[RemoteControl] SetUnloaderDelay({0}) => {1}", delayValue, result);
                }
                return;
            }

            // Route to FollowBot via Message system
            if (CommandToMessageId.TryGetValue(command, out string messageId))
            {
                var bot = BotManager.Current;
                if (bot == null)
                {
                    Log.WarnFormat("[RemoteControl] No bot selected, cannot dispatch command: {0}", command);
                    return;
                }

                var msg = new Message(messageId);
                var result = bot.Message(msg);
                Log.InfoFormat("[RemoteControl] Dispatched '{0}' => Message '{1}' => {2}", command, messageId, result);
            }
            else
            {
                Log.WarnFormat("[RemoteControl] Unknown command: {0}", command);
            }
        }
    }
}
