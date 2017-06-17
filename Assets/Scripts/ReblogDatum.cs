using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

internal class ReblogDatum : DatumBase
{
    private readonly DatumBase _parent;
    public DatumBase Parent { get { return _parent; } }

    public ReblogDatum(
        string name, string accountUrl, string avatarUrl,
        string parentName, string parentAccountUrl)
        : base(name, accountUrl, avatarUrl)
    {
        _parent = new DatumBase(parentName, parentAccountUrl, null);
    }
}
