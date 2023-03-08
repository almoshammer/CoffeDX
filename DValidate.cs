using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX
{
    public static class DValidate
    {
        public static bool Exists(PropertyInfo pi, DataRow dr)
        {
            foreach (DataColumn dc in dr.Table.Columns)
            {
                if (dc.ColumnName.ToLower() == pi.Name.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsNumber(object value)
        {
            return !(value == null || value.ToString().Length == 0 || value.ToString().Length > 330);
        }
        public static bool InArray(string value, params string[] list)
        {
            if (value == null || list == null || list.Length == 0) return false;
            foreach (string v in list)
            {
                if (v.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
