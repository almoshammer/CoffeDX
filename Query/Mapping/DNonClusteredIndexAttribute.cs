using System;

namespace CoffeDX.Query.Mapping
{
    public class DNonClusteredIndexAttribute : Attribute
    {
        string[] _fields;
        string[] _includes = null;

        public string[] fields => _fields;
        public string[] includes => _includes;
        public DNonClusteredIndexAttribute(string[] fields, string[] includes = null)
        {
            this._fields = fields;
            this._includes = includes;
        }
    }
}
