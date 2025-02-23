using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using QTIParserApp.ViewModel;
using System;
using Windows.System;

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
