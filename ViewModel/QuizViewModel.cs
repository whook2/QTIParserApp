using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using QTIParserApp.Model;

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

        public ObservableCollection<FormattedQuestion> FormattedQuestions { get; private set; } = new();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                string extractPath = Path.Combine(Path.GetTempPath(), "ExtractedQTI");
                Directory.CreateDirectory(extractPath);

                ZipFile.ExtractToDirectory(file.Path, extractPath, true);
                Debug.WriteLine($"[DEBUG] Extracted ZIP to: {extractPath}");

                string quizFilePath = Directory.GetFiles(extractPath, "*.xml", SearchOption.AllDirectories)
                                              .FirstOrDefault(f => !f.Contains("imsmanifest.xml") && !f.Contains("assessment_meta.xml"));

                string manifestPath = Directory.GetFiles(extractPath, "imsmanifest.xml", SearchOption.AllDirectories).FirstOrDefault();

                if (quizFilePath != null)
                {
                    Quiz parsedQuiz = QTIParser.ParseQTI(quizFilePath, manifestPath, extractPath);

                    if (parsedQuiz != null)
                    {
                        CurrentQuiz = parsedQuiz;
                        OnPropertyChanged(nameof(CurrentQuiz));

                        FormattedQuestions.Clear();
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
                    Debug.WriteLine("[ERROR] No valid QTI XML file found.");
                }
            }
        }
    }
}
