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

namespace CoffeDX.Database
{
    public class AccessDB
    {
        public AccessQuery Query = new AccessQuery();
        public bool flag_check_connection = false;

        protected delegate bool licDelegate();

        protected delegate void _00119561();
        string connectionString;
        private SqlConnection conn;
        public void closeConnection()
        {
            if (conn != null && conn.State != System.Data.ConnectionState.Closed)
                conn.Close();
        }
        private void getConnection()
        {
            System.IO.FileStream file = new System.IO.FileStream(_00CONSTANT.CONN_PATH, System.IO.FileMode.OpenOrCreate);
            byte[] data = new byte[file.Length];
            file.Read(data, 0, (int)file.Length); file.Close();
            foreach (byte d in data) connectionString += Convert.ToChar(d);

            connectionString = _0021763() ? connectionString : "";
            try
            {
                if (conn == null || flag_check_connection == true)
                {
                    conn = new SqlConnection(connectionString);
                    conn.StateChange += (s, e) =>
                    {
                        if (e.CurrentState == ConnectionState.Broken)
                        {
                            ExHanlder.handle(null, ExHanlder.ERR.INS, ExHanlder.PROMP_TYPE.MSG, _00CONSTANT.CONN_BROKEN);
                        }
                    };
                    flag_check_connection = false;
                    conn = _0021763() ? conn : null;
                }
                if (conn?.State == ConnectionState.Open)
                {
                    ExHanlder.handle(null, ExHanlder.ERR.INS, ExHanlder.PROMP_TYPE.HID, _00CONSTANT.CONN_OPENED);
                }
                if (conn?.State == System.Data.ConnectionState.Closed)
                    conn?.Open();
            }
            catch (System.Exception ex)
            {
                ExHanlder.handle(ex, ExHanlder.ERR.APP, ExHanlder.PROMP_TYPE.MSG, _00CONSTANT.DB_CONN_ERROR1);
                Application.Exit();
            }
        }
    }

    public class AccessQuery
    {
        List<string> tablesList = new List<string>();
        private List<string> selectList = new List<string>();
        private List<DKeyValue> whereList = new List<DKeyValue>(); // add support sub where

        public DataTable get(SqlConnection conn, string query)
        {
            try
            {
                if (conn == null) return null;
                if (conn.State == ConnectionState.Closed)
                    conn.Open();
                DataTable dt = new DataTable();
                dt.Load(new SqlCommand(query, conn).ExecuteReader());
                return dt;
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                conn.Close();
            }
        }


        public string generateQuery()
        {
            /* Select */
            string _query = "SELECT "+ string.Join(",", selectList);
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
