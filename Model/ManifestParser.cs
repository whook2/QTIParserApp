using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace QTIParserApp.Model
{
    class ManifestParser
    {
        public static void AttachMediaToQuestions(Quiz quiz, string manifestPath, string extractPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(manifestPath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ims", "http://www.imsglobal.org/xsd/imsccv1p3/imscp_v1p1");

            Dictionary<string, string> fileMappings = new Dictionary<string, string>();
            XmlNodeList resourceNodes = doc.SelectNodes("//ims:resource", nsmgr);

            foreach (XmlNode resource in resourceNodes)
            {
                string resourceId = resource.Attributes["identifier"]?.InnerText;
                string fileName = resource.Attributes["href"]?.InnerText;

                if (!string.IsNullOrEmpty(resourceId) && !string.IsNullOrEmpty(fileName))
                {
                    fileMappings[resourceId] = fileName;
                }
            }

            foreach (var question in quiz.Questions)
            {
                foreach (var attachment in question.Attachments)
                {
                    if (fileMappings.TryGetValue(attachment.FilePath, out string actualFilePath))
                    {
                        string fullPath = Path.Combine(extractPath, "web_resources", actualFilePath);
                        if (File.Exists(fullPath))
                        {
                            attachment.FilePath = fullPath;
                        }
                        else
                        {
                            Debug.WriteLine($"[WARNING] Missing file: {fullPath}");
                        }
                    }
                }
            }
        }
    }
}
