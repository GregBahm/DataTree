using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

class DataProcessor
{
    private readonly string RawDataFolder;

    private int _index;

    public DataProcessor(string rawDataFolder)
    {
        RawDataFolder = rawDataFolder;
    }

    public Node ProcessData()
    {
        _index = 0;
        List<ReblogDatum> reblogs = new List<ReblogDatum>();

        string[] filePaths = Directory.GetFiles(RawDataFolder);
        foreach (string filePath in filePaths)
        {
            string data = File.ReadAllText(filePath);
            reblogs.AddRange(ProcessData(data));
        }

        return ProcessBuilders(reblogs);
    }

    private Node ProcessBuilders(IEnumerable<ReblogDatum> reblogData)
    {
        IEnumerable<NodeBuilder> builders = GetBaseBuilders(reblogData);
        PatchBrokeConnections(builders);
        CullCircularDependencies(builders);
        NodeBuilder rootBuilder = builders.First(item => item.RawIndex == 0).Parent;
        Node ret = rootBuilder.ToNode(null, _index);
        return ret;
    }

    private void PatchBrokeConnections(IEnumerable<NodeBuilder> builders)
    {
        // So the logic for patching broken connections up is:
        // Is their parent null? If yes, find the item with the index one higher than their own. Make that their parent.

        NodeBuilder[] sortedBuilders = new NodeBuilder[_index];
        foreach (NodeBuilder builder in builders.Where(item => item.Parent != null))
        {
            sortedBuilders[builder.RawIndex] = builder;
        }
        foreach (NodeBuilder builder in builders.Where(item => item.Parent == null))
        {
            for (int i = builder.RawIndex; i > 0; i--)
            {
                if (sortedBuilders[i] != null)
                {
                    builder.Parent = sortedBuilders[i];
                    break;
                }
            }
        }
    }

    private IEnumerable<NodeBuilder> GetBaseBuilders(IEnumerable<ReblogDatum> reblogData)
    {
        Dictionary<string, NodeBuilder> builderDictionary = new Dictionary<string, NodeBuilder>();
        foreach (ReblogDatum item in reblogData)
        {
            if (!builderDictionary.ContainsKey(item.Parent.Key))
            {
                NodeBuilder parent = new NodeBuilder(item.Parent.Key, item.Parent.AccountUrl, item.Parent.AvatarUrl);
                parent.RawIndex = item.RawIndex;
                builderDictionary.Add(item.Parent.Key, parent);
            }
            if (!builderDictionary.ContainsKey(item.Key))
            {
                NodeBuilder builder = new NodeBuilder(item.Name, item.AccountUrl, item.AvatarUrl);
                builderDictionary.Add(item.Key, builder);
            }
            NodeBuilder baseBuilder = builderDictionary[item.Key];
            NodeBuilder parentBuilder = builderDictionary[item.Parent.Key];
            baseBuilder.AvatarUrl = item.AvatarUrl; // Builders built from reblog parents don't have avatar urls
            baseBuilder.Parent = parentBuilder;
            baseBuilder.RawIndex = Math.Min(baseBuilder.RawIndex, item.RawIndex); // Parent builders aren't built with a real index, so go with the higher index
        }
        return builderDictionary.Values;
    }

    private void CullCircularDependencies(IEnumerable<NodeBuilder> nodes)
    {
        foreach (NodeBuilder item in nodes)
        {
            HashSet<NodeBuilder> parentChain = new HashSet<NodeBuilder>();
            NodeBuilder parent = item.Parent;
            while (parent != null)
            {
                if (parentChain.Contains(parent))
                {
                    item.Parent = GetMaxParent(parentChain); //TODO: validate this logic
                    break;
                }
                parentChain.Add(parent);
                parent = parent.Parent;
            }
        }
    }

    private NodeBuilder GetMaxParent(IEnumerable<NodeBuilder> parentChain)
    {
        int highestIndex = 0;
        NodeBuilder ret = null;
        foreach (NodeBuilder item in parentChain)
        {
            if (item.RawIndex > highestIndex)
            {
                highestIndex = item.RawIndex;
                ret = item;
            }
        }
        return ret;
    }

    private IEnumerable<ReblogDatum> ProcessData(string data)
    {
        List<ReblogDatum> reblogs = new List<ReblogDatum>();
        string[] split = data.Split(new[] { "<li class=\"note" }, StringSplitOptions.RemoveEmptyEntries);
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
                _index++;
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

        return new ReblogDatum(name, url, avatarUrl, parentName, parentUrl, _index);
    }
}
