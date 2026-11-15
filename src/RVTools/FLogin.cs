using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using log4net;
using RVTools.Properties;

namespace RVTools;

public class FLogin : Form
{
	public StringCollection VIHOSTS;

	private static readonly ILog logger = LogManager.GetLogger(typeof(FLogin));

	private IContainer components;

	private Button btLogin;

	private Button btCancel;

	private Label label2;

	private Label label1;

	private Label label3;

	private TextBox txtUsername;

	private TextBox txtPassword;

	private PictureBox pictureBox1;

	private Panel panel1;

	private Label lblVersionInfo;

	private Label label5;

	private ComboBox cbHost;

	private CheckBox cbUseWindowsCredentials;

	public FLogin()
	{
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod.Name + " Started ");
		}
		InitializeComponent();
		panel1.BackColor = Color.FromArgb(116, 152, 191);
		lblVersionInfo.Text = "RVTools " + Assembly.GetExecutingAssembly().GetName().Version.Major + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor + "." + Assembly.GetExecutingAssembly().GetName().Version.Build;
		_ = Assembly.GetExecutingAssembly().GetName().Version;
		try
		{
			VIHOSTS = new StringCollection();
			if (Settings.Default.VIHOSTS != null)
			{
				VIHOSTS = Settings.Default.VIHOSTS;
				StringEnumerator enumerator = VIHOSTS.GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						string current = enumerator.Current;
						if (logger.IsDebugEnabled)
						{
							MethodBase currentMethod2 = MethodBase.GetCurrentMethod();
							logger.Debug(currentMethod2.Name + " cbHost.Items.Add(VIHOST) " + current);
						}
						cbHost.Items.Add(current);
					}
				}
				finally
				{
					if (enumerator is IDisposable disposable)
					{
						disposable.Dispose();
					}
				}
				cbHost.Text = Settings.Default.LastHost;
			}
		}
		catch
		{
		}
		txtUsername.Text = WindowsIdentity.GetCurrent().Name;
		cbUseWindowsCredentials.Checked = Settings.Default.UseWindowsCredentials;
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod3 = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod3.Name + " Exit ");
		}
	}

	private void btLogin_Click(object sender, EventArgs e)
	{
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod.Name + " Started ");
		}
		if (string.IsNullOrEmpty(cbHost.Text))
		{
			MessageBox.Show("Please, enter Server name!", "RVTools", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod2 = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod2.Name + " Exit ");
			}
			return;
		}
		Cursor = Cursors.WaitCursor;
		VISDK.Server = cbHost.Text.Trim();
		VISDK.Username = txtUsername.Text.Trim();
		VISDK.Password = new NetworkCredential(null, txtPassword.Text).SecurePassword;
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod3 = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod3.Name + " VISDK.Server " + VISDK.Server);
			logger.Debug(currentMethod3.Name + " VISDK.Username " + VISDK.Username);
			logger.Debug(currentMethod3.Name + " VISDK.Password ********");
		}
		VISDK.Connect(cbUseWindowsCredentials.Checked);
		Settings.Default.LastHost = cbHost.Text;
		Settings.Default.UseWindowsCredentials = cbUseWindowsCredentials.Checked;
		Settings.Default.Save();
		Cursor = Cursors.Default;
		if (VISDK.Connected)
		{
			if (!VIHOSTS.Contains(cbHost.Text))
			{
				VIHOSTS.Add(cbHost.Text);
				Settings.Default.VIHOSTS = VIHOSTS;
				Settings.Default.Save();
			}
			Close();
		}
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod4 = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod4.Name + " Exit ");
		}
	}

	private void btCancel_Click(object sender, EventArgs e)
	{
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod.Name + " Started ");
		}
		Close();
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod2 = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod2.Name + " Exit ");
		}
	}

	private void cbUseWindowsCredentials_CheckedChanged(object sender, EventArgs e)
	{
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod.Name + " Started ");
		}
		if (cbUseWindowsCredentials.Checked)
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod2 = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod2.Name + " cbUseWindowsCredentials.Checked == true ");
			}
			txtUsername.Enabled = false;
			txtPassword.Enabled = false;
			txtUsername.Text = WindowsIdentity.GetCurrent().Name;
			txtPassword.Text = "";
		}
		else
		{
			if (logger.IsDebugEnabled)
			{
				MethodBase currentMethod3 = MethodBase.GetCurrentMethod();
				logger.Debug(currentMethod3.Name + " cbUseWindowsCredentials.Checked == false ");
			}
			txtUsername.Enabled = true;
			txtPassword.Enabled = true;
			txtUsername.Text = "";
			txtPassword.Text = "";
		}
		if (logger.IsDebugEnabled)
		{
			MethodBase currentMethod4 = MethodBase.GetCurrentMethod();
			logger.Debug(currentMethod4.Name + " Exit ");
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RVTools.FLogin));
		this.btLogin = new System.Windows.Forms.Button();
		this.btCancel = new System.Windows.Forms.Button();
		this.label2 = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this.label3 = new System.Windows.Forms.Label();
		this.txtUsername = new System.Windows.Forms.TextBox();
		this.txtPassword = new System.Windows.Forms.TextBox();
		this.panel1 = new System.Windows.Forms.Panel();
		this.lblVersionInfo = new System.Windows.Forms.Label();
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		this.label5 = new System.Windows.Forms.Label();
		this.cbHost = new System.Windows.Forms.ComboBox();
		this.cbUseWindowsCredentials = new System.Windows.Forms.CheckBox();
		this.panel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		base.SuspendLayout();
		this.btLogin.Location = new System.Drawing.Point(222, 261);
		this.btLogin.Name = "btLogin";
		this.btLogin.Size = new System.Drawing.Size(75, 23);
		this.btLogin.TabIndex = 4;
		this.btLogin.Text = "Login";
		this.btLogin.UseVisualStyleBackColor = true;
		this.btLogin.Click += new System.EventHandler(this.btLogin_Click);
		this.btCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btCancel.Location = new System.Drawing.Point(119, 261);
		this.btCancel.Name = "btCancel";
		this.btCancel.Size = new System.Drawing.Size(75, 23);
		this.btCancel.TabIndex = 5;
		this.btCancel.Text = "Cancel";
		this.btCancel.UseVisualStyleBackColor = true;
		this.btCancel.Click += new System.EventHandler(this.btCancel_Click);
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(14, 179);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(61, 13);
		this.label2.TabIndex = 3;
		this.label2.Text = "User name:";
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(14, 150);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(99, 13);
		this.label1.TabIndex = 4;
		this.label1.Text = "IP address / Name:";
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(14, 209);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(56, 13);
		this.label3.TabIndex = 5;
		this.label3.Text = "Password:";
		this.txtUsername.Enabled = false;
		this.txtUsername.Location = new System.Drawing.Point(119, 179);
		this.txtUsername.Name = "txtUsername";
		this.txtUsername.Size = new System.Drawing.Size(178, 20);
		this.txtUsername.TabIndex = 2;
		this.txtPassword.Enabled = false;
		this.txtPassword.Location = new System.Drawing.Point(119, 209);
		this.txtPassword.Name = "txtPassword";
		this.txtPassword.PasswordChar = '*';
		this.txtPassword.Size = new System.Drawing.Size(178, 20);
		this.txtPassword.TabIndex = 3;
		this.panel1.BackColor = System.Drawing.SystemColors.HotTrack;
		this.panel1.Controls.Add(this.lblVersionInfo);
		this.panel1.Controls.Add(this.pictureBox1);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel1.Location = new System.Drawing.Point(0, 0);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(322, 83);
		this.panel1.TabIndex = 10;
		this.lblVersionInfo.AutoSize = true;
		this.lblVersionInfo.Font = new System.Drawing.Font("Verdana", 18f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblVersionInfo.ForeColor = System.Drawing.Color.White;
		this.lblVersionInfo.Location = new System.Drawing.Point(110, 30);
		this.lblVersionInfo.Name = "lblVersionInfo";
		this.lblVersionInfo.Size = new System.Drawing.Size(0, 29);
		this.lblVersionInfo.TabIndex = 10;
		this.pictureBox1.Image = RVTools.Properties.Resources.rvtools_64x64;
		this.pictureBox1.Location = new System.Drawing.Point(15, 10);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(64, 64);
		this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
		this.pictureBox1.TabIndex = 9;
		this.pictureBox1.TabStop = false;
		this.label5.Location = new System.Drawing.Point(15, 95);
		this.label5.Name = "label5";
		this.label5.Size = new System.Drawing.Size(279, 47);
		this.label5.TabIndex = 11;
		this.label5.Text = "To directly manage a single host, enter the IP address or host name.To manage multiple hosts, enter the IP address or name of VirtualCenter Server.";
		this.cbHost.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
		this.cbHost.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
		this.cbHost.FormattingEnabled = true;
		this.cbHost.Location = new System.Drawing.Point(119, 148);
		this.cbHost.Name = "cbHost";
		this.cbHost.Size = new System.Drawing.Size(178, 21);
		this.cbHost.Sorted = true;
		this.cbHost.TabIndex = 1;
		this.cbUseWindowsCredentials.AutoSize = true;
		this.cbUseWindowsCredentials.Checked = true;
		this.cbUseWindowsCredentials.CheckState = System.Windows.Forms.CheckState.Checked;
		this.cbUseWindowsCredentials.Location = new System.Drawing.Point(119, 236);
		this.cbUseWindowsCredentials.Name = "cbUseWindowsCredentials";
		this.cbUseWindowsCredentials.Size = new System.Drawing.Size(184, 17);
		this.cbUseWindowsCredentials.TabIndex = 6;
		this.cbUseWindowsCredentials.Text = "&Use Windows session credentials";
		this.cbUseWindowsCredentials.UseVisualStyleBackColor = true;
		this.cbUseWindowsCredentials.CheckedChanged += new System.EventHandler(this.cbUseWindowsCredentials_CheckedChanged);
		base.AcceptButton = this.btLogin;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.CancelButton = this.btCancel;
		base.ClientSize = new System.Drawing.Size(322, 302);
		base.Controls.Add(this.cbUseWindowsCredentials);
		base.Controls.Add(this.cbHost);
		base.Controls.Add(this.label5);
		base.Controls.Add(this.panel1);
		base.Controls.Add(this.txtPassword);
		base.Controls.Add(this.txtUsername);
		base.Controls.Add(this.label3);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.btCancel);
		base.Controls.Add(this.btLogin);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "FLogin";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Login";
		this.panel1.ResumeLayout(false);
		this.panel1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
