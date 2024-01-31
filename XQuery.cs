using CoffeDX.Database;
using CoffeDX.Query.Mapping;
using CoffeDX.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Reflection;
using System.Text;

namespace CoffeDX
{
    public delegate void DgWhere(SubWhere query);
    public class XQuery : ISelect
    {
        private SelectQuery _select;
        private UpdateQuery _update;
        private string tableName { get; set; } = null;
        public XQuery()
        {
            _select = new SelectQuery(null);
        }
        public XQuery(object table)
        {
            if (typeof(string) == table.GetType()) this.tableName = table.ToString();
            else if (table is Type)
            {
                var _type = (table as Type);

                this.tableName = "t_" + _type.Name;
                if (Attribute.IsDefined(_type, typeof(DEntityAttribute)))
                {
                    var prop = _type.GetCustomAttribute<DEntityAttribute>();
                    if (!string.IsNullOrWhiteSpace(prop.Name)) this.tableName = "t_" + prop.Name;
                }
            }
            else
            {
                this.tableName = "t_" + table.GetType().Name;
                if (Attribute.IsDefined(table.GetType(), typeof(DEntityAttribute)))
                {
                    var prop = table.GetType().GetCustomAttribute<DEntityAttribute>();
                    if (!string.IsNullOrWhiteSpace(prop.Name)) this.tableName = "t_" + prop.Name;
                }
            }
            if (_select == null)
                _select = new SelectQuery(this.tableName);
        }

