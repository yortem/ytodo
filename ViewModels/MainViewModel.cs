using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using yTodo.Models;
using yTodo.Services;

// Type Aliases to resolve ambiguities between WPF and WinForms
using Application = System.Windows.Application;

namespace yTodo.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly StorageService _storageService = new StorageService();
        private readonly UrlService _urlService = new UrlService();
        private DispatcherTimer? _saveTimer;
        private AppData? _appData;

        public ObservableCollection<EntryViewModel> Entries { get; } = new ObservableCollection<EntryViewModel>();

        public ObservableCollection<string> AvailableFonts { get; } = new ObservableCollection<string>
        {
            "Segoe UI", "Assistant", "Heebo", "Rubik", "David Libre", "Frank Ruhl Libre", "Tahoma", "Arial", "Consolas"
        };

        public MainViewModel()
        {
            Entries.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
                {
                    foreach (EntryViewModel item in e.NewItems)
                    {
                        item.PropertyChanged += OnItemPropertyChanged;
                        item.FontFamily = SelectedFont;
                        item.FontSize = SelectedFontSize;
                    }
                }
                TriggerSave();
            };
        }

        private void InitializeTimer()
        {
            if (_saveTimer != null) return;
            _saveTimer = new DispatcherTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(2);
            _saveTimer.Tick += async (s, e) => { _saveTimer.Stop(); await SaveAsync(); };
        }

        public AppSettings? Settings => _appData?.Settings;

        public bool IsRtl
        {
            get => Settings?.IsRtl ?? false;
            set
            {
                if (Settings != null && Settings.IsRtl != value)
                {
                    Settings.IsRtl = value;
                    OnPropertyChanged(nameof(IsRtl));
                    OnPropertyChanged(nameof(FlowDirection));
                    TriggerSave();
                }
            }
        }

        public string SelectedFont
        {
            get => Settings?.FontFamily ?? "Segoe UI";
            set
            {
                if (Settings != null && Settings.FontFamily != value)
                {
                    Settings.FontFamily = value;
                    foreach (var entry in Entries) entry.FontFamily = value;
                    OnPropertyChanged(nameof(SelectedFont));
                    TriggerSave();
                }
            }
        }

        public double SelectedFontSize
        {
            get => Settings?.FontSize ?? 16;
            set
            {
                if (Settings != null && Math.Abs(Settings.FontSize - value) > 0.1)
                {
                    Settings.FontSize = value;
                    foreach (var entry in Entries) entry.FontSize = value;
                    OnPropertyChanged(nameof(SelectedFontSize));
                    TriggerSave();
                }
            }
        }

        public double SelectedLineSpacing
        {
            get => Settings?.LineSpacing ?? 12;
            set
            {
                if (Settings != null && Math.Abs(Settings.LineSpacing - value) > 0.1)
                {
                    Settings.LineSpacing = value;
                    OnPropertyChanged(nameof(SelectedLineSpacing));
                    TriggerSave();
                }
            }
        }

        public string SelectedBackgroundColor
        {
            get => Settings?.BackgroundColor ?? "#1E1E1E";
            set
            {
                if (Settings != null && Settings.BackgroundColor != value)
                {
                    // Basic validation to ensure it's a hex color
                    if (value.StartsWith("#") && (value.Length == 7 || value.Length == 9))
                    {
                        Settings.BackgroundColor = value;
                        OnPropertyChanged(nameof(SelectedBackgroundColor));
                        TriggerSave();
                    }
                }
            }
        }

        public string AppTitle
        {
            get => Settings?.AppTitle ?? "yTodo";
            set
            {
                if (Settings != null && Settings.AppTitle != value)
                {
                    Settings.AppTitle = value;
                    OnPropertyChanged(nameof(AppTitle));
                    TriggerSave();
                }
            }
        }

        public System.Windows.FlowDirection FlowDirection => IsRtl ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        private int _saveCounter = 0;

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is EntryViewModel entry)
            {
                if (e.PropertyName == nameof(EntryViewModel.IsPlaceholder))
                {
                    if (!entry.IsPlaceholder) EnsurePlaceholders();
                }

                if (e.PropertyName == nameof(EntryViewModel.Content) ||
                    e.PropertyName == nameof(EntryViewModel.IsDone) ||
                    e.PropertyName == nameof(EntryViewModel.Type) || 
                    e.PropertyName == nameof(EntryViewModel.IsPlaceholder)) // trigger save on placeholder change (removal)
                {
                    TriggerSave();
                }
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                _appData = await _storageService.LoadAsync();
                Entries.Clear(); // Ensure clear before add
                foreach (var entry in _appData.Entries)
                {
                    var vm = new EntryViewModel(entry, _urlService)
                    {
                        FontFamily = SelectedFont,
                        FontSize = SelectedFontSize
                    };
                    Entries.Add(vm);
                }

                EnsurePlaceholders();

                if (Entries.Count == 0) // Should be handled by EnsurePlaceholders, but safe keep
                {
                   // AddEntry(-1, ""); 
                }

                OnPropertyChanged(nameof(Settings));
                // ... rest of properties
                OnPropertyChanged(nameof(IsRtl));
                OnPropertyChanged(nameof(FlowDirection));
                OnPropertyChanged(nameof(SelectedFont));
                OnPropertyChanged(nameof(SelectedFontSize));
                OnPropertyChanged(nameof(SelectedLineSpacing));
                OnPropertyChanged(nameof(SelectedBackgroundColor));
            }
            catch { }
        }

        private void EnsurePlaceholders()
        {
            // 1. Check end of list
            if (Entries.Count == 0 || !Entries.Last().IsPlaceholder)
            {
                Entries.Add(CreatePlaceholder());
            }

            // 2. Check before each header
            for (int i = 1; i < Entries.Count; i++)
            {
                if (Entries[i].IsHeader && !Entries[i-1].IsPlaceholder)
                {
                    Entries.Insert(i, CreatePlaceholder());
                    i++; // Skip the header we just pushed
                }
            }
        }

        private EntryViewModel CreatePlaceholder()
        {
            return new EntryViewModel(_urlService)
            {
                IsPlaceholder = true,
                Content = "",
                Type = "Task", // Start as task so it has bullet? Or Note? User said "Add new task".
                FontFamily = SelectedFont,
                FontSize = SelectedFontSize
            };
        }

        private void TriggerSave()
        {
            InitializeTimer();
            if (_saveTimer != null) { _saveTimer.Stop(); _saveTimer.Start(); }
        }

        public async Task SaveAsync()
        {
            if (_appData == null) return;
            try
            {
                _saveCounter++;
                int currentSave = _saveCounter;
                StatusMessage = "Saving...";

                _appData.Entries = Entries.Where(e => !e.IsPlaceholder).Select(e => e.ToModel()).ToList();
                await _storageService.SaveAsync(_appData);

                StatusMessage = "Saved";
                
                // Clear the message after 3 seconds, but only if a newer save hasn't started
                _ = Task.Run(async () => {
                    await Task.Delay(3000);
                    if (_saveCounter == currentSave)
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            if (_saveCounter == currentSave) StatusMessage = "";
                        });
                    }
                });
            }
            catch 
            {
                StatusMessage = "Error saving";
            }
        }

        public void Save()
        {
            if (_appData == null) return;
            try
            {
                _appData.Entries = Entries.Where(e => !e.IsPlaceholder).Select(e => e.ToModel()).ToList();
                _storageService.Save(_appData);
            }
            catch { }
        }

        public EntryViewModel AddEntry(int index = -1, string content = "")
        {
            var newEntry = new EntryViewModel(_urlService)
            {
                Content = content,
                FontFamily = SelectedFont,
                FontSize = SelectedFontSize
            };
            if (index == -1 || index >= Entries.Count) Entries.Add(newEntry);
            else Entries.Insert(index + 1, newEntry);
            
            EnsurePlaceholders(); // Check if we broke stricture
            return newEntry;
        }

        public void RemoveEntry(EntryViewModel entry) 
        {
            Entries.Remove(entry);
            EnsurePlaceholders(); // Restore placeholder if last item removed
        }

        public async Task ExportToTxtAsync(string filePath)
        {
            try
            {
                var content = string.Join(Environment.NewLine, Entries.Where(e => !e.IsPlaceholder).Select(e =>
                {
                    if (e.IsHeader) return $"## {e.Content}";
                    if (e.IsTask) return $"- {(e.IsDone ? "[x]" : "[ ]")} {e.Content}";
                    return e.Content;
                }));
                await System.IO.File.WriteAllTextAsync(filePath, content);
            }
            catch { }
        }

        public async Task ExportToJsonAsync(string filePath)
        {
            if (_appData == null) return;
            try
            {
                // Create a temporary AppData object to avoid modifying the original during export
                var exportData = new AppData 
                { 
                    Entries = _appData.Entries.Where(e => true).ToList(), // Filtered below 
                    Settings = _appData.Settings 
                };
                exportData.Entries = Entries.Where(e => !e.IsPlaceholder).Select(e => e.ToModel()).ToList();
                
                var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, json);
            }
            catch { }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
