using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnippetDropperSetup
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }
    }

    internal sealed class SetupForm : Form
    {
        private const string AppName = "SnippetDropper";
        private readonly string installDirectory;
        private readonly string installedExe;
        private readonly string desktopShortcut;
        private readonly string startMenuShortcut;
        private readonly CheckBox addDesktopShortcut;
        private readonly CheckBox launchAfterInstall;
        private readonly Label status;

        internal SetupForm()
        {
            installDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName);
            installedExe = Path.Combine(installDirectory, "SnippetDropper.exe");
            desktopShortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "SnippetDropper.lnk");
            startMenuShortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "SnippetDropper.lnk");

            Text = "SnippetDropper Setup";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(470, 244);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var title = new Label
            {
                Text = "Install SnippetDropper",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(22, 18),
                AutoSize = true
            };

            var description = new Label
            {
                Text = "A small floating drag-to-paste snippet tool for terminals and editors.",
                Font = new Font("Segoe UI", 9),
                Location = new Point(25, 60),
                Size = new Size(420, 36)
            };

            addDesktopShortcut = new CheckBox
            {
                Text = "Create a Desktop shortcut",
                Checked = true,
                AutoSize = true,
                Location = new Point(28, 102)
            };

            launchAfterInstall = new CheckBox
            {
                Text = "Launch SnippetDropper after installation",
                Checked = true,
                AutoSize = true,
                Location = new Point(28, 128)
            };

            status = new Label
            {
                Text = File.Exists(installedExe) ? "SnippetDropper is already installed. Install will update it." : "",
                ForeColor = Color.DimGray,
                Location = new Point(25, 160),
                Size = new Size(420, 26)
            };

            var install = new Button
            {
                Text = File.Exists(installedExe) ? "Update" : "Install",
                Location = new Point(276, 198),
                Size = new Size(82, 30)
            };
            install.Click += delegate { Install(); };

            var uninstall = new Button
            {
                Text = "Uninstall",
                Enabled = File.Exists(installedExe),
                Location = new Point(184, 198),
                Size = new Size(82, 30)
            };
            uninstall.Click += delegate { Uninstall(); };

            var cancel = new Button
            {
                Text = "Cancel",
                Location = new Point(368, 198),
                Size = new Size(82, 30)
            };
            cancel.Click += delegate { Close(); };

            Controls.AddRange(new Control[]
            {
                title, description, addDesktopShortcut, launchAfterInstall,
                status, uninstall, install, cancel
            });
        }

        private void Install()
        {
            try
            {
                if (!CloseRunningApp())
                {
                    return;
                }

                Directory.CreateDirectory(installDirectory);
                ExtractApplication(installedExe);
                CreateShortcut(startMenuShortcut, installedExe);

                if (addDesktopShortcut.Checked)
                {
                    CreateShortcut(desktopShortcut, installedExe);
                }
                else
                {
                    DeleteIfExists(desktopShortcut);
                }

                status.Text = "Installation complete.";
                if (launchAfterInstall.Checked)
                {
                    Process.Start(installedExe);
                }

                MessageBox.Show(
                    "SnippetDropper has been installed.\n\nUse the Desktop or Start Menu shortcut to launch it.",
                    "SnippetDropper Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Installation failed:\n\n" + exception.Message,
                    "SnippetDropper Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void Uninstall()
        {
            DialogResult result = MessageBox.Show(
                "Remove SnippetDropper?\n\nYour saved snippets and transparency settings will be kept.",
                "SnippetDropper Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                if (!CloseRunningApp())
                {
                    return;
                }

                DeleteIfExists(desktopShortcut);
                DeleteIfExists(startMenuShortcut);
                DeleteIfExists(installedExe);
                if (Directory.Exists(installDirectory) && Directory.GetFileSystemEntries(installDirectory).Length == 0)
                {
                    Directory.Delete(installDirectory);
                }

                MessageBox.Show(
                    "SnippetDropper has been removed.",
                    "SnippetDropper Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Uninstall failed:\n\n" + exception.Message,
                    "SnippetDropper Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static bool CloseRunningApp()
        {
            Process[] processes = Process.GetProcessesByName("SnippetDropper");
            if (processes.Length == 0)
            {
                return true;
            }

            DialogResult result = MessageBox.Show(
                "SnippetDropper is currently running. Close it so setup can continue?",
                "SnippetDropper Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return false;
            }

            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(2000);
                }
                catch
                {
                    // Continue; writing the file will report an actionable error if it remains locked.
                }
            }

            return true;
        }

        private static void ExtractApplication(string destination)
        {
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SnippetDropper.Payload.exe"))
            {
                if (resource == null)
                {
                    throw new InvalidOperationException("The embedded SnippetDropper application is missing.");
                }

                using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write))
                {
                    resource.CopyTo(output);
                }
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("Windows shortcut support is unavailable.");
            }

            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                new object[] { shortcutPath });

            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember(
                "TargetPath",
                BindingFlags.SetProperty,
                null,
                shortcut,
                new object[] { targetPath });
            shortcutType.InvokeMember(
                "WorkingDirectory",
                BindingFlags.SetProperty,
                null,
                shortcut,
                new object[] { Path.GetDirectoryName(targetPath) });
            shortcutType.InvokeMember(
                "Description",
                BindingFlags.SetProperty,
                null,
                shortcut,
                new object[] { "Launch SnippetDropper" });
            shortcutType.InvokeMember(
                "Save",
                BindingFlags.InvokeMethod,
                null,
                shortcut,
                null);

            Marshal.FinalReleaseComObject(shortcut);
            Marshal.FinalReleaseComObject(shell);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
