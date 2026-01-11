using System;
using System.Windows;
using yTodo.ViewModels;
using Microsoft.Win32;

namespace yTodo
{
    public partial class SettingsWindow : Window
    {
        public MainViewModel ViewModel => (MainViewModel)DataContext;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void OnColorSwatchClicked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Background is System.Windows.Media.SolidColorBrush brush)
            {
                var c = brush.Color;
                string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                ViewModel.SelectedBackgroundColor = hex;
            }
        }

        private void OnPickColorClicked(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            
            // Try to set initial color from current selection
            try
            {
                var currentColor = System.Drawing.ColorTranslator.FromHtml(ViewModel.SelectedBackgroundColor);
                colorDialog.Color = currentColor;
            }
            catch { }

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = colorDialog.Color;
                string hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                ViewModel.SelectedBackgroundColor = hex;
            }
        }

        private async void OnExportClicked(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|JSON Files (*.json)|*.json",
                FileName = "yTodo_Export"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                if (saveFileDialog.FileName.EndsWith(".json"))
                {
                    await ViewModel.ExportToJsonAsync(saveFileDialog.FileName);
                }
                else
                {
                    await ViewModel.ExportToTxtAsync(saveFileDialog.FileName);
                }
                System.Windows.MessageBox.Show("Export Successful!");
            }
        }
    }
}
