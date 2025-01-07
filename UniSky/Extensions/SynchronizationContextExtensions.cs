using System;
using System.Threading;

namespace UniSky.Extensions;

public static class SynchronizationContextExtensions
{
    public static void Post(this SynchronizationContext context, Action action)
    {
        static void Execute(object a)
            => ((Action)a)();

        context.Post(Execute, action);
    }
}
