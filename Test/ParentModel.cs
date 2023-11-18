using CoffeDX.Query.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Test
{
    [DEntity]
    public class ParentModel
    {
        [DPrimaryKey]
        public int Id { get; set; }
      
        public string Name { get; set; }
    }
}
