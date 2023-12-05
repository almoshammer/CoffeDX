using CoffeDX.Database;
using CoffeDX.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoffeDX
{
    public enum ON_CONSTRAINT_EVENT { NOACTION,CASECASE };
    public static class Program
    {
        public static void Main()
        {
            SQLServer.DBName = "db_newmax001";
            SQLServer.AUTH_TYPE = AUTHTYPE.LOCAL;
            SQLServer.ServerName = ".\\SQLSERVER_BELAL";

            var assm = Assembly.GetAssembly(typeof(BaseModel));
            SQLServer.Migrate(assm);
            Console.Write("Completed");
        }
    }
}
