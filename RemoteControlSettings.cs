using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using Newtonsoft.Json;

namespace RemoteControl
{
    public class RemoteControlSettings : JsonSettings
    {
        private static RemoteControlSettings _instance;
        public static RemoteControlSettings Instance => _instance ?? (_instance = new RemoteControlSettings());

        public RemoteControlSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, "RemoteControl.json"))
        {
            if (RemoteBots == null)
                RemoteBots = new ObservableCollection<string>();
        }

        private int _listenPort = 5200;
        private bool _isCommanderMode;
        private ObservableCollection<string> _remoteBots;

        /// <summary>
        /// TCP port to listen on for incoming commands. All bots use this.
        /// </summary>
        [DefaultValue(5200)]
        public int ListenPort
        {
            get => _listenPort;
            set
            {
                _listenPort = value;
                NotifyPropertyChanged(() => ListenPort);
            }
        }

        /// <summary>
        /// When true, this instance shows the commander UI with buttons to send commands
        /// to all remote bots AND the local bot. Only enable on the Main PC bot instance.
        /// </summary>
        [DefaultValue(false)]
        public bool IsCommanderMode
        {
            get => _isCommanderMode;
            set
            {
                _isCommanderMode = value;
                NotifyPropertyChanged(() => IsCommanderMode);
            }
        }

        /// <summary>
        /// List of remote bot addresses in "IP:Port" format (e.g., "127.0.0.1:5200" or "HOSTNAME:5200").
        /// Only used in Commander mode.
        /// </summary>
        public ObservableCollection<string> RemoteBots
        {
            get => _remoteBots;
            set
            {
                _remoteBots = value;
                NotifyPropertyChanged(() => RemoteBots);
            }
        }
    }
}
