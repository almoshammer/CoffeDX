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
using System.IO;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using CoffeDX.Shared;
using System.Dynamic;
using System.Xml.Linq;

namespace CoffeDX.Database
{
    public enum AUTHTYPE { LOCAL, AUTH }
    public class SQLServer
    {
        public static string GetConnectionString()
        {
            return $"Data Source={ServerName};Initial Catalog={DBName};Integrated Security=True;";
        }
        public static string GetConnectionString(string _database)
        {
            return $"Data Source={ServerName};Initial Catalog={_database};Integrated Security=True;";
        }
        public static string DBNamePostfix
        {
            get => KeyValDB.GetString("DABEBASE_POSTFIX");
            set => KeyValDB.SetString("DABEBASE_POSTFIX", value);
        }
        public static string DBName
        {
            get => KeyValDB.GetString("DATABASE_NAME" + DBNamePostfix);
            set => KeyValDB.SetString("DATABASE_NAME" + DBNamePostfix, value);
        }
        public static string ServerName
        {
            get => KeyValDB.GetString("S_SERVER_NAME");
            set => KeyValDB.SetString("S_SERVER_NAME", value);
        }
        public static string Username
        {
            get => KeyValDB.GetString("SERVER_USER_NAME");
            set => KeyValDB.SetString("SERVER_USER_NAME", value);
        }
        public static string Password
        {
            get => KeyValDB.GetString("SERVER_PASSWORD");
            set => KeyValDB.SetString("SERVER_PASSWORD", value);
        }
        public static AUTHTYPE AUTH_TYPE { get; set; } = AUTHTYPE.LOCAL;

        public static bool flag_check_connection = true;

