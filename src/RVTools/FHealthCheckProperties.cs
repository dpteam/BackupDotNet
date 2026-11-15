using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using RVTools.Properties;

namespace RVTools;

public class FHealthCheckProperties : Form
{
	private IContainer components;

	private GroupBox groupBox1;

	private CheckBox cbCheckCDROM;

	private CheckBox cbCheckUSB;

	private CheckBox cbCheckGuestStorage;

	private CheckBox cbCheckSnapShots;

	private NumericUpDown numCheckGuestStorage;

	private Label label1;

	private CheckBox cbCheckTools;

	private Label label2;

	private NumericUpDown numCheckHostStorage;

	private CheckBox cbCheckHostStorage;

	private Button buttonReset;

	private Button buttonOK;

	private NumericUpDown numCheckVMonStorage;

	private CheckBox cbCheckVMonStorage;

	private NumericUpDown numCheckVMPerCore;

	private CheckBox cbCheckVMPerCore;

	private CheckBox cbCheckZombies;

	private CheckBox cbCheckInconsistentFolderNames;

	private CheckBox cbCheckPerformanceTips;

	private CheckBox cbCheckSecurityIssues;

	private NumericUpDown numCheckCertificateExpire;

	private CheckBox cbCheckCertificateExpire;

	public FHealthCheckProperties()
	{
		InitializeComponent();
	}

	private void FHealthCheckProperties_Load(object sender, EventArgs e)
	{
		cbCheckCDROM.Checked = HealthClass.CheckCDROM;
		cbCheckUSB.Checked = HealthClass.CheckUSB;
		cbCheckSnapShots.Checked = HealthClass.CheckSnapshot;
		cbCheckTools.Checked = HealthClass.CheckTools;
		cbCheckZombies.Checked = HealthClass.CheckZombies;
		cbCheckInconsistentFolderNames.Checked = HealthClass.CheckInconsistentFolderNames;
		cbCheckSecurityIssues.Checked = HealthClass.CheckSecurityIssues;
		cbCheckPerformanceTips.Checked = HealthClass.CheckPerformanceTips;
		cbCheckGuestStorage.Checked = HealthClass.CheckGuestStorage;
		numCheckGuestStorage.Value = HealthClass.CheckGuestStoragePercentage;
		cbCheckHostStorage.Checked = HealthClass.CheckHostStorage;
		numCheckHostStorage.Value = HealthClass.CheckHostStoragePercentage;
		cbCheckVMPerCore.Checked = HealthClass.CheckVCPUsPerCore;
		numCheckVMPerCore.Value = HealthClass.CheckVCPUsPerCoreMaxvalue;
		cbCheckVMonStorage.Checked = HealthClass.CheckVMsOnStorage;
		numCheckVMonStorage.Value = HealthClass.CheckVMsOnStorageMaxvalue;
		cbCheckCertificateExpire.Checked = HealthClass.CheckCertificateExpire;
		numCheckCertificateExpire.Value = HealthClass.CheckCertificateExpireValue;
	}

