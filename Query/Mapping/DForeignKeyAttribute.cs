using System;

namespace CoffeDX.Query.Mapping
{
    public class DForeignKeyAttribute : Attribute
    {
        public DForeignKeyAttribute(Type ParentModel)
        {
            this.ParentModel = ParentModel;
            this.ParentKey = null;
            this.constraint_event = ON_CONSTRAINT_EVENT.NOACTION;
        }
        public DForeignKeyAttribute(Type ParentModel, ON_CONSTRAINT_EVENT constraint_event)
        {
            this.ParentModel = ParentModel;
            this.ParentKey = null;
            this.constraint_event = constraint_event;
        }
        public DForeignKeyAttribute(Type ParentModel, string ParentKey)
        {
            this.ParentModel = ParentModel;
            this.ParentKey = ParentKey;
            this.constraint_event = ON_CONSTRAINT_EVENT.NOACTION;
        }
    
   
        public ON_CONSTRAINT_EVENT constraint_event { get; set; }
        public Type ParentModel { get; set; }
        public string ParentKey { get; set; }
    }
}
