using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace QTIParserApp.Model
{
    class QTIParser
    {
        public static Quiz ParseQTI(string filePath)
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

                XmlNodeList answerNodes = questionNode.SelectNodes(".//qti:response_label", nsmgr);
                foreach (XmlNode answerNode in answerNodes)
                {
                    string answerId = answerNode.Attributes["ident"].InnerText;
                    string answerText = answerNode.SelectSingleNode("./qti:material/qti:mattext", nsmgr)?.InnerText ?? "";
                    bool isCorrect = questionNode.SelectSingleNode($"./qti:resprocessing/qti:respcondition/qti:conditionvar/qti:varequal[text()='{answerId}']", nsmgr) != null;

                    question.Answers.Add(new Answer(answerId, answerText, isCorrect));
                }

                string manifestPath = Path.Combine(Path.GetDirectoryName(filePath), "imsmanifest.xml");
                ParseManifestAttachments(manifestPath, question);

                quiz.Questions.Add(question);
            }

            return quiz;
        }

        private static void ParseManifestAttachments(string manifestPath, Question question)
        {
            if (!File.Exists(manifestPath))
                return;

            XmlDocument manifestDoc = new XmlDocument();
            manifestDoc.Load(manifestPath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(manifestDoc.NameTable);
            nsmgr.AddNamespace("ims", "http://www.imsglobal.org/xsd/imscp_v1p1");

            XmlNodeList resources = manifestDoc.SelectNodes("//ims:resource", nsmgr);
            foreach (XmlNode resource in resources)
            {
                XmlNode fileNode = resource.SelectSingleNode("ims:file", nsmgr);
                if (fileNode != null)
                {
                    string href = fileNode.Attributes["href"]?.InnerText;
                    if (!string.IsNullOrEmpty(href))
                    {
                        question.Attachments.Add(new QuestionAttachment(href, "file"));
                    }
                }
            }
        }
    }
}

