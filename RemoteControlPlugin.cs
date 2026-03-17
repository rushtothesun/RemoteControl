using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using log4net;

namespace RemoteControl
{
    /// <summary>
    /// DreamPoeBot plugin that provides:
    /// - TCP listener on all instances (receives commands from commander)
    /// - Commander UI on the main PC (sends commands to all bots)
    /// </summary>
    public class RemoteControlPlugin : IPlugin, IStartStopEvents
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private RemoteControlGui _gui;
        private readonly TcpCommandServer _server = new TcpCommandServer();

        public string Name => "RemoteControl";
        public string Author => "Custom";
        public string Description => "LAN-based remote command center for FollowBot. Replaces in-game chat commands.";
        public string Version => "1.0";
        public UserControl Control => _gui ?? (_gui = new RemoteControlGui());
        public JsonSettings Settings => RemoteControlSettings.Instance;

        /// <summary>
        /// Called when DPB loads the plugin. Wire up event handler only.
        /// </summary>
        public void Initialize()
        {
            _server.OnCommandReceived += CommandDispatcher.Dispatch;
            Log.InfoFormat("[RemoteControl] Plugin initialized.");
        }

        /// <summary>
        /// Called when DPB unloads the plugin. Full cleanup.
        /// </summary>
        public void Deinitialize()
        {
            _server.OnCommandReceived -= CommandDispatcher.Dispatch;
            _server.Stop();
            Log.WarnFormat("[RemoteControl] Plugin deinitialized.");
        }

        /// <summary>
        /// Called when the Enabled checkbox is checked. Starts the listener.
        /// </summary>
        public void Enable()
        {
            var settings = RemoteControlSettings.Instance;
            _server.Start(settings.ListenPort);
            Log.WarnFormat("[RemoteControl] Enabled. Listener on port {0}. Commander mode: {1}",
                settings.ListenPort, settings.IsCommanderMode);
        }

        /// <summary>
        /// Called when the Enabled checkbox is unchecked. Stops the listener.
        /// </summary>
        public void Disable()
        {
            _server.Stop();
            Log.WarnFormat("[RemoteControl] Disabled. Listener stopped.");
        }

        /// <summary>
        /// Called when the bot starts. Nothing extra needed —
        /// the listener is already running from Initialize().
        /// </summary>
        public void Start()
        {
            Log.InfoFormat("[RemoteControl] Bot started. Listener already active.");
        }

        /// <summary>
        /// Called when the bot stops. Listener keeps running.
        /// </summary>
        public void Stop()
        {
            Log.InfoFormat("[RemoteControl] Bot stopped. Listener still active.");
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
    }
}
