 using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System.Linq;
using System.IO;
using System.Drawing;

[ComVisible(true)]
[COMServerAssociation(AssociationType.AllFiles)]
[COMServerAssociation(AssociationType.Directory)]
public class JessShellExtension : SharpContextMenu
{
    protected override bool CanShowMenu()
    {
        // You can add logic here to determine if the menu should be shown
        return true;
    }

    protected override ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();

        var submitItem = new ToolStripMenuItem (
            "Submit to JESS",
            ShellExtension.Properties.Resources.favicon_png
        );
        submitItem.Click += (sender, args) => HandleSubmission();

        var watchItem = new ToolStripMenuItem
        {
            Text = "Watch with JESS",
            Image = ShellExtension.Properties.Resources.favicon_png
        };
        watchItem.Click += (sender, args) => HandleWatch();

        menu.Items.Add(submitItem);
        menu.Items.Add(watchItem);

        return menu;
    }

    private void HandleSubmission()
    {
        string FileName = null, Arguments = null;
        try {
            // Get selected item paths
            var selectedPaths = SelectedItemPaths.ToList();
            // FileName = "C:\\Program Files\\jess_client\\Jess_client\\jess_client.exe",
            FileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "jess_client.exe");
            Arguments = $"CONTEXTMENU1 {string.Join(" ", selectedPaths.Select(p => $"\"{p}\""))}";

            // Launch your application with the paths
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = FileName,
                Arguments = Arguments,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error calling JESS_client to submit files: {ex.Message}. JESS_client: {FileName}; Args: {Arguments}", "JESS Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void HandleWatch()
    {
        string FileName = null, Arguments = null;
        try
        {
            var selectedPaths = SelectedItemPaths.ToList();
            // FileName = "C:\\Program Files\\jess_client\\Jess_client\\jess_client.exe",
            FileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "jess_client.exe");
            Arguments = $"CONTEXTMENU2 {string.Join(" ", selectedPaths.Select(p => $"\"{p}\""))}";

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = FileName,
                Arguments = Arguments,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error calling JESS_client to set up watch: {ex.Message}. JESS_client: {FileName}; Args: {Arguments}", "JESS Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}