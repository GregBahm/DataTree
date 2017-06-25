using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TumblrScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            ProcessData();
        }

        private static void ProcessData()
        {
            string rawDataFolder = @"C:\Users\Lisa\Documents\DataTree\SamplePostData\Raw";
            DataProcessor processor = new DataProcessor(rawDataFolder, @"http://concretization.tumblr.com");
            Node rootNode = processor.ProcessData();
            //PrintNode(rootNode, 0);


            string avatarsFolder = @"C:\Users\Lisa\Documents\DataTree\SamplePostData\Avatars\";
            DownloadAvatars(rootNode, avatarsFolder);
            Console.Read();
        }

        private static void DownloadAvatars(Node rootNode, string avatarsFolder)
        {
            HashSet<Node> nodes = new HashSet<Node>(GetAllNodes(rootNode));
            int index = 0;
            List<string> barfedOnList = new List<string>();
            using (WebClient client = new WebClient())
            {
                foreach (Node node in nodes)
                {
                    if(node.AvatarUrl == null)
                    {
                        Console.WriteLine("Skipping " + node.SubUrl);
                    }
                    else
                    {
                        try
                        {
                            Console.Clear();
                            Console.WriteLine("Downloading avatars");
                            Console.WriteLine(index + " of " + nodes.Count);
                            foreach (string barf in barfedOnList)
                            {
                                Console.WriteLine("Barfed on " + barf);
                            }
                            index++;
                            string outputPath = avatarsFolder + node.SubUrl + Path.GetExtension(node.AvatarUrl);
                            client.DownloadFile(new Uri(node.AvatarUrl), outputPath);
                        }
                        catch
                        {
                            barfedOnList.Add(node.SubUrl);
                        }
                    }
                }
            }
        }

        private static IEnumerable<Node> GetAllNodes(Node node)
        {
            yield return node;
            foreach (Node child in node.Children)
            {
                IEnumerable<Node> ret = GetAllNodes(child);
                foreach (Node childRet in ret)
                {
                    yield return childRet;
                }
            }
        }

        private static void DownloadAvatars(Node node, string avatarsFolder, WebClient client)
        {
            if(node.AvatarUrl != null)
            {
            }
            foreach (Node child in node.Children)
            {
                DownloadAvatars(child, avatarsFolder, client);
            }
        }

        private static void PrintNode(Node node, int increment)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < increment; i++)
            {
                stringBuilder.Append("\t");
            }
            stringBuilder.Append(node.SubUrl);
            Console.WriteLine(stringBuilder);
            foreach (Node child in node.Children)
            {
                PrintNode(child, increment + 1);
            }
        }

        private void DownloadSampleData()
        {
            string outputFolder = @"C:\Users\Lisa\Documents\DataTree\SamplePostData\Raw\";
            string urlBase = @"http://concretization.tumblr.com";
            string firstNoteUrl = @"/notes/37821502058/cxKEtsNNf?from_c=1443040163";
            RawDataDownloader downloader = new RawDataDownloader(outputFolder, urlBase, firstNoteUrl);
            downloader.DownloadRawData();
        }
    }
}
