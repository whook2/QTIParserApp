/*using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using QTIParserApp.Model;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace QTIParserApp.ViewModel
{
    public class QuizViewModel : INotifyPropertyChanged
    {
        private Quiz _currentQuiz;
        public Quiz CurrentQuiz
        {
            get => _currentQuiz;
            set
            {
                _currentQuiz = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FormattedQuestion> FormattedQuestions { get; private set; } = new ObservableCollection<FormattedQuestion>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Constructor to initialize default values
        public QuizViewModel()
        {
            // Initialize an empty quiz to prevent null reference issues in XAML bindings
            CurrentQuiz = new Quiz("default_id", "No Quiz Loaded", 1);
        }

        public async void LoadQTIFile(Window window)
        {
            var picker = new FileOpenPicker();
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".xml");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string filePath = file.Path;
                Quiz parsedQuiz = QTIParser.ParseQTI(filePath);

                if (parsedQuiz != null)
                {
                    CurrentQuiz = parsedQuiz;
                    OnPropertyChanged(nameof(CurrentQuiz));  // Notify UI

                    // Convert each Question to a FormattedQuestion for WebView2
                    FormattedQuestions = new ObservableCollection<FormattedQuestion>();
                    foreach (var question in parsedQuiz.Questions)
                    {
                        FormattedQuestions.Add(new FormattedQuestion(question));
                    }

                    OnPropertyChanged(nameof(FormattedQuestions));  // Notify UI
                }
            }
        }
    }

    // New helper class for WebView2 support
    public class FormattedQuestion
    {
        public string QuestionType { get; set; }
        public string Text { get; set; }

        public string FormattedText => "data:text/html," + WebUtility.HtmlDecode(Text);

        public FormattedQuestion(Question question)
        {
            QuestionType = question.QuestionType;
            Text = question.Text;
        }
    }
}*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using QTIParserApp.Model;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace QTIParserApp.ViewModel
{
    public class QuizViewModel : INotifyPropertyChanged
    {
        private Quiz _currentQuiz;
        public Quiz CurrentQuiz
        {
            get => _currentQuiz;
            set
            {
                _currentQuiz = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FormattedQuestion> FormattedQuestions { get; private set; } = new ObservableCollection<FormattedQuestion>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Constructor to initialize default values
        public QuizViewModel()
        {
            // Initialize an empty quiz to prevent null reference issues in XAML bindings
            CurrentQuiz = new Quiz("default_id", "No Quiz Loaded", 1);
        }

        public async void LoadQTIFile(Window window)
        {
            var picker = new FileOpenPicker();
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".xml");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string filePath = file.Path;
                Quiz parsedQuiz = QTIParser.ParseQTI(filePath);

                if (parsedQuiz != null)
                {
                    CurrentQuiz = parsedQuiz;
                    OnPropertyChanged(nameof(CurrentQuiz));  // Notify UI

                    // Convert each Question to a FormattedQuestion for WebView2
                    FormattedQuestions = new ObservableCollection<FormattedQuestion>();
                    foreach (var question in parsedQuiz.Questions)
                    {
                        FormattedQuestions.Add(new FormattedQuestion(question));
                    }

                    OnPropertyChanged(nameof(FormattedQuestions));  // Notify UI
                }
            }
        }
    }

    // New helper class for WebView2 support
    public class FormattedQuestion
    {
        public string QuestionType { get; set; }
        public string Text { get; set; }

        public string FormattedText => "data:text/html," + WebUtility.HtmlDecode(Text);

        public FormattedQuestion(Question question)
        {
            QuestionType = question.QuestionType;
            Text = question.Text;
        }
    }
}