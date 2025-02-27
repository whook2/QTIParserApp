using System;
using System.Collections.Generic;

namespace QTIParserApp.Model
{
    public class Question
    {
        public string QuestionId { get; set; }
        public string QuestionType { get; set; }
        public string Text { get; set; }
        public double PointsPossible { get; set; }  // NEW: Store how much the question is worth
        public List<Answer> Answers { get; set; }
        public List<QuestionAttachment> Attachments { get; set; }  // Stores images, files, LaTeX, etc.

        public Question(string questionId, string questionType, string text)
        {
            QuestionId = questionId;
            QuestionType = questionType;
            Text = text;
            PointsPossible = 1.0; // Default to 1 point if not specified
            Answers = new List<Answer>();
            Attachments = new List<QuestionAttachment>();  // Prevent null issues
        }
    }
}


