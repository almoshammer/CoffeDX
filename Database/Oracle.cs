using CoffeDX.Query.Mapping;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoffeDX.Database
{
    public enum ORACLE_AUTHTYPE { LOCAL, AUTH }
    public class Oracle
    {
        public static string GetConnectionString()
        {
            return $"DATA SOURCE={ServerName}; USER ID={Username};PASSWORD={Password}";
        }
        public static string GetConnectionString(string _database)
        {
            return $"DATA SOURCE={ServerName}; USER ID={Username};PASSWORD={Password}";
        }
        public static string DBNamePostfix
        {
            get => KeyValDB.GetString("Oracle_DABEBASE_POSTFIX");
            set => KeyValDB.SetString("Oracle_DABEBASE_POSTFIX", value);
        }
        public static string DBName
        {
            get => KeyValDB.GetString("Oracle_DATABASE_NAME" + DBNamePostfix);
            set => KeyValDB.SetString("Oracle_DATABASE_NAME" + DBNamePostfix, value);
        }
        public static string ServerName
        {
            get => KeyValDB.GetString("Oracle_S_SERVER_NAME");
            set => KeyValDB.SetString("Oracle_S_SERVER_NAME", value);
        }
        public static string Username
        {
            get => KeyValDB.GetString("Oracle_SERVER_USER_NAME");
            set => KeyValDB.SetString("Oracle_SERVER_USER_NAME", value);
        }
        public static string Password
        {
            get => KeyValDB.GetString("Oracle_SERVER_PASSWORD");
            set => KeyValDB.SetString("Oracle_SERVER_PASSWORD", value);
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
            var connStr = $"DATA SOURCE={ServerName}; USER ID={Username};PASSWORD={Password}";

            try
            {
                using (var connection = new SqlConnection(connStr))
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
            var connStr = $"DATA SOURCE={ServerName}; USER ID={Username};PASSWORD={Password}";

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
            StringBuilder indexes = new StringBuilder();
            StringBuilder sequences = new StringBuilder();

            /* Generate Scripts */
            foreach (var tp in assembly.GetTypes())
            {
                if (tp.IsClass && tp.IsPublic && Attribute.IsDefined(tp, typeof(DEntityAttribute), false))
                {

                    string table = $"t_{tp.Name}";//
                    if (allowDrop)
                        tables.Append($"BEGIN EXECUTE IMMEDIATE 'DROP TABLE {table}'; EXCEPTION WHEN OTHERS THEN NULL; END;\n");

                    tables.Append($"BEGIN EXECUTE IMMEDIATE '\n");
                    tables.Append($"CREATE TABLE {table} (\n");
                    var props = tp.GetProperties();

                    var pkKeys = new List<string>();

                    foreach (var prop in props)
                    {
                        string field = $"\"{prop.Name}\"";
                        string typeName = $"{field} {GetSQLServerFieldType(prop)}";
                        if (Attribute.IsDefined(prop, typeof(DPrimaryKeyAttribute))) pkKeys.Add(field);
                        if (Attribute.IsDefined(prop, typeof(DIncrementalAttribute)))
                        {
                            /* Oracle Version 12c */
                            //typeName += " GENERATED ALWAYS AS IDENTITY "; 
                            /* /Oracle Version 12c */

                            /* Oracle Version <=11g */
                            if (allowDrop) sequences.AppendLine($"BEGIN EXECUTE IMMEDIATE 'DROP SEQUENCE seq_{table}_{prop.Name};' EXCEPTION WHEN OTHERS THEN NULL; END;");
                            sequences.AppendLine($"BEGIN EXECUTE IMMEDIATE 'CREATE SEQUENCE seq_{table}_{prop.Name} START WITH 1;' EXCEPTION WHEN OTHERS THEN NULL; END;");
                            sequences.AppendLine($@"
                                                   CREATE OR REPLACE TRIGGER tgr_seq_{table}_{prop.Name}
                                                   BEFORE INSERT ON {table} 
                                                   FOR EACH ROW
                                                   BEGIN
                                                     SELECT seq_{table}_{prop.Name}.NEXTVAL
                                                     INTO   :new.{field}
                                                     FROM   dual;
                                                   END;");
                            /* /Oracle Version <=11g */
                        }

                        else if (Attribute.IsDefined(prop, typeof(DForeignKeyAttribute)))
                        {
                            var fAttr = prop.GetCustomAttribute<DForeignKeyAttribute>();
                            var parentKey = fAttr.ParentKey;

                            var on_constr_event = fAttr.constraint_event == ON_CONSTRAINT_EVENT.CASCADE ? "ON DELETE CASCADE" : "";

                            if (parentKey == null || parentKey.Length == 0)
                                parentKey = GetPrimaryKey(fAttr.ParentModel);
                            var ctr_name = $"fk_{table}_TO_t_{fAttr.ParentModel.Name}";
                            if (allowDrop) dropRelations.Append($"BEGIN EXECUTE IMMEDIATE 'ALTER TABLE {table} DROP CONSTRAINT {ctr_name}'; EXCEPTION WHEN OTHERS THEN NULL; END;\n");

                            fKeys.Append($"BEGIN EXECUTE IMMEDIATE '");
                            fKeys.Append($"ALTER TABLE {table} ADD CONSTRAINT {ctr_name} FOREIGN KEY({field}) REFERENCES t_{fAttr.ParentModel.Name}({parentKey}) {on_constr_event};");
                            fKeys.Append("'; EXCEPTION WHEN OTHERS THEN NULL; END;\n");
                        }
                        tables.Append($"{typeName}");
                        if (prop != props[props.Length - 1]) tables.Append(",\n");
                    }
                    /* Add PK Relations */
                    if (pkKeys.Count > 0) tables.AppendLine($",\nCONSTRAINT pk_{table} PRIMARY KEY ({string.Join(",", pkKeys)})\n");
                    /* /Add PK Relations */
                    tables.Append(" );' \n");

                    if (Attribute.IsDefined(tp, typeof(DNonClusteredIndexAttribute), false))
                    {
                        var ix = tp.GetCustomAttribute<DNonClusteredIndexAttribute>();
                        ix.fields = ix.fields.Select(item => $"\"{item}\"").ToArray();
                        indexes.Append($"\nBEGIN EXECUTE IMMEDIATE 'CREATE INDEX IX_{table} ON {table}({string.Join(",", ix.fields)});'; EXCEPTION WHEN OTHERS THEN NULL; END;");
                    }
                }
            }
            /* /Generate Scripts */
            try
            {
                using (var connection = new OracleConnection(GetConnectionString()))
                {

                    connection.Open();
                    var strTables = tables?.ToString();
                    var strKeys = fKeys?.ToString();
                    var Indx = indexes?.ToString();
                    /* 1: Drop The Old Relations*/
                    var cmd = new OracleCommand(dropRelations.ToString(), connection);
                    cmd.CommandTimeout = 240;
                    if (dropRelations.Length > 10) cmd.ExecuteNonQuery();
                    /* 2: Alter tables*/
                    cmd.CommandText = strTables;
                    cmd.ExecuteNonQuery();
                    /* 3: Add The New Relations*/
                    if (strKeys != null && strKeys.Length > 10 && strKeys.ToLower().Contains("alter"))
                    {
                        cmd.CommandText = strKeys;
                        cmd.ExecuteNonQuery();
                    }
                    /* 4: Add New Indexes*/
                    if (Indx != null && Indx.Length > 10)// Length(10): To insure that there're no white spaces
                    {
                        cmd.CommandText = Indx;
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
            foreach (var item in type.GetProperties()) if (Attribute.IsDefined(item, typeof(DPrimaryKeyAttribute))) return $"\"{item.Name}\"";
            return null;
        }
        private static string GetSQLServerFieldType(PropertyInfo prop)
        {
            var tp = prop.PropertyType;

            if (tp == typeof(long)) return "LONG";
            if (tp == typeof(byte[])) return "BLOB";
            if (tp == typeof(bool)) return "NUMBER(1)";
            if (tp == typeof(string)) return "Varchar(255) NULL";
            if (tp == typeof(char)) return "Char";
            if (tp == typeof(int)) return "Int";

            if (tp == typeof(DateTime))
            {
                if (Attribute.IsDefined(prop, typeof(DTimeAttribute))) return "TIMESTAMP";
                return "Date";
            }
            if (tp == typeof(DateTimeOffset))
            {
                if (Attribute.IsDefined(prop, typeof(DTimeAttribute))) return "TIMESTAMP";
                return "TIMESTAMP";
            }
            if (tp == typeof(TimeSpan)) return "TIMESTAMP";
            if (tp == typeof(double)) return "Float default 0";
            if (tp == typeof(float)) return "Float default 0";
            if (tp == typeof(decimal)) return "Float default 0";

            if (tp == typeof(long?)) return "LONG NULL";
            if (tp == typeof(byte?[])) return "BLOB NULL";
            if (tp == typeof(bool?)) return "BIT NULL";
            if (tp == typeof(char?)) return "NUMBER(1) NULL";
            if (tp == typeof(int?)) return "Int NULL";
            if (tp == typeof(DateTime?))
            {
                if (Attribute.IsDefined(prop, typeof(DTimeAttribute))) return "TIMESTAMP NULL";
                return "Date NULL";
            }
            if (tp == typeof(DateTimeOffset?))
            {
                if (Attribute.IsDefined(prop, typeof(DTimeAttribute))) return "TIMESTAMP NULL";
                return "TIMESTAMP NULL";
            }
            if (tp == typeof(TimeSpan?)) return "TIMESTAMP NULL";
            if (tp == typeof(double?)) return "Float NULL default 0";
            if (tp == typeof(float?)) return "Float NULL default 0";
            if (tp == typeof(decimal?)) return "Float NULL default 0";

            return "Varchar2(255)";
        }
    }
}
