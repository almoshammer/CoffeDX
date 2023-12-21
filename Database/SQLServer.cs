using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Reflection;
using CoffeDX.Query.Mapping;
using System.Security;

namespace CoffeDX.Database
{
    public enum AUTHTYPE { LOCAL, AUTH }
    public class SQLServer
    {
        public static string DBName { get; set; }
        public static string ServerName { get; set; }
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static AUTHTYPE AUTH_TYPE { get; set; } = AUTHTYPE.LOCAL;

        public static bool flag_check_connection = true;

        private static SqlConnection conn;
        public static void closeConnection()
        {
            if (conn != null && conn.State != ConnectionState.Closed)
                conn.Close();
        }
        public static T getConnection<T>(DObjectT<T> @object)
        {

            return getConnection(@object, DBName);
        }

        public static T getConnection<T>(DObjectT<T> @object, string dbname)
        {
            string connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=True;";
            if (AUTH_TYPE == AUTHTYPE.LOCAL)
                connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=True;";
            else if (AUTH_TYPE == AUTHTYPE.AUTH)
                connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=True;";
            try
            {
                if (conn == null || flag_check_connection == true)
                {
                    conn = new SqlConnection(connStr);
                    conn.StateChange += (s, e) =>
                    {
                        if (e.CurrentState == ConnectionState.Broken)
                        {
                            //!error
                        }
                    };
                    flag_check_connection = false;
                }
                if (conn?.State == ConnectionState.Open)
                {
                    // ExHanlder.handle(null, ExHanlder.ERR.INS, ExHanlder.PROMP_TYPE.HID, _00CONSTANT.CONN_OPENED);
                }
                if (conn?.State == System.Data.ConnectionState.Closed)
                    conn?.Open();
                var result = @object(conn);
                conn.Close();
                return result;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                // ExHanlder.handle(ex, ExHanlder.ERR.APP, ExHanlder.PROMP_TYPE.MSG, _00CONSTANT.DB_CONN_ERROR1);
                // Application.Exit();
                return Activator.CreateInstance<T>();
            }
        }

        public static void Migrate(Assembly assembly,bool allowDrop=false)
        {
            StringBuilder tables = new StringBuilder();
            StringBuilder fKeys = new StringBuilder();


            StringBuilder dropRelations = new StringBuilder();


            foreach (var tp in assembly.GetTypes())
            {
                if (tp.IsClass && tp.IsPublic && Attribute.IsDefined(tp, typeof(DEntityAttribute)))
                {
                    string table = $"t_{tp.Name}";
                    if(allowDrop)
                    tables.Append($"If Exists(Select * From Information_Schema.Tables Where Table_Schema = 'dbo' And Table_Name = '{table}') Drop Table dbo.{table};\n");


                    tables.Append($"If Not Exists(Select * From Information_Schema.Tables Where Table_Schema = 'dbo' And Table_Name = '{table}')\n");
                    tables.Append($"Create Table {table} (\n");
                    var props = tp.GetProperties();
                    foreach (var prop in props)
                    {
                        string typeName = $"{prop.Name} {GetSQLServerFieldType(prop)}";
                        if (Attribute.IsDefined(prop, typeof(DPrimaryKeyAttribute))) typeName += " Primary Key";
                        if (Attribute.IsDefined(prop, typeof(DIncrementalAttribute))) typeName += " Identity(1,1) ";
                        
                        else if (Attribute.IsDefined(prop, typeof(DForeignKeyAttribute)))
                        {
                            var fAttr = prop.GetCustomAttribute<DForeignKeyAttribute>();
                            var parentKey = fAttr.ParentKey;

                            var on_constr_event = fAttr.constraint_event == ON_CONSTRAINT_EVENT.CASECASE ? "ON DELETE CASECASE":"";

                            if (parentKey == null || parentKey.Length == 0) 
                                parentKey = GetPrimaryKey(fAttr.ParentModel);
                            // TODO create forieghn key with casecade on delete only, and non constrained field if value not found
                            var ctr_name = $"fk_{table}_TO_t_{fAttr.ParentModel.Name}";
                            if (allowDrop) {
                                dropRelations.Append($"If Exists(Select * From Information_Schema.REFERENTIAL_CONSTRAINTS Where CONSTRAINT_NAME = '{ctr_name}')\n");
                                dropRelations.Append($"Alter table {table} Drop Constraint {ctr_name};\n");
                            }
                            fKeys.Append($"If Not Exists(Select * From Information_Schema.REFERENTIAL_CONSTRAINTS Where CONSTRAINT_NAME = '{ctr_name}')\n");
                            fKeys.Append($"Alter Table dbo.{table} With NoCheck Add Constraint {ctr_name} Foreign Key({prop.Name}) References dbo.t_{fAttr.ParentModel.Name}({parentKey}) {on_constr_event};\n");
                            // new DKeyValue(prop.Name, fAttr.ParentModel)
                        }
                        tables.Append($"{typeName},\n");
                    }
                    tables.Append(" ); \n");
                }
            }

            getConnection<bool>((conn) =>
            {
                try
                {
                    string strTables = tables.ToString();
                    string strKeys = fKeys.ToString();

                    /*1*/
                    var cmd = new SqlCommand(dropRelations.ToString(), (SqlConnection)conn);
                    cmd.ExecuteNonQuery();
                    /*2*/
                    cmd.CommandText = strTables;
                    cmd.ExecuteNonQuery();
                    /*3*/
                    cmd.CommandText = strKeys;
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Migrate(getCon..):" + ex.Message);
                    return false;
                }

            }, DBName);

        }
        private static string GetPrimaryKey(Type type)
        {
            foreach (var item in type.GetProperties())
            {
                if (Attribute.IsDefined(item, typeof(DPrimaryKeyAttribute)))
                {
                    return item.Name;
                }
            }
            return null;
        }
        private static string GetSQLServerFieldType(PropertyInfo prop)
        {
            var tp = prop.PropertyType;

            if (tp == typeof(long)) return "BIGINT";
            if (tp == typeof(byte[])) return "Image";
            if (tp == typeof(bool)) return "BIT";
            if (tp == typeof(string)) return "Varchar(MAX) NULL";
            if (tp == typeof(char)) return "Char";
            if (tp == typeof(int)) return "Int";
            if (tp == typeof(DateTime)) return "Date";
            if (tp == typeof(DateTimeOffset)) return "DateTimeOffset";

            if (tp == typeof(long?)) return "BIGINT NULL";
            if (tp == typeof(byte?[])) return "Image NULL";
            if (tp == typeof(bool?)) return "BIT NULL";
            if (tp == typeof(char?)) return "Char NULL";
            if (tp == typeof(int?)) return "Int NULL";
            if (tp == typeof(DateTime?)) return "Date NULL";
            if (tp == typeof(DateTimeOffset?)) return "DateTimeOffset NULL";

            return "Varchar(20)";
        }
    }
}
