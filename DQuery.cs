using CoffeDX.Database;
using CoffeDX.Query.Mapping;
using CoffeDX.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace CoffeDX
{
    public class DQuery : Shared.Database, Shared.IQuery
    {

        public enum CRUD { NONE, INSERT, UPDATE, DELETE }
        public enum INC_TYPE { INCLUDE, EXCLOUD }
        public enum QUERY_TYPE { INSERT, UPDATE, DELETE };
        public enum ENTITY_TYPE { ENTITY, TABLE };
        public enum UpdateCheck
        {
            Always,
            Never,
            WhenChanged
        }
        private object entity;

        private readonly ENTITY_TYPE entity_type;
        private readonly INC_TYPE inc_type;
        private string table_name;
        private Condition condition;

        private string[] columns;

        List<Parameter> paramsList = new List<Parameter>();
        List<string> generatedFields = new List<string>();
        public DQuery(object entity, INC_TYPE inc_type = INC_TYPE.INCLUDE, params string[] columns)
        {
            this.inc_type = inc_type;
            this.entity = entity;
            this.columns = columns;

            if (entity.GetType() == typeof(string)) entity_type = ENTITY_TYPE.TABLE;
            else entity_type = ENTITY_TYPE.ENTITY;

            if (entity_type == ENTITY_TYPE.ENTITY)
            {
                if (Attribute.IsDefined(entity.GetType(), typeof(DEntityAttribute)))
                {
                    this.table_name = entity.GetType().GetCustomAttribute<DEntityAttribute>(false).Name;
                    if (this.table_name == null || this.table_name.Length == 0) return;
                }
                throw new Exception("يجب تحديد اسم للجدول");
            }
            else table_name = entity.ToString();
        }
        private SqlCommand _cmd;
        private SqlCommand cmd
        {
            get
            {
                if (_cmd == null) _cmd = CreateCommand("");
                return _cmd;
            }
        }
        private string getSQLCommand(QUERY_TYPE q_TYPE)
        {
            string query = "";
            string paramters = "";
            string generated_outputs = "";
            bool first_flag = true;

            if (q_TYPE == QUERY_TYPE.INSERT)
            {
                string values = "";
                foreach (Parameter p in paramsList)
                {
                    if (first_flag)
                    {
                        paramters += "@" + p.name;
                        values += p.value;
                        first_flag = false;
                    }
                    else
                    {
                        paramters += ",@" + p.name;
                        values += "," + p.value;
                    }
                }
                if (generatedFields.Count > 0)
                {
                    generated_outputs = " Output ";
                    for (int i = 0; i < generatedFields.Count; i++)
                    {
                        generated_outputs += "Inserted." + generatedFields[i];
                        if ((i + 1) < generatedFields.Count) generated_outputs += ",";
                    }
                }
                query = $"INSERT INTO {table_name} ({paramters.Replace("@", "")}) {generated_outputs} VALUES ({paramters})";
            }
            else if (q_TYPE == QUERY_TYPE.UPDATE)
            {
                //<! -- Begin set parameters -- >
                foreach (Parameter p in paramsList)
                {
                    if (first_flag)
                    {
                        paramters += p.name + "=@" + p.name;
                        first_flag = false;
                    }
                    else
                    {
                        paramters += "," + p.name + "=@" + p.name;
                    }
                }
                query = "UPDATE " + table_name + " SET " + paramters + " " + getConditions();
            }
            else if (q_TYPE == QUERY_TYPE.DELETE)
            {
                query = "DELETE FROM " + table_name + " " + getConditions();
            }

            return query;
        }
        private string getConditions()
        {
            //<! -- Begin set conditions -- >
            string conditions_str = null;
            if (condition != null) conditions_str = condition.getConditions();
            //<! -- check if no conditions specifies -- >
            if (condition == null || conditions_str == null || conditions_str.Length == 0)
            {
                //<! -- Set condition for a primary key field -->
                bool has_primary_key = false;
                foreach (PropertyInfo prop in entity.GetType().GetProperties())
                {
                    if (Attribute.IsDefined(prop, typeof(DColumnAttribute)))
                    {
                        if (prop.GetCustomAttribute<DColumnAttribute>().IsPrimaryKey)
                        {
                            conditions_str = " WHERE " + prop.Name + " = " + prop.GetValue(entity);
                            has_primary_key = true;
                            break;
                        }
                    }
                }
                if (!has_primary_key && (conditions_str == null || conditions_str.Length == 0))
                    throw new Exception("لم يتم تحديد بارامتيرات كشروط ولا يوجد مفتاح أساسي");
            }
            return conditions_str;
        }
        private void setParamters(QUERY_TYPE query_type)
        {
            bool primey_key_flag = false;
            foreach (PropertyInfo prop in entity.GetType().GetProperties())
            {
                //<! -- check if is a primary key -- >
                if (Attribute.IsDefined(prop, typeof(DColumnAttribute)))
                {
                    DColumnAttribute attr = prop.GetCustomAttribute<DColumnAttribute>(false);
                    if (attr.IsDbGenerated)
                    {
                        generatedFields.Add(prop.Name);
                        continue;
                    }
                    else if (attr.IsDiscriminator
                        || (attr.UpdateCheck == UpdateCheck.Never
                        && query_type == QUERY_TYPE.UPDATE)) continue;

                    if (!primey_key_flag)
                    {
                        if (attr.IsPrimaryKey)
                        {
                            primey_key_flag = true;
                            if (attr.IsDbGenerated) continue;
                        }
                    }
                }
                //<! -- end check -- >
                checkIncludeType(prop.Name, () => paramsList.Add(new Parameter(prop.Name, prop.GetValue(entity), false)));
            }

        }
        private void checkIncludeType(string prop_name, Exec.DGFinally action)
        {
            if (this.inc_type == INC_TYPE.INCLUDE)
            {
                if (DValidate.InArray(prop_name, this.columns)) action();
            }
            else if (this.inc_type == INC_TYPE.EXCLOUD)
            {
                if (!DValidate.InArray(prop_name, this.columns)) action();

            }
        }
        public Condition where(string eq1, object eq2)
        {
            if (condition == null) condition = new Condition(this, eq1, "=", eq2);
            return condition;
        }
        public Condition where(string eq1, string op, string eq2)
        {
            if (condition == null) condition = new Condition(this, eq1, op, eq2);
            return condition;
        }
        public void insert(Exec.DGExec action)
        {
            Get_00Execute._0021762(handler =>
             {
                 if (!handler)
                 {
                     return false;
                 }
                 setParamters(QUERY_TYPE.INSERT);

                 cmd.CommandText = getSQLCommand(QUERY_TYPE.INSERT);

                 foreach (Parameter p in paramsList) cmd.Parameters.AddWithValue("@" + p.name, tst(p.value));
                 Result result = new Result();
                 try
                 {
                     result.Value = cmd.ExecuteScalar();
                     result.IsSuccess = true;
                     action?.Invoke(result);
                 }
                 catch (Exception ex)
                 {
                     action?.Invoke(new Result
                     {
                         IsSuccess = false,
                         Exception = ex
                     });
                 }
                 finally
                 {
                     dispose();
                 }

                 return false;
             });
        }
        private object tst(object value)
        {
            if (value == null) return "";
            return value;
        }
        public void update(Exec.DGExec action)
        {
            Get_00Execute._0021762(handler =>
            {
                if (!handler)
                {
                    return false;
                }
                setParamters(QUERY_TYPE.UPDATE);
                cmd.CommandText = getSQLCommand(QUERY_TYPE.UPDATE);

                foreach (Parameter p in paramsList) cmd.Parameters.AddWithValue("@" + p.name, p.value);

                Result result = new Result();
                try
                {
                    result.Value = cmd.ExecuteScalar();
                    result.IsSuccess = true;
                    action?.Invoke(result);
                }
                catch (Exception ex)
                {
                    action?.Invoke(new Result
                    {
                        IsSuccess = false,
                        Exception = ex
                    });
                }
                finally
                {
                    dispose();
                }

                return false;
            });
        }
        public void delete(Exec.DGExec action)
        {
            Get_00Execute._0021762(handler =>
            {
                if (!handler)
                {
                    return false;
                }
                cmd.CommandText = getSQLCommand(QUERY_TYPE.DELETE);

                Result result = new Result();
                try
                {
                    result.Value = cmd.ExecuteScalar();
                    result.IsSuccess = true;
                    action?.Invoke(result);
                }
                catch (Exception ex)
                {
                    action?.Invoke(new Result
                    {
                        IsSuccess = false,
                        Exception = ex,
                    });
                }
                finally
                {
                    dispose();
                }
                return false;
            });
        }
        public void dispose()
        {
            CloseConnection();
            cmd.Parameters.Clear();
            this.table_name = "";
            this.entity = "";
            this.paramsList = null;
            this.columns = null;
            this.condition = null;
        }

        public IThen Insert(Exec.DGExec result)
        {
            throw new NotImplementedException();
        }

        public IThen Update(Exec.DGExec result)
        {
            throw new NotImplementedException();
        }

        public IThen Delete(Exec.DGExec result)
        {
            throw new NotImplementedException();
        }

        public IThen SelectList<T>(Exec.DGExec result)
        {
            throw new NotImplementedException();
        }

        public IThen SelectTable(Exec.DGExec result)
        {
            throw new NotImplementedException();
        }
        #region "Classes"
        public class Condition : IQuery
        {
            #region "Definations"
            Wh wh;
            List<And> andList;
            List<Or> orList;
            IQuery execute;
            #endregion
            #region "Functions" 
            public Condition(IQuery execute, string q1, string p, object q2)
            {
                this.execute = execute;
                this.wh = new Wh(q1, p, q2);
            }
            public Condition and(string q1, string p, object p2)
            {
                if (andList == null) andList = new List<And>();
                andList.Add(new And(q1, " " + p + " ", p2));
                return this;
            }
            public Condition and(string q1, object p2)
            {
                if (andList == null) andList = new List<And>();
                andList.Add(new And(q1, " = ", p2));
                return this;
            }
            public Condition or(string q1, string p, object p2)
            {
                if (orList == null) orList = new List<Or>();
                orList.Add(new Or(q1, " " + p + " ", p2));
                return this;
            }
            public Condition or(string q1, object p2)
            {
                if (orList == null) orList = new List<Or>();
                orList.Add(new Or(q1, " = ", p2));
                return this;
            }
            public string getConditions()
            {
                string conditions = null;
                if (wh == null) return null;

                conditions = wh.ToString();
                if (andList != null && andList.Count > 0)
                    foreach (And and in andList) conditions += and.ToString();
                if (orList != null && orList.Count > 0)
                    foreach (Or or in orList) conditions += or.ToString();

                return conditions;
            }
            #endregion
            #region "Classes"

            private class Wh
            {
                private string eq1;
                private string op;
                private object eq2;

                public Wh(string eq1, string op, object eq2)
                {
                    string eq = "";

                    if (eq2.GetType() == typeof(string))
                    {
                        eq = "'" + eq2.ToString() + "'";
                    }
                    else eq = eq2.ToString();

                    this.eq1 = eq1;
                    this.op = op;
                    this.eq2 = eq;
                }
                public override string ToString()
                {
                    return " WHERE " + eq1 + " " + op + " " + eq2;
                }
            }
            private class And
            {
                private string eq1;
                private string op;
                private object eq2;

                public And(string eq1, string op, object eq2)
                {
                    string eq_1 = "";

                    if (eq2.GetType() == typeof(string))
                    {
                        eq_1 = "'" + eq2.ToString() + "'";
                    }
                    else eq_1 = eq2.ToString();


                    this.eq1 = eq1;
                    this.op = op;
                    this.eq2 = eq_1;
                }
                public override string ToString()
                {
                    return " AND " + eq1 + " " + op + " " + eq2;
                }

            }
            private class Or
            {
                private string eq1;
                private string op;
                private object eq2;

                public Or(string eq1, string op, object eq2)
                {
                    string eq_1 = "";

                    if (eq2.GetType() == typeof(string))
                    {
                        eq_1 = "'" + eq2.ToString() + "'";
                    }
                    else eq_1 = eq2.ToString();


                    this.eq1 = eq1;
                    this.op = op;
                    this.eq2 = eq_1;
                }
                public override string ToString()
                {
                    return " OR " + eq1 + " " + op + " " + eq2;
                }
            }
            #endregion
            #region "IExecute"

            public IThen Insert(Exec.DGExec result)
            {
                return execute.Insert(result);
            }

            public IThen Update(Exec.DGExec result)
            {
                return execute.Update(result);
            }

            public IThen Delete(Exec.DGExec result)
            {
                return execute.Delete(result);
            }

            public IThen SelectList<T>(Exec.DGExec result)
            {
                return execute.SelectList<T>(result);
            }

            public IThen SelectTable(Exec.DGExec result)
            {
                return execute.SelectTable(result);
            }
            #endregion
        }
        private class Parameter
        {
            public string name;
            public object value;
            public bool is_primary_key;

            public Parameter(string name, object value, bool is_primary)
            {
                this.name = name;
                this.value = value;
                this.is_primary_key = is_primary;
            }
        }
        #endregion
    }

    public class AccessQuery : AccessDB
    {

        List<string> tablesList = new List<string>();
        private List<string> selectList = new List<string>();
        private List<DKeyValue> whereList = new List<DKeyValue>(); // add support sub where


        public AccessQuery Select(params string[] fields)
        {
            selectList.Clear();
            selectList.AddRange(fields);
            return this;
        }
        public AccessQuery Where(string key, object value)
        {
            whereList.Add(new DKeyValue(key, value));
            return this;
        }

        public AccessQuery(params string[] tables)
        {
            tablesList.AddRange(tables);
        }

        public AccessQuery()
        {

        }
        public DataTable get()
        {
            string _query = generateQuery();
            return getConnection(result =>
            {
                var conn = (OleDbConnection)result;
                DataTable dt = new DataTable();
                if (conn.State == ConnectionState.Open) dt.Load(new OleDbCommand(_query, conn).ExecuteReader());

                return dt;
            });
        }
        public string generateQuery()
        {
            /* Select */
            string _query = "SELECT " + string.Join(",", selectList);
            /* /Select */

            /* Tables */
            _query += " " + string.Join(",", tablesList);
            /* /Tables */

            /* Where */
            _query += " " + string.Join("AND", whereList.Select(item => item.key + '=' + item.value));
            /* /Where */
            return _query;
        }
    }
}
