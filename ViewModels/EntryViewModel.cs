using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using yTodo.Models;
using yTodo.Services;

namespace yTodo.ViewModels
{
    public class EntryViewModel : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private string _type = "Note";
        private bool _isDone;
        private string? _color;
        private string? _displayTitle;
        private string? _url;
        private readonly UrlService _urlService;

        private string _fontFamily = "Segoe UI";
        private double _fontSize = 16;

        public EntryViewModel(UrlService urlService)
        {
            _urlService = urlService;
        }

        public EntryViewModel(TodoEntry entry, UrlService urlService) : this(urlService)
        {
            _type = entry.Type;
            _isDone = entry.IsDone;
            _color = entry.Color;

            string rawContent = entry.Content;
            if (_type == "Header" && rawContent.StartsWith("## ")) rawContent = rawContent.Substring(3);
            else if (_type == "Task" && rawContent.StartsWith("- ")) rawContent = rawContent.Substring(2);

            _content = rawContent;

            if (entry.Metadata != null)
            {
                _url = entry.Metadata.Url;
                _displayTitle = entry.Metadata.Title;
            }
            UpdateColor();
        }

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    if (value.StartsWith("## "))
                    {
                        Type = "Header";
                        value = value.Substring(3);
                    }
                    else if (value.StartsWith("- "))
                    {
                        Type = "Task";
                        value = value.Substring(2);
                    }

                    _content = value;
                    OnPropertyChanged();
                    UpdateColor();
                    CheckForUrl();
                }
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsHeader));
                    OnPropertyChanged(nameof(IsTask));
                    OnPropertyChanged(nameof(IsNote));
                    OnPropertyChanged(nameof(DisplayFontSize));
                    UpdateColor();
                }
            }
        }

        public bool IsDone
        {
            get => _isDone;
            set
            {
                if (_isDone != value)
                {
                    _isDone = value;
                    OnPropertyChanged();
                    UpdateColor();
                }
            }
        }

        public string? Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public string? DisplayTitle
        {
            get => _displayTitle;
            set { _displayTitle = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLink)); }
        }

        public string? Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLink)); }
        }

        public string FontFamily
        {
            get => _fontFamily;
            set { _fontFamily = value; OnPropertyChanged(); }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayFontSize));
            }
        }

        public double DisplayFontSize => IsHeader ? FontSize * 1.5 : FontSize;

        public bool IsHeader => Type == "Header";
        public bool IsTask => Type == "Task";
        public bool IsNote => Type == "Note";
        public bool HasLink => !string.IsNullOrEmpty(DisplayTitle) || !string.IsNullOrEmpty(Url);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void UpdateColor()
        {
            if (IsDone && IsTask)
            {
                Color = "#4CAF50"; // Green for completed tasks
                return;
            }

            if (IsHeader)
            {
                Color = "#FFFFFF"; // Pure white for headers
                return;
            }

            // White with 20% transparency (80% opacity)
            Color = "#CCFFFFFF";
        }

        private async void CheckForUrl()
        {
            var detectedUrl = _urlService.ExtractUrl(_content);
            if (!string.IsNullOrEmpty(detectedUrl) && detectedUrl != _url)
            {
                _url = detectedUrl;
                var title = await _urlService.FetchTitleAsync(detectedUrl);
                if (!string.IsNullOrEmpty(title))
                {
                    DisplayTitle = title;
                }
                else
                {
                    DisplayTitle = detectedUrl;
                }
            }
        }

        public TodoEntry ToModel()
        {
            return new TodoEntry
            {
                Type = Type,
                Content = Content,
                IsDone = IsDone,
                Color = Color,
                Metadata = HasLink ? new EntryMetadata { Url = Url ?? string.Empty, Title = DisplayTitle ?? string.Empty } : null
            };
        }
    }
}
