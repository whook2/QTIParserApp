using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTIParserApp.Model
{
    /*public class Question
    {
        public string QuestionId { get; set; }
        public string QuizId { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string OriginalContent { get; set; }
        public string CleanedContent { get; set; }
        public double Points { get; set; }
        public bool RequiresFileUpload { get; set; }
        public List<Answer> Answers { get; set; } = new List<Answer>();
        public List<QuestionAttachment> Attachments { get; set; } = new List<QuestionAttachment>();

        public Question(string questionId, string quizId, string title, string type, string originalContent, string cleanedContent, double points)
        {
            QuestionId = questionId;
            QuizId = quizId;
            Title = title;
            Type = type;
            OriginalContent = originalContent;
            CleanedContent = cleanedContent;
            Points = points;
        }
    }*/


    public class Question
    {
        public string QuestionId { get; set; }
        public string QuestionType { get; set; }
        public string Text { get; set; }  // Stores formatted question text
        public List<Answer> Answers { get; set; } = new();
        public List<QuestionAttachment> Attachments { get; set; } = new();

        public Question(string questionId, string questionType, string text)
        {
            QuestionId = questionId;
            QuestionType = questionType;
            Text = text;
        }
    }
}
