namespace Xdxd.DotNet.Testing;

using System.Collections;
using System.Collections.Generic;

public class FalsyStringData : IEnumerable<object[]>
{
    private static readonly object[][] Data =
    {
        new object[] { null },
        new object[] { "" },
        new object[] { " " },
        new object[] { "  " },
        new object[] { "\t" },
        new object[] { "\r" },
        new object[] { "\n" },
    };

    public IEnumerator<object[]> GetEnumerator()
    {
        return ((IEnumerable<object[]>)Data).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
