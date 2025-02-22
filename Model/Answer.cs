namespace QTIParserApp.Model
{
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

