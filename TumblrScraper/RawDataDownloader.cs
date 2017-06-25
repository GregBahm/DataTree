using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TumblrScraper
{
    class RawDataDownloader
    {
        private readonly string OutputFolder;
        private readonly string UrlBase;
        private readonly string FirstNoteUrl;

        public RawDataDownloader(string outputFolder, string urlBase, string firstNoteUrl)
        {
            OutputFolder = outputFolder;
            UrlBase = urlBase;
            FirstNoteUrl = firstNoteUrl;
        }

        public void DownloadRawData()
        {
            using (WebClient webClient = new WebClient())
            {
                Next(FirstNoteUrl, webClient);
            }
        }

        private void Next(string noteUrlPart, WebClient webClient)
        {
            string pageContent = GetPage(noteUrlPart, webClient);
            SavePage(pageContent, noteUrlPart);
            string nextNoteUrlPart = GetNoteUrlPart(pageContent);
            if (nextNoteUrlPart == null)
            {
                return;
            }
            Next(nextNoteUrlPart, webClient);
        }

        private void SavePage(string pageContent, string noteUrl)
        {
            string noteNumber = noteUrl.Split('=')[1];
            string outputPath = OutputFolder + noteNumber + ".html";
            File.WriteAllText(outputPath, pageContent);
        }

        private string GetNoteUrlPart(string page)
        {
            string[] splitOne = page.Split(new[] { "tumblrReq.open('GET','" }, StringSplitOptions.RemoveEmptyEntries);
            if (splitOne.Length == 1)
            {
                return null;
            }
            return splitOne[1].Split('\'')[0];
        }

        private string GetPage(string noteUrlPart, WebClient webClient)
        {
            string fullPath = UrlBase + noteUrlPart;
            Console.WriteLine("Downloading " + fullPath);
            return webClient.DownloadString(fullPath);
        }
    }
}
