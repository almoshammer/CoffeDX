using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Shared
{
    public class Exec
    {
        public delegate void DGExec(Result result);
        public delegate void DGException(Exception ex);
        public delegate void DGFinally();
    }
    public class Result
    {

        public Result() { }
        public Result(bool isSuccess, Exception Exception, System.Data.DataTable Table, int Code = 0)
        {
            this.Exception = Exception;
            this.Exception = Exception;
            this.Table = Table;
            this.Code = Code;
        }
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
        public System.Data.DataTable Table { get; set; }
        public object Value { get; set; }
        public int Code { get; set; }
    }
}
