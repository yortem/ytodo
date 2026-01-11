using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Win32;
using yTodo.ViewModels;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading.Tasks;
using System.Linq;

// Type Aliases to resolve ambiguities between WPF and WinForms
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using ListBox = System.Windows.Controls.ListBox;
using Point = System.Windows.Point;
using Cursor = System.Windows.Input.Cursor;
using DataObject = System.Windows.DataObject;
using DataFormats = System.Windows.DataFormats;

namespace yTodo
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private SettingsWindow? _settingsWindow;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                DataContext = ViewModel;
                SetupTrayIcon();

                this.Loaded += async (s, e) =>
                {
                    try
                    {
                        await ViewModel.LoadAsync();
                        ViewModel.PropertyChanged += (vs, ve) =>
                        {
                            if (ve.PropertyName == nameof(MainViewModel.AppTitle) && _notifyIcon != null)
                            {
                                _notifyIcon.Text = ViewModel.AppTitle;
                            }
                        };
                        
                        if (_notifyIcon != null) _notifyIcon.Text = ViewModel.AppTitle;

                        // Restore Window Position and Size
                        if (ViewModel.Settings != null)
                        {
                            if (!double.IsNaN(ViewModel.Settings.WindowTop)) this.Top = ViewModel.Settings.WindowTop;
                            if (!double.IsNaN(ViewModel.Settings.WindowLeft)) this.Left = ViewModel.Settings.WindowLeft;
                            if (ViewModel.Settings.WindowWidth > 0) this.Width = ViewModel.Settings.WindowWidth;
                            if (ViewModel.Settings.WindowHeight > 0) this.Height = ViewModel.Settings.WindowHeight;
                            if (ViewModel.Settings.WindowState == 2) this.WindowState = WindowState.Maximized;
                        }

                        if (ViewModel.Entries.Count > 0) FocusEntry(ViewModel.Entries.Last());
                    }
                    catch { }
                };
            }
            catch { }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyDarkMode();
        }

        private void ApplyDarkMode()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    int darkMode = 1;
                    DwmSetWindowAttribute(helper.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                    
                    // Set default caption color to match #1E1E1E (R=30, G=30, B=30)
                    // COLORREF is 0x00BBGGRR
                    int color = 0x001E1E1E;
                    DwmSetWindowAttribute(helper.Handle, DWMWA_CAPTION_COLOR, ref color, sizeof(int));
                }
            }
            catch { }
        }

        #region Tray Icon
        private void SetupTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            try
            {
                var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/task.ico"))?.Stream;
                if (iconStream != null)
                {
                    _notifyIcon.Icon = new Icon(iconStream);
                }
            }
            catch { }

            _notifyIcon.Text = "yTodo";
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Exit", null, (s, e) => Application.Current.Shutdown());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Focus();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
            base.OnStateChanged(e);
        }
        #endregion

        private void OnTextBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var textBox = (TextBox)sender;
            var entry = (EntryViewModel)textBox.DataContext;
            int index = ViewModel.Entries.IndexOf(entry);

            if (e.Key == Key.Enter)
            {
                // If next item is placeholder, focus it
                if (index < ViewModel.Entries.Count - 1 && ViewModel.Entries[index + 1].IsPlaceholder)
                {
                    e.Handled = true;
                    FocusEntry(ViewModel.Entries[index + 1]);
                }
                else
                {
                    string prefix = entry.IsTask ? "- " : "";
                    var newEntry = ViewModel.AddEntry(index, prefix);
                    e.Handled = true;
                    FocusEntry(newEntry);
                }
            }
            else if (e.Key == Key.Back && string.IsNullOrEmpty(textBox.Text))
            {
                if (entry.IsTask)
                {
                    entry.Type = "Note";
                    e.Handled = true;
                }
                else if (ViewModel.Entries.Count > 1 && index > 0)
                {
                    var previousEntry = ViewModel.Entries[index - 1];
                    ViewModel.RemoveEntry(entry);
                    FocusEntry(previousEntry);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up)
            {
                if (index > 0)
                {
                    FocusEntry(ViewModel.Entries[index - 1]);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down)
            {
                if (index < ViewModel.Entries.Count - 1)
                {
                    FocusEntry(ViewModel.Entries[index + 1]);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Tab)
            {
                bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                int direction = isShift ? -1 : 1;
                int newIndex = index + direction;

                if (newIndex >= 0 && newIndex < ViewModel.Entries.Count)
                {
                    FocusEntry(ViewModel.Entries[newIndex]);
                    e.Handled = true;
                }
            }
        }

        private void OnTextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!string.IsNullOrEmpty(text) && (text.Contains("\n") || text.Contains("\r")))
                {
                    var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrEmpty(l))
                                    .ToList();

                    if (lines.Count > 1)
                    {
                        var textBox = (TextBox)sender;
                        var entry = (EntryViewModel)textBox.DataContext;
                        int index = ViewModel.Entries.IndexOf(entry);

                        entry.Content = lines[0];

                        for (int i = 1; i < lines.Count; i++)
                        {
                            ViewModel.AddEntry(index + i - 1, lines[i]);
                        }

                        e.CancelCommand();
                        e.Handled = true;
                    }
                }
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 3)
            {
                if (sender is TextBox textBox)
                {
                    textBox.SelectAll();
                    e.Handled = true;
                }
            }
        }

        private void FocusEntry(EntryViewModel entry)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var container = (ListBoxItem)ListControl.ItemContainerGenerator.ContainerFromItem(entry);
                if (container != null)
                {
                    var tb = FindVisualChild<TextBox>(container);
                    if (tb != null)
                    {
                        tb.Focus();
                        tb.CaretIndex = tb.Text.Length;
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null) return null!;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t) return t;
                T childOfChild = FindVisualChild<T>(child!);
                if (childOfChild != null) return childOfChild;
            }
            return null!;
        }

        private void OnAddClicked(object sender, RoutedEventArgs e)
        {
            var newEntry = ViewModel.AddEntry(-1, "");
            FocusEntry(newEntry);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Visual visual)
            {
                var clickedItem = FindVisualParent<ListBoxItem>(visual);
                var clickedButton = FindVisualParent<System.Windows.Controls.Button>(visual);

                if (clickedItem == null && clickedButton == null)
                {
                    if (ViewModel.Entries.Count > 0) FocusEntry(ViewModel.Entries.Last());
                }
            }
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null!;
            if (parentObject is T parent) return parent;
            return FindVisualParent<T>(parentObject);
        }

        private void OnDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is EntryViewModel entry)
            {
                ViewModel.RemoveEntry(entry);
            }
        }

        private void OnToggleCheckClicked(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is EntryViewModel entry)
            {
                if (!entry.IsTask)
                {
                    entry.Type = "Task";
                    entry.IsDone = false;
                }
                else
                {
                    entry.IsDone = !entry.IsDone;
                }
            }
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow();
                _settingsWindow.Owner = this;
                _settingsWindow.DataContext = ViewModel;
                _settingsWindow.Show();
            }
            else
            {
                _settingsWindow.Activate();
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var listBoxItem = FindVisualParent<ListBoxItem>(textBox);
            if (listBoxItem != null)
            {
                listBoxItem.IsSelected = true;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var listBoxItem = FindVisualParent<ListBoxItem>(textBox);
            if (listBoxItem != null)
            {
                listBoxItem.IsSelected = false;
            }
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                if (e.Uri != null)
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = e.Uri.AbsoluteUri,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening link: {ex.Message}", "yTodo Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            // Save Window Position and Data
            if (ViewModel.Settings != null)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    ViewModel.Settings.WindowTop = this.Top;
                    ViewModel.Settings.WindowLeft = this.Left;
                    ViewModel.Settings.WindowWidth = this.Width;
                    ViewModel.Settings.WindowHeight = this.Height;
                }
                else
                {
                    ViewModel.Settings.WindowTop = this.RestoreBounds.Top;
                    ViewModel.Settings.WindowLeft = this.RestoreBounds.Left;
                    ViewModel.Settings.WindowWidth = this.RestoreBounds.Width;
                    ViewModel.Settings.WindowHeight = this.RestoreBounds.Height;
                }
                ViewModel.Settings.WindowState = (int)this.WindowState;
            }
            ViewModel.Save();

            base.OnClosed(e);
        }
    }
}
