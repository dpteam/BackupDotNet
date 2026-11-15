using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using RVTools.Properties;

namespace RVTools;

internal class AboutBox : Form
{
	private static readonly ILog logger = LogManager.GetLogger(typeof(AboutBox));

	private IContainer components;

	private Panel panel1;

	private Label lblVersion;

	private PictureBox pictureBox1;

	private Label label1;

	private Label label2;

	private Button button1;

	private LinkLabel WebLinkLabel;

	private LinkLabel MaiLinkLabel;

	private LinkLabel linkLabel1;

	private Label versionInfoLabel;

	public string AssemblyTitle
	{
		get
		{
			object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), inherit: false);
			if (customAttributes.Length != 0)
			{
				AssemblyTitleAttribute assemblyTitleAttribute = (AssemblyTitleAttribute)customAttributes[0];
				if (assemblyTitleAttribute.Title != "")
				{
					return assemblyTitleAttribute.Title;
				}
			}
			return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
		}
	}

	public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

	public string AssemblyDescription
	{
		get
		{
			object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), inherit: false);
			if (customAttributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyDescriptionAttribute)customAttributes[0]).Description;
		}
	}

	public string AssemblyProduct
	{
		get
		{
			object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), inherit: false);
			if (customAttributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyProductAttribute)customAttributes[0]).Product;
		}
	}

	public string AssemblyCopyright
	{
		get
		{
			object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), inherit: false);
			if (customAttributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyCopyrightAttribute)customAttributes[0]).Copyright;
		}
	}

	public string AssemblyCompany
	{
		get
		{
			object[] customAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), inherit: false);
			if (customAttributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyCompanyAttribute)customAttributes[0]).Company;
		}
	}

	public AboutBox()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		InitializeComponent();
		panel1.BackColor = Color.FromArgb(116, 152, 191);
		lblVersion.Text = "RVTools " + Assembly.GetExecutingAssembly().GetName().Version;
		versionInfoLabel.Text = "v4.6.1.3  Standard Edition [Production]";
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void button1_Click(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Close();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void MailLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Process.Start("mailto:rvtools@dell.com?subject=RVTools");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void WebLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Process.Start("http://www.robware.net");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		Process.Start("http://www.twitter.com/rvtools");
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RVTools.AboutBox));
		this.panel1 = new System.Windows.Forms.Panel();
		this.lblVersion = new System.Windows.Forms.Label();
		this.pictureBox1 = new System.Windows.Forms.PictureBox();
		this.label1 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this.button1 = new System.Windows.Forms.Button();
		this.linkLabel1 = new System.Windows.Forms.LinkLabel();
		this.WebLinkLabel = new System.Windows.Forms.LinkLabel();
		this.MaiLinkLabel = new System.Windows.Forms.LinkLabel();
		this.versionInfoLabel = new System.Windows.Forms.Label();
		this.panel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
		base.SuspendLayout();
		this.panel1.BackColor = System.Drawing.SystemColors.HotTrack;
		this.panel1.Controls.Add(this.lblVersion);
		this.panel1.Controls.Add(this.pictureBox1);
		this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
		this.panel1.Location = new System.Drawing.Point(0, 0);
		this.panel1.Margin = new System.Windows.Forms.Padding(0);
		this.panel1.Name = "panel1";
		this.panel1.Size = new System.Drawing.Size(461, 92);
		this.panel1.TabIndex = 11;
		this.lblVersion.AutoSize = true;
		this.lblVersion.Font = new System.Drawing.Font("Verdana", 15.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.lblVersion.ForeColor = System.Drawing.Color.White;
		this.lblVersion.Location = new System.Drawing.Point(120, 33);
		this.lblVersion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.lblVersion.Name = "lblVersion";
		this.lblVersion.Size = new System.Drawing.Size(0, 32);
		this.lblVersion.TabIndex = 10;
		this.pictureBox1.Image = RVTools.Properties.Resources.rvtools_32x32px;
		this.pictureBox1.Location = new System.Drawing.Point(21, 12);
		this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
		this.pictureBox1.Name = "pictureBox1";
		this.pictureBox1.Size = new System.Drawing.Size(73, 68);
		this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
		this.pictureBox1.TabIndex = 9;
		this.pictureBox1.TabStop = false;
		this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.ForeColor = System.Drawing.Color.Red;
		this.label1.Location = new System.Drawing.Point(8, 112);
		this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(437, 28);
		this.label1.TabIndex = 12;
		this.label1.Text = "nice to haveware...";
		this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.label2.Font = new System.Drawing.Font("Verdana", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(8, 144);
		this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(441, 28);
		this.label2.TabIndex = 13;
		this.label2.Text = "Author : Dell Technologies";
		this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.button1.Location = new System.Drawing.Point(141, 278);
		this.button1.Margin = new System.Windows.Forms.Padding(4);
		this.button1.Name = "button1";
		this.button1.Size = new System.Drawing.Size(177, 38);
		this.button1.TabIndex = 15;
		this.button1.Text = "Ok";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(this.button1_Click);
		this.linkLabel1.Image = (System.Drawing.Image)resources.GetObject("linkLabel1.Image");
		this.linkLabel1.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.linkLabel1.Location = new System.Drawing.Point(139, 240);
		this.linkLabel1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.linkLabel1.Name = "linkLabel1";
		this.linkLabel1.Size = new System.Drawing.Size(180, 16);
		this.linkLabel1.TabIndex = 22;
		this.linkLabel1.TabStop = true;
		this.linkLabel1.Text = "twitter.com/rvtools";
		this.linkLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
		this.WebLinkLabel.Image = RVTools.Properties.Resources.world_go;
		this.WebLinkLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.WebLinkLabel.Location = new System.Drawing.Point(137, 203);
		this.WebLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.WebLinkLabel.Name = "WebLinkLabel";
		this.WebLinkLabel.Size = new System.Drawing.Size(227, 33);
		this.WebLinkLabel.TabIndex = 21;
		this.WebLinkLabel.TabStop = true;
		this.WebLinkLabel.Text = "www.robware.net/rvtools";
		this.WebLinkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.WebLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.WebLinkLabel_LinkClicked);
		this.MaiLinkLabel.Image = RVTools.Properties.Resources.email;
		this.MaiLinkLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
		this.MaiLinkLabel.Location = new System.Drawing.Point(137, 185);
		this.MaiLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.MaiLinkLabel.Name = "MaiLinkLabel";
		this.MaiLinkLabel.Size = new System.Drawing.Size(196, 16);
		this.MaiLinkLabel.TabIndex = 20;
		this.MaiLinkLabel.TabStop = true;
		this.MaiLinkLabel.Text = "rvtools@dell.com";
		this.MaiLinkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		this.MaiLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.MailLinkLabel_LinkClicked);
		this.versionInfoLabel.Font = new System.Drawing.Font("Verdana", 7.8f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.versionInfoLabel.ForeColor = System.Drawing.SystemColors.GrayText;
		this.versionInfoLabel.Location = new System.Drawing.Point(4, 320);
		this.versionInfoLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.versionInfoLabel.Name = "versionInfoLabel";
		this.versionInfoLabel.Size = new System.Drawing.Size(457, 21);
		this.versionInfoLabel.TabIndex = 23;
		this.versionInfoLabel.Text = "Version: xxx";
		this.versionInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 16f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(461, 343);
		base.Controls.Add(this.versionInfoLabel);
		base.Controls.Add(this.linkLabel1);
		base.Controls.Add(this.WebLinkLabel);
		base.Controls.Add(this.MaiLinkLabel);
		base.Controls.Add(this.button1);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.label1);
		base.Controls.Add(this.panel1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Margin = new System.Windows.Forms.Padding(4);
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "AboutBox";
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "About";
		this.panel1.ResumeLayout(false);
		this.panel1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
		base.ResumeLayout(false);
	}
}
