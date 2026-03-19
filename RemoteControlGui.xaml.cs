using System;
using System.Collections.Specialized;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DreamPoeBot.Loki.Common;
using log4net;

namespace RemoteControl
{
    public partial class RemoteControlGui : UserControl
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public RemoteControlGui()
        {
            InitializeComponent();

            // Rebuild panels when remote bots list changes
            var settings = RemoteControlSettings.Instance;
            if (settings.RemoteBots != null)
                settings.RemoteBots.CollectionChanged += (s, e) => RebuildBotPanels();

            Loaded += (s, e) => RebuildBotPanels();
        }

        /// <summary>
        /// Rebuilds the command panels — one for "Local Bot" + one per remote bot.
        /// </summary>
        private void RebuildBotPanels()
        {
            BotPanelsContainer.Children.Clear();

            // Local bot panel
            AddBotPanel("Local Bot", null);

            // Remote bot panels
            var settings = RemoteControlSettings.Instance;
            if (settings.RemoteBots != null)
            {
                foreach (var entry in settings.RemoteBots)
                {
                    if (string.IsNullOrWhiteSpace(entry)) continue;
                    // Entry format: "IP:Port" or "IP:Port|Label"
                    var parts = entry.Split('|');
                    var address = parts[0].Trim();
                    var label = parts.Length > 1 ? parts[1].Trim() : address;
                    AddBotPanel(label, address);
                }
            }
        }

        /// <summary>
        /// Creates a compact command panel for a single bot.
        /// </summary>
        private void AddBotPanel(string label, string address)
        {
            var tag = address ?? "LOCAL";

            var group = new GroupBox
            {
                Header = label,
                Margin = new Thickness(3),
                BorderBrush = address == null
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(66, 133, 244)),
                BorderThickness = new Thickness(2),
                MinWidth = 170
            };

            var stack = new StackPanel { Margin = new Thickness(2) };

            // Bot Lifecycle — Start/Stop Bot (full text, colored)
            var lifecycleRow = new WrapPanel { Margin = new Thickness(0, 2, 0, 4) };
            lifecycleRow.Children.Add(MakeButton("Start Bot", "BotStart", tag,
                Color.FromRgb(76, 175, 80), Brushes.White));
            lifecycleRow.Children.Add(MakeButton("Stop Bot", "BotStop", tag,
                Color.FromRgb(244, 67, 54), Brushes.White));
            stack.Children.Add(lifecycleRow);

            // Toggle rows: Label [✓] [✗]
            stack.Children.Add(MakeToggleRow("Follow", "StartFollow", "StopFollow", tag));

