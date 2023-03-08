using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Shared
{
    public static class _Constants
    {
        public const int YEAR = 2023;
        public const int MONTH = 1;
        public const string CONN_PATH = "D:\\connection.txt";
        public const string DB_NAME = "db_newmax";
        public const string DB_CONN_ERROR1 = "خطأ في الاتصال بقواعد البيانات";
        public const string LIC_ERROR = "فشل في المعالجة";
        public const string CONN_OPENED = "الاتصال مفتوح مسبقاً قد يسبب ذلك بعض المشاكل";
        public const string CONN_BROKEN = "Connection Broken!";

        private static Hashtable vars = new Hashtable();
        public static object GetValue(string key)
        {
            return vars[key];
        }
    }
}
