// -----------------------------------------------------------------------
// <copyright file="FlexDownloader.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLog;

namespace QPAS
{
    [Export(typeof(IStatementDownloader))]
    [ExportMetadata("Name", "Interactive Brokers")]
    public class FlexDownloader : IStatementDownloader
    {
        public string Name { get { return "Interactive Brokers"; } }

        public async Task<string> DownloadStatement()
        {
            var logger = LogManager.GetCurrentClassLogger();

            string flexToken = Properties.Settings.Default.flexToken;
            string flexID = Properties.Settings.Default.flexID;
            if (String.IsNullOrEmpty(flexToken))
                throw new Exception("Flex token is empty.");
            if (String.IsNullOrEmpty(flexID))
                throw new Exception("Flex ID is empty.");

            //first we send a request for the statement
            string theURL = string.Format("https://gdcdyn.interactivebrokers.com/Universal/servlet/FlexStatementService.SendRequest?t={0}&q={1}&v=3", flexToken, flexID);
            using (var client = new HttpClient())
            {
                string contents = await client.GetStringAsync(theURL).ConfigureAwait(false);

                XDocument response = XDocument.Load(new StringReader(contents));
                var xElement = response.Element("FlexStatementResponse");
                if (xElement == null)
                {
                    throw new Exception("FlexStatementResponse is null.");
                }

                var element = xElement.Element("Status");
                if (element == null)
                {
                    throw new Exception("Status is null.");
                }

                if (element.Value != "Success")
                {
                    var xElement1 = xElement.Element("ErrorMessage");
                    if (xElement1 != null)
                        throw new Exception(xElement1.Value);
                    else
                        throw new Exception("Unspecified Error. Status: " + element.Value);
                }

                string referenceCode;
                var element1 = xElement.Element("ReferenceCode");
                if (element1 != null)
                {
                    referenceCode = element1.Value;
                }
                else
                {
                    throw new Exception("ReferenceCode is null");
                }

                bool retrieved = false;
                string flex = "";

                //then we wait a bit and try to get the statement we requested
                theURL = string.Format(@"https://gdcdyn.interactivebrokers.com/Universal/servlet/FlexStatementService.GetStatement?q={0}&t={1}&v=3",
                                        referenceCode,
                                        flexToken);
                System.Threading.Thread.Sleep(1000);
                while (!retrieved)
                {
                    flex = await client.GetStringAsync(theURL).ConfigureAwait(false);
                    //check if we actually got the fucking thing
                    XDocument xdoc = XDocument.Load(new StringReader(flex));

                    //get all the categories in the file
                    XElement statementResponse = xdoc.Element("FlexStatementResponse");
                    if (statementResponse != null)
                    {
                        var error = statementResponse.Element("ErrorCode");
                        if (error == null || error.Value != "1019")
                        {
                            logger.Log(LogLevel.Error, "Got FlexStatementResponse but had error. Contents: ");
                            logger.Log(LogLevel.Error, statementResponse.ToString());
                            throw new Exception(string.Format("Error code problem. Code: {0}", error));
                            
                        }

                        //we have to wait more for the statement to be generated
                        if (error.Value == "1019")
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    else
                    {

                        var flexQueryResponse = xdoc.Element("FlexQueryResponse");
                        if (flexQueryResponse != null)
                            retrieved = true;
                    }
                }

                try
                {
                    SaveFlexStatementToDisk(flex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error writing statement to file: " + ex.Message);
                }

                return flex;
            }
        }

        private static void SaveFlexStatementToDisk(string flex)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.statementSaveLocation)) return;

            if (!Directory.Exists(Properties.Settings.Default.statementSaveLocation))
            {
                Directory.CreateDirectory(Properties.Settings.Default.statementSaveLocation);
            }
            string filename = string.Format("{0}\\{1}.xml", Properties.Settings.Default.statementSaveLocation, DateTime.Now.ToString("yyyy-MM-dd HHmmss"));

            int i = 1;
            while (File.Exists(filename))
            {
                filename = string.Format("{0}\\{1} ({2})",
                    Properties.Settings.Default.statementSaveLocation,
                    DateTime.Now.ToLongDateString(),
                    i);
                i++;
            }

            File.WriteAllText(filename, flex);
        }
    }
}