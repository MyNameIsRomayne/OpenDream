using OpenDreamShared.Network.Messages;
using OpenDreamClient.Input;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamClient.Interface.Html;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace OpenDreamClient.Interface.Controls {
    [Virtual]
    public class InfoPanel : Control {
        public string PanelName { get; }

        protected InfoPanel(string name) {
            PanelName = name;
            TabContainer.SetTabTitle(this, name);
        }
    }

    public sealed class StatPanel : InfoPanel {
        private sealed class StatEntry {
            public readonly RichTextLabel NameLabel = new();
            public readonly RichTextLabel ValueLabel = new();

            private readonly ControlInfo _owner;
            private readonly FormattedMessage _nameText = new();
            private readonly FormattedMessage _valueText = new();

            public StatEntry(ControlInfo owner) {
                _owner = owner;
            }

            public void Clear() {
                _nameText.Clear();
                _valueText.Clear();

                NameLabel.SetMessage(_nameText);
                ValueLabel.SetMessage(_valueText);
            }

            public void UpdateLabels(string name, string value) {
                // TODO: Tabs should align with each other.
                //       Probably should be done by RT, but it just ignores them currently.
                name = name.Replace("\t", "    ");
                value = value.Replace("\t", "    ");

                _nameText.Clear();
                _valueText.Clear();

                // Use the default color and font
                _nameText.PushColor(Color.Black);
                _valueText.PushColor(Color.Black);
                _nameText.PushTag(new MarkupNode("font", null, null));
                _valueText.PushTag(new MarkupNode("font", null, null));

                if (_owner.InfoDescriptor.AllowHtml) {
                    // TODO: Look into using RobustToolbox's markup parser once it's customizable enough
                    HtmlParser.Parse(name, _nameText);
                    HtmlParser.Parse(value, _valueText);
                } else {
                    _nameText.AddText(name);
                    _valueText.AddText(value);
                }

                NameLabel.SetMessage(_nameText);
                ValueLabel.SetMessage(_valueText);
            }
        }

        private readonly ControlInfo _owner;
        private readonly GridContainer _grid;
        private readonly List<StatEntry> _entries = new();

        public StatPanel(ControlInfo owner, string name) : base(name) {
            _owner = owner;
            _grid = new() {
                Columns = 2
            };

            var scrollViewer = new ScrollContainer() {
                HScrollEnabled = false,
                Children = { _grid }
            };
            AddChild(scrollViewer);
        }

        public void UpdateLines(List<(string Name, string Value)> lines) {
            for (int i = 0; i < Math.Max(_entries.Count, lines.Count); i++) {
                var entry = GetEntry(i);

                if (i < lines.Count) {
                    var line = lines[i];

                    entry.UpdateLabels(line.Name, line.Value);
                } else {
                    entry.Clear();
                }
            }
        }

        private StatEntry GetEntry(int index) {
            // Expand the entries if there aren't enough
            if (_entries.Count <= index) {
                for (int i = _entries.Count; i <= index; i++) {
                    var entry = new StatEntry(_owner);

                    _grid.AddChild(entry.NameLabel);
                    _grid.AddChild(entry.ValueLabel);
                    _entries.Add(entry);
                }
            }

            return _entries[index];
        }
    }

    public sealed class VerbPanel : InfoPanel {
        [Dependency] private readonly IDreamInterfaceManager _dreamInterface = default!;
        private readonly GridContainer _grid;

        public VerbPanel(string name) : base(name) {
            _grid = new GridContainer { Columns = 4 };
            IoCManager.InjectDependencies(this);
            AddChild(_grid);
        }

        public void RefreshVerbs() {
            _grid.Children.Clear();

            foreach ((string verbName, string verbId, string verbCategory) in _dreamInterface.AvailableVerbs) {
                if (verbCategory != PanelName)
                    continue;

                Button verbButton = new Button() {
                    Margin = new Thickness(2),
                    MinWidth = 100,
                    Text = verbName
                };

                verbButton.Label.Margin = new Thickness(6, 0, 6, 2);
                verbButton.OnPressed += _ => {
                    EntitySystem.Get<DreamCommandSystem>().RunCommand(verbId);
                };

                _grid.Children.Add(verbButton);
            }
        }
    }

    public sealed class ControlInfo : InterfaceControl {
        public ControlDescriptorInfo InfoDescriptor => (ControlDescriptorInfo)ControlDescriptor;

        [Dependency] private readonly IClientNetManager _netManager = default!;

        private TabContainer _tabControl;
        private readonly Dictionary<string, StatPanel> _statPanels = new();
        private readonly SortedDictionary<string, VerbPanel> _verbPanels = new();

        private bool _defaultPanelSent = false;

        public ControlInfo(ControlDescriptor controlDescriptor, ControlWindow window) : base(controlDescriptor, window) {
            IoCManager.InjectDependencies(this);
        }

        protected override Control CreateUIElement() {
            _tabControl = new TabContainer();
            _tabControl.OnTabChanged += OnSelectionChanged;

            RefreshVerbs();

            return _tabControl;
        }

        public void RefreshVerbs() {
            foreach (var panel in _verbPanels) {
                _verbPanels[panel.Key].RefreshVerbs();
            }
        }

        public void SelectStatPanel(string statPanelName) {
            if (_statPanels.TryGetValue(statPanelName, out var panel))
                _tabControl.CurrentTab = panel.GetPositionInParent();
        }

        public void UpdateStatPanels(MsgUpdateStatPanels pUpdateStatPanels) {
            //Remove any panels the packet doesn't contain
            foreach (KeyValuePair<string, StatPanel> existingPanel in _statPanels) {
                if (!pUpdateStatPanels.StatPanels.ContainsKey(existingPanel.Key)) {
                    _tabControl.RemoveChild(existingPanel.Value);
                    _statPanels.Remove(existingPanel.Key);
                }
            }

            foreach (var updatingPanel in pUpdateStatPanels.StatPanels) {
                if (!_statPanels.TryGetValue(updatingPanel.Key, out var panel)) {
                    panel = CreateStatPanel(updatingPanel.Key);
                }

                panel.UpdateLines(updatingPanel.Value);
            }

            // Tell the server we're ready to receive data
            if (!_defaultPanelSent && _tabControl.ChildCount > 0) {
                var msg = new MsgSelectStatPanel() {
                    StatPanel = _tabControl.GetActualTabTitle(0)
                };

                _netManager.ClientSendMessage(msg);
                _defaultPanelSent = true;
            }
        }

        public bool HasVerbPanel(string name) {
            return _verbPanels.ContainsKey(name);
        }

        public VerbPanel CreateVerbPanel(string name) {
            var panel = new VerbPanel(name);
            _verbPanels.Add(name, panel);
            SortPanels();

            return panel;
        }

        public StatPanel CreateStatPanel(string name) {
            var panel = new StatPanel(this, name);
            panel.Margin = new Thickness(20, 2);
            _statPanels.Add(name, panel);
            SortPanels();
            return panel;
        }

        private void SortPanels() {
            _tabControl.Children.Clear();
            foreach(var (_, statPanel) in _statPanels) {
                _tabControl.AddChild(statPanel);
            }

            foreach(var (_, verbPanel) in _verbPanels) {
                _tabControl.AddChild(verbPanel);
            }
        }

        private void OnSelectionChanged(int tabIndex) {
            InfoPanel panel = (InfoPanel)_tabControl.GetChild(tabIndex);
            var msg = new MsgSelectStatPanel() {
                StatPanel = panel.PanelName
            };

            _netManager.ClientSendMessage(msg);
        }
    }
}
