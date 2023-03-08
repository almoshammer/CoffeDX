using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeDX.Shared
{
    public static class ExHandler
    {
        /// <summary>
        /// Erorrs levels or types
        /// </summary>
        public enum ERR
        {
            NON,
            LIC,
            APP,
            FAC,
            CLS,
            FUN,
            INS,
            ENG,
        }
        public enum PROMP_TYPE
        {
            NON,
            MSG,
            FORM,
            HID,
        }
        public static void handle(Exception ex, ERR err, PROMP_TYPE prmp = PROMP_TYPE.MSG, string comment = null, string fname = null)
        {
            if (err == ERR.LIC)
            {
                if (prmp == PROMP_TYPE.MSG)
                    System.Windows.Forms.MessageBox.Show(_Constants.LIC_ERROR);
            }
            else if (ex == null)
            {
                if (prmp == PROMP_TYPE.MSG)
                    System.Windows.Forms.MessageBox.Show(comment + "\n" + fname);
            }
            else if (err == ERR.APP)
            {
                if (prmp == PROMP_TYPE.MSG)
                    System.Windows.Forms.MessageBox.Show(ex.StackTrace);
            }
            else if (err == ERR.FUN || err == ERR.INS)
            {
                if (prmp == PROMP_TYPE.MSG)
                    System.Windows.Forms.MessageBox.Show(comment + "\n" + ex.Message);
            }
        }
        private static void showHiddenError(string error)
        {

        }
    }
}
