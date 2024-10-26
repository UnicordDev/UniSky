using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniSky.Converters
{
    public class Static
    {
        public static bool Equals(int a, int b)
            => a == b;
        public static bool AtLeast(int a, int b)
            => a >= b;

        public static bool NotNull(object x)
            => x is not null;
    }
}
