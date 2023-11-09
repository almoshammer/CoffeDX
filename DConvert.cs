using CoffeDX.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX
{
    public class DConvert
    {
        public static int ToInt(object value, int defaultValue = 0)
        {
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return int.Parse(ToNumber(value));
        }
        public static long ToLong(object value, long defaultValue = 0)
        {
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return long.Parse(ToNumber(value));
        }
        private static string ToNumber(object value)
        {
            if (!DValidate.IsNumber(value)) return "0";

            value = value.ToString().Trim();
            string serial = "";
            if ((value + "").StartsWith("-")) serial = "-";
            foreach (char c in value.ToString().Trim()) if (char.IsDigit(c)) serial += c + "";
            return (serial.Length == 0 || serial == "-") ? "0" : serial;
        }
        public static double ToDouble(object value, double defaultValue = 0)
        {
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return Convert.ToDouble(ToNumber(value));
        }
        public static decimal ToDecimal(object value, decimal defaultValue = 0)
        {
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return Convert.ToDecimal(ToNumber(value));
        }
        public static bool ToBool(object st)
        {
            if (st == null)
            {
                return false;
            }
            st = st.ToString().Trim().ToLower();
            if (st.ToString() == "1" || st.ToString() == "true")
            {
                return true;
            }

            if (st.ToString().Length == 0)
            {
                return false;
            }

            return Convert.ToBoolean(st);
        }
        public static A ToEntity<A>(DataRow dr)
        {
            A entity = Activator.CreateInstance<A>();
            PropertyInfo[] properties = typeof(A).GetProperties();

            foreach (PropertyInfo prop in properties)
                if (DValidate.Exists(prop, dr)) CheckType.setProperyValue(entity, prop, dr[prop.Name.ToLower()]);
            return entity;
        }
        public static List<object> ToList(DataTable table)
        {
            if (table == null) return null;

            List<object> list = new List<object>();


            foreach (DataRow dr in table.Rows)
            {
                dynamic dvv = new ExpandoObject();
                IDictionary<string, object> d_string = dvv;

                foreach (DataColumn dc in table.Columns)
                {
                    d_string.Add(dc.ColumnName, dr[dc.ColumnName]);
                }
                list.Add(dvv);
            }

            return list;
        }
        public static List<R> ToList<R>(DataTable table)
        {
            if (table == null) return null;

            List<R> list = new List<R>();


            foreach (DataRow dr in table.Rows)
            {
                R entity = Activator.CreateInstance<R>();

                foreach (DataColumn dc in table.Columns)
                {
                    if (CheckType.hasProperty(entity.GetType(), dc.ColumnName))
                    {
                        CheckType.setProperyValue(entity, entity.GetType().GetProperty(dc.ColumnName), dr[dc.ColumnName]);
                    }
                }
                list.Add(entity);
            }

            return list;
        }
        public static System.Drawing.Image ToImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 1) return null;
            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    return System.Drawing.Image.FromStream(ms);
                }
            }
            catch (Exception ex)
            {
                Shared.ExHandler.handle(ex, Shared.ExHandler.ERR.FUN);
                return null;
            }
        }
        /// <summary>
        ///   Convert image To array of bytes
        /// </summary>
        /// <param name="image"></param>
        /// <returns>byte[]</returns>
        public static byte[] ToArray(System.Drawing.Image image)
        {
            if (image == null)
            {
                return new byte[1];
            }

            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, image.RawFormat);
                return ms.ToArray();
            }
        }
    }
}
