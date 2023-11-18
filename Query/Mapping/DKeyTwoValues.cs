using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Query.Mapping
{
    internal class DKeyTwoValues
    {
        public string key { get; set; }
        public object value1 { get; set; }
        public object value2 { get; set; }
        public DKeyTwoValues(string key, string value1, string value2)
        {
            this.key = key;
            this.value1 = value1;
            this.value2 = value2;
        }
    }
}
