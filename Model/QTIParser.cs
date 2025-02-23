using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net; // for WebUtility
using System.Xml;

namespace QTIParserApp.Model
{
    class QTIParser
    {
        public static Quiz ParseQTI(string quizFilePath, string manifestPath, string extractPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(quizFilePath);

            // QTI doc namespace manager
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("qti", "http://www.imsglobal.org/xsd/ims_qtiasiv1p2");

            // Identify quiz ID & Title
            XmlNode quizNode = doc.SelectSingleNode("//qti:assessment", nsmgr);
            string quizId = quizNode?.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();
            string quizTitle = quizNode?.Attributes["title"]?.InnerText ?? "Untitled Quiz";

            Quiz quiz = new Quiz(quizId, quizTitle, 1);
            Debug.WriteLine($"[DEBUG] Quiz Loaded: {quizTitle} (ID: {quizId})");

            // Parse <item> nodes for questions
            XmlNodeList questionNodes = doc.SelectNodes("//qti:item", nsmgr);
            foreach (XmlNode questionNode in questionNodes)
            {
                // Get question ID
                string questionId = questionNode.Attributes["ident"]?.InnerText ?? Guid.NewGuid().ToString();

                // Optional: parse question_type from <qtimetadatafield>
                XmlNode questionTypeNode = questionNode.SelectSingleNode(
                    "./qti:itemmetadata/qti:qtimetadata/qti:qtimetadatafield[qti:fieldlabel='question_type']/qti:fieldentry",
                    nsmgr
                );
                string questionType = questionTypeNode?.InnerText ?? "unknown";

                // Optional: parse points_possible
                double questionPoints = 1.0; // default
                XmlNode pointsNode = questionNode.SelectSingleNode(
                    "./qti:itemmetadata/qti:qtimetadata/qti:qtimetadatafield[qti:fieldlabel='points_possible']/qti:fieldentry",
                    nsmgr
                );
                if (pointsNode != null && double.TryParse(pointsNode.InnerText, out double parsedPoints))
                {
                    questionPoints = parsedPoints;
                }

                // Grab the main question text (often from the first mattext)
                XmlNode textNode = questionNode.SelectSingleNode("./qti:presentation/qti:material/qti:mattext", nsmgr);
                string encodedText = textNode?.InnerXml ?? "No question text";
                // DECODE the HTML entities so &lt;img&gt; becomes <img>
                string questionText = WebUtility.HtmlDecode(encodedText);

                // Create the question object, storing real HTML in 'Text'
                Question question = new Question(questionId, questionType, questionText)
                {
                    PointsPossible = questionPoints
                };

                // Also parse every <mattext> block for potential <img>/<a> attachments
                XmlNodeList mattextNodes = questionNode.SelectNodes(".//qti:mattext", nsmgr);
                foreach (XmlNode matNode in mattextNodes)
                {
                    string matEncoded = matNode.InnerXml;
                    // decode so we can find <img> or <a>
                    string content = WebUtility.HtmlDecode(matEncoded);

                    // if there's "<img", parse src
                    if (content.IndexOf("<img", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foreach (string src in ExtractHtmlAttribute(content, "img", "src"))
                        {
                            question.Attachments.Add(new QuestionAttachment(src, "image"));
                            Debug.WriteLine($"[DEBUG] Found <img> src => {src}");
                        }
                    }
                    // if there's "<a", parse href
                    if (content.IndexOf("<a ", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foreach (string href in ExtractHtmlAttribute(content, "a", "href"))
                        {
                            question.Attachments.Add(new QuestionAttachment(href, "document"));
                            Debug.WriteLine($"[DEBUG] Found <a> href => {href}");
                        }
                    }
                }

                quiz.Questions.Add(question);
            }

            // Debug: show attachments before fixup
            Debug.WriteLine("***** Attachments Summary (before fixups) *****");
            foreach (var q in quiz.Questions)
            {
                Debug.WriteLine($"Q {q.QuestionId} (Type: {q.QuestionType}) => {q.Attachments.Count} attachments");
                foreach (var att in q.Attachments)
                {
                    Debug.WriteLine($"    {att.FilePath}");
                }
            }

            // If manifest is present, parse resource->file mappings, then fix local paths
            if (!string.IsNullOrEmpty(manifestPath))
            {
                Dictionary<string, string> fileMappings = ParseManifest(manifestPath);
                AttachMediaToQuestions(quiz, fileMappings, extractPath);
            }

            // Debug: show attachments after fixup
            Debug.WriteLine("***** Attachments Summary (after fixups) *****");
            foreach (var q in quiz.Questions)
            {
                Debug.WriteLine($"Q {q.QuestionId} (Type: {q.QuestionType}) => {q.Attachments.Count} attachments");
                foreach (var att in q.Attachments)
                {
                    Debug.WriteLine($"    {att.FilePath}");
                }
            }

            return quiz;
        }

        // Parse the imsmanifest.xml for "webcontent" resources (ignore QTI or meta)
        private static Dictionary<string, string> ParseManifest(string manifestPath)
        {
            Dictionary<string, string> fileMappings = new Dictionary<string, string>();

            XmlDocument doc = new XmlDocument();
            doc.Load(manifestPath);

            // default namespace in your imsmanifest.xml
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ims", "http://www.imsglobal.org/xsd/imsccv1p1/imscp_v1p1");

            XmlNodeList resourceNodes = doc.SelectNodes("//ims:resource", nsmgr);
            foreach (XmlNode resource in resourceNodes)
            {
                string resourceId = resource.Attributes["identifier"]?.InnerText;
                string resourceType = resource.Attributes["type"]?.InnerText;
                if (string.IsNullOrEmpty(resourceId) || string.IsNullOrEmpty(resourceType))
                    continue;

                // Only handle "webcontent"
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

        // Replaces $IMS-CC-FILEBASE$ references in question attachments with local file paths.
        // This version checks the fileMappings dictionary to adjust the relative path.
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
                        // Remove the prefix "$IMS-CC-FILEBASE$/"
                        string relativePath = originalPath.Replace("$IMS-CC-FILEBASE$/", "");

                        // Remove any query parameters
                        int questionMark = relativePath.IndexOf('?');
                        if (questionMark != -1)
                            relativePath = relativePath.Substring(0, questionMark);

                        // URL-decode (e.g., convert %20 to space)
                        relativePath = WebUtility.UrlDecode(relativePath);

                        // Attempt to find a matching mapping from the manifest.
                        // For example, if relativePath is "Uploaded Media/france picture.webp"
                        // and fileMappings contains "web_resources/Uploaded Media/france picture.webp",
                        // use that mapping.
                        string mappingPath = fileMappings.Values.FirstOrDefault(
                            v => v.EndsWith(relativePath, StringComparison.OrdinalIgnoreCase)
                        );
                        if (mappingPath != null)
                        {
                            relativePath = mappingPath;
                        }

                        // Build the absolute path.
                        // If relativePath already starts with "web_resources", then use it;
                        // otherwise, prepend "web_resources" to it.
                        string fullPath;
                        if (relativePath.StartsWith("web_resources", StringComparison.OrdinalIgnoreCase))
                            fullPath = Path.Combine(extractPath, relativePath);
                        else
                            fullPath = Path.Combine(extractPath, "web_resources", relativePath);

                        if (File.Exists(fullPath))
                        {
                            question.Attachments[i] = new QuestionAttachment(fullPath, question.Attachments[i].Type);
                            Debug.WriteLine($"[DEBUG] Replaced Attachment Path: {originalPath} -> {fullPath}");
                        }
                        else
                        {
                            Debug.WriteLine($"[ERROR] File Not Found: {fullPath}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the given attribute's value from <tagName ... attributeName="..."> in 'content'.
        /// Handles both double quotes (attr="...") and single quotes (attr='...').
        /// </summary>
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
                // Find the next occurrence of <tagName
                int tagPos = lowerContent.IndexOf("<" + lowerTag, searchIndex, StringComparison.Ordinal);
                if (tagPos < 0) break;
                int closeTagPos = lowerContent.IndexOf(">", tagPos, StringComparison.Ordinal);
                if (closeTagPos < 0) break;

                int chunkLength = (closeTagPos - tagPos) + 1;
                string chunk = content.Substring(tagPos, chunkLength); // original snippet
                string chunkLower = chunk.ToLowerInvariant();

                // Look for attributeName="..."
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

                // Look for attributeName='...'
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
    }
}



