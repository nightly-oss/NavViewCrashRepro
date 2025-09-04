using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NavViewCrashRepro
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            _ = Refresh();
        }

        private async Task Refresh()
        {
            WorkspacesMenu.MenuItems.Clear();

            var root = ApplicationData.Current.LocalFolder;
            var workspaces = await root.GetFoldersAsync();

            foreach (var ws in workspaces)
            {
                var wsItem = new NavigationViewItem
                {
                    Content = ws.Name,
                    Tag = ws
                };

                await LoadWorkspaceFolders(ws, wsItem);

                WorkspacesMenu.MenuItems.Add(wsItem);
            }
        }

        private async Task LoadWorkspaceFolders(StorageFolder ws, NavigationViewItem wsItem)
        {
            var folders = await ws.GetFoldersAsync();

            foreach (var f in folders)
            {
                SolidColorBrush brush = new SolidColorBrush(Colors.Gray);

                try
                {
                    var jsonFile = await f.GetFileAsync($"{f.Name}_folder_properties.json");
                    var json = await FileIO.ReadTextAsync(jsonFile);
                    var props = JsonSerializer.Deserialize<FolderProps>(json);
                    brush = StringToBrush(props?.Color ?? "#808080");
                }
                catch { }

                var fItem = new NavigationViewItem
                {
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new FontIcon { Glyph = "", Foreground = brush, Margin = new Thickness(0,0,6,0) },
                            new TextBlock { Text = f.Name }
                        }
                    },
                    Tag = f
                };

                wsItem.MenuItems.Add(fItem);
            }
        }

        private static SolidColorBrush StringToBrush(string hex)
        {
            if (hex.StartsWith("#")) hex = hex[1..];
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }

        // --- Buttons ---

        private async void CreateWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var root = ApplicationData.Current.LocalFolder;
            await root.CreateFolderAsync($"WS_{Guid.NewGuid():N}");
            await Refresh();
        }

        private async void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            if (WorkspacesMenu.MenuItems.Count == 0) return;
            var wsItem = (NavigationViewItem)WorkspacesMenu.MenuItems[0];
            var ws = (StorageFolder)wsItem.Tag;
            await ws.CreateFolderAsync($"F_{Guid.NewGuid():N}");
            await Refresh();
        }

        private async void RecolorFolder_Click(object sender, RoutedEventArgs e)
        {
            if (WorkspacesMenu.MenuItems.Count == 0) return;
            var wsItem = (NavigationViewItem)WorkspacesMenu.MenuItems[0];
            if (wsItem.MenuItems.Count == 0) return;

            var folderItem = (NavigationViewItem)wsItem.MenuItems[0];
            var folder = (StorageFolder)folderItem.Tag;

            var props = new FolderProps { Color = "#FF0000" }; // force red
            var json = JsonSerializer.Serialize(props);
            var jsonFile = await folder.CreateFileAsync($"{folder.Name}_folder_properties.json", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(jsonFile, json);

            await Refresh();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Refresh();
        }
    }

    public class FolderProps
    {
        public string Color { get; set; }
    }
}
