using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;

namespace QTIParserApp.Model
{
    public class QTIParser
    {
        public static Quiz ParseQTI(string quizFilePath, string manifestPath, string extractPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(quizFilePath);

            // Set up XML namespace manager.
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("qti", "http://www.imsglobal.org/xsd/ims_qtiasiv1p2");

            // Identify quiz ID & Title.
            XmlNode quizNode = doc.SelectSingleNode("//qti:assessment", nsmgr);
            string quizId = quizNode?.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
            string quizTitle = quizNode?.Attributes["title"]?.InnerText ?? "Untitled Quiz";

            Quiz quiz = new Quiz(quizId, quizTitle, 1);
            Debug.WriteLine($"[DEBUG] Quiz Loaded: {quizTitle} (ID: {quizId})");

            // Get all <item> nodes for questions.
            XmlNodeList questionNodes = doc.SelectNodes("//qti:item", nsmgr);
            foreach (XmlNode questionNode in questionNodes)
            {
                // Get question ID.
                string questionId = questionNode.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();

                // Parse question_type and points_possible.
                XmlNode questionTypeNode = questionNode.SelectSingleNode(
                    "./qti:itemmetadata/qti:qtimetadata/qti:qtimetadatafield[qti:fieldlabel='question_type']/qti:fieldentry",
                    nsmgr);
                string questionType = questionTypeNode?.InnerText ?? "unknown";

                double questionPoints = 1.0;
                XmlNode pointsNode = questionNode.SelectSingleNode(
                    "./qti:itemmetadata/qti:qtimetadata/qti:qtimetadatafield[qti:fieldlabel='points_possible']/qti:fieldentry",
                    nsmgr);
                if (pointsNode != null && double.TryParse(pointsNode.InnerText, out double parsedPoints))
                {
                    questionPoints = parsedPoints;
                }

                // Grab the main question text (HTML)
                XmlNode textNode = questionNode.SelectSingleNode("./qti:presentation/qti:material/qti:mattext", nsmgr);
                string encodedText = textNode?.InnerXml ?? "No question text";
                string questionText = WebUtility.HtmlDecode(encodedText);
                // Rewrite any "$IMS-CC-FILEBASE$" references to local file URLs.
                questionText = FixupLocalReferencesInHtml(questionText, extractPath);

                // Create the Question object.
                Question question = new Question(questionId, questionType, questionText)
                {
                    PointsPossible = questionPoints
                };

                // Parse attachments (for images, links, etc.)
                XmlNodeList mattextNodes = questionNode.SelectNodes(".//qti:mattext", nsmgr);
                foreach (XmlNode matNode in mattextNodes)
                {
                    string matEncoded = matNode.InnerXml;
                    string content = WebUtility.HtmlDecode(matEncoded);

                    if (content.IndexOf("<img", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foreach (string src in ExtractHtmlAttribute(content, "img", "src"))
                        {
                            question.Attachments.Add(new QuestionAttachment(src, "image"));
                            Debug.WriteLine($"[DEBUG] Found <img> src => {src}");
                        }
                    }
                    if (content.IndexOf("<a ", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foreach (string href in ExtractHtmlAttribute(content, "a", "href"))
                        {
                            question.Attachments.Add(new QuestionAttachment(href, "document"));
                            Debug.WriteLine($"[DEBUG] Found <a> href => {href}");
                        }
                    }
                }

                // Use a switch to call specialized answer parsing.
                switch (questionType)
                {
                    case "multiple_choice_question":
                    case "true_false_question":
                        ParseMultipleChoiceOrTF(questionNode, question, nsmgr);
                        break;
                    case "multiple_answers_question":
                        ParseMultipleAnswers(questionNode, question, nsmgr);
                        break;
                    case "short_answer_question":
                        ParseShortAnswer(questionNode, question, nsmgr);
                        break;
                    case "fill_in_multiple_blanks_question":
                        ParseFillInBlanks(questionNode, question, nsmgr);
                        break;
                    case "multiple_dropdowns_question":
                        ParseMultipleDropdowns(questionNode, question, nsmgr);
                        break;
                    case "matching_question":
                        ParseMatching(questionNode, question, nsmgr);
                        break;
                    case "calculated_question":
                        ParseCalculated(questionNode, question, nsmgr);
                        break;
                    case "numerical_question":
                        ParseNumerical(questionNode, question, nsmgr);
                        break;
                    case "essay_question":
                        // Typically no answers to parse.
                        break;
                    case "file_upload_question":
                        // Typically no answer choices.
                        break;
                    case "text_only_question":
                        // No answers.
                        break;
                    default:
                        Debug.WriteLine($"[DEBUG] No specialized parsing for question type: {questionType}");
                        break;
                }

                quiz.Questions.Add(question);
            }

            // Parse manifest and fix attachment paths using file mappings.
            if (!string.IsNullOrEmpty(manifestPath))
            {
                Dictionary<string, string> fileMappings = ParseManifest(manifestPath);
                AttachMediaToQuestions(quiz, fileMappings, extractPath);
            }

            return quiz;
        }

        #region Specialized Parsing Methods

        private static void ParseMultipleChoiceOrTF(XmlNode questionNode, Question question, XmlNamespaceManager nsmgr)
        {
            XmlNodeList labelNodes = questionNode.SelectNodes(".//qti:response_label", nsmgr);
            foreach (XmlNode labelNode in labelNodes)
            {
                string answerId = labelNode.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
                XmlNode answerTextNode = labelNode.SelectSingleNode(".//qti:material/qti:mattext", nsmgr);
                string answerText = answerTextNode?.InnerText ?? "No text";
                question.Answers.Add(new Answer(answerId, answerText, isCorrect: false));
            }

            XmlNodeList respConditions = questionNode.SelectNodes(".//qti:respcondition", nsmgr);
            foreach (XmlNode condition in respConditions)
            {
                XmlNode varEqualNode = condition.SelectSingleNode(".//qti:varequal", nsmgr);
                if (varEqualNode != null)
                {
                    string correctId = varEqualNode.InnerText.Trim();
                    var matching = question.Answers.FirstOrDefault(a => a.AnswerId == correctId);
                    if (matching != null)
                    {
                        matching.IsCorrect = true;
                    }
                }
            }
        }

        private static void ParseMultipleAnswers(XmlNode questionNode, Question question, XmlNamespaceManager nsmgr)
        {
            XmlNodeList labelNodes = questionNode.SelectNodes(".//qti:response_label", nsmgr);
            foreach (XmlNode labelNode in labelNodes)
            {
                string answerId = labelNode.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
                XmlNode answerTextNode = labelNode.SelectSingleNode(".//qti:material/qti:mattext", nsmgr);
                string answerText = answerTextNode?.InnerText ?? "No text";
                question.Answers.Add(new Answer(answerId, answerText, isCorrect: false));
            }

            XmlNodeList respConditions = questionNode.SelectNodes(".//qti:respcondition", nsmgr);
            foreach (XmlNode condition in respConditions)
            {
                XmlNodeList varEqualNodes = condition.SelectNodes(".//qti:varequal", nsmgr);
                foreach (XmlNode ve in varEqualNodes)
                {
                    string correctId = ve.InnerText.Trim();
                    var matching = question.Answers.FirstOrDefault(a => a.AnswerId == correctId);
                    if (matching != null)
                    {
                        matching.IsCorrect = true;
                    }
                }
            }
        }

        private static void ParseShortAnswer(XmlNode questionNode, Question question, XmlNamespaceManager nsmgr)
        {
            XmlNodeList varEqualNodes = questionNode.SelectNodes(".//qti:respcondition//qti:varequal", nsmgr);
            foreach (XmlNode ve in varEqualNodes)
            {
                string acceptableAnswer = ve.InnerText.Trim();
                question.Answers.Add(new Answer(Guid.NewGuid().ToString(), acceptableAnswer, isCorrect: true));
            }
        }

        private static void ParseFillInBlanks(XmlNode questionNode, Question question, XmlNamespaceManager nsmgr)
        {
            XmlNodeList responseNodes = questionNode.SelectNodes(".//qti:response_lid", nsmgr);
            foreach (XmlNode response in responseNodes)
            {
                XmlNode answerTextNode = response.SelectSingleNode(".//qti:material/qti:mattext", nsmgr);
                string answerText = answerTextNode?.InnerText ?? "No text";
                XmlNodeList labelNodes = response.SelectNodes(".//qti:response_label", nsmgr);
                foreach (XmlNode label in labelNodes)
                {
                    string answerId = label.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
                    XmlNode labelTextNode = label.SelectSingleNode(".//qti:material/qti:mattext", nsmgr);
                    string labelText = labelTextNode?.InnerText ?? "No option";
                    question.Answers.Add(new Answer(answerId, labelText, isCorrect: false));
                }
            }
            XmlNodeList respConditions = questionNode.SelectNodes(".//qti:respcondition", nsmgr);
            foreach (XmlNode condition in respConditions)
            {
                XmlNode varEqualNode = condition.SelectSingleNode(".//qti:varequal", nsmgr);
                if (varEqualNode != null)
                {
                    string correctId = varEqualNode.InnerText.Trim();
                    var matching = question.Answers.FirstOrDefault(a => a.AnswerId == correctId);
                    if (matching != null)
                        matching.IsCorrect = true;
                }
            }
        }

        private static void ParseMultipleDropdowns(XmlNode questionNode, Question question, XmlNamespaceManager nsmgr)
        {
            XmlNodeList responseNodes = questionNode.SelectNodes(".//qti:response_lid", nsmgr);
            foreach (XmlNode response in responseNodes)
            {
                XmlNodeList labelNodes = response.SelectNodes(".//qti:response_label", nsmgr);
                foreach (XmlNode label in labelNodes)
                {
                    string answerId = label.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
                    XmlNode labelTextNode = label.SelectSingleNode(".//qti:material/qti:mattext", nsmgr);
                    string labelText = labelTextNode?.InnerText ?? "No option";
                    question.Answers.Add(new Answer(answerId, labelText, isCorrect: false));
                }
            }
            XmlNodeList respConditions = questionNode.SelectNodes(".//qti:respcondition", nsmgr);
            foreach (XmlNode condition in respConditions)
            {
                XmlNode varEqualNode = condition.SelectSingleNode(".//qti:varequal", nsmgr);
                if (varEqualNode != null)
                {
                    string correctId = varEqualNode.InnerText.Trim();
                    var matching = question.Answers.FirstOrDefault(a => a.AnswerId == correctId);
                    if (matching != null)
                        matching.IsCorrect = true;
                }
            }
        }

        private static void ParseMatching(XmlNode questionNode, Question question, XmlNamespaceManager nsmgr)
        {
            XmlNodeList responseNodes = questionNode.SelectNodes(".//qti:response_lid", nsmgr);
            foreach (XmlNode response in responseNodes)
            {
                XmlNodeList labelNodes = response.SelectNodes(".//qti:response_label", nsmgr);
                foreach (XmlNode label in labelNodes)
                {
                    string answerId = label.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
                    XmlNode labelTextNode = label.SelectSingleNode(".//qti:material/qti:mattext", nsmgr);
                    string labelText = labelTextNode?.InnerText ?? "No option";
                    question.Answers.Add(new Answer(answerId, labelText, isCorrect: false));
                }
            }
            XmlNodeList respConditions = questionNode.SelectNodes(".//qti:respcondition", nsmgr);
            foreach (XmlNode condition in respConditions)
            {
                XmlNode varEqualNode = condition.SelectSingleNode(".//qti:varequal", nsmgr);
                if (varEqualNode != null)
                {
                    string correctId = varEqualNode.InnerText.Trim();
                    var matching = question.Answers.FirstOrDefault(a => a.AnswerId == correctId);
                    if (matching != null)
                        matching.IsCorrect = true;
                }
            }
        }

        private static void ParseCalculated(XmlNode questionNode, Question question, XmlNamespaceManager nsmgr)
        {
            Debug.WriteLine("[DEBUG] Calculated question parsed; no static answers.");
        }

        private static void ParseNumerical(XmlNode questionNode, Question question, XmlNamespaceManager nsmgr)
        {
            XmlNode varEqualNode = questionNode.SelectSingleNode(".//qti:respcondition//qti:varequal", nsmgr);
            if (varEqualNode != null)
            {
                string numericAnswer = varEqualNode.InnerText.Trim();
                question.Answers.Add(new Answer(Guid.NewGuid().ToString(), numericAnswer, isCorrect: true));
            }
        }

        // Essay, file_upload, and text_only questions require no answer parsing.

        #endregion

        #region Helper Methods

        private static string FixupLocalReferencesInHtml(string html, string extractPath)
        {
            const string prefix = "$IMS-CC-FILEBASE$/";
            int searchIndex = 0;
            while ((searchIndex = html.IndexOf(prefix, searchIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                int start = searchIndex;
                int nextQuotePos = FindNextQuote(html, start + prefix.Length);
                if (nextQuotePos < 0)
                    break;
                string originalRef = html.Substring(start, nextQuotePos - start);
                string relativePath = originalRef.Substring(prefix.Length);
                int questionMark = relativePath.IndexOf('?');
                if (questionMark >= 0)
                    relativePath = relativePath.Substring(0, questionMark);
                relativePath = WebUtility.UrlDecode(relativePath);
                // Use the constant "web_resources" folder.
                string localPath = Path.Combine(extractPath, "web_resources", relativePath);
                string fileUrl = "file:///" + localPath.Replace('\\', '/');
                html = html.Remove(start, originalRef.Length);
                html = html.Insert(start, fileUrl);
                searchIndex = start + fileUrl.Length;
            }
            return html;
        }

        private static int FindNextQuote(string text, int startIndex)
        {
            int dq = text.IndexOf('"', startIndex);
            int sq = text.IndexOf('\'', startIndex);
            if (dq < 0) return (sq < 0) ? -1 : sq;
            if (sq < 0) return dq;
            return Math.Min(dq, sq);
        }

        private static Dictionary<string, string> ParseManifest(string manifestPath)
        {
            Dictionary<string, string> fileMappings = new Dictionary<string, string>();
            XmlDocument doc = new XmlDocument();
            doc.Load(manifestPath);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ims", "http://www.imsglobal.org/xsd/imsccv1p1/imscp_v1p1");
            XmlNodeList resourceNodes = doc.SelectNodes("//ims:resource", nsmgr);
            foreach (XmlNode resource in resourceNodes)
            {
                string resourceId = resource.Attributes["identifier"]?.InnerText;
                string resourceType = resource.Attributes["type"]?.InnerText;
                if (string.IsNullOrEmpty(resourceId) || string.IsNullOrEmpty(resourceType))
                    continue;
                if (!resourceType.Contains("webcontent", StringComparison.OrdinalIgnoreCase))
                    continue;
                XmlNode fileNode = resource.SelectSingleNode("ims:file", nsmgr);
                if (fileNode != null)
                {
                    string fileName = fileNode.Attributes["href"]?.InnerText;
                    if (!string.IsNullOrEmpty(fileName))
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
            foreach (var question in quiz.Questions)
            {
                for (int i = 0; i < question.Attachments.Count; i++)
                {
                    string originalPath = question.Attachments[i].FilePath;
                    if (originalPath.IndexOf("$IMS-CC-FILEBASE$", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Debug.WriteLine($"[DEBUG] Found $IMS-CC-FILEBASE$ in: {originalPath}");
                        string relativePath = originalPath.Replace("$IMS-CC-FILEBASE$/", "");
                        int questionMark = relativePath.IndexOf('?');
                        if (questionMark != -1)
                            relativePath = relativePath.Substring(0, questionMark);
                        relativePath = WebUtility.UrlDecode(relativePath);
                        // Construct expected path using the constant "web_resources"
                        string fullPath = Path.Combine(extractPath, "web_resources", relativePath);
                        if (!File.Exists(fullPath))
                        {
                            // If not found, perform a recursive search.
                            string fileName = Path.GetFileName(relativePath);
                            string[] foundFiles = Directory.GetFiles(extractPath, fileName, SearchOption.AllDirectories);
                            if (foundFiles.Length > 0)
                            {
                                fullPath = foundFiles[0];
                                Debug.WriteLine($"[DEBUG] Found file via recursive search: {fullPath}");
                            }
                        }
                        if (File.Exists(fullPath))
                        {
                            string newUrl = "file:///" + fullPath.Replace('\\', '/');
                            // Replace the attachment with the new file URL.
                            question.Attachments[i] = new QuestionAttachment(newUrl, question.Attachments[i].Type);
                            Debug.WriteLine($"[DEBUG] Replaced Attachment Path: {originalPath} -> {fullPath}");
                            // Also update the HTML if it still contains the original reference.
                            question.Text = question.Text.Replace(originalPath, newUrl);
                        }
                        else
                        {
                            Debug.WriteLine($"[ERROR] File Not Found: {fullPath}");
                        }
                    }
                }
            }
        }

        private static List<string> ExtractHtmlAttribute(string content, string tagName, string attributeName)
        {
            var results = new List<string>();
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(tagName) || string.IsNullOrEmpty(attributeName))
                return results;
            string lowerContent = content.ToLowerInvariant();
            string lowerTag = tagName.ToLowerInvariant();
            string lowerAttr = attributeName.ToLowerInvariant();
            int searchIndex = 0;
            while (true)
            {
                int tagPos = lowerContent.IndexOf("<" + lowerTag, searchIndex, StringComparison.Ordinal);
                if (tagPos < 0) break;
                int closeTagPos = lowerContent.IndexOf(">", tagPos, StringComparison.Ordinal);
                if (closeTagPos < 0) break;
                int chunkLength = (closeTagPos - tagPos) + 1;
                string chunk = content.Substring(tagPos, chunkLength);
                string chunkLower = chunk.ToLowerInvariant();
                string attrDouble = lowerAttr + "=\"";
                int attrPos = chunkLower.IndexOf(attrDouble, StringComparison.Ordinal);
                if (attrPos >= 0)
                {
                    attrPos += attrDouble.Length;
                    int endQuote = chunkLower.IndexOf("\"", attrPos, StringComparison.Ordinal);
                    if (endQuote > attrPos)
                    {
                        string val = chunk.Substring(attrPos, endQuote - attrPos);
                        results.Add(val);
                    }
                }
                string attrSingle = lowerAttr + "='";
                int attrPos2 = chunkLower.IndexOf(attrSingle, StringComparison.Ordinal);
                if (attrPos2 >= 0)
                {
                    attrPos2 += attrSingle.Length;
                    int endQuote = chunkLower.IndexOf("'", attrPos2, StringComparison.Ordinal);
                    if (endQuote > attrPos2)
                    {
                        string val = chunk.Substring(attrPos2, endQuote - attrPos2);
                        results.Add(val);
                    }
                }
                searchIndex = closeTagPos + 1;
            }
            return results;
        }

        #endregion
    }
}




