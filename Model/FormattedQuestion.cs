using System.Collections.ObjectModel;
using System.Net;

namespace QTIParserApp.Model
{
    public class FormattedQuestion
    {
        public string QuestionType { get; set; }
        public string Text { get; set; }
        public string FormattedText => "data:text/html," + WebUtility.HtmlDecode(Text);
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

