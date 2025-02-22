using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Xml;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using QTIParserApp.Model;
using System.Diagnostics;

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
                Debug.WriteLine($"[DEBUG] Extracted QTI ZIP to: {extractPath}");

                string manifestPath = Directory.GetFiles(extractPath, "imsmanifest.xml", SearchOption.AllDirectories).FirstOrDefault();
                string quizFilePath = Directory.GetFiles(extractPath, "*.xml", SearchOption.AllDirectories)
                                .FirstOrDefault(f => !f.Contains("imsmanifest.xml") && !f.Contains("assessment_meta.xml"));

                Debug.WriteLine($"[DEBUG] Found imsmanifest.xml at: {manifestPath}");
                Debug.WriteLine($"[DEBUG] Found quiz file at: {quizFilePath}");

                if (quizFilePath != null)
                {
                    Quiz parsedQuiz = QTIParser.ParseQTI(quizFilePath, manifestPath, extractPath);
                    Debug.WriteLine($"[DEBUG] Parsed Quiz Title: {parsedQuiz.Title}");
                    Debug.WriteLine($"[DEBUG] Number of Questions: {parsedQuiz.Questions.Count}");

                    if (parsedQuiz != null && parsedQuiz.Questions.Count > 0)
                    {
                        CurrentQuiz = parsedQuiz;
                        OnPropertyChanged(nameof(CurrentQuiz));

                        FormattedQuestions.Clear();
                        foreach (var question in parsedQuiz.Questions)
                        {
                            Debug.WriteLine($"[DEBUG] Question: {question.Text} - Type: {question.QuestionType}");
                            FormattedQuestions.Add(new FormattedQuestion(question));
                        }
                        OnPropertyChanged(nameof(FormattedQuestions));
                    }
                    else
                    {
                        Debug.WriteLine("[ERROR] Quiz was parsed but contains no questions.");
                    }
                }
                else
                {
                    Debug.WriteLine("[ERROR] No valid QTI XML file found.");
                }
            }
        }
    }

    public class FormattedQuestion
    {
        public string QuestionType { get; set; }
        public string Text { get; set; }
        public string FormattedText => "data:text/html," + WebUtility.HtmlDecode(Text);
        public List<QuestionAttachment> Attachments { get; set; } = new List<QuestionAttachment>();

        public FormattedQuestion(Question question)
        {
            QuestionType = question.QuestionType;
            Text = question.Text;
            Attachments = question.Attachments;
        }
    }
}
