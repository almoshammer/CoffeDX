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
            if (conn != null && conn.State != System.Data.ConnectionState.Closed)
                conn.Close();
        }
        public static T getConnection<T>(DObjectT<T> @object)
        {
            string connStr = $"Data Source={ServerName};Initial Catalog={DBName};Integrated Security=True;";
            if (AUTH_TYPE == AUTHTYPE.LOCAL)
                connStr = $"Data Source={ServerName};Initial Catalog={DBName};Integrated Security=True;";
            else if (AUTH_TYPE == AUTHTYPE.AUTH)
                connStr = $"Data Source={ServerName};Initial Catalog={DBName};Integrated Security=True;";
            try
            {
                if (conn == null || flag_check_connection == true)
                {
                    conn = new SqlConnection(connStr);
                    conn.StateChange += (s, e) =>
                    {
                        if (e.CurrentState == ConnectionState.Broken)
                        {
                            //error
                        }
                    };
                    flag_check_connection = false;
                }
                if (conn?.State == ConnectionState.Open)
                {
                    //ExHanlder.handle(null, ExHanlder.ERR.INS, ExHanlder.PROMP_TYPE.HID, _00CONSTANT.CONN_OPENED);
                }
                if (conn?.State == System.Data.ConnectionState.Closed)
                    conn?.Open();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                //ExHanlder.handle(ex, ExHanlder.ERR.APP, ExHanlder.PROMP_TYPE.MSG, _00CONSTANT.DB_CONN_ERROR1);
                // Application.Exit();
            }

            return @object(conn);
        }

        public static void Migrate(Assembly assembly)
        {
            StringBuilder tablesBuilder = new StringBuilder();
            foreach (var tp in assembly.GetTypes())
            {
                if (tp.IsClass && tp.IsPublic && Attribute.IsDefined(tp, typeof(DEntityAttribute)))
                {

                }
            }
        }


        private static string GetSQLServerFieldType(Type tp)
        {
            string type = null;


            return type;
        } 
    }
}
