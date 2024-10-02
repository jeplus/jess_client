using System;
using System.Windows.Forms;
using ensims.jess_client.Classes;


namespace ensims.jess_client.Forms
{
    

    public partial class frmProgress : Form
    {
        public int ProgressBarValue
        {
            get { return prgbar.Value; }
            set { prgbar.Value = value; }
        }

        public string FileUploadMessage
        {
            get { return lblFileUploadMessage.Text; }
            set { lblFileUploadMessage.Text  = value; }
        }

        #region FormEvents
        
        public frmProgress()
        {
            InitializeComponent();
        }

        private void frmProgress_Load(object sender, EventArgs e)
        {
            lblUploading.Text = GlobalUtility.UploadzipFile;
            lblJESSon.Text = GlobalUtility.JESSServer;
            prgbar.Value = 0;         
        }

        #endregion

        #region FormControlEvents
        
        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close(); 
        }

        private void cmdRunInBackground_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        #endregion
        
    }
}
