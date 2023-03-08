using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Query.Mapping
{
    public class DColumnAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsDbGenerated { get; set; }
        public bool IsDiscriminator { get; set; }
        public DQuery.UpdateCheck UpdateCheck { get; set; }
    }
}
