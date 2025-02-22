using Microsoft.UI.Xaml;
using QTIParserApp.ViewModel;

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
            ViewModel.LoadQTIFile(this);  // Pass MainWindow reference for file picker
        }
    }
}
