using System.Collections.ObjectModel;

namespace QTIParserApp.Model
{
    public class FormattedQuestion
    {
        public string QuestionType { get; set; }
        public string Text { get; set; }
        public ObservableCollection<QuestionAttachment> Attachments { get; set; } = new();
        public ObservableCollection<Answer> Answers { get; set; } = new();

        public FormattedQuestion(Question question)
        {
            QuestionType = question.QuestionType;
            Text = question.Text;
            foreach (var attachment in question.Attachments)
            {
                Attachments.Add(attachment);
            }
            foreach (var answer in question.Answers)
            {
                Answers.Add(answer);
            }
        }
    }

}

