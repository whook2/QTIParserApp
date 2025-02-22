namespace QTIParserApp.Model
{
    public class QuestionAttachment
    {
        public string FilePath { get; set; }  // Stores the actual file path or URL
        public string AttachmentType { get; set; }  // Example: "image", "link", "table", "latex"

        public QuestionAttachment(string filePath, string attachmentType)
        {
            FilePath = filePath;
            AttachmentType = attachmentType;
        }
    }
}

