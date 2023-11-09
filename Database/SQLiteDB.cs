
using System.Windows.Forms;

namespace CoffeDX.Database
{
    public class SQLiteDB
    {
        public string DB_Name { get; set; } = "sqlite_coffe.db";


        public string DB_Path { get; set; } = Application.StartupPath + "\\data";


        //public SQLiteConnection Connection
        //{
        //    get
        //    {
        //        //IL_001d: Unknown result type (might be due to invalid IL or missing references)
        //        object obj = ((object)db) ?? ((object)new SQLiteConnection(Path.Combine(DB_Path, DB_Name), true));
        //        SQLiteConnection result = (SQLiteConnection)obj;
        //        db = (SQLiteConnection)obj;
        //        return result;
        //    }
        //}
    }
}
