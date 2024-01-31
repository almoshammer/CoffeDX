using CoffeDX.Shared;
using DBreeze.Storage;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoffeDX.Query.Mapping;
using System.Data.OleDb;

namespace CoffeDX.Database
{
    public class AccessDB
    {
        public static string DBName { get; set; }
        public bool flag_check_connection = true;
        private OleDbConnection conn;
        public void closeConnection()
        {
            if (conn != null && conn.State != System.Data.ConnectionState.Closed)
                conn.Close();
        }
        public T getConnection<T>(DObject<T> @object)
        {

            try
            {
                if (conn == null || flag_check_connection == true)
                {
                    conn = new OleDbConnection($"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={DBName};Persist Security Info=False;");
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

    }
}
