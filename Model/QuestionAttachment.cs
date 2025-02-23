namespace QTIParserApp.Model
{
    public class QuestionAttachment
    {
        public string FilePath { get; set; }  // Stores the actual file path or URL
        public string Type { get; set; }  // Example: "image", "link", "table", "latex"

        public QuestionAttachment(string filePath, string type)
        {
            FilePath = filePath;
            Type = type;
        }
    }
}

