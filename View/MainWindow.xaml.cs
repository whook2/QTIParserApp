using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using QTIParserApp.Model;
using QTIParserApp.ViewModel;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace QTIParserApp.View
{
    public sealed partial class MainWindow : Window
    {
        public QuizViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            ViewModel = new QuizViewModel();
            this.RootGrid.DataContext = ViewModel;
        }

        private void LoadQTIFile_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadQTIFile(this);
        }

        private void QuestionWebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            string url = e.Uri;
            if (url.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                _ = Launcher.LaunchUriAsync(new Uri(url));
            }
        }
    }
}