        public static DataTable ExecTable(string _query)
        {
            return SQLServer.getConnection(conn =>
            {
                DataTable result = new DataTable();
                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    result.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
                return result;
            });
        }
        public static int ExecNon(string _query)
        {
            return SQLServer.getConnection(conn =>
            {

                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    return cmd.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
                return 0;
            });
        }
        public static object ExecScaller(string _query)
        {
            return SQLServer.getConnection(conn =>
            {

                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    return cmd.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    return null;
                }
            });
        }
        public static SqlDataReader ExecReader(string _query)
        {
            return SQLServer.getConnection(conn =>
            {
                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    return cmd.ExecuteReader();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    return null;
                }
            });
        }
        public ISelect Select(params string[] fields)
        {
            if (_select == null)
                _select = new SelectQuery(tableName);
            _select.select(fields);
            return this;
        }
        public ISelect From(params string[] tables)
        {
            if (_select == null)
                _select = new SelectQuery(tableName);
            _select.from(tables);
            return this;
        }
        public ISelect Set(params string[] fields)
        {
            if (_update == null) _update = new UpdateQuery(tableName);
            _update.Set(fields);
            return this;
        }
        public ISelect Exclude(params string[] fields)
        {
            if (_select == null) _select = new SelectQuery(tableName);
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
                    if (conn == null) return table;
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    table.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
                return table;
            });
        }
        public int Update(object model = null)
        {
            if (_update == null) _update = new UpdateQuery(model);
            else if (model != null) _update.SetModel(model);

            if (!string.IsNullOrWhiteSpace(this.tableName)) _update.table = this.tableName;
            //_update.table = this.tableName;
            var _query = _update.GetQuery(_select.whereList.ToString());
            return SQLServer.getConnection(conn =>
            {
                var affectedRows = 0;
                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    var lst = _update.GetParams();
                    for (int i = 0; i < lst.Count; i++) cmd.Parameters.AddWithValue(lst.GetKey(i).ToString(), lst[lst.GetKey(i).ToString()] ?? DBNull.Value);
                    affectedRows = cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }

                return affectedRows;
            });
        }
        public long Insert(object model)
        {
            InsertQuery _insert = new InsertQuery(model);
            if (!string.IsNullOrWhiteSpace(this.tableName)) _insert.table = this.tableName;
            var affectedRows = -1;
            var _query = _insert.GetQuery();
            return SQLServer.getConnection(conn =>
            {

                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    var lst = _insert.GetParams();
                    for (int i = 0; i < lst.Count; i++) cmd.Parameters.AddWithValue(lst.GetKey(i).ToString(), lst[lst.GetKey(i).ToString()] ?? DBNull.Value);

                    if (_query.Contains("Inserted"))
                    {
                        object result = cmd.ExecuteScalar();
                        affectedRows = DConvert.ToInt(result);
                    }
                    else
                    {
                        affectedRows = cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }

                return affectedRows;
            });
        }


        public int InsertTable(DataTable table)
        {
            string message = "";
            if (table == null || table.Rows.Count == 0)
            {
                message = "لايوجد سجلات لاضافتها";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(table.TableName))
            {
                message = "يجب تحديد اسم جدول";
                throw new Exception(message);
                return 0;
            }
            return SQLServer.getConnection(@conn =>
            {
                try
                {
                    SqlBulkCopy sqlBulkCopy = new SqlBulkCopy((SqlConnection)@conn);
                    sqlBulkCopy.DestinationTableName = table.TableName;
                    sqlBulkCopy.WriteToServer(table);
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }

                return 1;
            });
        }

        public int InsertList<T>(List<T> list, string tableName)
        {
            try
            {
                DataTable table = new DataTable();

                PropertyInfo[] columns = typeof(T).GetProperties();
                foreach (PropertyInfo item in columns)
                {
                    table.Columns.Add(item.Name, item.PropertyType);
                }

                foreach (T item in list)
                {
                    DataRow dr = table.NewRow();
                    foreach (PropertyInfo col in columns)
                    {
                        dr[col.Name] = col.GetValue(item);
                    }
                }

                table.TableName = tableName;

                string message = "";
                if (table == null || table.Rows.Count == 0)
                {
                    message = "لايوجد سجلات لاضافتها";
                    return 0;
                }

                if (string.IsNullOrWhiteSpace(table.TableName))
                {
                    message = "يجب تحديد اسم جدول";
                    throw new Exception(message);
                    return 0;
                }
                return SQLServer.getConnection(@conn =>
                {
                    SqlBulkCopy sqlBulkCopy = new SqlBulkCopy((SqlConnection)@conn);
                    sqlBulkCopy.DestinationTableName = table.TableName;
                    sqlBulkCopy.WriteToServer(table);
                    return 1;
                });
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                return 0;
            }
        }
        public int Delete(object model = null)
        {
            DeleteQuery _delete = new DeleteQuery(model ?? this.tableName);
            if (!string.IsNullOrWhiteSpace(this.tableName)) _delete.table = this.tableName;

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
                    throw new Exception(ex.Message);
                    return -1;
                }
            });
            return 0;
        }

        public ISelect Between(string field, object value1, object value2)
        {

            if (value1 is string || value1 is DateTime || value1 is DateTime? || value1 is SqlDateTime || value1 is SqlDateTime?)
            {
                value1 = $"'{value1}'";
                value2 = $"'{value2}'";
            }
            _select.whereList.Append($"{field} BETWEEN {value1} AND {value2}");
            return this;
        }

        public ISelect Join(object table, string field1, string field2)
        {
            string _table = "";

            if (typeof(string) == table.GetType()) _table = table.ToString();
            else if (table is Type)
            {
                var _type = (table as Type);

                _table = "t_" + _type.Name;
                if (Attribute.IsDefined(_type, typeof(DEntityAttribute)))
                {
                    var prop = _type.GetCustomAttribute<DEntityAttribute>();
                    if (!string.IsNullOrWhiteSpace(prop.Name)) _table = "t_" + prop.Name;
                }
            }
            else
            {
                _table = "t_" + table.GetType().Name;
                if (Attribute.IsDefined(table.GetType(), typeof(DEntityAttribute)))
                {
                    var prop = table.GetType().GetCustomAttribute<DEntityAttribute>();
                    if (!string.IsNullOrWhiteSpace(prop.Name)) _table = "t_" + prop.Name;
                }
            }

            //_select.tables.Add(_table);
            _select.innerJoinList.Append($" Join {_table} ON {field1}={field2}");
            return this;
        }
        public ISelect LeftJoin(object table, string field1, string field2)
        {
            string _table = "";

            if (typeof(string) == table.GetType()) _table = table.ToString();
            else if (table is Type)
            {
                var _type = (table as Type);

                _table = "t_" + _type.Name;
                if (Attribute.IsDefined(_type, typeof(DEntityAttribute)))
                {
                    var prop = _type.GetCustomAttribute<DEntityAttribute>();
                    if (!string.IsNullOrWhiteSpace(prop.Name)) _table = "t_" + prop.Name;
                }
            }
            else
            {
                _table = "t_" + table.GetType().Name;
                if (Attribute.IsDefined(table.GetType(), typeof(DEntityAttribute)))
                {
                    var prop = table.GetType().GetCustomAttribute<DEntityAttribute>();
                    if (!string.IsNullOrWhiteSpace(prop.Name)) _table = "t_" + prop.Name;
                }
            }

            _select.leftJoinList.Append($" Left Join {_table} ON {field1}={field2}");
            return this;
        }

        public IWhere Where(string key, object value)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" Where ");
            else _select.whereList.Append(" And ");
            string vStr = "";
            if (value.GetType() == typeof(string)) vStr = $"'{value}'"; else vStr = $"{value}";
            _select.whereList.Append($"{key}={vStr}");
            return this;
        }
        public IWhere Where(string query)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" Where ");
            else _select.whereList.Append(" And ");
            _select.whereList.Append(query);
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
            if (value.GetType() == typeof(string)) vStr = $"'{value}'"; else vStr = $"{value}";
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

        public object Max(string fieldName, object @default)
        {
            if (_select == null) _select = new SelectQuery(tableName);
            _select.select($"IIF(MAX({fieldName}) IS NULL,0,MAX({fieldName}))");

            string _query = _select.GetQuery();
            return SQLServer.getConnection(conn =>
            {
                try
                {
                    DataTable table = new DataTable();
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    return cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    return 0;
                }

            });
        }
        public DataRow First()
        {
            if (_select == null) _select = new SelectQuery(tableName);

            string _query = _select.GetQueryFirst();
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
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
                if (table.Rows.Count == 0) table.Rows.Add();
                return table.Rows[0];
            });
        }
        public long Count()
        {
            string _query = _select.GetQueryCount();
            return SQLServer.getConnection(conn =>
            {
                long count = 0;
                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    count = DConvert.ToLong(cmd.ExecuteScalar());
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }

                return 0;
            });
        }

        public T First<T>()
        {
            if (_select == null) _select = new SelectQuery(tableName);
            T instance = Activator.CreateInstance<T>();
            string _query = _select.GetQueryFirst();
            return SQLServer.getConnection(conn =>
            {
                DataTable table = new DataTable();

                try
                {
                    var cmd = new SqlCommand(_query, (SqlConnection)conn);
                    table.Load(cmd.ExecuteReader());
                    if (table.Rows.Count == 0) return instance;
                    return DConvert.ToEntity<T>(table.Rows[0]);
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }


                return instance;
            });
        }

        public IWhere OrderBy(params string[] fields)
        {
            if (_select == null) _select = new SelectQuery(this.tableName);
            _select.OrderBy(fields);
            return this;
        }

        private class SelectQuery
        {
            public List<string> tables = new List<string>();
            public StringBuilder innerJoinList = new StringBuilder();
            public StringBuilder leftJoinList = new StringBuilder();
            public StringBuilder whereList = new StringBuilder();

            public List<string> _exclude = new List<string>();

            public List<string> orderByList = new List<string>();


            public SelectQuery(string table)
            {
                this.tables.Add(table);
            }
            public SelectQuery from(params string[] tables)
            {
                this.tables.AddRange(tables);
                return this;
            }
            private List<string> fields = new List<string>();
            public SelectQuery select(params string[] fields)
            {
                this.fields.AddRange(fields);
                return this;
            }
            public SelectQuery OrderBy(params string[] fields)
            {
                orderByList.AddRange(fields);
                return this;
            }
            public string GetQuery()
            {
                if (this.fields.Count == 0) this.fields.Add("*");
                return $"SELECT {string.Join(",", this.fields)} FROM {string.Join(",", tables)} {innerJoinList} {leftJoinList} {whereList} {(orderByList.Count == 0 ? "" : "ORDER BY")} {string.Join(",", orderByList)}";
            }
            public string GetQueryFirst()
            {
                if (this.fields.Count == 0) this.fields.Add("*");
                return $"SELECT TOP 1 {string.Join(",", this.fields)} FROM {string.Join(",", tables)} {innerJoinList} {leftJoinList} {whereList}";
            }
            public string GetQueryCount()
            {
                return $"SELECT count(*) AS rows_count FROM {string.Join(",", tables)} {innerJoinList} {leftJoinList} {whereList}";
            }
        }
        private class UpdateQuery
        {
            public string table { get; set; }
            private List<string> fields = new List<string>();
            public object model { get; set; }
            private string pK;
            SortedList paramsList = new SortedList();
            public UpdateQuery Set(params string[] args)
            {
                this.fields.AddRange(args);
                return this;
            }
            public void SetModel(object model)
            {
                this.model = model;
                if (model == null) return;
                if (model is string) this.table = model.ToString();
                else
                {
                    if (Attribute.IsDefined(model.GetType(), typeof(DEntityAttribute)))
                    {
                        var attr = model.GetType().GetCustomAttribute<DEntityAttribute>();
                        if (!string.IsNullOrWhiteSpace(attr.Name)) this.table = "t_" + attr.Name;
                        else this.table = "t_" + this.model.GetType().Name;
                    }
                    foreach (var item in model.GetType().GetProperties())
                    {
                        if (Attribute.IsDefined(item, typeof(DPreventUpdateAttribute))) continue;


                        object value = item.GetValue(model);
                        if (Attribute.IsDefined(item, typeof(DPrimaryKeyAttribute)))
                        {
                            pK = $"WHERE {item.Name}=@{item.Name}";
                        }
                        if (Attribute.IsDefined(item, typeof(DForeignKeyAttribute)))
                        {
                            var number = DConvert.ToLong(value, 0);
                            if (number <= 0) value = null;
                        }
                        paramsList.Add($"@{item.Name}", value);
                        if (Attribute.IsDefined(item, typeof(DIncrementalAttribute))) continue;
                        fields.Add($"{item.Name}=@{item.Name}");
                    }
                }


            }
            public UpdateQuery(object model)
            {
                SetModel(model);
            }
            public SortedList GetParams() => this.paramsList;
            public string GetQuery(string whereList)
            {
                if (this.fields.Count == 0) return "";
                var privateWh = whereList;
                if (string.IsNullOrWhiteSpace(whereList)) privateWh = pK;
                return $"UPDATE {this.table} SET {string.Join(",", this.fields)} {privateWh}";
            }
        }
        private class InsertQuery
        {
            public string table { get; set; }
            private List<string> keys = new List<string>();
            private List<string> values = new List<string>();

            private List<string> outFileds = new List<string>();
            public object model { get; set; }
            private SortedList paramsList = new SortedList();
            //private string pK;

            public InsertQuery(object model)
            {
                this.model = model;
                if (model.GetType() == typeof(string)) this.table = "t_" + model.ToString(); else this.table = "t_" + model.GetType().Name;
                if (Attribute.IsDefined(model.GetType(), typeof(DEntityAttribute)))
                {
                    var attr = model.GetType().GetCustomAttribute<DEntityAttribute>();
                    if (!string.IsNullOrWhiteSpace(attr.Name)) this.table = "t_" + attr.Name;
                    else this.table = "t_" + model.GetType().Name;
                }
                foreach (var item in model.GetType().GetProperties())
                {
                    object value = item.GetValue(model);
                    if (Attribute.IsDefined(item, typeof(DIncrementalAttribute)))
                    {
                        outFileds.Add("Inserted." + item.Name);
                        continue;
                    }
                    if (Attribute.IsDefined(item, typeof(DForeignKeyAttribute)))
                    {
                        var number = DConvert.ToLong(value, 0);
                        if (number <= 0) value = null;
                    }
                    //if (fV == null || fV.ToString().Length == 0) continue;
                    keys.Add($"{item.Name}");
                    values.Add($"@{item.Name}");
                    paramsList.Add($"@{item.Name}", value);
                }
            }
            public string GetQuery()
            {
                if (this.keys.Count == 0) return "";

                var _insterted = "";
                if (outFileds.Count > 0)
                {
                    _insterted += $" OUTPUT {string.Join(",", outFileds)}";
                }

                return $"INSERT INTO {this.table}({string.Join(",", this.keys)}) {_insterted} VALUES ({string.Join(",", this.values)})";
            }
            public SortedList GetParams() => this.paramsList;
        }
        private class DeleteQuery
        {
            public string table { get; set; }
            public object model { get; set; }
            public string pk = null;
            public DeleteQuery(object model)
            {
                this.model = model;
                if (model is Type)
                {
                    this.table = "t_" + ((Type)model).Name;
                }
                else
                {
                    if (model.GetType() == typeof(string)) this.table = model.ToString(); else this.table = "t_" + model.GetType().Name;
                    if (Attribute.IsDefined(model.GetType(), typeof(DEntityAttribute)))
                    {
                        var attr = model.GetType().GetCustomAttribute<DEntityAttribute>();
                        if (!string.IsNullOrWhiteSpace(attr.Name)) this.table = "t_" + attr.Name;
                        else this.table = "t_" + model.GetType().Name;
                    }
                }
                foreach (PropertyInfo prop in model.GetType().GetProperties())
                {
                    if (Attribute.IsDefined(prop, typeof(DPrimaryKeyAttribute)))
                    {
                        pk = prop.Name + "=" + DConvert.ToSqlValue(prop.GetValue(model));
                    }
                }
            }
            public string GetQuery(string whereList)
            {
                string whPrivate = whereList + "";
                if (!string.IsNullOrWhiteSpace(pk))
                {
                    whPrivate = $"WHERE {pk}";
                }
                else if (string.IsNullOrWhiteSpace(whereList))
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
        ISelect Join(object table, string field1, string field2);
        ISelect LeftJoin(object table, string field1, string field2);
        ISelect From(params string[] tables);
        ISelect Select(params string[] tables);

    }
    public interface IWhere : SubWhere
    {
        DataTable Get();
        DataRow First();
        IWhere OrderBy(params string[] fields);
        IWhere Where(string query);
        T First<T>();
        int Update(object model = null);
        int Delete(object model = null);
        object Max(string fieldName, object @default);
        long Count();

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