        private static SqlConnection conn;
        public static void closeConnection()
        {
            if (conn != null && conn.State != ConnectionState.Closed)
                conn.Close();
        }
        public static Task<T> getConnection<T>(DObject<T> @object)
        {

            return getConnection(@object, DBName);
        }
        public static T getOnlineConnection<T>(DObject<T> @object)
        {
            return getOnlineConnection(@object, DBName);
        }
        public static T getOnlineConnection<T>(DObject<T> @object, string DatabaseName)
        {
            var dbname = DBName;
            if (!string.IsNullOrWhiteSpace(DatabaseName)) dbname = DatabaseName;

            if (string.IsNullOrWhiteSpace(dbname))
            {
                MessageBox.Show("عطل فني - (You need to set database name) \n يرجى التواصل مع الدعم الفني");
                return @object(null);
            }
            string connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=True;";
            if (AUTH_TYPE == AUTHTYPE.LOCAL)
                connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=True;";
            else if (AUTH_TYPE == AUTHTYPE.AUTH)
                connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=True;";
            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    var res = @object(connection);
                    connection.Close();
                    return res;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return @object(null);
            }
        }
        public static bool RestoreDB(string _databaseName, string path, DVoid action, bool isAsync = false)
        {
            return getOnlineConnection(conn =>
            {
                try
                {
                    ServerConnection sc = new ServerConnection((conn as SqlConnection));
                    Server server = new Server(sc);
                    Restore destination = new Restore();
                    destination.Action = RestoreActionType.Database;
                    destination.Database = _databaseName;
                    BackupDeviceItem deviceItem = new BackupDeviceItem(path, DeviceType.File);
                    destination.Devices.Add(deviceItem);
                    destination.ReplaceDatabase = true;
                    //  destination.NoRecovery = true;
                    // destination.NoRewind = true;
                    server.KillAllProcesses(_databaseName);
                    server.KillDatabase(_databaseName);
                    if (isAsync) destination.SqlRestoreAsync(server);
                    else destination.SqlRestore(server);
                    return true;
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }
            }, _databaseName);
        }
        public static bool BackupDB(string _databaseName, string path, DVoid action, bool isAsync = false)
        {
            return getOnlineConnection(conn =>
            {
                try
                {
                    ServerConnection sc = new ServerConnection((conn as SqlConnection));
                    Server server = new Server(sc);
                    Backup destination = new Backup();
                    destination.Action = BackupActionType.Database;
                    destination.Database = _databaseName;
                    BackupDeviceItem deviceItem = new BackupDeviceItem(path, DeviceType.File);
                    destination.Devices.Add(deviceItem);
                    server.KillAllProcesses(_databaseName);
                    server.KillDatabase(_databaseName);
                    if (isAsync) destination.SqlBackupAsync(server);
                    else destination.SqlBackup(server);
                    return true;
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }
            }, _databaseName);
        }
        public async static Task<T> getConnection<T>(DObject<T> @object, string DatabaseName)
        {
            var dbname = DBName;
            if (!string.IsNullOrWhiteSpace(DatabaseName)) dbname = DatabaseName;

            if (string.IsNullOrWhiteSpace(dbname))
            {
                MessageBox.Show("عطل فني - (You need to set database name) \n يرجى التواصل مع الدعم الفني");
                return @object(conn);
            }
            string connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=SSPI;Pooling=false;";
            if (AUTH_TYPE == AUTHTYPE.LOCAL)
                connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=SSPI;Pooling=false;";
            else if (AUTH_TYPE == AUTHTYPE.AUTH)
                connStr = $"Data Source={ServerName};Initial Catalog={dbname};Integrated Security=SSPI;Pooling=false; ";//Connection Lifetime=100;
            try
            {
                //if (conn == null || flag_check_connection == true)
                //{
                //    conn = new SqlConnection(connStr);
                //    conn.StateChange += (s, e) =>
                //    {
                //        if (e.CurrentState == ConnectionState.Broken)
                //        {
                //            //!error
                //        }
                //    };
                //    flag_check_connection = false;
                //}
                //if (conn?.State == ConnectionState.Open)
                //{
                //    // ExHanlder.handle(null, ExHanlder.ERR.INS, ExHanlder.PROMP_TYPE.HID, _00CONSTANT.CONN_OPENED);
                //    conn?.Close();
                //}
                //if (conn?.State == System.Data.ConnectionState.Closed) conn?.OpenAsync();

                //var result = @object(conn);
                ////dynamic result = Activator.CreateInstance<T>();
                ////using (var cc = new SqlConnection(connStr))
                ////{
                ////    cc.Open();
                ////    result = @object(cc);
                ////}
                //conn.Close();

                dynamic result = new ExpandoObject();
                using (var conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    result = @object(conn);
                }
                return result;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                // ExHanlder.handle(ex, ExHanlder.ERR.APP, ExHanlder.PROMP_TYPE.MSG, _00CONSTANT.DB_CONN_ERROR1);
                // Application.Exit();
                return @object(conn);
            }
        }
        public static void Migrate(Assembly assembly, bool allowDrop = false)
        {
            StringBuilder tables = new StringBuilder();
            StringBuilder fKeys = new StringBuilder();


            StringBuilder dropRelations = new StringBuilder();


            foreach (var tp in assembly.GetTypes())
            {
                if (tp.IsClass && tp.IsPublic && Attribute.IsDefined(tp, typeof(DEntityAttribute), false))
                {
                    string table = $"t_{tp.Name}";
                    if (allowDrop)
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

                            var on_constr_event = fAttr.constraint_event == ON_CONSTRAINT_EVENT.CASECASE ? "ON DELETE CASECASE" : "";

                            if (parentKey == null || parentKey.Length == 0)
                                parentKey = GetPrimaryKey(fAttr.ParentModel);
                            // TODO create forieghn key with casecade on delete only, and non constrained field if value not found
                            var ctr_name = $"fk_{table}_TO_t_{fAttr.ParentModel.Name}";
                            if (allowDrop)
                            {
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
            try
            {
                using (var connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                    string strTables = tables.ToString();
                    string strKeys = fKeys.ToString();
                    /*1*/
                    var cmd = new SqlCommand(dropRelations.ToString(), connection);
                    if (dropRelations.Length > 10) cmd.ExecuteNonQuery();
                    /*2*/
                    cmd.CommandText = strTables;
                    cmd.ExecuteNonQuery();
                    /*3*/
                    if (strKeys != null && strKeys.Length > 10 && strKeys.Contains("Alter"))
                    {
                        cmd.CommandText = strKeys;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Migrate(getCon..):" + ex.Message);
            }

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
            if (tp == typeof(double)) return "Float default 0";
            if (tp == typeof(float)) return "Float default 0";
            if (tp == typeof(decimal)) return "Float default 0";

            if (tp == typeof(long?)) return "BIGINT NULL";
            if (tp == typeof(byte?[])) return "Image NULL";
            if (tp == typeof(bool?)) return "BIT NULL";
            if (tp == typeof(char?)) return "Char NULL";
            if (tp == typeof(int?)) return "Int NULL";
            if (tp == typeof(DateTime?)) return "Date NULL";
            if (tp == typeof(DateTimeOffset?)) return "DateTimeOffset NULL";
            if (tp == typeof(double?)) return "Float NULL default 0";
            if (tp == typeof(float?)) return "Float NULL default 0";
            if (tp == typeof(decimal?)) return "Float NULL default 0";

            return "Varchar(20)";
        }
    }
}
