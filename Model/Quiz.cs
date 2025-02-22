using System;
using System.Collections.Generic;


namespace QTIParserApp.Model
{
    public class Quiz
    {
        public string QuizId { get; set; }
        public string Title { get; set; }
        public int MaxAttempts { get; set; }
        public List<Question> Questions { get; set; } = new List<Question>();

        //public Quiz() { }  // Default constructor for the database

        public Quiz(string quizId, string title, int maxAttempts)
        {
            QuizId = quizId;
            Title = title;
            MaxAttempts = maxAttempts;
        }
    }
}
