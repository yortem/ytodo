using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace yTodo.Models
{
    public class AppData
    {
        public List<TodoEntry> Entries { get; set; } = new List<TodoEntry>();
        public AppSettings Settings { get; set; } = new AppSettings();
    }

    public class AppSettings
    {
        public bool IsRtl { get; set; } = false;
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 16;
        public double LineSpacing { get; set; } = 12;
        public string BackgroundColor { get; set; } = "#1E1E1E"; // New property
        public string AppTitle { get; set; } = "yTodo";

        // Window Placement
        public double WindowTop { get; set; } = 100;
        public double WindowLeft { get; set; } = 100;
        public double WindowWidth { get; set; } = 450;
        public double WindowHeight { get; set; } = 800;
        public int WindowState { get; set; } = 0; // 0: Normal, 1: Minimized, 2: Maximized
    }

    public class TodoEntry
    {
        public string Type { get; set; } = "Note"; // Header, Task, Note
        public string Content { get; set; } = string.Empty;
        public bool IsDone { get; set; } = false;
        public string? Color { get; set; }
        public EntryMetadata? Metadata { get; set; }
    }

    public class EntryMetadata
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