            // Follow location sub-toggles: compact row
            var followLocRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(55, 0, 0, 2) };
            followLocRow.Children.Add(MakeSmallLabel("Town"));
            followLocRow.Children.Add(MakeButton("✓", "FollowTownOn", tag, Color.FromRgb(76, 175, 80), Brushes.White, 20, 10));
            followLocRow.Children.Add(MakeButton("✗", "FollowTownOff", tag, Color.FromRgb(244, 67, 54), Brushes.White, 20, 10));
            followLocRow.Children.Add(MakeSmallLabel("HO"));
            followLocRow.Children.Add(MakeButton("✓", "FollowHideoutOn", tag, Color.FromRgb(76, 175, 80), Brushes.White, 20, 10));
            followLocRow.Children.Add(MakeButton("✗", "FollowHideoutOff", tag, Color.FromRgb(244, 67, 54), Brushes.White, 20, 10));
            followLocRow.Children.Add(MakeSmallLabel("Heist"));
            followLocRow.Children.Add(MakeButton("✓", "FollowHeistOn", tag, Color.FromRgb(76, 175, 80), Brushes.White, 20, 10));
            followLocRow.Children.Add(MakeButton("✗", "FollowHeistOff", tag, Color.FromRgb(244, 67, 54), Brushes.White, 20, 10));
            stack.Children.Add(followLocRow);

            stack.Children.Add(MakeToggleRow("Attack", "StartAttack", "StopAttack", tag));
            stack.Children.Add(MakeToggleRow("Loot", "StartLoot", "StopLoot", tag));
            stack.Children.Add(MakeToggleRow("Auto-TP", "StartPortal", "StopPortal", tag));

            // Action buttons
            var actionsRow = new WrapPanel { Margin = new Thickness(0, 4, 0, 2) };
            actionsRow.Children.Add(MakeButton("Teleport", "Teleport", tag));
            actionsRow.Children.Add(MakeButton("Open Portal", "OpenPortal", tag));
            stack.Children.Add(actionsRow);

            var actionsRow2 = new WrapPanel { Margin = new Thickness(0, 2, 0, 2) };
            actionsRow2.Children.Add(MakeButton("Enter Portal", "EnterPortal", tag));
            actionsRow2.Children.Add(MakeButton("New Instance", "NewInstance", tag));
            stack.Children.Add(actionsRow2);

            // Stash actions
            var otherRow = new WrapPanel { Margin = new Thickness(0, 2, 0, 2) };
            otherRow.Children.Add(MakeButton("Stash", "Stash", tag));
            otherRow.Children.Add(MakeButton("Stash $", "StashCurrency", tag));
            stack.Children.Add(otherRow);

            // --- Stash settings ---
            stack.Children.Add(MakeToggleRow("AutoDep", "AutoDepositOn", "AutoDepositOff", tag));

            var stashTypeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
            stashTypeRow.Children.Add(new TextBlock
            {
                Text = "Stash",
                Width = 55,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            });
            stashTypeRow.Children.Add(MakeButton("Regular", "UseRegularStash", tag, Color.FromRgb(66, 133, 244), Brushes.White, 0, 10));
            stashTypeRow.Children.Add(MakeButton("Guild", "UseGuildStash", tag, Color.FromRgb(255, 152, 0), Brushes.White, 0, 10));
            stack.Children.Add(stashTypeRow);

            // --- Ultimatum settings ---
            stack.Children.Add(MakeToggleRow("Ult TP", "UltPortalOn", "UltPortalOff", tag));

            // Timer slider row
            var timerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 3) };
            timerRow.Children.Add(new TextBlock
            {
                Text = "Timer",
                Width = 55,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            });
            var timerSlider = new Slider
            {
                Minimum = 1,
                Maximum = 60,
                Value = 15,
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Tag = tag
            };
            var timerLabel = new TextBlock
            {
                Text = "15s",
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                Width = 25
            };
            timerSlider.ValueChanged += (s, args) =>
            {
                var val = (int)args.NewValue;
                timerLabel.Text = val + "s";
                var t = (string)timerSlider.Tag;
                var cmd = "SetUltTimer:" + val;
                var settings = RemoteControlSettings.Instance;

                if (SyncAllCheckbox?.IsChecked == true && settings.IsCommanderMode)
                {
                    CommandDispatcher.Dispatch(cmd);
                    SendToAllRemotes(cmd);
                }
                else if (t == "LOCAL")
                {
                    CommandDispatcher.Dispatch(cmd);
                }
                else if (settings.IsCommanderMode)
                {
                    ThreadPool.QueueUserWorkItem(_ => HttpCommandSender.Send(t, cmd));
                }
            };
            timerRow.Children.Add(timerSlider);
            timerRow.Children.Add(timerLabel);
            stack.Children.Add(timerRow);

            var unloaderRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            unloaderRow.Children.Add(new TextBlock
            {
                Text = "Unloader",
                Width = 55,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            });
            unloaderRow.Children.Add(MakeButton("✓", "Unloader", tag, Color.FromRgb(76, 175, 80), Brushes.White, 28, 10));
            stack.Children.Add(unloaderRow);

            var delayRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 3) };
            delayRow.Children.Add(new TextBlock
            {
                Text = "Delay",
                Width = 55,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            });
            var delaySlider = new Slider
            {
                Minimum = 1500,
                Maximum = 10000,
                Value = 2000,
                Width = 80,
                VerticalAlignment = VerticalAlignment.Center,
                TickFrequency = 100,
                IsSnapToTickEnabled = true,
                Tag = tag
            };
            var delayLabel = new TextBlock
            {
                Text = "2.0s",
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0),
                Width = 30
            };
            delaySlider.ValueChanged += (s, args) =>
            {
                var val = (int)args.NewValue;
                delayLabel.Text = (val / 1000.0).ToString("0.0") + "s";
                var t = (string)delaySlider.Tag;
                var cmd = "SetUnloaderDelay:" + val;
                var settings = RemoteControlSettings.Instance;

                if (SyncAllCheckbox?.IsChecked == true && settings.IsCommanderMode)
                {
                    CommandDispatcher.Dispatch(cmd);
                    SendToAllRemotes(cmd);
                }
                else if (t == "LOCAL")
                {
                    CommandDispatcher.Dispatch(cmd);
                }
                else if (settings.IsCommanderMode)
                {
                    ThreadPool.QueueUserWorkItem(_ => HttpCommandSender.Send(t, cmd));
                }
            };
            delayRow.Children.Add(delaySlider);
            delayRow.Children.Add(delayLabel);
            stack.Children.Add(delayRow);

            group.Content = stack;
            BotPanelsContainer.Children.Add(group);
        }

        /// <summary>
        /// Creates a row with: [Label]  [✓] [✗]
        /// </summary>
        private StackPanel MakeToggleRow(string label, string startCmd, string stopCmd, string target)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };

            row.Children.Add(new TextBlock
            {
                Text = label,
                Width = 55,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            });

            row.Children.Add(MakeButton("✓", startCmd, target,
                Color.FromRgb(76, 175, 80), Brushes.White, 24, 13));
            row.Children.Add(MakeButton("✗", stopCmd, target,
                Color.FromRgb(244, 67, 54), Brushes.White, 24, 13));

            return row;
        }

        /// <summary>
        /// Creates a small text label for sub-toggle rows.
        /// </summary>
        private TextBlock MakeSmallLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 1, 0)
            };
        }

        /// <summary>
        /// Creates a styled command button.
        /// </summary>
        private Button MakeButton(string text, string command, string target,
            Color? bg = null, Brush fg = null, int minWidth = 0, int fontSize = 11)
        {
            var btn = new Button
            {
                Content = text,
                Tag = command + "|" + target,
                Margin = new Thickness(2, 1, 2, 1),
                Padding = new Thickness(6, 2, 6, 2),
                FontSize = fontSize,
                FontWeight = FontWeights.Bold
            };

            if (bg.HasValue)
                btn.Background = new SolidColorBrush(bg.Value);
            if (fg != null)
                btn.Foreground = fg;
            if (minWidth > 0)
                btn.MinWidth = minWidth;

            btn.Click += BotCommand_Click;
            return btn;
        }

        /// <summary>
        /// Handles a command button click. Sends to the specific bot,
        /// or to ALL bots if "Sync All" is checked.
        /// </summary>
        private void BotCommand_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var tagParts = (button.Tag as string)?.Split('|');
            if (tagParts == null || tagParts.Length < 2) return;

            var command = tagParts[0];
            var target = tagParts[1]; // "LOCAL" or "IP:Port"
            var settings = RemoteControlSettings.Instance;
            var syncAll = SyncAllCheckbox.IsChecked == true;

            // Guard: if commander mode is off, only allow local dispatch
            if (!settings.IsCommanderMode)
            {
                CommandDispatcher.Dispatch(command);
                UpdateStatus("Local: " + command);
                return;
            }

            if (syncAll)
            {
                // Send to ALL bots: local + all remotes
                CommandDispatcher.Dispatch(command);
                SendToAllRemotes(command);
                UpdateStatus("ALL: " + command);
            }
            else
            {
                // Send only to the targeted bot
                if (target == "LOCAL")
                {
                    CommandDispatcher.Dispatch(command);
                    UpdateStatus("Local: " + command);
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        HttpCommandSender.Send(target, command);
                    });
                    UpdateStatus(target + ": " + command);
                }
            }
        }

        /// <summary>
        /// Sends a command to all configured remote bots on a background thread.
        /// </summary>
        private void SendToAllRemotes(string command)
        {
            var settings = RemoteControlSettings.Instance;
            if (settings.RemoteBots == null || settings.RemoteBots.Count == 0) return;

            var addresses = new string[settings.RemoteBots.Count];
            settings.RemoteBots.CopyTo(addresses, 0);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach (var entry in addresses)
                {
                    if (string.IsNullOrWhiteSpace(entry)) continue;
                    var address = entry.Split('|')[0].Trim();
                    HttpCommandSender.Send(address, command);
                }
            });
        }

        private void AddBot_Click(object sender, RoutedEventArgs e)
        {
            var address = NewBotAddress.Text?.Trim();
            if (string.IsNullOrEmpty(address)) return;

            var label = NewBotLabel.Text?.Trim();
            var entry = string.IsNullOrEmpty(label) ? address : address + "|" + label;

            var settings = RemoteControlSettings.Instance;
            settings.RemoteBots.Add(entry);
            NewBotAddress.Text = "";
            NewBotLabel.Text = "";
            UpdateStatus("Added: " + entry);
            // Panel rebuild happens via CollectionChanged
        }

        private void RemoveBot_Click(object sender, RoutedEventArgs e)
        {
            var selected = BotListBox.SelectedItem as string;
            if (selected == null) return;

            RemoteControlSettings.Instance.RemoteBots.Remove(selected);
            UpdateStatus("Removed: " + selected);
            // Panel rebuild happens via CollectionChanged
        }

        private void SyncAllCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateStatus(SyncAllCheckbox.IsChecked == true ? "Sync mode: ALL bots" : "Sync mode: Individual");
        }

        private void UpdateStatus(string text)
        {
            if (StatusText == null) return;
            StatusText.Text = DateTime.Now.ToString("HH:mm:ss") + " — " + text;
        }
    }
}
