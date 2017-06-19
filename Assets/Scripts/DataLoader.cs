using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

class DataProcessor
{
    private readonly string _rawDataFolder;
    public string RawDataFolder { get { return _rawDataFolder; } }
    private readonly string _avatarFolder;
    public string AvatarFolder { get { return _avatarFolder; } }
    private readonly string _rootAccountUrl;
    public string RootAccountUrl { get { return _rootAccountUrl; } }

    public DataProcessor(string dataFolder, string rootAccountUrl)
    {
        _rawDataFolder = Path.Combine(dataFolder, "Raw");
        _avatarFolder = Path.Combine(dataFolder, "Avatars");
        _rootAccountUrl = rootAccountUrl;
    }

    public static DataProcessor GetTestProcessor()
    {
        string projectFolder = Path.GetFullPath(Path.Combine(Application.dataPath, @"..\"));
        string dataFolder = Path.Combine(projectFolder, "SamplePostData");
        return new DataProcessor(dataFolder, @"http://concretization.tumblr.com/");
    }

    public Node ProcessData()
    {
        List<ReblogDatum> reblogs = new List<ReblogDatum>();

        string[] filePaths = Directory.GetFiles(_rawDataFolder);
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
        NodeBuilder rootBuilder = new NodeBuilder(_rootAccountUrl, _rootAccountUrl, null);
        IEnumerable<NodeBuilder> builders = GetBaseBuilders(rootBuilder, reblogData).ToList();
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
        builderDictionary.Add(_rootAccountUrl, root);
        foreach (ReblogDatum item in reblogData)
        {
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
