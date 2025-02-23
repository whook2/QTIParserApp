using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;

namespace QTIParserApp.Model
{
    class QTIParser
    {
        public static Quiz ParseQTI(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            // Create XML namespace manager
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("qti", "http://www.imsglobal.org/xsd/ims_qtiasiv1p2");

            // Locate the quiz information
            XmlNode quizNode = doc.SelectSingleNode("//qti:assessment", nsmgr);
            string quizId = quizNode?.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
            string quizTitle = quizNode?.Attributes["title"]?.InnerText ?? "Untitled Quiz";

            Quiz quiz = new Quiz(quizId, quizTitle, 1);
            Debug.WriteLine($"[DEBUG] Quiz Loaded: {quizTitle} (ID: {quizId})");

            // Locate all questions
            XmlNodeList questionNodes = doc.SelectNodes("//qti:item", nsmgr);
            foreach (XmlNode questionNode in questionNodes)
            {
                string questionId = questionNode.Attributes["ident"].InnerText;
                string questionType = questionNode.SelectSingleNode("./qti:itemmetadata/qti:qtimetadata/qti:qtimetadatafield[qti:fieldlabel='question_type']/qti:fieldentry", nsmgr)?.InnerText ?? "unknown";
                string questionText = questionNode.SelectSingleNode("./qti:presentation/qti:material/qti:mattext", nsmgr)?.InnerXml ?? "No question text";
                double pointsPossible = double.Parse(questionNode.SelectSingleNode("./qti:itemmetadata/qti:qtimetadata/qti:qtimetadatafield[qti:fieldlabel='points_possible']/qti:fieldentry", nsmgr)?.InnerText ?? "1");

                Question question = new Question(questionId, questionType, questionText) { PointsPossible = pointsPossible };

                Debug.WriteLine($"[DEBUG] Question: {questionType} | Points: {pointsPossible} | Text: {questionText}");

                // Locate answer choices
                XmlNodeList answerNodes = questionNode.SelectNodes(".//qti:response_label", nsmgr);
                foreach (XmlNode answerNode in answerNodes)
                {
                    string answerId = answerNode.Attributes["ident"].InnerText;
                    string answerText = answerNode.SelectSingleNode("./qti:material/qti:mattext", nsmgr)?.InnerText ?? "";
                    bool isCorrect = questionNode.SelectSingleNode($"./qti:resprocessing/qti:respcondition/qti:conditionvar/qti:varequal[text()='{answerId}']", nsmgr) != null;

                    question.Answers.Add(new Answer(answerId, answerText, isCorrect));
                    Debug.WriteLine($"  [DEBUG] Answer: {answerText} | Correct: {isCorrect}");
                }

                // Extract Attachments (Images, Files, LaTeX, Links, Tables)
                XmlNodeList attachmentNodes = questionNode.SelectNodes(".//qti:mattext", nsmgr);
                foreach (XmlNode attachmentNode in attachmentNodes)
                {
                    string content = attachmentNode.InnerXml;

                    if (content.Contains("<img"))
                    {
                        string src = ExtractBetween(content, "src=\"", "\"");
                        question.Attachments.Add(new QuestionAttachment(src, "image"));
                        Debug.WriteLine($"  [DEBUG] Image found: {src}");
                    }
                    if (content.Contains("<a "))
                    {
                        string href = ExtractBetween(content, "href=\"", "\"");
                        question.Attachments.Add(new QuestionAttachment(href, "link"));
                        Debug.WriteLine($"  [DEBUG] Link found: {href}");
                    }
                    if (content.Contains("<table"))
                    {
                        question.Attachments.Add(new QuestionAttachment("Embedded Table", "table"));
                        Debug.WriteLine("  [DEBUG] Table found.");
                    }
                    if (content.Contains("equation_images"))
                    {
                        string latexUrl = ExtractBetween(content, "src=\"", "\"");
                        question.Attachments.Add(new QuestionAttachment(latexUrl, "latex"));
                        Debug.WriteLine($"  [DEBUG] LaTeX found: {latexUrl}");
                    }
                }

                quiz.Questions.Add(question);
            }

            Debug.WriteLine($"[DEBUG] Finished parsing quiz. Total Questions: {quiz.Questions.Count}");
            return quiz;
        }

        private static string ExtractBetween(string text, string start, string end)
        {
            int startIndex = text.IndexOf(start);
            if (startIndex == -1) return "";
            startIndex += start.Length;
            int endIndex = text.IndexOf(end, startIndex);
            return endIndex == -1 ? "" : text.Substring(startIndex, endIndex - startIndex);
        }
    }
}
