using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
