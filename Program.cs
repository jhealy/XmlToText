using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

using HtmlAgilityPack;

// extended from Simon Mourier's answer in https://stackoverflow.com/questions/10980237/xml-to-text-convert 

namespace XmlToText
{
    class Program
    {
        static string inboundDir = "inbound";
        static string outboundDir = "outbound";

        static string xmlExt = ".xml";
        static string convertedExt = ".txt";

        static List<string> noiseList = new List<string> { "sort_number", "relevancy", "copyright", "content_type", "charging_class", "iora_exclude", "internal_status", "external_status", "private_status", "territory_specific", 
            "language_code", "bind_point", "document_node", "show_content", "knowledge_gateway" };

        static void Main(string[] args)
        {
            CH.Msg("XmlToText");

            CH.Info("dumping parameters");
            CH.Msg(nameof(inboundDir), inboundDir);
            CH.Msg(nameof(outboundDir), outboundDir);

            CH.Msg(nameof(xmlExt), xmlExt);
            CH.Msg(nameof(convertedExt), convertedExt);

            if ( CheckInboundDir() == false )
            {
                CH.Err("CheckInboundDir() failure, aborting");
                return;
            }
            if ( CheckOutboundDir() == false )
            {
                CH.Err("CheckoutboundDir() failure, aborting");
                return;
            }

            // ListInboundFiles();

            ProcessFiles();

            CH.Pause();
        }

        private static bool ProcessFiles()
        {
            int fileNum = 0;

            CH.Msg("processing files");

            var files = System.IO.Directory.EnumerateFiles(inboundDir, "*" + xmlExt);
            string outboundPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, outboundDir);
            CH.Msg(nameof(outboundPath), outboundPath);

            foreach (string file in files)
            {
                string inboundText = System.IO.File.ReadAllText(file);
                string filename = System.IO.Path.GetFileName(file).Replace(xmlExt, convertedExt);
                string outputFile = System.IO.Path.Combine(outboundPath, filename);

                string convertedText = ConvertXmlToText(inboundText);
                if (convertedText != string.Empty)
                { 
                    System.IO.File.WriteAllText(outputFile, convertedText);
                    CH.Msg(fileNum.ToString(), outputFile);
                    fileNum++;
                }
                else
                {
                    CH.Err("failed to process " + file);
                    CH.Err("ConvertXmlToText returned string.empty");
                }
            }

            return true;
        }

        public static string StripHtml(string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;
            //get rid of HTML tags
            var output = Regex.Replace(source, "<[^>]*>", string.Empty);
            //get rid of multiple blank lines
            output = Regex.Replace(output, @"^\s*$\n", string.Empty, RegexOptions.Multiline);
            return HttpUtility.HtmlDecode(output);
        }

        private static string ConvertXmlToText(string inboundText)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(inboundText);
            }
            catch (Exception ex)
            {
                CH.Err(ex.ToString());
                throw;
            }

            StringBuilder sb = new StringBuilder(1024);
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (!(string.IsNullOrEmpty(node.Name) || string.IsNullOrEmpty(node.InnerText)))
                {
                    string nodeName = node.Name;

                    // only process if tag is not on the noise list.  it allows us to ignore tags.
                    if (noiseList.Contains(nodeName.ToLower()) == false)
                    {
                        string value = node.InnerText;

                        // in my example, 'text' is an inline cdata html field.  We are going to strip out the html
                        if (nodeName.ToLower() =="text")
                        {
                            value = StripHtml(value);
                        }

                        sb.Append(char.ToUpper(node.Name[0]));
                        sb.Append(node.Name.Substring(1));
                        sb.Append(": ");
                        sb.AppendLine(value);
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        private static void ListInboundFiles()
        {
            var files = System.IO.Directory.EnumerateFiles(inboundDir);
            CH.Info($"Listing files in '{inboundDir}' directory");
            foreach ( string file in files )
            {
                CH.Msg(file);
            }
        }

        private static bool CheckOutboundDir()
        {
            if (System.IO.Directory.Exists(outboundDir))
            {
                try
                {
                    System.IO.Directory.Delete(outboundDir, true);
                    CH.Msg("outboundDir was purged: " + outboundDir);
                }
                catch (Exception ex)
                {
                    CH.Err(ex.ToString());
                    return false;
                }
            }

            System.IO.Directory.CreateDirectory(outboundDir);
            CH.Msg($"outboundDir created.");
            CH.Msg(nameof(outboundDir), outboundDir);

            return true;
        }

        private static bool CheckInboundDir()
        {
            if (System.IO.Directory.Exists(inboundDir))
            {
                CH.Msg(nameof(inboundDir), "ok");
                return true;
            }
            else
            {
                CH.Err("inboundDir does not exist.  Please specify a directory that exists.");
                CH.Err(nameof(inboundDir), inboundDir);
                return false;
            }
        }
    }
}


