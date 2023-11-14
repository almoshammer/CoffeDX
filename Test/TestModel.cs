using CoffeDX.Query.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Test
{
    [DEntity]
    public class TestModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public float Balance { get; set; }
        public long created_at { get; set; }
        public DateTime created_time { get; set; }
        public bool is_active { get; set; }

    }
}
