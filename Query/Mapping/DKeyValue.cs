using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Query.Mapping
{
    public class DKeyValue
    {
        public string key { get; set; }
        public object value { get; set; }
        public DKeyValue(string key, object value)
        {
            this.key = key;
            this.value = value;
        }

    }
}
