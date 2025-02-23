using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace QTIParserApp.Model
{
    class QTIParser
    {
        public static Quiz ParseQTI(string quizFilePath, string manifestPath, string extractPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(quizFilePath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("qti", "http://www.imsglobal.org/xsd/ims_qtiasiv1p2");

            XmlNode quizNode = doc.SelectSingleNode("//qti:assessment", nsmgr);
            string quizId = quizNode?.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
            string quizTitle = quizNode?.Attributes["title"]?.InnerText ?? "Untitled Quiz";

            Quiz quiz = new Quiz(quizId, quizTitle, 1);
            Debug.WriteLine($"[DEBUG] Quiz Loaded: {quizTitle} (ID: {quizId})");

            XmlNodeList questionNodes = doc.SelectNodes("//qti:item", nsmgr);
            foreach (XmlNode questionNode in questionNodes)
            {
                string questionId = questionNode.Attributes["ident"].InnerText;
                string questionText = questionNode.SelectSingleNode("./qti:presentation/qti:material/qti:mattext", nsmgr)?.InnerXml ?? "No question text";
                Question question = new Question(questionId, "unknown", questionText);

                XmlNodeList attachmentNodes = questionNode.SelectNodes(".//qti:mattext", nsmgr);
                foreach (XmlNode attachmentNode in attachmentNodes)
                {
                    string content = attachmentNode.InnerXml;

                    if (content.Contains("<img"))
                    {
                        string src = ExtractBetween(content, "src=\"", "\"");
                        question.Attachments.Add(new QuestionAttachment(src, "image"));
                    }
                    if (content.Contains("<a "))
                    {
                        string href = ExtractBetween(content, "href=\"", "\"");
                        question.Attachments.Add(new QuestionAttachment(href, "document"));
                    }
                }

                quiz.Questions.Add(question);
            }

            if (manifestPath != null)
            {
                Dictionary<string, string> fileMappings = ParseManifest(manifestPath);
                AttachMediaToQuestions(quiz, fileMappings, extractPath);
            }

            return quiz;
        }

        private static Dictionary<string, string> ParseManifest(string manifestPath)
        {
            Dictionary<string, string> fileMappings = new Dictionary<string, string>();

            XmlDocument doc = new XmlDocument();
            doc.Load(manifestPath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ims", "http://www.imsglobal.org/xsd/imscp_v1p1");

            XmlNodeList resourceNodes = doc.SelectNodes("//ims:resource", nsmgr);
            foreach (XmlNode resource in resourceNodes)
            {
                string resourceId = resource.Attributes["identifier"]?.InnerText;
                XmlNode fileNode = resource.SelectSingleNode("ims:file", nsmgr);
                if (fileNode != null)
                {
                    string fileName = fileNode.Attributes["href"]?.InnerText;
                    if (!string.IsNullOrEmpty(resourceId) && !string.IsNullOrEmpty(fileName))
                    {
                        fileMappings[resourceId] = fileName;
                        Debug.WriteLine($"[DEBUG] Found Attachment Mapping: {resourceId} -> {fileName}");
                    }
                }
            }

            return fileMappings;
        }

        private static void AttachMediaToQuestions(Quiz quiz, Dictionary<string, string> fileMappings, string extractPath)
        {
            string mediaBasePath = Path.Combine(extractPath, "web_resources", "Uploaded Media");

            foreach (var question in quiz.Questions)
            {
                for (int i = 0; i < question.Attachments.Count; i++)
                {
                    string attachmentPath = question.Attachments[i].FilePath;

                    if (attachmentPath.Contains("$IMS-CC-FILEBASE$"))
                    {
                        string relativePath = attachmentPath.Replace("$IMS-CC-FILEBASE$/Uploaded%20Media/", "");
                        string fullPath = Path.Combine(mediaBasePath, relativePath);

                        if (File.Exists(fullPath))
                        {
                            question.Attachments[i] = new QuestionAttachment(fullPath, question.Attachments[i].Type);
                            Debug.WriteLine($"[DEBUG] Replaced Attachment Path: {attachmentPath} -> {fullPath}");
                        }
                        else
                        {
                            Debug.WriteLine($"[ERROR] File Not Found: {fullPath}");
                        }
                    }
                }
            }
        }

        private static string ExtractBetween(string text, string start, string end)
        {
            int startIndex = text.IndexOf(start, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1) return "";
            startIndex += start.Length;
            int endIndex = text.IndexOf(end, startIndex, StringComparison.OrdinalIgnoreCase);
            return endIndex == -1 ? "" : text.Substring(startIndex, endIndex - startIndex);
        }
    }
}
