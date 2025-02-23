using System.Collections.ObjectModel;

namespace QTIParserApp.Model
{
    public class FormattedQuestion
    {
        public string QuestionType { get; set; }
        public string Text { get; set; }

        // Because we've already decoded the HTML in QTIParser, we can pass it
        // directly to the WebView. No need for another decode step here:
        public string FormattedText => "data:text/html," + Text;

        public ObservableCollection<QuestionAttachment> Attachments { get; set; } = new();

        public FormattedQuestion(Question question)
        {
            QuestionType = question.QuestionType;
            Text = question.Text;
            foreach (var attachment in question.Attachments)
            {
                Attachments.Add(attachment);
            }
        }
    }
}

