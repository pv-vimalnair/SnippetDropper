using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SnippetDropper
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool ownsMutex;
            using (var mutex = new Mutex(true, "SnippetDropper.SingleInstance", out ownsMutex))
            {
                if (!ownsMutex)
                {
                    MessageBox.Show("SnippetDropper is already running.", "SnippetDropper",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new SnippetWidget());
            }
        }
    }

    internal sealed class SnippetRecord
    {
        public string text { get; set; }
        public string createdAt { get; set; }
    }

    internal sealed class AppSettings
    {
        public double collapsedOpacity { get; set; }
        public int x { get; set; }
        public int y { get; set; }
    }

    internal sealed class Storage
    {
        private readonly string directory;
        private readonly string snippetsPath;
        private readonly string settingsPath;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public Storage()
        {
            directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SnippetDropper");
            snippetsPath = Path.Combine(directory, "snippets.json");
            settingsPath = Path.Combine(directory, "settings.json");
        }

        public List<SnippetRecord> LoadSnippets()
        {
            try
            {
                if (File.Exists(snippetsPath))
                {
                    var snippets = serializer.Deserialize<List<SnippetRecord>>(File.ReadAllText(snippetsPath));
                    if (snippets != null)
                    {
                        return snippets;
                    }
                }
            }
            catch
            {
                // Use defaults if a user-edited file is invalid.
            }

            return new List<SnippetRecord>
            {
                NewSnippet("flutter run"),
                NewSnippet("flutter pub get"),
                NewSnippet("flutter clean"),
                NewSnippet("dart fix --apply")
            };
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    var settings = serializer.Deserialize<AppSettings>(File.ReadAllText(settingsPath));
                    if (settings != null)
                    {
                        settings.collapsedOpacity = Clamp(settings.collapsedOpacity, 0.10, 0.90);
                        return settings;
                    }
                }
            }
            catch
            {
                // Use defaults if settings cannot be read.
            }

            return new AppSettings { collapsedOpacity = 0.28, x = 40, y = 80 };
        }

        public void SaveSnippets(List<SnippetRecord> snippets)
        {
            EnsureDirectory();
            File.WriteAllText(snippetsPath, serializer.Serialize(snippets));
        }

        public void SaveSettings(AppSettings settings)
        {
            EnsureDirectory();
            File.WriteAllText(settingsPath, serializer.Serialize(settings));
        }

        public static SnippetRecord NewSnippet(string text)
        {
            return new SnippetRecord { text = text, createdAt = DateTime.UtcNow.ToString("o") };
        }

        private void EnsureDirectory()
        {
            Directory.CreateDirectory(directory);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }

    internal class NoActivateForm : Form
    {
        private const int WsExNoActivate = 0x08000000;
        private const int WmMouseActivate = 0x0021;
        private const int MaNoActivate = 0x0003;

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WsExNoActivate;
                return parameters;
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmMouseActivate)
            {
                message.Result = (IntPtr)MaNoActivate;
                return;
            }

            base.WndProc(ref message);
        }
    }

    internal static class Win32
    {
        private const int VkLeftButton = 0x01;
        private const ushort VkControl = 0x11;
        private const ushort VkShift = 0x10;
        private const ushort VkInsert = 0x2D;
        private const ushort VkV = 0x56;
        private const uint KeyEventKeyUp = 0x0002;
        private const int InputKeyboard = 1;

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr window, StringBuilder className, int maxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, Input[] inputs, int size);

        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr window, int message, int wParam, int lParam);

        internal static bool IsLeftButtonDown()
        {
            return (GetAsyncKeyState(VkLeftButton) & 0x8000) != 0;
        }

        internal static void Paste(IntPtr target)
        {
            if (target != IntPtr.Zero && GetForegroundWindow() != target)
            {
                SetForegroundWindow(target);
                Thread.Sleep(35);
            }

            var className = new StringBuilder(256);
            GetClassName(target, className, className.Capacity);
            if (className.ToString() == "ConsoleWindowClass")
            {
                SendChord(VkShift, VkInsert);
            }
            else
            {
                SendChord(VkControl, VkV);
            }
        }

        private static void SendChord(ushort modifier, ushort key)
        {
            var inputs = new[]
            {
                KeyInput(modifier, false),
                KeyInput(key, false),
                KeyInput(key, true),
                KeyInput(modifier, true)
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
        }

        private static Input KeyInput(ushort key, bool keyUp)
        {
            var input = new Input();
            input.type = InputKeyboard;
            input.value.keyboard.virtualKey = key;
            input.value.keyboard.flags = keyUp ? KeyEventKeyUp : 0;
            return input;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            internal int type;
            internal InputUnion value;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            internal MouseInput mouse;

            [FieldOffset(0)]
            internal KeyboardInput keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            internal int x;
            internal int y;
            internal uint mouseData;
            internal uint flags;
            internal uint time;
            internal IntPtr extraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            internal ushort virtualKey;
            internal ushort scanCode;
            internal uint flags;
            internal uint time;
            internal IntPtr extraInfo;
        }
    }

    internal sealed class SnippetWidget : NoActivateForm
    {
        private const int CollapsedHeight = 26;
        private const int RowHeight = 26;
        private const int MaxVisible = 4;
        private const int WidgetWidth = 240;
        private const int WmNonClientLeftButtonDown = 0xA1;
        private const int HitTestCaption = 0x2;

        private readonly Storage storage = new Storage();
        private readonly List<SnippetRecord> snippets;
        private readonly AppSettings settings;
        private readonly Label header;
        private readonly Label hint;
        private readonly FlowLayoutPanel panel;
        private readonly NoActivateForm ghost;
        private readonly Label ghostText;
        private readonly System.Windows.Forms.Timer hoverTimer;
        private readonly System.Windows.Forms.Timer dragTimer;
        private readonly NotifyIcon tray;
        private readonly List<ToolStripMenuItem> transparencyItems = new List<ToolStripMenuItem>();

        private bool expanded;
        private bool dragArmed;
        private bool dragging;
        private string dragText = "";
        private Point dragStart;
        private IntPtr pasteTarget;

        internal SnippetWidget()
        {
            snippets = storage.LoadSnippets();
            settings = storage.LoadSettings();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Location = KeepOnScreen(new Point(settings.x, settings.y));
            Size = new Size(WidgetWidth, CollapsedHeight);
            BackColor = Color.FromArgb(24, 24, 24);
            Opacity = settings.collapsedOpacity;

            var font = new Font("Segoe UI", 9);
            header = NewLabel("SNIPS", font, new Point(8, 0), new Size(174, CollapsedHeight));
            hint = NewLabel("...", font, new Point(WidgetWidth - 36, 0), new Size(28, CollapsedHeight));
            hint.TextAlign = ContentAlignment.MiddleRight;

            panel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Location = new Point(0, CollapsedHeight),
                Size = new Size(WidgetWidth, 1),
                BackColor = Color.FromArgb(24, 24, 24),
                Visible = false
            };

            ghost = new NoActivateForm
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                TopMost = true,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(32, 32, 32),
                Opacity = 0.92,
                Size = new Size(220, 24)
            };
            ghostText = NewLabel("", font, Point.Empty, ghost.ClientSize);
            ghostText.Dock = DockStyle.Fill;
            ghostText.Padding = new Padding(8, 0, 8, 0);
            ghost.Controls.Add(ghostText);

            ContextMenuStrip menu = BuildMenu();
            ContextMenuStrip = menu;
            header.ContextMenuStrip = menu;
            hint.ContextMenuStrip = menu;
            panel.ContextMenuStrip = menu;

            Controls.Add(header);
            Controls.Add(hint);
            Controls.Add(panel);
            UpdateVisibleSnippets();

            MouseEventHandler moveWidget = delegate(object sender, MouseEventArgs args)
            {
                if (args.Button != MouseButtons.Left)
                {
                    return;
                }

                Win32.ReleaseCapture();
                Win32.SendMessage(Handle, WmNonClientLeftButtonDown, HitTestCaption, 0);
            };
            header.MouseDown += moveWidget;
            hint.MouseDown += moveWidget;

            tray = new NotifyIcon
            {
                Text = "SnippetDropper",
                Icon = SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = menu
            };
            tray.DoubleClick += delegate { OpenManager(); };

            hoverTimer = new System.Windows.Forms.Timer { Interval = 80 };
            hoverTimer.Tick += delegate { CheckHover(); };
            hoverTimer.Start();

            dragTimer = new System.Windows.Forms.Timer { Interval = 16 };
            dragTimer.Tick += delegate { CheckDrag(); };
            dragTimer.Start();

            FormClosed += delegate { ShutDown(); };
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            ToolStripItem manage = menu.Items.Add("Manage snippets...");
            manage.Click += delegate { OpenManager(); };

            var transparencyMenu = new ToolStripMenuItem("Transparency");
            menu.Items.Add(transparencyMenu);
            AddTransparencyOption(transparencyMenu, "10% transparent (nearly solid)", 0.90);
            AddTransparencyOption(transparencyMenu, "30% transparent", 0.70);
            AddTransparencyOption(transparencyMenu, "50% transparent", 0.50);
            AddTransparencyOption(transparencyMenu, "70% transparent", 0.30);
            AddTransparencyOption(transparencyMenu, "85% transparent (very faint)", 0.15);
            UpdateTransparencyChecks();

            menu.Items.Add("-");
            ToolStripItem exit = menu.Items.Add("Exit");
            exit.Click += delegate { Close(); };
            return menu;
        }

        private void AddTransparencyOption(ToolStripMenuItem parent, string text, double opacity)
        {
            var item = new ToolStripMenuItem(text) { Tag = opacity };
            item.Click += delegate
            {
                settings.collapsedOpacity = (double)item.Tag;
                if (!expanded)
                {
                    Opacity = settings.collapsedOpacity;
                }

                UpdateTransparencyChecks();
                SaveState();
            };
            parent.DropDownItems.Add(item);
            transparencyItems.Add(item);
        }

        private void UpdateTransparencyChecks()
        {
            foreach (ToolStripMenuItem item in transparencyItems)
            {
                item.Checked = Math.Abs((double)item.Tag - settings.collapsedOpacity) < 0.001;
            }
        }

        private void UpdateVisibleSnippets()
        {
            panel.Controls.Clear();
            int visibleCount = Math.Min(MaxVisible, snippets.Count);

            if (visibleCount == 0)
            {
                Label empty = NewSnippetLabel("(right-click to add snippets)");
                empty.ForeColor = Color.Gainsboro;
                panel.Controls.Add(empty);
                return;
            }

            for (int index = snippets.Count - 1; index >= 0 && panel.Controls.Count < visibleCount; index--)
            {
                Label label = NewSnippetLabel(snippets[index].text);
                label.Cursor = Cursors.Hand;
                label.MouseDown += delegate(object sender, MouseEventArgs args)
                {
                    if (args.Button != MouseButtons.Left)
                    {
                        return;
                    }

                    dragArmed = true;
                    dragging = false;
                    dragText = ((Label)sender).Text;
                    dragStart = Cursor.Position;
                    pasteTarget = Win32.GetForegroundWindow();
                };
                label.ContextMenuStrip = ContextMenuStrip;
                panel.Controls.Add(label);
            }
        }

        private Label NewSnippetLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(36, 36, 36),
                AutoSize = false,
                Size = new Size(WidgetWidth - 12, RowHeight),
                Margin = new Padding(6, 2, 6, 2),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };
        }

        private static Label NewLabel(string text, Font font, Point location, Size size)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(24, 24, 24),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = location,
                Size = size,
                Font = font
            };
        }

        private void CheckHover()
        {
            if (dragArmed)
            {
                return;
            }

            if (Bounds.Contains(Cursor.Position))
            {
                ExpandWidget();
            }
            else
            {
                CollapseWidget();
            }
        }

        private void CheckDrag()
        {
            if (!dragArmed)
            {
                return;
            }

            if (Win32.IsLeftButtonDown())
            {
                Point cursor = Cursor.Position;
                int distance = Math.Abs(cursor.X - dragStart.X) + Math.Abs(cursor.Y - dragStart.Y);
                if (!dragging && distance >= 6)
                {
                    dragging = true;
                    ghostText.Text = dragText;
                    ghost.Show();
                }

                if (dragging)
                {
                    ghost.Location = new Point(cursor.X + 14, cursor.Y + 14);
                }

                return;
            }

            bool shouldPaste = dragging;
            string text = dragText;
            IntPtr target = pasteTarget;
            ResetDrag();
            if (shouldPaste)
            {
                PasteSnippet(text, target);
            }
        }

        private static void PasteSnippet(string text, IntPtr target)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return;
            }

            for (int attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    Clipboard.SetText(text);
                    Thread.Sleep(35);
                    Win32.Paste(target);
                    return;
                }
                catch
                {
                    Thread.Sleep(35);
                }
            }
        }

        private void ResetDrag()
        {
            ghost.Hide();
            dragArmed = false;
            dragging = false;
            dragText = "";
            dragStart = Point.Empty;
            pasteTarget = IntPtr.Zero;
        }

        private void ExpandWidget()
        {
            if (expanded)
            {
                return;
            }

            expanded = true;
            UpdateVisibleSnippets();
            panel.Height = panel.Controls.Count * (RowHeight + 4) + 6;
            panel.Visible = true;
            Height = CollapsedHeight + panel.Height;
            Opacity = 0.88;
        }

        private void CollapseWidget()
        {
            if (!expanded)
            {
                return;
            }

            expanded = false;
            panel.Visible = false;
            Height = CollapsedHeight;
            Opacity = settings.collapsedOpacity;
        }

        private void OpenManager()
        {
            using (var dialog = new ManageSnippetsForm(snippets))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    snippets.Clear();
                    snippets.AddRange(dialog.Snippets);
                    storage.SaveSnippets(snippets);
                    UpdateVisibleSnippets();
                }
            }
        }

        private void SaveState()
        {
            settings.x = Left;
            settings.y = Top;
            storage.SaveSettings(settings);
        }

        private void ShutDown()
        {
            settings.x = Left;
            settings.y = Top;
            try { storage.SaveSnippets(snippets); } catch { }
            try { storage.SaveSettings(settings); } catch { }
            hoverTimer.Stop();
            dragTimer.Stop();
            tray.Visible = false;
            tray.Dispose();
            ghost.Close();
            ghost.Dispose();
        }

        private static Point KeepOnScreen(Point requested)
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int x = Math.Max(workingArea.Left, Math.Min(requested.X, workingArea.Right - WidgetWidth));
            int y = Math.Max(workingArea.Top, Math.Min(requested.Y, workingArea.Bottom - CollapsedHeight));
            return new Point(x, y);
        }
    }

    internal sealed class ManageSnippetsForm : Form
    {
        private readonly ListBox list = new ListBox();
        private readonly TextBox text = new TextBox();
        private readonly List<SnippetRecord> snippets = new List<SnippetRecord>();

        internal ManageSnippetsForm(List<SnippetRecord> source)
        {
            foreach (SnippetRecord snippet in source)
            {
                snippets.Add(new SnippetRecord { text = snippet.text, createdAt = snippet.createdAt });
            }

            Text = "Manage Snippets";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(540, 380);
            MinimumSize = new Size(540, 380);
            TopMost = true;

            list.Location = new Point(12, 12);
            list.Size = new Size(330, 320);
            list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            list.SelectedIndexChanged += delegate
            {
                if (list.SelectedIndex >= 0 && list.SelectedIndex < snippets.Count)
                {
                    text.Text = snippets[list.SelectedIndex].text;
                }
            };

            text.Location = new Point(358, 12);
            text.Size = new Size(154, 22);
            text.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            text.KeyDown += delegate(object sender, KeyEventArgs args)
            {
                if (args.KeyCode == Keys.Enter)
                {
                    AddSnippet();
                    args.SuppressKeyPress = true;
                }
            };

            Button add = NewButton("Add", 358, 44, 72);
            add.Click += delegate { AddSnippet(); };

            Button update = NewButton("Update", 440, 44, 72);
            update.Click += delegate
            {
                int index = list.SelectedIndex;
                if (index < 0 || String.IsNullOrWhiteSpace(text.Text))
                {
                    return;
                }

                snippets[index].text = text.Text;
                RefreshList(index);
            };

            Button remove = NewButton("Remove", 358, 82, 154);
            remove.Click += delegate
            {
                int index = list.SelectedIndex;
                if (index < 0)
                {
                    return;
                }

                snippets.RemoveAt(index);
                RefreshList(snippets.Count == 0 ? -1 : Math.Min(index, snippets.Count - 1));
            };

            Button moveUp = NewButton("Move up", 358, 120, 72);
            moveUp.Click += delegate
            {
                int index = list.SelectedIndex;
                if (index <= 0)
                {
                    return;
                }

                SnippetRecord previous = snippets[index - 1];
                snippets[index - 1] = snippets[index];
                snippets[index] = previous;
                RefreshList(index - 1);
            };

            Button moveDown = NewButton("Move down", 440, 120, 72);
            moveDown.Click += delegate
            {
                int index = list.SelectedIndex;
                if (index < 0 || index >= snippets.Count - 1)
                {
                    return;
                }

                SnippetRecord next = snippets[index + 1];
                snippets[index + 1] = snippets[index];
                snippets[index] = next;
                RefreshList(index + 1);
            };

            Button save = NewButton("Save and close", 358, 304, 154);
            save.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            save.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            Button cancel = NewButton("Cancel", 358, 266, 154);
            cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancel.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[] { list, text, add, update, remove, moveUp, moveDown, cancel, save });
            RefreshList(snippets.Count == 0 ? -1 : snippets.Count - 1);
        }

        internal List<SnippetRecord> Snippets
        {
            get { return snippets; }
        }

        private Button NewButton(string label, int x, int y, int width)
        {
            return new Button
            {
                Text = label,
                Location = new Point(x, y),
                Size = new Size(width, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
        }

        private void AddSnippet()
        {
            if (String.IsNullOrWhiteSpace(text.Text))
            {
                return;
            }

            snippets.Add(Storage.NewSnippet(text.Text));
            RefreshList(snippets.Count - 1);
            text.SelectAll();
        }

        private void RefreshList(int selectedIndex)
        {
            list.Items.Clear();
            foreach (SnippetRecord snippet in snippets)
            {
                list.Items.Add(snippet.text);
            }

            list.SelectedIndex = selectedIndex;
        }
    }
}
