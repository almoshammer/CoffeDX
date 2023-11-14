using CoffeDX.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX
{
    public static class Program
    {
        public static void Main()
        {
            SQLServer.DBName = "db_newmax001";
            SQLServer.AUTH_TYPE = AUTHTYPE.LOCAL;
            SQLServer.ServerName = ".\\SQLSERVER_BELAL";
        }
    }
}