	private void buttonOK_Click(object sender, EventArgs e)
	{
		Settings.Default.HCCDROM = cbCheckCDROM.Checked;
		Settings.Default.HCUSB = cbCheckUSB.Checked;
		Settings.Default.HCSnapshot = cbCheckSnapShots.Checked;
		Settings.Default.HCTools = cbCheckTools.Checked;
		Settings.Default.HCZombies = cbCheckZombies.Checked;
		Settings.Default.HCInconsistentFolderNames = cbCheckInconsistentFolderNames.Checked;
		Settings.Default.HCSecurityIssues = cbCheckSecurityIssues.Checked;
		Settings.Default.HCPerformanceTips = cbCheckPerformanceTips.Checked;
		Settings.Default.HCGuestStorage = cbCheckGuestStorage.Checked;
		Settings.Default.HCGuestStoragePercentage = (int)numCheckGuestStorage.Value;
		Settings.Default.HCHostStorage = cbCheckHostStorage.Checked;
		Settings.Default.HCHostStoragePercentage = (int)numCheckHostStorage.Value;
		Settings.Default.HCVMPerCore = cbCheckVMPerCore.Checked;
		Settings.Default.HCVMPerCoreMaxvalue = numCheckVMPerCore.Value;
		Settings.Default.HCVMsOnStorage = cbCheckVMonStorage.Checked;
		Settings.Default.HCVMsOnStorageMaxvalue = (int)numCheckVMonStorage.Value;
		Settings.Default.HCCertificateExpire = cbCheckCertificateExpire.Checked;
		Settings.Default.HCCertificateExpireDays = (int)numCheckCertificateExpire.Value;
		Settings.Default.Save();
		HealthClass.CheckCDROM = cbCheckCDROM.Checked;
		HealthClass.CheckUSB = cbCheckUSB.Checked;
		HealthClass.CheckSnapshot = cbCheckSnapShots.Checked;
		HealthClass.CheckTools = cbCheckTools.Checked;
		HealthClass.CheckZombies = cbCheckZombies.Checked;
		HealthClass.CheckInconsistentFolderNames = cbCheckInconsistentFolderNames.Checked;
		HealthClass.CheckSecurityIssues = cbCheckSecurityIssues.Checked;
		HealthClass.CheckPerformanceTips = cbCheckPerformanceTips.Checked;
		HealthClass.CheckGuestStorage = cbCheckGuestStorage.Checked;
		HealthClass.CheckGuestStoragePercentage = (int)numCheckGuestStorage.Value;
		HealthClass.CheckHostStorage = cbCheckHostStorage.Checked;
		HealthClass.CheckHostStoragePercentage = (int)numCheckHostStorage.Value;
		HealthClass.CheckVCPUsPerCore = cbCheckVMPerCore.Checked;
		HealthClass.CheckVCPUsPerCoreMaxvalue = numCheckVMPerCore.Value;
		HealthClass.CheckVMsOnStorage = cbCheckVMonStorage.Checked;
		HealthClass.CheckVMsOnStorageMaxvalue = (int)numCheckVMonStorage.Value;
		HealthClass.CheckCertificateExpire = cbCheckCertificateExpire.Checked;
		HealthClass.CheckCertificateExpireValue = (int)numCheckCertificateExpire.Value;
		Close();
	}

