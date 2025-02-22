using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTIParserApp.Model
{
    /*public class Answer
    {
        public string AnswerId { get; set; }
        public string QuestionId { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
        public string MatchPair { get; set; }

        public Answer(string answerId, string questionId, string text, bool isCorrect, string matchPair = null)
        {
            AnswerId = answerId;
            QuestionId = questionId;
            Text = text;
            IsCorrect = isCorrect;
            MatchPair = matchPair;
        }
    }*/

    public class Answer
    {
        public string AnswerId { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }

        public Answer(string answerId, string text, bool isCorrect)
        {
            AnswerId = answerId;
            Text = text;
            IsCorrect = isCorrect;
        }
    }
}
