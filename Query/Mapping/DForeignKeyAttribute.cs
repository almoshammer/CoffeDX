using System;

namespace CoffeDX.Query.Mapping
{
    public class DForeignKeyAttribute : Attribute
    {
        public DForeignKeyAttribute(Type ParentModel, string ParentKey)
        {
            this.ParentModel = ParentModel;
            this.ParentKey = ParentKey;
        }
        public DForeignKeyAttribute(Type ParentModel)
        {
            this.ParentModel = ParentModel;
            this.ParentKey = null;
        }
        public Type ParentModel { get; set; }
        public string ParentKey { get; set; }
    }
}
