using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTIParserApp.Model
{
    /*public class QuestionAttachment
    {
        public int AttachmentId { get; set; }
        public string QuestionId { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }

        public QuestionAttachment(string questionId, string filePath, string fileType)
        {
            QuestionId = questionId;
            FilePath = filePath;
            FileType = fileType;
        }
    }*/

    public class QuestionAttachment
    {
        public string FilePath { get; set; }
        public string FileType { get; set; }  // "image", "document", "latex", "table", "link"

        public QuestionAttachment(string filePath, string fileType)
        {
            FilePath = filePath;
            FileType = fileType;
        }
    }
}
