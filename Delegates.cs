using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX
{
    public delegate void DVoid();
    public delegate void DObject(object value);
    public delegate T DObjectT<T>(object value);
}
