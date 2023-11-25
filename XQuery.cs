using CoffeDX.Database;
using CoffeDX.Query.Mapping;
using CoffeDX.Test;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoffeDX
{
    public delegate void DgWhere(SubWhere query);
    public class XQuery : ISelect
    {
        private static SelectQuery _select;
        private string tableName { get; set; } = null;
        public XQuery()  {  }
        public XQuery(object table)
        {
            if (typeof(string) == table.GetType()) this.tableName = table.ToString();
            else
            {
                this.tableName = table.GetType().Name;
                if (Attribute.IsDefined(table.GetType(), typeof(DEntityAttribute)))
                {
                    var prop = table.GetType().GetCustomAttribute<DEntityAttribute>();
                    if (prop.Name != null && prop.Name.Length > 0) this.tableName = prop.Name;
                }
            }
        }

        public ISelect Select(params string[] fields)
        {
            if (_select == null)
                _select = new SelectQuery(tableName);
            _select.select(fields);
            return this;
        }
        public DataTable Get()
        {
            if (_select == null) _select = new SelectQuery(tableName);

            string _query = _select.GetQuery();
            return SQLServer.getConnection(conn =>
            {
                DataTable table = new DataTable();
                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    table.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                return table;
            });
        }
        public int Update(object model)
        {
            UpdateQuery _update = new UpdateQuery(model);
            var _query = _update.GetQuery(_select.whereList.ToString());

            return SQLServer.getConnection(conn =>
            {
                try
                {
                    var affectedRows = new SqlCommand(_query, (SqlConnection)conn).ExecuteNonQuery();
                    return affectedRows;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return -1;
                }
            });
        }
        public static int Delete(object model)
        {
            DeleteQuery _delete = new DeleteQuery(model);
            var _query = _delete.GetQuery(_select.whereList.ToString());

            SQLServer.getConnection<int>(conn =>
            {
                try
                {
                    var affectedRows = new SqlCommand(_query, (SqlConnection)conn).ExecuteNonQuery();
                    return affectedRows;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return -1;
                }
            });
            return 0;
        }
        public ISelect Join(string table, string field1, string field2)
        {
            _select.tables.Add(table);
            _select.innerJoinList.Append($" Join {table} ON {field1}={field2}");
            return this;
        }
        public ISelect LeftJoin(string table, string field1, string field2)
        {
            _select.tables.Add(table);
            _select.leftJoinList.Append($" Left Join {table} ON {field1}={field2}");
            return this;
        }

        public IWhere Where(string key, object value)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" Where ");
            else _select.whereList.Append(" And ");
            string vStr = "";
            if (value.GetType() == typeof(string)) vStr = $"'{value}'";
            _select.whereList.Append($"{key}={vStr}");
            return this;
        }
        public IWhere Where(DgWhere wh)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" Where ");
            else _select.whereList.Append(" And ");
            _select.whereList.Append("(");
            wh(this);
            _select.whereList.Append(")");
            return this;
        }
        public IWhere OrWhere(string key, object value)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" Where ");
            else _select.whereList.Append(" Or ");
            string vStr = "";
            if (value.GetType() == typeof(string)) vStr = $"'{value}'";
            _select.whereList.Append($"{key}={vStr}");
            return this;
        }
        public IWhere OrWhere(DgWhere wh)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" Where ");
            else _select.whereList.Append(" Or ");
            _select.whereList.Append("(");
            wh(this);
            _select.whereList.Append(")");
            return this;
        }

        public DataRow First()
        {
            if (_select == null) _select = new SelectQuery(tableName);

            string _query = _select.GetQuery();
            return SQLServer.getConnection(conn =>
            {
                DataTable table = new DataTable();
                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    table.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                if (table.Rows.Count == 0) table.Rows.Add();
                return table.Rows[0];
            });
        }

        public T First<T>()
        {
            if (_select == null) _select = new SelectQuery(tableName);

            T instance = Activator.CreateInstance<T>();

            string _query = _select.GetQuery();
            return SQLServer.getConnection(conn =>
            {
                DataTable table = new DataTable();
                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    table.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                if (table.Rows.Count == 0) table.Rows.Add();
                DConvert.ToEntity<T>(table.Rows[0]);
                return instance;
            });
        }

        private class SelectQuery
        {
            public List<string> tables = new List<string>();
            public StringBuilder innerJoinList = new StringBuilder();
            public StringBuilder leftJoinList = new StringBuilder();
            public StringBuilder whereList = new StringBuilder();
            public SelectQuery(string table)
            {
                this.tables.Add(table);
            }
            private List<string> fields = new List<string>();
            public SelectQuery select(params string[] fields)
            {
                this.fields.AddRange(fields);
                return this;
            }
            public string GetQuery()
            {
                if (this.fields.Count == 0) this.fields.Add("*");
                return $"SELECT {string.Join(",", this.fields)} FROM {string.Join(",", tables)} {innerJoinList} {leftJoinList} {whereList}";
            }
        }
        private class UpdateQuery
        {
            private string table { get; set; }
            private List<string> fields = new List<string>();
            public object model { get; set; }
            private string pK;

            public UpdateQuery(object model)
            {
                this.model = model;
                if (model.GetType() == typeof(string)) this.table = model.ToString(); else this.table = model.GetType().Name;
                if (Attribute.IsDefined(model.GetType(), typeof(DEntityAttribute)))
                {
                    var attr = model.GetType().GetCustomAttribute<DEntityAttribute>();
                    if (attr.Name != null || attr.Name.Length > 0) this.table = attr.Name;
                }
                foreach (var item in model.GetType().GetProperties())
                {
                    if (Attribute.IsDefined(item, typeof(DPreventUpdateAttribute))) continue;

                    if (Attribute.IsDefined(item, typeof(DPrimaryKeyAttribute)))
                    {
                        var pkVal = item.GetValue(model);
                        if (pkVal.GetType() == typeof(string)) pkVal = $"'{pkVal}'";
                        pK = $"WHERE {item.Name}={pkVal}";
                    }
                    if (Attribute.IsDefined(item, typeof(DIncrementalAttribute))) continue;
                    string fieldValue;
                    object fV = item.GetValue(model);
                    if (fV == null || fV.ToString().Length == 0) continue;

                    if (fV.GetType() == typeof(string))
                    {
                        fieldValue = $"'{fV.ToString()}'";
                    }
                    else fieldValue = fV.ToString();
                    fields.Add($"{item.Name}={fieldValue}");
                }
            }
            public string GetQuery(string whereList)
            {
                if (this.fields.Count == 0) return "";
                var privateWh = whereList;
                if (whereList == null || whereList.Length == 0) privateWh = pK;
                return $"UPDATE {this.table} SET {string.Join(",", this.fields)} {privateWh}";
            }
        }
        private class InsertQuery
        {
            private string table { get; set; }
            private List<string> keys = new List<string>();
            private List<string> values = new List<string>();
            public object model { get; set; }
            private string pK;

            public InsertQuery(object model)
            {
                this.model = model;
                if (model.GetType() == typeof(string)) this.table = model.ToString(); else this.table = model.GetType().Name;
                if (Attribute.IsDefined(model.GetType(), typeof(DEntityAttribute)))
                {
                    var attr = model.GetType().GetCustomAttribute<DEntityAttribute>();
                    if (attr.Name != null || attr.Name.Length > 0) this.table = attr.Name;
                }
                foreach (var item in model.GetType().GetProperties())
                {
                    if (Attribute.IsDefined(item, typeof(DIncrementalAttribute))) continue;
                    string fieldValue;
                    object fV = item.GetValue(model);
                    if (fV == null || fV.ToString().Length == 0) continue;

                    if (fV.GetType() == typeof(string))
                    {
                        fieldValue = $"'{fV}'";
                    }
                    else fieldValue = fV.ToString();

                    keys.Add($"{item.Name}");
                    values.Add($"{fieldValue}");
                }
            }
            public string GetQuery()
            {
                if (this.keys.Count == 0) return "";
                return $"INSERT INTO {this.table}({string.Join(",", this.keys)}) VALUES ({string.Join(",", this.values)})";
            }
        }
        private class DeleteQuery
        {
            public string table { get; set; }
            public object model { get; set; }
            public DeleteQuery(object model)
            {
                this.model = model;
                if (model.GetType() == typeof(string)) this.table = model.ToString(); else this.table = model.GetType().Name;
            }
            public string GetQuery(string whereList)
            {
                string whPrivate = whereList + "";
                if (whereList == null && whereList.Length == 0)
                {
                    foreach (var item in this.model.GetType().GetProperties())
                    {
                        if (Attribute.IsDefined(item, typeof(DPrimaryKeyAttribute)))
                        {
                            var fVal = item.GetValue(this.model);
                            if (fVal.GetType() == typeof(string)) fVal = $"'{fVal}'";
                            whPrivate = $"WHERE {item.Name}={fVal}";
                        }
                    }
                }
                return $"DELETE FROM {table} {whPrivate}";
            }

        }

        public class Pagination
        {
            public int page { get; set; } = 1;
            public int total { get; set; } = 0;
            public int pageSize { get; set; } = 0;
            public virtual DataTable Next()
            {
                DataTable dt = new DataTable();
                return dt;
            }
            public virtual DataTable Prev()
            {
                DataTable dt = new DataTable();
                return dt;
            }
            public virtual DataTable First()
            {
                DataTable dt = new DataTable();
                return dt;
            }
            public virtual DataTable Last()
            {
                DataTable dt = new DataTable();
                return dt;
            }
        }
    }

    public interface ISelect : IWhere
    {
        ISelect Join(string table, string field1, string field2);
        ISelect LeftJoin(string table, string field1, string field2);

    }
    public interface IWhere : SubWhere
    {
        DataTable Get();
        DataRow First();
        T First<T>();
        int Update(object model);
    }
    public interface SubWhere
    {
        IWhere Where(DgWhere wh);
        IWhere Where(string key, object value);
        IWhere OrWhere(DgWhere wh);
        IWhere OrWhere(string key, object value);
    }
    public interface IUpdate : IWhere
    {
    }
    public interface IDelete
    {
    }
}
