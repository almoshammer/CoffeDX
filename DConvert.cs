using CoffeDX.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
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
        public static string ToSqlValue(object input)
        {
            if (input == null) return "null";
            if (input.GetType() == typeof(bool)) return DConvert.ToInt(input) + "";
            if (input is string) return $"'{input}'";
            #region DateTime ---condations

            if (input is DateTime)
            {
                if (input.ToString().Contains("01/01/1990")) return "null";
                return "'" + ((DateTime)input).ToString("yyyy-MM-ddTHH:mm:ss") + "'";
            }
            //yyyy-MM-dd HH:mm:ss.fff
            if (input is DateTime?)
            {
                if (input.ToString().Contains("01/01/1990")) return "null";
                return "'" + ((DateTime?)input).Value.ToString("yyyy-MM-ddTHH:mm:ss") + "'";
            }
            if (input is SqlDateTime)
            {
                if (input.ToString().Contains("01/01/1990")) return "null";
                return "'" + ((SqlDateTime)input).Value.ToString("yyyy-MM-ddTHH:mm:ss") + "'";
            }
            if (input is SqlDateTime?)
            {
                if (input.ToString().Contains("01/01/1990")) return "null";
                return "'" + ((SqlDateTime?)input).Value.Value.ToString("yyyy-MM-ddTHH:mm:ss") + "'";
            }
            #endregion
            if (input is int? || input is long? || input is decimal? || input is double?)
            {
                if (DConvert.ToInt(input.ToString()) == 0) return "null";
            }

            if (input is byte[])
            {
                return "0x" + BitConverter.ToString(((byte[])input), 0).Replace("-", string.Empty);
            }
            return input.ToString();
        }
        public static void SetColumnsNull(DataTable dataList)
        {
            if (dataList != null || dataList.Columns.Count == 0) return;
            for (int i = 0; i < dataList.Columns.Count; i++)
            {
                dataList.Columns[i].AllowDBNull = true;
            }
        }
        public static int ToInt(object value, int defaultValue = 0)
        {
            if (value != null && value.GetType() == typeof(bool)) return Convert.ToInt16(value);
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return int.Parse(ToNumber(value));
        }
        public static long ToLong(object value, long defaultValue = 0)
        {
            if (value != null && value.GetType() == typeof(bool)) return Convert.ToInt16(value); ;
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return long.Parse(ToNumber(value));
        }
        private static string ToNumber(object value)
        {
            if (value != null && value.GetType() == typeof(bool)) return Convert.ToInt16(value) + "";
            if (!DValidate.IsNumber(value)) return "0";

            value = value.ToString().Trim();
            string serial = "";
            if ((value + "").StartsWith("-")) serial = "-";
            foreach (char c in value.ToString().Trim())
                if (char.IsDigit(c)) serial += c + "";
                else if (c == '.') serial += c + "";
            return (serial.Length == 0 || serial == "-") ? "0" : serial;
        }
        public static double ToDouble(object value, double defaultValue = 0)
        {
            if (value != null && value.GetType() == typeof(bool)) return Convert.ToInt16(value); ;
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return Convert.ToDouble(ToNumber(value));
        }
        public static DateTime ToDateTime(DateTime? date)
        {
            if (date != null && date.Value != DateTime.MinValue)
            {
                return date.Value;
            }
            else
                return new DateTime();
        }
        public static decimal ToDecimal(object value, decimal defaultValue = 0)
        {
            if (value != null && value.GetType() == typeof(bool)) return Convert.ToInt16(value); ;
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return Convert.ToDecimal(ToNumber(value));
        }
        public static float ToFloat(object value, float defaultValue = 0)
        {
            if (value != null && value.GetType() == typeof(bool)) return Convert.ToInt16(value); ;
            if (!DValidate.IsNumber(value))
            {
                return defaultValue;
            }
            return Convert.ToSingle(ToNumber(value));
        }
        public static bool ToBool(object st)
        {
            if (st.GetType() == typeof(bool)) return (bool)st;
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
        public static DataTable ToTable(object obj)
        {
            DataTable table = new DataTable();
            if (obj == null || obj is string) return table;
            foreach (var prop in ((Type)obj).GetProperties())
                table.Columns.Add(prop.Name, prop.GetType());
            return table;
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
