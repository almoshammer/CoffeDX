using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Shared
{
    public  class Database : HotManager
    {
        public enum DatabaseType
        {
            None,
            SQlServer,
            MySQL,
            Access,
        }
        public static DatabaseType Type { get; set; }
        public static string DatabaseName { get; set;}
        public static string DatabaseVersion { get; set;}
        public static string GetConnectionString()
        {
            return "";
        }
        public static object GetConnection()
        {
            return null;
        }
        public static void CloseConnection()
        {

        }

        public static SqlCommand CreateCommand(string command)
        {
            return null;
        }

    }
}
