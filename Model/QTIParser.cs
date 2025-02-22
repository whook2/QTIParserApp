using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;

namespace QTIParserApp.Model
{
    class QTIParser
    {
        public static Quiz ParseQTI(string filePath, string manifestPath, string extractPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("qti", "http://www.imsglobal.org/xsd/ims_qtiasiv1p2");

            XmlNode quizNode = doc.SelectSingleNode("//qti:assessment", nsmgr);
            string quizId = quizNode?.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
            string quizTitle = quizNode?.Attributes["title"]?.InnerText ?? "Untitled Quiz";

            Quiz quiz = new Quiz(quizId, quizTitle, 1);

            XmlNodeList questionNodes = doc.SelectNodes("//qti:item", nsmgr);
            foreach (XmlNode questionNode in questionNodes)
            {
                string questionId = questionNode.Attributes["ident"].InnerText;
                string questionType = questionNode.SelectSingleNode("./qti:itemmetadata/qti:qtimetadata/qti:qtimetadatafield[qti:fieldlabel='question_type']/qti:fieldentry", nsmgr)?.InnerText ?? "unknown";
                string questionText = questionNode.SelectSingleNode("./qti:presentation/qti:material/qti:mattext", nsmgr)?.InnerXml ?? "No question text";

                Question question = new Question(questionId, questionType, questionText);
                quiz.Questions.Add(question);
            }

            if (!string.IsNullOrEmpty(manifestPath))
            {
                ManifestParser.AttachMediaToQuestions(quiz, manifestPath, extractPath);
            }

            return quiz;
        }
    }
}
