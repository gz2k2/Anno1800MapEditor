using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Providers;

namespace AnnoMapEditor
{
    public partial class App : Application
    {
        public readonly static string TitleShort = "Community Map Editor";
        public readonly static string Title = $"{TitleShort} for Anno 1800";

        public static bool ConfirmClose()
        {
            return MessageBox.Show(
                "Are you sure you want to close the application?\n\nAny unsaved changes will be lost.",
                TitleShort,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        public App() {
            ResxLocalizationProvider provider = ResxLocalizationProvider.Instance;

            provider.SearchCultures = new List<System.Globalization.CultureInfo>()
            {
                System.Globalization.CultureInfo.GetCultureInfo("en"),
                System.Globalization.CultureInfo.GetCultureInfo("de"),
            };
            LocalizeDictionary.Instance.Culture = CultureInfo.GetCultureInfo("en");

            int i = 0;
        }
    }
}
