using System;

namespace CoffeDX.Query.Mapping
{
    public class DNonClusteredIndexAttribute : Attribute
    {
        string[] _fields;
        string[] _includes = null;

        public string[] fields
        {
            get => _fields;
            set => _fields = value;
        }
        public string[] includes
        {
            get => _includes;
            set => _includes = value;
        }
        public DNonClusteredIndexAttribute(string[] fields, string[] includes = null)
        {
            this._fields = fields;
            this._includes = includes;
        }
    }
}
