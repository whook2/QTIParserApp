using System.Collections.ObjectModel;

namespace QTIParserApp.Model
{
    public class FormattedQuestion
    {
        public string QuestionType { get; set; }
        public string Text { get; set; }
        // Now, FormattedText is simply the file URL to the question's HTML.
        public string FormattedText => Text;

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


