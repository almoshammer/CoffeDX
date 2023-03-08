using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Query.Mapping
{
    public class DEntityAttribute : Attribute
    {
        public string Name { get; set; }
        public int Version { get; set; }
    }
}
