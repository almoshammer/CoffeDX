using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoffeDX.Database
{
    public partial class FrmSetupSqlServer : Form
    {
        public FrmSetupSqlServer()
        {
            InitializeComponent();

            Load += (s, e) =>
            {
                foreach(var item in DSqlServerHelper.ListLocalSqlInstances())
                {
                    cmbServers.Items.Add(item);
                }
            };
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnReconnect_Click(object sender, EventArgs e)
        {
            if(cmbServers.SelectedIndex < 0 || string.IsNullOrWhiteSpace(cmbServers.Text))
            {
                MessageBox.Show("يرجى تحديد اسم السيرفر");
                return;
            }
            SQLServer.ServerName = cmbServers.Text;
            Close();
            SQLServer.getConnection<string>(res =>
            {

                return "test connection";
            });
        }
    }
}
