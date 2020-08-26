using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbpAspNetCoreDemo.Laobai
{
    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}:{Name},{nameof(Age)}:{Age}";
        }
    }
}
