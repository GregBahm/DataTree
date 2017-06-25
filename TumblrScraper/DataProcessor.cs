using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TumblrScraper
{
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
            if (builders.Count(item => item.Parent == null) != 1)
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

    internal class DatumBase
    {
        private readonly string _name;
        public string Name { get { return _name; } }

        private readonly string _accountUrl;
        public string AccountUrl { get { return _accountUrl; } }

        public string AvatarUrl { get; set; }

        public string Key { get { return _accountUrl; } }

        public DatumBase(string name, string accountUrl, string avatarUrl)
        {
            _name = name;
            _accountUrl = accountUrl;
            AvatarUrl = avatarUrl;
        }
    }

    internal class ReblogDatum : DatumBase
    {
        private readonly DatumBase _parent;
        public DatumBase Parent { get { return _parent; } }

        public ReblogDatum(
            string name, string accountUrl, string avatarUrl,
            string parentName, string parentAccountUrl
            )
            : base(name, accountUrl, avatarUrl)
        {
            _parent = new DatumBase(parentName, parentAccountUrl, null);
        }
    }

    internal class NodeBuilder : DatumBase
    {
        public int RawIndex { get; set; }

        private readonly List<NodeBuilder> _children;
        public IEnumerable<NodeBuilder> Children { get { return _children; } }

        private NodeBuilder _parent;
        public NodeBuilder Parent
        {
            get { return _parent; }
            set
            {
                if(_parent == value)
                {
                    return;
                }
                if(_parent != null)
                {
                    _parent._children.Remove(this);
                }
                _parent = value;
                if(_parent != null)
                {
                    _parent._children.Add(this);
                }
            }
        }

        public NodeBuilder(string name, string accountUrl, string avatarUrl)
            : base(name, accountUrl, avatarUrl)
        {
            _children = new List<NodeBuilder>();
        }
        
        public override string ToString()
        {
            return AccountUrl + " + " + _children.Count + " children";
        }

        public Node ToNode(Node parent)
        {
            return new Node(parent, Name, AccountUrl, AvatarUrl, Children);
        }
    }
    
    public class Node
    {
        private readonly int _index;
        public int Index { get { return _index; } }

        private readonly Node _parent;
        public Node Parent { get { return _parent; } }

        private readonly string _name;
        public string Name { get { return _name; } }

        private readonly string _accountUrl;
        public string AccountUrl { get { return _accountUrl; } }

        private readonly string _avatarUrl;
        public string AvatarUrl { get { return _avatarUrl; } }

        private readonly string _subUrl;
        public string SubUrl { get { return _subUrl; } }

        private readonly IEnumerable<Node> _children;
        public IEnumerable<Node> Children { get { return _children; } }

        internal Node(Node parent, string name, string accountUrl, string avatarUrl, IEnumerable<NodeBuilder> children)
        {
            _parent = parent;
            _name = name;
            _accountUrl = accountUrl;
            _avatarUrl = avatarUrl;
            _subUrl = accountUrl.Replace("https://", "").Replace("http://", "").Split('.')[0];
            _children = children.Select(item => item.ToNode(this)).ToArray();
        }
    }
}
