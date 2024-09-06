using CoffeDX.Query.Mapping;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
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
            return CoffeDX.Database.Oracle.getOnlineConnection(conn =>
            {
                DataTable result = new DataTable();
                try
                {
                    var command = new OracleCommand(_query, (OracleConnection)conn);
                    result.Load(command.ExecuteReader());
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
            return CoffeDX.Database.Oracle.getOnlineConnection(conn =>
            {
                try
                {
                    var command = new OracleCommand(_query, (OracleConnection)conn);
                    command.CommandTimeout = 120;
                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
                return 0;
            });
        }
        public static object ExecScalar(string _query)
        {
            return CoffeDX.Database.Oracle.getOnlineConnection(conn =>
            {

                try
                {
                    var command = new OracleCommand(_query, (OracleConnection)conn);
                    return command.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    return null;
                }
            });
        }
        public static void ExecReader(string _query, DVoid<OracleDataReader> reader)
        {
            CoffeDX.Database.Oracle.getOnlineConnection(conn =>
           {
               try
               {
                   var command = new OracleCommand(_query, (OracleConnection)conn);
                   reader(command.ExecuteReader());
               }
               catch (Exception ex)
               {
                   System.Windows.Forms.MessageBox.Show(ex.Message);
               }
               return "";
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
            var table = new DataTable();
            try
            {
                if (_select == null) _select = new SelectQuery(tableName);
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_select.GetQuery(), connection);
                    table.Load(command.ExecuteReader());
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
            }
            return table;
        }
        public int Update(object model = null)
        {
            if (_update == null) _update = new UpdateQuery(model);
            else if (model != null) _update.SetModel(model);

            if (!string.IsNullOrWhiteSpace(this.tableName)) _update.table = this.tableName;
            //_update.table = this.tableName;

            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_update.GetQuery(_select.whereList.ToString()), connection);
                    var lst = _update.GetParams();
                    if(lst!=null && lst.Count > 0) command.BindByName = true;
                    for (int i = 0; i < lst.Count; i++)
                        command.Parameters.Add(lst.GetKey(i).ToString(), lst[lst.GetKey(i).ToString()] ?? DBNull.Value);
                    command.CommandTimeout = 120;
                    return command.ExecuteNonQuery();
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return -1;
            }
        }
        public long Insert(object model)
        {
            var _insert = new InsertQuery(model);
            if (!string.IsNullOrWhiteSpace(this.tableName)) _insert.table = this.tableName;
            long output = 0;
            var _query = _insert.GetQuery();

            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_query, connection);
                    var lst = _insert.GetParams();
                    command.BindByName = true;

                    for (int i = 0; i < lst.Count; i++) command.Parameters.Add(lst.GetKey(i).ToString(), lst[lst.GetKey(i).ToString()]);

                    if (_query.Contains("RETURNING"))
                    {
                        foreach (var item in _insert.outFileds) command.Parameters.Add(item, OracleDbType.Int64, ParameterDirection.Output);
                        command.ExecuteNonQuery();
                        output = DConvert.ToLong(command.Parameters[_insert.outFileds[0]].Value);
                    }
                    else output = command.ExecuteNonQuery();//Affected Rows
                    return output;
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return output;
            }
        }


        public int InsertTable(DataTable table)
        {
            var message = "";
            if (table == null || table.Rows.Count == 0)
            {
                message = "لايوجد سجلات لاضافتها";
                return 0;
            }

            if (string.IsNullOrWhiteSpace(table.TableName))
            {
                message = "يجب تحديد اسم جدول";
                throw new Exception(message);
            }
            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var sqlBulkCopy = new OracleBulkCopy(connection);
                    sqlBulkCopy.DestinationTableName = table.TableName;
                    sqlBulkCopy.WriteToServer(table);
                    return table.Rows.Count;
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return 0;
            }
        }

        public int InsertList<T>(List<T> list, string tableName)
        {
            var table = new DataTable();

            var columns = typeof(T).GetProperties();
            foreach (var item in columns)
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

            var message = "";
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

            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var sqlBulkCopy = new OracleBulkCopy(connection);
                    sqlBulkCopy.DestinationTableName = table.TableName;
                    sqlBulkCopy.WriteToServer(table);
                    return 1;
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return 0;
            }

        }
        public int Delete(object model = null)
        {
            DeleteQuery _delete = new DeleteQuery(model ?? this.tableName);
            if (!string.IsNullOrWhiteSpace(this.tableName)) _delete.table = this.tableName;

            var _query = _delete.GetQuery(_select.whereList.ToString());

            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_query, connection);
                    command.CommandTimeout = 120;
                    var affectedRows = new OracleCommand(_query, connection).ExecuteNonQuery();
                    return affectedRows;
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return 0;
            }

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

            if (table is string) _table = table.ToString();
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
            //if (field1.Contains("."))
            //{
            //    field1 = field1.Insert(field1.IndexOf(".") + 1, "\"");
            //    field1 = field1.Insert(field1.Length, "\"");
            //}
            //if (field2.Contains("."))
            //{
            //    field2 = field2.Insert(field2.IndexOf(".") + 1, "\"");
            //    field2 = field2.Insert(field2.Length, "\"");
            //}

            //_select.tables.Add(_table);
            _select.innerJoinList.Append($" Join {_table} ON {field1}={field2}");
            return this;
        }
        public ISelect LeftJoin(object table, string field1, string field2)
        {
            var _table = "";

            if (table is string) _table = table.ToString();
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
            //if (field1.Contains("."))
            //{
            //    field1 = field1.Insert(field1.IndexOf(".") + 1, "\"");
            //    field1 = field1.Insert(field1.Length, "\"");    
            //}
            //if (field2.Contains(".") && !field2.ToLower().Contains("and") && !field2.ToLower().Contains("or"))
            //{
            //    field2 = field2.Insert(field2.IndexOf(".") + 1, "\"");
            //    field2 = field2.Insert(field2.Length, "\"");
            //}
            _select.leftJoinList.Append($" Left Join {_table} ON {field1}={field2}");
            return this;
        }

        public IWhere Where(string key, object value)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" Where ");
            else if (_select.whereList[_select.whereList.Length - 1] != '(') _select.whereList.Append(" And ");

            string vStr = "";
            if (value.GetType() == typeof(string)) vStr = $"'{value}'"; else vStr = $"{value}";
            _select.whereList.Append($"{(key.Contains(".") ? key : "\"" + key + "\"")}={vStr}");
            return this;
        }
        public IWhere Where(string query)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" WHERE ");
            else if (_select.whereList[_select.whereList.Length - 1] != '(') _select.whereList.Append(" And ");
            _select.whereList.Append(query);
            return this;
        }
        public IWhere Where(DgWhere wh)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" WHERE ");
            else if (_select.whereList[_select.whereList.Length - 1] != '(') _select.whereList.Append(" And ");

            _select.whereList.Append("(");
            wh(this);
            _select.whereList.Append(")");
            return this;
        }
        public IWhere OrWhere(string key, object value)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" WHERE ");
            else if (_select.whereList[_select.whereList.Length - 1] != '(') _select.whereList.Append(" OR ");

            var vStr = "";
            if (value.GetType() == typeof(string)) vStr = $"'{value}'"; else vStr = $"{value}";
            _select.whereList.Append($"{(key.Contains(".") ? key : "\"" + key + "\"")}={vStr}");
            return this;
        }
        public IWhere OrWhere(DgWhere wh)
        {
            if (_select.whereList.Length == 0) _select.whereList.Append(" WHERE ");
            else if (_select.whereList[_select.whereList.Length - 1] != '(') _select.whereList.Append(" OR ");

            _select.whereList.Append("(");
            wh(this);
            _select.whereList.Append(")");
            return this;
        }

        public object Max(string fieldName, object @default)
        {
            if (_select == null) _select = new SelectQuery(tableName);
            _select.select($"NVL(MAX({tableName}.\"{fieldName}\"),0)");

            var _query = _select.GetQuery();
            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_query, connection);
                    return command.ExecuteScalar();
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return 0;
            }
        }
        public DataRow First()
        {
            if (_select == null) _select = new SelectQuery(tableName);
            var _query = _select.GetQueryFirst();
            var table = new DataTable();

            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_query, connection);
                    table.Load(command.ExecuteReader());
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
            }
            finally
            {
                if (table.Rows.Count == 0)
                {
                    foreach (DataColumn dc in table.Columns) dc.AllowDBNull = true;
                    table.Rows.Add();
                }
            }

            return table.Rows[0];
        }
        public long Count()
        {
            var _query = _select.GetQueryCount();
            var count = 0l;
            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_query, connection);
                    count = DConvert.ToLong(command.ExecuteScalar());
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return 0;
            }
            return count;
        }
        public object GetValue(string field)
        {
            var _query = _select.GetQueryValue(field);
            object value = null;
            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_query, connection);
                    value = command.ExecuteScalar();
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return 0;
            }
            return value;
        }
        public double GetDouble(string field)
        {
            var _query = _select.GetQueryValue(field);
            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_query, connection);
                    return DConvert.ToDouble(command.ExecuteScalar());
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return 0;
            }
        }
        public T First<T>()
        {
            var table = new DataTable();
            if (_select == null) _select = new SelectQuery(tableName);
            var instance = Activator.CreateInstance<T>();
            var _query = _select.GetQueryFirst();

            try
            {
                using (var connection = new OracleConnection(CoffeDX.Database.Oracle.GetConnectionString()))
                {
                    connection.Open();
                    var command = new OracleCommand(_query, connection);
                    table.Load(command.ExecuteReader());
                    if (table.Rows.Count == 0) return instance;
                    return DConvert.ToEntity<T>(table.Rows[0]);
                }
            }
            catch (OracleException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message, "" + e.Number);
                return instance;
            }
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
                if (table != null && table.Length > 0)
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
                this.fields.AddRange(fields.Select(s => (s.Contains(".") || s.Contains(" ")) ? s : $"\"{s}\""));
                return this;
            }
            public SelectQuery OrderBy(params string[] fields)
            {
                orderByList.AddRange(fields.Select(s => (s.Contains(".") || s.Contains(" ")) ? s : $"\"{s}\""));
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
                return $"SELECT {string.Join(",", this.fields)} FROM {string.Join(",", tables)} {innerJoinList} {leftJoinList} {whereList} {(whereList.Length > 4 ? " AND" : " WHERE")} ROWNUM=1";
            }
            public string GetQueryCount()
            {
                return $"SELECT count(*) AS r_count FROM {string.Join(",", tables)} {innerJoinList} {leftJoinList} {whereList}";
            }
            public string GetQueryValue(string field)
            {
                var f = field.Contains('.') || field.Contains('(') ? field : $"\"{field}\"";
                return $"SELECT {f} FROM {string.Join(",", tables)} {innerJoinList} {leftJoinList} {whereList}";
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
                            pK = $"WHERE \"{item.Name}\"=:{item.Name}";
                        }
                        if (Attribute.IsDefined(item, typeof(DForeignKeyAttribute)))
                        {
                            var number = DConvert.ToLong(value, 0);
                            if (number <= 0) value = null;
                        }

                        if (item.PropertyType == typeof(bool))
                        {
                            paramsList.Add($":{item.Name}", DConvert.ToInt(value));
                            if (Attribute.IsDefined(item, typeof(DIncrementalAttribute))) continue;
                            fields.Add($"\"{item.Name}\"=:{item.Name}");
                            continue;
                        }
                        paramsList.Add($":{item.Name}", value);
                        if (Attribute.IsDefined(item, typeof(DIncrementalAttribute))) continue;
                        fields.Add($"\"{item.Name}\"=:{item.Name}");
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

            public List<string> outFileds = new List<string>();
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
                        outFileds.Add($"{item.Name}");
                        continue;
                    }
                    if (Attribute.IsDefined(item, typeof(DForeignKeyAttribute)))
                    {
                        var number = DConvert.ToLong(value, 0);
                        if (number <= 0) value = null;
                    }
                    //if (fV == null || fV.ToString().Length == 0) continue;
                    keys.Add($"\"{item.Name}\"");
                    values.Add($":{item.Name}");
                    if (item.PropertyType == typeof(bool))
                    {
                        paramsList.Add($":{item.Name}", DConvert.ToInt(value));
                        continue;
                    }
                    paramsList.Add($":{item.Name}", value);
                }
            }
            public string GetQuery()
            {
                if (this.keys.Count == 0) return "";

                var _insterted = "";
                if (outFileds.Count > 0)
                {
                    _insterted += $" RETURNING {string.Join(",", outFileds.Select(s => $"\"{s}\""))} INTO {string.Join(",", outFileds.Select(s => $":{s}"))}";
                }

                return $"INSERT INTO {this.table}({string.Join(",", this.keys)}) VALUES ({string.Join(",", this.values)}) {_insterted}";
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
                    if (Attribute.IsDefined(prop, typeof(DPrimaryKeyAttribute))) pk = $"\"{prop.Name}\"=" + DConvert.ToSqlValue(prop.GetValue(model));
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
                            whPrivate = $"WHERE \"{item.Name}\"={fVal}";
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
        object GetValue(string field);
        double GetDouble(string field);
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
