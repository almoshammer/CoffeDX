﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX
{
    public delegate void DVoid();
    public delegate void DVoid<T>(T value);
    public delegate void DObject(object value);
    public delegate T DObject<T>(object value);
}
