using CoffeDX.Query.Mapping;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Oracle.ManagedDataAccess.Client;
using OracleInternal.Secure.Network;
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
using static System.Net.Mime.MediaTypeNames;

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
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }
            });
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
                catch (OracleException ex)
                {
                    MessageBox.Show(ex.Message);
                    return false;
                }
            });
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
            var tables = new StringBuilder();
            var constraints = new StringBuilder();
            var dropConstraints = new StringBuilder();
            var indexes = new StringBuilder();
            var sequences = new StringBuilder();
            /* Generate Scripts */
            foreach (var tp in assembly.GetTypes())
            {
                if (tp.IsClass && tp.IsPublic && Attribute.IsDefined(tp, typeof(DEntityAttribute), false))
                {
                    if (tables.Length == 0)
                    {
                        tables.Append("DECLARE v_exist INT;");
                        tables.Append("\nBEGIN");
                    }

                    string table = $"t_{tp.Name}".ToUpper();

                    if (allowDrop)
                    {
                        tables.Append($"\nSELECT COUNT(*) INTO v_exist FROM user_tables WHERE table_name = '{table}';");
                        tables.Append($"\nIF v_exist > 0 THEN EXECUTE IMMEDIATE 'DROP TABLE {table}'; END IF;");
                    }

                    tables.Append($"\nSELECT COUNT(*) INTO v_exist FROM user_tables WHERE table_name = '{table}';");
                    tables.Append($"\nIF v_exist = 0 THEN EXECUTE IMMEDIATE 'CREATE TABLE {table} ( ");

                    var props = tp.GetProperties();

                    var pkKeys = new List<string>();

                    foreach (var prop in props)
                    {
                        string field = $"\"{prop.Name}\"";
                        string typeName = $"{field} {GetSQLServerFieldType(prop)}";
                        if (Attribute.IsDefined(prop, typeof(DPrimaryKeyAttribute))) pkKeys.Add(field);
                        if (Attribute.IsDefined(prop, typeof(DIncrementalAttribute)))
                        {
                            if (sequences.Length == 0)
                            {
                                sequences.Append("DECLARE v_exist INT;");
                                sequences.Append("\nBEGIN");
                            }
                            var seq_name = "SEQ_" + CreateMD5($"seq_{table}_{prop.Name}".ToUpper()).Substring(0, 20);
                            var tgr_name = "TGR_" + CreateMD5($"tgr_seq_{table}_{prop.Name}".ToUpper()).Substring(0, 20);
                            /* Oracle Version 12c */
                            //typeName += " GENERATED ALWAYS AS IDENTITY "; 
                            /* /Oracle Version 12c */

                            /* Oracle Version <=11g */
                            if (allowDrop)
                            {
                                sequences.Append($"\nSELECT COUNT(*) INTO v_exist FROM user_sequences WHERE sequence_name = '{seq_name}';");
                                sequences.Append($"\nIF v_exist > 0 THEN EXECUTE IMMEDIATE 'DROP SEQUENCE {seq_name}'; END IF;");
                            }
                            sequences.Append($"\nSELECT COUNT(*) INTO v_exist FROM user_sequences WHERE sequence_name = '{seq_name}';");
                            sequences.Append($"\nIF v_exist = 0 THEN EXECUTE IMMEDIATE 'CREATE SEQUENCE {seq_name} START WITH 1'; END IF;");
                            sequences.Append($"\nEXECUTE IMMEDIATE 'CREATE OR REPLACE TRIGGER {tgr_name} BEFORE INSERT ON {table} FOR EACH ROW BEGIN SELECT {seq_name}.NEXTVAL INTO ' || ':' || 'new.{field} FROM dual; END' || ';';");
                            /* /Oracle Version <=11g */
                        }
                        else if (Attribute.IsDefined(prop, typeof(DForeignKeyAttribute)))
                        {
                            if (constraints.Length == 0)
                            {
                                constraints.Append("DECLARE v_exist INT;");
                                constraints.Append("\nBEGIN");
                                dropConstraints.Append("DECLARE v_exist INT;");
                                dropConstraints.Append("\nBEGIN");
                            }
                            var fAttr = prop.GetCustomAttribute<DForeignKeyAttribute>();
                            var parentKey = fAttr.ParentKey;

                            if (parentKey == null || parentKey.Length == 0) parentKey = GetPrimaryKey(fAttr.ParentModel);

                            var ctr_name = "FK_" + CreateMD5($"{table}_TO_t_{fAttr.ParentModel.Name}".ToUpper()).Substring(0, 20);
                            var on_constr_event = fAttr.constraint_event == ON_CONSTRAINT_EVENT.CASCADE ? "ON DELETE CASCADE" : "";
                            if (allowDrop)
                            {
                                dropConstraints.Append($"\nSELECT COUNT(*) INTO v_exist FROM user_constraints WHERE constraint_name = '{ctr_name}';");
                                dropConstraints.Append($"\nIF v_exist > 0 THEN EXECUTE IMMEDIATE 'ALTER TABLE {table} DROP CONSTRAINT {ctr_name}'; END IF;");
                            }

                            constraints.Append($"\nSELECT COUNT(*) INTO v_exist FROM user_constraints WHERE constraint_name = '{ctr_name}';");
                            constraints.Append($"\nIF v_exist = 0 THEN EXECUTE IMMEDIATE 'ALTER TABLE {table} ADD CONSTRAINT {ctr_name} FOREIGN KEY({field}) REFERENCES t_{fAttr.ParentModel.Name}(\"{parentKey}\") {on_constr_event}'; END IF;");
                        }
                        tables.Append($"{typeName}");
                        if (prop != props[props.Length - 1]) tables.Append(", ");
                    }
                    /* Add PK Relations */
                    if (pkKeys.Count > 0) tables.AppendLine($", CONSTRAINT pk_{table} PRIMARY KEY ({string.Join(",", pkKeys)}) ");
                    /* /Add PK Relations */

                    tables.Append(" )'; END IF; \n");

                    if (Attribute.IsDefined(tp, typeof(DNonClusteredIndexAttribute), false))
                    {
                        if (indexes.Length == 0)
                        {
                            indexes.Append("DECLARE v_exist INT;");
                            indexes.Append("\nBEGIN");
                        }

                        var ix = tp.GetCustomAttribute<DNonClusteredIndexAttribute>();
                        ix.fields = ix.fields.Select(item => $"\"{item}\"").ToArray();
                        var ix_name = $"IX_{table}";
                        if (allowDrop)
                        {
                            indexes.Append($"\nSELECT COUNT(*) INTO v_exist FROM user_indexes WHERE index_name = '{ix_name}';");
                            indexes.Append($"\nIF v_exist > 0 THEN EXECUTE IMMEDIATE 'DROP INDEX {ix_name}'; END IF;");
                        }
                        indexes.Append($"\nSELECT COUNT(*) INTO v_exist FROM user_indexes WHERE index_name = '{ix_name}';");
                        indexes.Append($"\nIF v_exist = 0 THEN EXECUTE IMMEDIATE 'CREATE INDEX {ix_name} ON {table}({string.Join(",", ix.fields)})'; END IF;");
                    }
                }
            }
            if (constraints.Length > 0) constraints.Append("\nEND;");
            if (tables.Length > 0) tables.Append("\nEND;");
            if (sequences.Length > 0) sequences.Append("\nEND;");
            if (dropConstraints.Length > 0) dropConstraints.Append("\nEND;");
            if (indexes.Length > 0) indexes.Append("\nEND;");
            /* /Generate Scripts */

            try
            {
                using (var connection = new OracleConnection(GetConnectionString()))
                {

                    connection.Open();

                    /* Drop The Old Constraints*/
                    var cmd = new OracleCommand(dropConstraints.ToString(), connection);
                    cmd.CommandTimeout = 240;
                    if (dropConstraints.Length > 10) cmd.ExecuteNonQuery();
                    /* /Drop The Old Constraints */

                    /* Alter tables */
                    if (tables.Length > 10)
                    {
                        cmd.CommandText = tables.ToString();
                        cmd.ExecuteNonQuery();
                    }
                    /* Alter tables */

                    /* Add The New Constraints */
                    if (constraints.Length > 10)
                    {
                        cmd.CommandText = constraints.ToString();
                        cmd.ExecuteNonQuery();
                    }
                    /* /Add The New Constraints */

                    /* Add Sequences */
                    if (sequences != null && sequences.Length > 10)
                    {
                        cmd.CommandText = sequences.ToString();
                        cmd.ExecuteNonQuery();
                    }
                    /* /Add Sequences */
                    /* Add New Indexes */
                    if (indexes.Length > 10) // Length(10): To insure that there're no white spaces
                    {
                        cmd.CommandText = indexes.ToString();
                        cmd.ExecuteNonQuery();
                    }
                    /* /Add New Indexes */
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Migrate(getCon..):" + ex.Message);
            }

        }
        private static string GetPrimaryKey(Type type)
        {
            foreach (var item in type.GetProperties()) if (Attribute.IsDefined(item, typeof(DPrimaryKeyAttribute))) return item.Name;
            return null;
        }
        private static string GetSQLServerFieldType(PropertyInfo prop)
        {
            var tp = prop.PropertyType;

            if (tp == typeof(long)) return "NUMBER(18)";
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

            if (tp == typeof(long?)) return "NUMBER(18) NULL";
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
            if (tp == typeof(double?)) return "Float NULL";
            if (tp == typeof(float?)) return "Float NULL";
            if (tp == typeof(decimal?)) return "Float NULL";

            return "Varchar2(255)";
            /*
             * Int32 => Oracle:Number(2) .. Oracle:Number(9)
             * Int64 => Oracle:Number(10) .. Oracle:Number(18)
             * Double => Oracle:Number(x,0) .. Oracle:Number(x,15)
             * Double => Oracle:Float
             * Decimal => Oracle:Number or Number(38)
             */
        }
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);

                //return Convert.ToHexString(hashBytes); // .NET 5 +

                //Convert the byte array to hexadecimal string prior to.NET 5
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("X2"));
                return sb.ToString();
            }
        }
    }
}
