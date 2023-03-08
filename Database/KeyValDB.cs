using CoffeDX.Query.Mapping;
using DBreeze;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoffeDX.Database
{
    public class KeyValDB
    {
        static DBreeze.Transactions.Transaction _db;
        public static DBreeze.Transactions.Transaction Db
        {
            get
            {
                if (_db == null)
                {
                    DBreezeConfiguration config = new DBreezeConfiguration();
                    config.Storage = DBreezeConfiguration.eStorage.DISK;
                    config.DBreezeDataFolderName = Application.StartupPath + "\\data\\panda";
                    DBreezeEngine engin = new DBreezeEngine(config);
                    _db = engin.GetTransaction();
                }
                return _db;
            }
        }
        public static bool SaveProps(object obj)
        {
            try
            {
                if (Attribute.IsDefined(obj.GetType(), typeof(DEntityAttribute)))
                {
                    string entity_name = obj.GetType().GetCustomAttribute<DEntityAttribute>(false).Name;
                    if (entity_name == null || entity_name.Length == 0) entity_name = obj.GetType().Name;
                    foreach (PropertyInfo item in obj.GetType().GetProperties())
                    {
                        Db.Insert(entity_name, item.Name, item.GetValue(obj).ToString());
                    }
                    Db.Commit();
                }

                return true;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void GetProps(ref object obj)
        {
            try
            {
                if (Attribute.IsDefined(obj.GetType(), typeof(DEntityAttribute)))
                {
                    string entity_name = obj.GetType().GetCustomAttribute<DEntityAttribute>(false).Name;
                    if (entity_name == null || entity_name.Length == 0) entity_name = obj.GetType().Name;
                    foreach (PropertyInfo item in obj.GetType().GetProperties())
                    {
                        item.SetValue(obj, Db.Select<string, string>(entity_name, item.Name).Value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
