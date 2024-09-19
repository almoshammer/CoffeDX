using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Shared
{
    public static class CheckType
    {
        public static object GetNonNullableValue(object value)
        {
            if (IsNullable(value))
            {
                return ChangeType(value, GetNullableType(value.GetType()));
            }
            return value;
        }
        public static V ChangeType<V>(object value)
        {
            var t = typeof(V);

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return default(V);
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return (V)Convert.ChangeType(value, t);
        }
        public static object ChangeType(object value, Type conversion)
        {
            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return Convert.ChangeType(value, t);
        }
        //=========================================================================================
        public static Type GetNullableType(Type t)
        {
            if (t.IsValueType)
            {
                return typeof(Nullable<>).MakeGenericType(t);
            }
            else
            {
                throw new ArgumentException();
            }
        }
        public static bool IsNullable<V>(V value)
        {
            return Nullable.GetUnderlyingType(typeof(V)) != null;
        }
        public static bool isInt(Type type) => (type == typeof(int) || type == typeof(long) || type == typeof(ushort) || type == typeof(uint));
        public static bool isNullableInt(Type type) => (type == typeof(int?) || type == typeof(long?) || type == typeof(ushort?) || type == typeof(uint));
        public static bool isNullableDecimal(Type type) => (type == typeof(decimal) || type == typeof(SqlDecimal));
        public static bool isNullableDouble(Type type) => (type == typeof(double?) || type == typeof(SqlDouble?));
        public static bool isNullableDateTime(Type type) => (type == typeof(DateTime?));
        public static bool isNullableSqlDateTime(Type type) => (type == typeof(SqlDateTime?));
        public static bool isString(Type type) => type == typeof(string);
        public static bool isLong(Type type) => type == typeof(long) || type == typeof(ulong);
        public static bool isDouble(Type type) => type == typeof(double) || type == typeof(SqlDouble);
        public static bool isFloat(Type type) => type == typeof(float);
        public static bool isDecimal(Type type) => type == typeof(decimal) || type == typeof(SqlDecimal);
        public static bool isDateTime(Type type) => type == typeof(DateTime) || type == typeof(DateTime?);
        public static bool isSqlDateTime(Type type) => type == typeof(SqlDateTime) || type == typeof(SqlDateTime?);
        public static DateTime toDateTime(object obj)
        {
            DateTime result = new DateTime();

            SqlDateTime dt = (SqlDateTime)obj;

            if (obj != null && obj.GetType() == typeof(SqlDateTime) && dt.IsNull == false)
            {
                result = dt.Value;
            }
            return result;
        }
        public static void assignDateTime(SqlDateTime dt, Exec.DGExec action)
        {
            if (dt.IsNull == false) action(null);
        }

        public static SqlDateTime toSqlDateTime(object obj)
        {

            SqlDateTime result = new SqlDateTime();
            if (obj.GetType() == typeof(DateTime))
            {
                DateTime dt = (DateTime)obj;
                result = new SqlDateTime(dt);
            }
            return result;
        }
        public static void setProperyValue(object entity, PropertyInfo prop, object value)
        {
            if (value == null || value == DBNull.Value)
            {
                //set default value
                if (isString(prop.PropertyType)) { prop.SetValue(entity, ""); return; }
                if (isInt(prop.PropertyType) || isDouble(prop.PropertyType) || isDouble(prop.PropertyType) || isFloat(prop.PropertyType))
                { prop.SetValue(entity, 0); return; }

                if (isDateTime(prop.PropertyType))
                { prop.SetValue(entity, new DateTime(1990, 1, 1)); return; }

                if (isSqlDateTime(prop.PropertyType))
                { prop.SetValue(entity, SqlDateTime.Null); return; }
                if (isNullableInt(prop.PropertyType)
                    || isNullableDecimal(prop.PropertyType)
                    || isNullableDouble(prop.PropertyType)
                    || isNullableDateTime(prop.PropertyType)
                    || isNullableSqlDateTime(prop.PropertyType)
                    || (prop.PropertyType == typeof(bool)))
                {
                    prop.SetValue(entity, null); return;
                }
                throw new Exception("نمط بيانات غير معرف");
            }
            else
            {

                if (value.GetType() != prop.PropertyType)
                {
                    if ((isSqlDateTime(prop.PropertyType) || isNullableSqlDateTime(prop.PropertyType))
                        && (isDateTime(value.GetType()) || isNullableDateTime(value.GetType())))
                    {
                        SqlDateTime ndt = new SqlDateTime(DateTime.Parse(value?.ToString()));
                        prop.SetValue(entity, ndt);
                    }
                    else if (prop.PropertyType == typeof(long?)) prop.SetValue(entity, DConvert.ToLong(value));
                    else if (prop.PropertyType == typeof(int?)) prop.SetValue(entity, DConvert.ToInt(value));
                    else if (prop.PropertyType == typeof(double?)) prop.SetValue(entity, DConvert.ToDouble(value));
                    else if (prop.PropertyType == typeof(decimal?)) prop.SetValue(entity, DConvert.ToDecimal(value));
                    else if (prop.PropertyType == typeof(float?)) prop.SetValue(entity, DConvert.ToFloat(value));
                    else
                    {
                        try
                        {
                            object new_obj = ChangeType(value, prop.PropertyType);
                            prop.SetValue(entity, new_obj);
                        }
                        catch
                        {

                        }
                    }
                } else prop.SetValue(entity, value);

                if (value == null) throw new Exception("نمط بيانات غير معرف");
            }
            //int
            //datetime
            //if (prop.PropertyType==typeof(SqlDateTime))
            //{
            //    prop.SetValue(entity,toSqlDateTime(value));
            //    return;
            //}
        }
        public static bool hasProperty(Type type, string name)
        {
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.Name.Equals(name)) return true;
            }
            return false;
        }
    }
}
