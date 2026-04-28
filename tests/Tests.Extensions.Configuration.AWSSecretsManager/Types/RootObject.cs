using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Types
{
    public class RootObject
    {
        public string Property { get; set; } = null!;

        public MidLevel Mid { get; set; } = null!;
    }
}