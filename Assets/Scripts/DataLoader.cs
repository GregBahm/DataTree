using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

class DataProcessor
{
    private readonly string RawDataFolder;
    private readonly string RootAccountUrl;

    public DataProcessor(string rawDataFolder, string rootAccountUrl)
    {
        RawDataFolder = rawDataFolder;
        RootAccountUrl = rootAccountUrl;
    }

    public Node ProcessData()
    {
        List<ReblogDatum> reblogs = new List<ReblogDatum>();

        string[] filePaths = Directory.GetFiles(RawDataFolder);
        foreach (string filePath in filePaths)
        {
            string fileData = File.ReadAllText(filePath);
            List<ReblogDatum> dataFromFile = ProcessDataFromFile(fileData).ToList();
            dataFromFile.Reverse(); // Order matters for the processor
            reblogs.AddRange(dataFromFile);
        }
        return ProcessBuilders(reblogs);
    }

    private Node ProcessBuilders(IEnumerable<ReblogDatum> reblogData)
    {
        NodeBuilder rootBuilder = new NodeBuilder(RootAccountUrl, RootAccountUrl, null);
        IEnumerable<NodeBuilder> builders = GetBaseBuilders(rootBuilder, reblogData).ToList();
        List<NodeBuilder> ahWhat = builders.Where(item => item.Parent == null).ToList();
        if(builders.Count(item => item.Parent == null) != 1)
        {
            throw new Exception("Data loader is not working right.");
        }
        Node ret = rootBuilder.ToNode(null);
        return ret;
    }

    private IEnumerable<NodeBuilder> GetBaseBuilders(NodeBuilder root, IEnumerable<ReblogDatum> reblogData)
    {
        Dictionary<string, NodeBuilder> builderDictionary = new Dictionary<string, NodeBuilder>();
        NodeBuilder mostRecentNode = root;
        builderDictionary.Add(RootAccountUrl, root);
        foreach (ReblogDatum item in reblogData)
        {
            if(item.Parent.Key == RootAccountUrl)
            {
                Debug.Log("hit");
            }
            NodeBuilder parentBuilder;
            if (builderDictionary.ContainsKey(item.Parent.Key))
            {
                parentBuilder = builderDictionary[item.Parent.Key];
            }
            else
            {
                //Need to add an alias in the dictionary.
                builderDictionary.Add(item.Parent.Key, mostRecentNode);
                parentBuilder = mostRecentNode;
            }
            NodeBuilder baseBuilder;
            if (!builderDictionary.ContainsKey(item.Key))
            {
                baseBuilder = new NodeBuilder(item.Name, item.AccountUrl, item.AvatarUrl);
                builderDictionary.Add(item.Key, baseBuilder);
            }
            else
            {
                baseBuilder = builderDictionary[item.Key];
            }
            
            baseBuilder.Parent = parentBuilder;
            mostRecentNode = baseBuilder;
        }
        return new HashSet<NodeBuilder>(builderDictionary.Values);
    }

    private IEnumerable<ReblogDatum> ProcessDataFromFile(string fileData)
    {
        List<ReblogDatum> reblogs = new List<ReblogDatum>();
        string[] split = fileData.Split(new[] { "<li class=\"note" }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < split.Length - 2; i++) //Toss the first and last two entries (which are always the "posted this" and "show more notes" entries
        {
            string datum = split[i];
            string term = datum.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (term == "like")
            {
                //Can't do shit with likes since we don't know where they came from
            }
            else if (term == "reblog")
            {
                reblogs.Add(BuilderFromReblog(datum));
            }
            else
            {
                throw new Exception("What dis?");
            }
        }
        return reblogs;
    }

    private ReblogDatum BuilderFromReblog(string datum)
    {
        if (!datum.Contains("without_commentary"))
        {
            //TODO: Transcribe the commentary
        }
        string[] splitOnHRef = datum.Split(new[] { "href=\"" }, StringSplitOptions.RemoveEmptyEntries);
        string urlLine = splitOnHRef[1]; //Contains url, avatar url, and name
        string[] splitUrlLine = urlLine.Split('\"');
        string url = splitUrlLine[0];
        string avatarUrl = splitUrlLine[4];
        string name = splitUrlLine[2];

        string parentLine = splitOnHRef[3]; // Contains parent url, and parent name
        string parentUrl = parentLine.Split('\"')[0];
        string parentName = parentLine.Split('>')[1].Split('<')[0];

        return new ReblogDatum(name, url, avatarUrl, parentName, parentUrl);
    }
}
