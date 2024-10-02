namespace ensims.jess_client.Forms
{
    partial class frmProgress
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.lblUploading = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblJESSon = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.prgbar = new System.Windows.Forms.ProgressBar();
            this.lblFileUploadMessage = new System.Windows.Forms.Label();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.cmdRunInBackground = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Uploading:";
            // 
            // lblUploading
            // 
            this.lblUploading.AutoSize = true;
            this.lblUploading.Location = new System.Drawing.Point(82, 13);
            this.lblUploading.Name = "lblUploading";
            this.lblUploading.Size = new System.Drawing.Size(92, 13);
            this.lblUploading.TabIndex = 1;
            this.lblUploading.Text = "Uploade file name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "To JESS on:";
            // 
            // lblJESSon
            // 
            this.lblJESSon.AutoSize = true;
            this.lblJESSon.Location = new System.Drawing.Point(82, 46);
            this.lblJESSon.Name = "lblJESSon";
            this.lblJESSon.Size = new System.Drawing.Size(67, 13);
            this.lblJESSon.TabIndex = 3;
            this.lblJESSon.Text = "JESS Server";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Progress:";
            // 
            // prgbar
            // 
            this.prgbar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.prgbar.Location = new System.Drawing.Point(82, 83);
            this.prgbar.Name = "prgbar";
            this.prgbar.Size = new System.Drawing.Size(432, 23);
            this.prgbar.Step = 1;
            this.prgbar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.prgbar.TabIndex = 5;
            // 
            // lblFileUploadMessage
            // 
            this.lblFileUploadMessage.AutoSize = true;
            this.lblFileUploadMessage.Location = new System.Drawing.Point(13, 131);
            this.lblFileUploadMessage.Name = "lblFileUploadMessage";
            this.lblFileUploadMessage.Size = new System.Drawing.Size(250, 13);
            this.lblFileUploadMessage.TabIndex = 6;
            this.lblFileUploadMessage.Text = "Files have been uploaded successfully. Access ID: ";
            // 
            // cmdCancel
            // 
            this.cmdCancel.Location = new System.Drawing.Point(331, 170);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(69, 26);
            this.cmdCancel.TabIndex = 1;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // cmdRunInBackground
            // 
            this.cmdRunInBackground.Location = new System.Drawing.Point(168, 170);
            this.cmdRunInBackground.Name = "cmdRunInBackground";
            this.cmdRunInBackground.Size = new System.Drawing.Size(148, 26);
            this.cmdRunInBackground.TabIndex = 0;
            this.cmdRunInBackground.Text = "Run in background";
            this.cmdRunInBackground.UseVisualStyleBackColor = true;
            this.cmdRunInBackground.Click += new System.EventHandler(this.cmdRunInBackground_Click);
            // 
            // frmProgress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(526, 211);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdRunInBackground);
            this.Controls.Add(this.lblFileUploadMessage);
            this.Controls.Add(this.prgbar);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblJESSon);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblUploading);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmProgress";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Upload Progress";
            this.Load += new System.EventHandler(this.frmProgress_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblUploading;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblJESSon;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar prgbar;
        private System.Windows.Forms.Label lblFileUploadMessage;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button cmdRunInBackground;
    }
}