using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        public QuizViewModel()
        {
            CurrentQuiz = new Quiz("default_id", "No Quiz Loaded", 1);
        }

        public async void LoadQTIFile(Window window)
        {
            var picker = new FileOpenPicker();
            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".zip");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "ExtractedQTI");
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(file.Path, tempPath);
                Debug.WriteLine($"[DEBUG] Extracted ZIP to: {tempPath}");

                string extractPath = Directory.GetDirectories(tempPath).FirstOrDefault();
                if (extractPath == null)
                {
                    Debug.WriteLine("[ERROR] No extracted folder found.");
                    return;
                }

                Debug.WriteLine($"[DEBUG] Extracted folder found: {extractPath}");

                string quizFilePath = Directory.GetFiles(extractPath, "*.xml", SearchOption.AllDirectories)
                                              .FirstOrDefault(f => !f.Contains("imsmanifest.xml") && !f.Contains("assessment_meta.xml"));

                if (quizFilePath == null)
                {
                    Debug.WriteLine("[ERROR] No valid quiz XML file found.");
                    return;
                }

                Debug.WriteLine($"[DEBUG] Found quiz file at: {quizFilePath}");

                Quiz parsedQuiz = QTIParser.ParseQTI(quizFilePath);
                if (parsedQuiz != null)
                {
                    CurrentQuiz = parsedQuiz;
                    OnPropertyChanged(nameof(CurrentQuiz));

                    FormattedQuestions = new ObservableCollection<FormattedQuestion>();
                    foreach (var question in parsedQuiz.Questions)
                    {
                        FormattedQuestions.Add(new FormattedQuestion(question));
                    }
                    OnPropertyChanged(nameof(FormattedQuestions));

                    Debug.WriteLine("[DEBUG] Quiz loaded successfully.");
                }
                else
                {
                    Debug.WriteLine("[ERROR] Failed to parse QTI file.");
                }
            }
            else
            {
                Debug.WriteLine("[INFO] No file was selected.");
            }
        }
    }

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
