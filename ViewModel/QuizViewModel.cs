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
                // Create temporary extraction folder.
                string extractRoot = Path.Combine(Path.GetTempPath(), "ExtractedQTI");
                Directory.CreateDirectory(extractRoot);
                string zipFolderName = Path.GetFileNameWithoutExtension(file.Name);
                string extractPath = Path.Combine(extractRoot, zipFolderName);
                Directory.CreateDirectory(extractPath);

                ZipFile.ExtractToDirectory(file.Path, extractPath, true);
                Debug.WriteLine($"[DEBUG] Extracted ZIP to: {extractPath}");

                string quizFilePath = Directory.GetFiles(extractPath, "*.xml", SearchOption.AllDirectories)
                                              .FirstOrDefault(f => !f.Contains("imsmanifest.xml", StringComparison.OrdinalIgnoreCase)
                                                                && !f.Contains("assessment_meta.xml", StringComparison.OrdinalIgnoreCase));
                string manifestPath = Directory.GetFiles(extractPath, "imsmanifest.xml", SearchOption.AllDirectories)
                                               .FirstOrDefault();

                if (quizFilePath != null)
                {
                    Quiz parsedQuiz = QTIParser.ParseQTI(quizFilePath, manifestPath, extractPath);

                    if (parsedQuiz != null)
                    {
                        // Persist the quiz attachments and questions to permanent storage.
                        PersistQuiz(parsedQuiz, extractPath);

                        CurrentQuiz = parsedQuiz;
                        //OnPropertyChanged(nameof(CurrentQuiz));

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

        // May be able to delete/change once we get the database working
        private void PersistQuiz(Quiz quiz, string extractPath)
        {
            string permanentRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QTIParserApp", "SavedQuizzes");
            Directory.CreateDirectory(permanentRoot);

            string quizPermanentFolder = Path.Combine(permanentRoot, quiz.QuizId);
            Directory.CreateDirectory(quizPermanentFolder);

            // For each attachment with a file:// URL, copy it to the permanent folder.
            foreach (var question in quiz.Questions)
            {
                foreach (var attachment in question.Attachments)
                {
                    if (attachment.FilePath.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                    {
                        string oldUrl = attachment.FilePath;
                        // Remove the "file:///" prefix and replace forward slashes.
                        string oldLocalPath = oldUrl.Replace("file:///", "").Replace('/', Path.DirectorySeparatorChar);
                        if (!File.Exists(oldLocalPath))
                        {
                            // If the file does not exist at the expected location, try recursive search.
                            string fileName = Path.GetFileName(oldLocalPath);
                            string[] foundFiles = Directory.GetFiles(extractPath, fileName, SearchOption.AllDirectories);
                            if (foundFiles.Length > 0)
                            {
                                oldLocalPath = foundFiles[0];
                                Debug.WriteLine($"[DEBUG] PersistQuiz recursive search found: {oldLocalPath}");
                            }
                        }
                        string fileNameOnly = Path.GetFileName(oldLocalPath);
                        string newLocalPath = Path.Combine(quizPermanentFolder, fileNameOnly);
                        try
                        {
                            File.Copy(oldLocalPath, newLocalPath, true);
                            string newUrl = "file:///" + newLocalPath.Replace('\\', '/');
                            // Update attachment and question HTML.
                            attachment.FilePath = newUrl;
                            question.Text = question.Text.Replace(oldUrl, newUrl);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ERROR] Persisting attachment failed: {ex.Message}");
                        }
                    }
                }
            }

            // Write each question's HTML content to a permanent .html file and update question.Text.
            foreach (var question in quiz.Questions)
            {
                string questionHtmlFile = Path.Combine(quizPermanentFolder, $"question_{question.QuestionId}.html");
                try
                {
                    File.WriteAllText(questionHtmlFile, question.Text);
                    string fileUrl = "file:///" + questionHtmlFile.Replace('\\', '/');
                    question.Text = fileUrl;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] Writing question HTML failed: {ex.Message}");
                }
            }
        }
    }
}


