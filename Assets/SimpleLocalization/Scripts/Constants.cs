using System.Collections.Generic;

namespace Assets.SimpleLocalization.Scripts
{
    public static class Constants
    {
        public const string LocalizationEditorUrl = "";
        public const string SheetResolverUrl = "https://script.google.com/macros/s/AKfycbycW2dsGZhc2xJh2Fs8yu9KUEqdM-ssOiK1AlES3crLqQa1lkDrI4mZgP7sJhmFlGAD/exec";
        public const string AssetUrlFree = "http://u3d.as/1cWr";
        public const string AssetUrlPro = "http://u3d.as/34Zv";
        public const string TableUrlPattern = "https://docs.google.com/spreadsheets/d/{0}";
        public const string ExampleTableId = "";
        public static readonly Dictionary<string, int> ExampleSheets = new() { { "UI", 0 } };
    }
}