using System;
using System.Collections.Generic;
using System.Linq;

namespace QTIParserApp.Model
{
    public class Quiz
    {
        public string QuizId { get; set; }
        public string Title { get; set; }
        public int MaxAttempts { get; set; }
        public List<Question> Questions { get; set; }

        // NEW: Calculate total points from all questions
        public double TotalPoints => Questions.Sum(q => q.PointsPossible);

        public Quiz(string quizId, string title, int maxAttempts)
        {
            QuizId = quizId;
            Title = title;
            MaxAttempts = maxAttempts;
            Questions = new List<Question>();  // Prevent null issues
        }
    }
}

