namespace Xdxd.DotNet.Testing;

using System;
using System.Collections.Generic;
using Autofac;
using Xunit;

public static class AssertExt
{
    public static void SequentialEqual<T>(IList<T> expectedSequence, IList<T> actualSequence)
    {
        Assert.Equal(expectedSequence.Count, actualSequence.Count);

        for (int i = 0; i < expectedSequence.Count; i++)
        {
            var expected = expectedSequence[i];
            var actual = actualSequence[i];

            Assert.Equal(expected, actual);
        }
    }
}

public class FunctionAutofacModule : Module
{
    private readonly Action<ContainerBuilder> func;

    public FunctionAutofacModule(Action<ContainerBuilder> func)
    {
        this.func = func;
    }

    protected override void Load(ContainerBuilder builder)
    {
        this.func(builder);
        base.Load(builder);
    }
}
