using BGlobal.OutlookAddInBPMONLine.Core.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BGlobal.OutlookAddInBPMOnline.WFrmBPMOnline.Forms
{
    public partial class FrmConnection : Form
    {
        public FrmConnection()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// http://localhost/bpmTranslate
        /// http://localhost/BPMAssessment/ServiceModel/AuthService.svc/Login
        /// ConfigurationManager.AppSettings["Url"].ToString()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtBoxServer.Text) || string.IsNullOrEmpty(this.txtBoxUser.Text) || string.IsNullOrEmpty(this.txtBoxPassword.Text))
            {
                System.Resources.ResourceManager rm = new System.Resources.ResourceManager(typeof(FrmConnection));
                MessageBox.Show("Servidor, Usuario y Clave son obligatorios, por favor revise.", "Error", MessageBoxButtons.OK);
            }
            else
            {
                ConfigurationManager.AppSettings["Server"] = this.txtBoxServer.Text;
                ConfigurationManager.AppSettings["User"] = this.txtBoxUser.Text;
                ConfigurationManager.AppSettings["Password"] = this.txtBoxPassword.Text;
                Properties.Settings.Default.Save();

                HelperOData oHelperData = new HelperOData(false);
                oHelperData.RemoveFileCookie();

                this.Close();
            }
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtBoxServer.Text) || string.IsNullOrEmpty(this.txtBoxUser.Text) || string.IsNullOrEmpty(this.txtBoxPassword.Text))
            {
                System.Resources.ResourceManager rm = new System.Resources.ResourceManager(typeof(FrmConnection));
                MessageBox.Show("Servidor, Usuario y Clave son obligatorios, por favor revise.", "Error", MessageBoxButtons.RetryCancel);
            }
            else
            {
                ConfigurationManager.AppSettings["Server"] = this.txtBoxServer.Text;
                ConfigurationManager.AppSettings["User"] = this.txtBoxUser.Text;
                ConfigurationManager.AppSettings["Password"] = this.txtBoxPassword.Text;
                Properties.Settings.Default.Save();

                HelperOData oHelperData = new HelperOData(true);
                oHelperData.RemoveFileCookie();

                if (oHelperData.ValidateConnection())
                {
                    Cursor.Current = Cursors.Default;
                    MessageBox.Show("Conexión Ok", "Success", MessageBoxButtons.OK);
                }
                else
                {
                    Cursor.Current = Cursors.Default;
                    MessageBox.Show("Error", "Error", MessageBoxButtons.RetryCancel);
                }
            }
        }
    }
}