	private void buttonReset_Click(object sender, EventArgs e)
	{
		cbCheckCDROM.Checked = HealthClass.DefaultCheckCDROM;
		cbCheckUSB.Checked = HealthClass.DefaultCheckUSB;
		cbCheckSnapShots.Checked = HealthClass.DefaultSnapshot;
		cbCheckTools.Checked = HealthClass.DefaultTools;
		cbCheckZombies.Checked = HealthClass.DefaultZombies;
		cbCheckInconsistentFolderNames.Checked = HealthClass.DefaultInconsistentFolderNames;
		cbCheckSecurityIssues.Checked = HealthClass.DefaultSecurityIssues;
		cbCheckPerformanceTips.Checked = HealthClass.DefaultPerformanceTips;
		cbCheckGuestStorage.Checked = HealthClass.DefaultCheckGuestStorage;
		numCheckGuestStorage.Value = HealthClass.DefaultCheckGuestStoragePercentage;
		cbCheckHostStorage.Checked = HealthClass.DefaultCheckHostStorage;
		numCheckHostStorage.Value = HealthClass.DefaultCheckHostStoragePercentage;
		cbCheckVMPerCore.Checked = HealthClass.DefaultCheckVCPUsPerCore;
		numCheckVMPerCore.Value = HealthClass.DefaultCheckVCPUsPerCoreMaxvalue;
		cbCheckVMonStorage.Checked = HealthClass.DefaultCheckVMsOnStorage;
		numCheckVMonStorage.Value = HealthClass.DefaultCheckVMsOnStorageMaxvalue;
		cbCheckCertificateExpire.Checked = HealthClass.DefaultCheckCertificateExpire;
		numCheckCertificateExpire.Value = HealthClass.DefaultCheckCertificateExpireValue;
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RVTools.FHealthCheckProperties));
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this.numCheckCertificateExpire = new System.Windows.Forms.NumericUpDown();
		this.cbCheckCertificateExpire = new System.Windows.Forms.CheckBox();
		this.cbCheckPerformanceTips = new System.Windows.Forms.CheckBox();
		this.cbCheckSecurityIssues = new System.Windows.Forms.CheckBox();
		this.cbCheckInconsistentFolderNames = new System.Windows.Forms.CheckBox();
		this.cbCheckZombies = new System.Windows.Forms.CheckBox();
		this.numCheckVMonStorage = new System.Windows.Forms.NumericUpDown();
		this.cbCheckVMonStorage = new System.Windows.Forms.CheckBox();
		this.numCheckVMPerCore = new System.Windows.Forms.NumericUpDown();
		this.cbCheckVMPerCore = new System.Windows.Forms.CheckBox();
		this.cbCheckTools = new System.Windows.Forms.CheckBox();
		this.label2 = new System.Windows.Forms.Label();
		this.numCheckHostStorage = new System.Windows.Forms.NumericUpDown();
		this.cbCheckHostStorage = new System.Windows.Forms.CheckBox();
		this.label1 = new System.Windows.Forms.Label();
		this.numCheckGuestStorage = new System.Windows.Forms.NumericUpDown();
		this.cbCheckGuestStorage = new System.Windows.Forms.CheckBox();
		this.cbCheckSnapShots = new System.Windows.Forms.CheckBox();
		this.cbCheckUSB = new System.Windows.Forms.CheckBox();
		this.cbCheckCDROM = new System.Windows.Forms.CheckBox();
		this.buttonReset = new System.Windows.Forms.Button();
		this.buttonOK = new System.Windows.Forms.Button();
		this.groupBox1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.numCheckCertificateExpire).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.numCheckVMonStorage).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.numCheckVMPerCore).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.numCheckHostStorage).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.numCheckGuestStorage).BeginInit();
		base.SuspendLayout();
		this.groupBox1.Controls.Add(this.numCheckCertificateExpire);
		this.groupBox1.Controls.Add(this.cbCheckCertificateExpire);
		this.groupBox1.Controls.Add(this.cbCheckPerformanceTips);
		this.groupBox1.Controls.Add(this.cbCheckSecurityIssues);
		this.groupBox1.Controls.Add(this.cbCheckInconsistentFolderNames);
		this.groupBox1.Controls.Add(this.cbCheckZombies);
		this.groupBox1.Controls.Add(this.numCheckVMonStorage);
		this.groupBox1.Controls.Add(this.cbCheckVMonStorage);
		this.groupBox1.Controls.Add(this.numCheckVMPerCore);
		this.groupBox1.Controls.Add(this.cbCheckVMPerCore);
		this.groupBox1.Controls.Add(this.cbCheckTools);
		this.groupBox1.Controls.Add(this.label2);
		this.groupBox1.Controls.Add(this.numCheckHostStorage);
		this.groupBox1.Controls.Add(this.cbCheckHostStorage);
		this.groupBox1.Controls.Add(this.label1);
		this.groupBox1.Controls.Add(this.numCheckGuestStorage);
		this.groupBox1.Controls.Add(this.cbCheckGuestStorage);
		this.groupBox1.Controls.Add(this.cbCheckSnapShots);
		this.groupBox1.Controls.Add(this.cbCheckUSB);
		this.groupBox1.Controls.Add(this.cbCheckCDROM);
		this.groupBox1.Location = new System.Drawing.Point(19, 13);
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.Size = new System.Drawing.Size(341, 391);
		this.groupBox1.TabIndex = 0;
		this.groupBox1.TabStop = false;
		this.groupBox1.Text = " Generate Health Check Message If ";
		this.numCheckCertificateExpire.Location = new System.Drawing.Point(273, 356);
		this.numCheckCertificateExpire.Maximum = new decimal(new int[4] { 9999, 0, 0, 0 });
		this.numCheckCertificateExpire.Name = "numCheckCertificateExpire";
		this.numCheckCertificateExpire.Size = new System.Drawing.Size(44, 20);
		this.numCheckCertificateExpire.TabIndex = 19;
		this.numCheckCertificateExpire.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
		this.numCheckCertificateExpire.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
		this.cbCheckCertificateExpire.AutoSize = true;
		this.cbCheckCertificateExpire.Location = new System.Drawing.Point(12, 357);
		this.cbCheckCertificateExpire.Name = "cbCheckCertificateExpire";
		this.cbCheckCertificateExpire.Size = new System.Drawing.Size(264, 17);
		this.cbCheckCertificateExpire.TabIndex = 18;
		this.cbCheckCertificateExpire.Text = " Number of days before certificate expires is < than";
		this.cbCheckCertificateExpire.UseVisualStyleBackColor = true;
		this.cbCheckPerformanceTips.AutoSize = true;
		this.cbCheckPerformanceTips.Location = new System.Drawing.Point(12, 181);
		this.cbCheckPerformanceTips.Name = "cbCheckPerformanceTips";
		this.cbCheckPerformanceTips.Size = new System.Drawing.Size(153, 17);
		this.cbCheckPerformanceTips.TabIndex = 17;
		this.cbCheckPerformanceTips.Text = "Performance tips are found";
		this.cbCheckPerformanceTips.UseVisualStyleBackColor = true;
		this.cbCheckSecurityIssues.AutoSize = true;
		this.cbCheckSecurityIssues.Location = new System.Drawing.Point(12, 158);
		this.cbCheckSecurityIssues.Name = "cbCheckSecurityIssues";
		this.cbCheckSecurityIssues.Size = new System.Drawing.Size(144, 17);
		this.cbCheckSecurityIssues.TabIndex = 16;
		this.cbCheckSecurityIssues.Text = "Security issues are found";
		this.cbCheckSecurityIssues.UseVisualStyleBackColor = true;
		this.cbCheckInconsistentFolderNames.AutoSize = true;
		this.cbCheckInconsistentFolderNames.Location = new System.Drawing.Point(12, 135);
		this.cbCheckInconsistentFolderNames.Name = "cbCheckInconsistentFolderNames";
		this.cbCheckInconsistentFolderNames.Size = new System.Drawing.Size(194, 17);
		this.cbCheckInconsistentFolderNames.TabIndex = 15;
		this.cbCheckInconsistentFolderNames.Text = "Inconsistent folder names are found";
		this.cbCheckInconsistentFolderNames.UseVisualStyleBackColor = true;
		this.cbCheckZombies.AutoSize = true;
		this.cbCheckZombies.Location = new System.Drawing.Point(12, 112);
		this.cbCheckZombies.Name = "cbCheckZombies";
		this.cbCheckZombies.Size = new System.Drawing.Size(215, 17);
		this.cbCheckZombies.TabIndex = 14;
		this.cbCheckZombies.Text = "Zombie files are found on the datastores";
		this.cbCheckZombies.UseVisualStyleBackColor = true;
		this.numCheckVMonStorage.Location = new System.Drawing.Point(273, 317);
		this.numCheckVMonStorage.Maximum = new decimal(new int[4] { 9999, 0, 0, 0 });
		this.numCheckVMonStorage.Name = "numCheckVMonStorage";
		this.numCheckVMonStorage.Size = new System.Drawing.Size(44, 20);
		this.numCheckVMonStorage.TabIndex = 13;
		this.numCheckVMonStorage.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
		this.numCheckVMonStorage.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
		this.cbCheckVMonStorage.AutoSize = true;
		this.cbCheckVMonStorage.Location = new System.Drawing.Point(12, 318);
		this.cbCheckVMonStorage.Name = "cbCheckVMonStorage";
		this.cbCheckVMonStorage.Size = new System.Drawing.Size(235, 17);
		this.cbCheckVMonStorage.TabIndex = 12;
		this.cbCheckVMonStorage.Text = "Number of running VMs per datastore > than";
		this.cbCheckVMonStorage.UseVisualStyleBackColor = true;
		this.numCheckVMPerCore.DecimalPlaces = 1;
		this.numCheckVMPerCore.Increment = new decimal(new int[4] { 5, 0, 0, 65536 });
		this.numCheckVMPerCore.Location = new System.Drawing.Point(273, 292);
		this.numCheckVMPerCore.Name = "numCheckVMPerCore";
		this.numCheckVMPerCore.Size = new System.Drawing.Size(44, 20);
		this.numCheckVMPerCore.TabIndex = 11;
		this.numCheckVMPerCore.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
		this.numCheckVMPerCore.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
		this.cbCheckVMPerCore.AutoSize = true;
		this.cbCheckVMPerCore.Location = new System.Drawing.Point(12, 293);
		this.cbCheckVMPerCore.Name = "cbCheckVMPerCore";
		this.cbCheckVMPerCore.Size = new System.Drawing.Size(259, 17);
		this.cbCheckVMPerCore.TabIndex = 10;
		this.cbCheckVMPerCore.Text = "Number of running virtual CPUs per core is > than";
		this.cbCheckVMPerCore.UseVisualStyleBackColor = true;
		this.cbCheckTools.AutoSize = true;
		this.cbCheckTools.Location = new System.Drawing.Point(12, 89);
		this.cbCheckTools.Name = "cbCheckTools";
		this.cbCheckTools.Size = new System.Drawing.Size(189, 17);
		this.cbCheckTools.TabIndex = 9;
		this.cbCheckTools.Text = "VMware tools status not \"toolsOK\"";
		this.cbCheckTools.UseVisualStyleBackColor = true;
		this.label2.AutoSize = true;
		this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label2.Location = new System.Drawing.Point(320, 251);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(16, 13);
		this.label2.TabIndex = 8;
		this.label2.Text = "%";
		this.numCheckHostStorage.Location = new System.Drawing.Point(273, 247);
		this.numCheckHostStorage.Name = "numCheckHostStorage";
		this.numCheckHostStorage.Size = new System.Drawing.Size(44, 20);
		this.numCheckHostStorage.TabIndex = 7;
		this.numCheckHostStorage.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
		this.numCheckHostStorage.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
		this.cbCheckHostStorage.AutoSize = true;
		this.cbCheckHostStorage.Location = new System.Drawing.Point(12, 248);
		this.cbCheckHostStorage.Name = "cbCheckHostStorage";
		this.cbCheckHostStorage.Size = new System.Drawing.Size(232, 17);
		this.cbCheckHostStorage.TabIndex = 6;
		this.cbCheckHostStorage.Text = "Free disk capacity on datastore is less than ";
		this.cbCheckHostStorage.UseVisualStyleBackColor = true;
		this.label1.AutoSize = true;
		this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this.label1.Location = new System.Drawing.Point(320, 226);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(16, 13);
		this.label1.TabIndex = 5;
		this.label1.Text = "%";
		this.numCheckGuestStorage.Location = new System.Drawing.Point(273, 222);
		this.numCheckGuestStorage.Name = "numCheckGuestStorage";
		this.numCheckGuestStorage.Size = new System.Drawing.Size(44, 20);
		this.numCheckGuestStorage.TabIndex = 4;
		this.numCheckGuestStorage.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
		this.numCheckGuestStorage.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
		this.cbCheckGuestStorage.AutoSize = true;
		this.cbCheckGuestStorage.Location = new System.Drawing.Point(12, 223);
		this.cbCheckGuestStorage.Name = "cbCheckGuestStorage";
		this.cbCheckGuestStorage.Size = new System.Drawing.Size(207, 17);
		this.cbCheckGuestStorage.TabIndex = 3;
		this.cbCheckGuestStorage.Text = "Free disk capacity in guest is less than";
		this.cbCheckGuestStorage.UseVisualStyleBackColor = true;
		this.cbCheckSnapShots.AutoSize = true;
		this.cbCheckSnapShots.Location = new System.Drawing.Point(12, 66);
		this.cbCheckSnapShots.Name = "cbCheckSnapShots";
		this.cbCheckSnapShots.Size = new System.Drawing.Size(157, 17);
		this.cbCheckSnapShots.TabIndex = 2;
		this.cbCheckSnapShots.Text = "VM has an active Snapshot";
		this.cbCheckSnapShots.UseVisualStyleBackColor = true;
		this.cbCheckUSB.AutoSize = true;
		this.cbCheckUSB.Location = new System.Drawing.Point(12, 43);
		this.cbCheckUSB.Name = "cbCheckUSB";
		this.cbCheckUSB.Size = new System.Drawing.Size(196, 17);
		this.cbCheckUSB.TabIndex = 1;
		this.cbCheckUSB.Text = "USB device is connected to the VM";
		this.cbCheckUSB.UseVisualStyleBackColor = true;
		this.cbCheckCDROM.AutoSize = true;
		this.cbCheckCDROM.Location = new System.Drawing.Point(12, 20);
		this.cbCheckCDROM.Name = "cbCheckCDROM";
		this.cbCheckCDROM.Size = new System.Drawing.Size(205, 17);
		this.cbCheckCDROM.TabIndex = 0;
		this.cbCheckCDROM.Text = "CDROM drive is connected to the VM";
		this.cbCheckCDROM.UseVisualStyleBackColor = true;
		this.buttonReset.Location = new System.Drawing.Point(19, 422);
		this.buttonReset.Name = "buttonReset";
		this.buttonReset.Size = new System.Drawing.Size(75, 23);
		this.buttonReset.TabIndex = 1;
		this.buttonReset.Text = "&Reset";
		this.buttonReset.UseVisualStyleBackColor = true;
		this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
		this.buttonOK.Location = new System.Drawing.Point(285, 422);
		this.buttonOK.Name = "buttonOK";
		this.buttonOK.Size = new System.Drawing.Size(75, 23);
		this.buttonOK.TabIndex = 2;
		this.buttonOK.Text = "&Ok";
		this.buttonOK.UseVisualStyleBackColor = true;
		this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
		base.AcceptButton = this.buttonOK;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(383, 460);
		base.Controls.Add(this.buttonOK);
		base.Controls.Add(this.buttonReset);
		base.Controls.Add(this.groupBox1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "FHealthCheckProperties";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "RVTools Health Check Properties";
		base.Load += new System.EventHandler(this.FHealthCheckProperties_Load);
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.numCheckCertificateExpire).EndInit();
		((System.ComponentModel.ISupportInitialize)this.numCheckVMonStorage).EndInit();
		((System.ComponentModel.ISupportInitialize)this.numCheckVMPerCore).EndInit();
		((System.ComponentModel.ISupportInitialize)this.numCheckHostStorage).EndInit();
		((System.ComponentModel.ISupportInitialize)this.numCheckGuestStorage).EndInit();
		base.ResumeLayout(false);
	}
}
