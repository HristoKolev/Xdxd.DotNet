namespace Xdxd.DotNet.Shared;

using System;
using System.Collections.Generic;

public class DetailedException : Exception
{
    public DetailedException()
    {
        this.Details = new Dictionary<string, object>();
    }

    public DetailedException(string message)
        : base(message)
    {
        this.Details = new Dictionary<string, object>();
    }

    public DetailedException(string message, Exception inner)
        : base(message, inner)
    {
        this.Details = new Dictionary<string, object>();
    }

    // ReSharper disable once CollectionNeverUpdated.Global
    public Dictionary<string, object> Details { get; }

    public string Fingerprint { get; set; }
}
