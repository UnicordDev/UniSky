using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
