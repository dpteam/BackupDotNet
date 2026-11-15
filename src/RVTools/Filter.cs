using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using RVTools.Properties;

namespace RVTools;

public class Filter : Form
{
	private static readonly ILog logger = LogManager.GetLogger(typeof(Filter));

	private IContainer components;

	private CheckBox cbTemplates;

	private GroupBox groupBox1;

	private CheckBox cbVM;

	private CheckBox cbPoweredOn;

	private CheckBox cbSuspended;

	private CheckBox cbPoweredOff;

	private Button btSetDefaults;

	private Button btOK;

	private GroupBox groupBox3;

	private ComboBox cbDatacenter;

	private ComboBox cbHost;

	private Label label3;

	private ComboBox cbCluster;

	private Label label2;

	private Label label1;

	private GroupBox groupBox2;

	private RadioButton rbExactly;

	private RadioButton rbContains;

	private RadioButton rbStartsWith;

	private Label label4;

	private TextBox textVMName;

	private GroupBox groupBox4;

	private RadioButton rbExactlyNotes;

	private RadioButton rbContainsNotes;

	private RadioButton rbStartsWithNotes;

	private Label label5;

	private TextBox textNotes;

	private GroupBox groupBox5;

	private CheckBox cbSaveFilter;

	private CheckBox cbSRMPlaceholders;

	public Filter()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		InitializeComponent();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void Filter_Load(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		cbVM.Checked = FilterClass.VirtualMachine;
		cbTemplates.Checked = FilterClass.Template;
		cbSRMPlaceholders.Checked = FilterClass.SRMPlaceholder;
		cbPoweredOn.Checked = FilterClass.PoweredOn;
		cbPoweredOff.Checked = FilterClass.PoweredOff;
		cbSuspended.Checked = FilterClass.Suspended;
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			string datacenterName = VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i));
			if (datacenterName != null && !cbDatacenter.Items.Contains(datacenterName))
			{
				cbDatacenter.Items.Add(datacenterName);
			}
		}
		cbDatacenter.Text = FilterClass.DataCenter;
		PopulateClusterComboBox();
		cbCluster.Text = FilterClass.Cluster;
		PopulateHostComboBox();
		cbHost.Text = FilterClass.Host;
		textVMName.Text = FilterClass.VMName;
		rbExactly.Checked = FilterClass.VMNameExactly;
		rbStartsWith.Checked = FilterClass.VMNameStartsWith;
		rbContains.Checked = FilterClass.VMNameContains;
		textNotes.Text = FilterClass.Notes;
		rbExactlyNotes.Checked = FilterClass.NotesExactly;
		rbStartsWithNotes.Checked = FilterClass.NotesStartsWith;
		rbContainsNotes.Checked = FilterClass.NotesContains;
		cbSaveFilter.Checked = FilterClass.SaveFilter;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void btSetDefaults_Click(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		cbVM.Checked = FilterClass.DefaultVirtualMachine;
		cbTemplates.Checked = FilterClass.DefaultTemplate;
		cbSRMPlaceholders.Checked = FilterClass.DefaultSRMPlaceholder;
		cbPoweredOn.Checked = FilterClass.DefaultPoweredOn;
		cbPoweredOff.Checked = FilterClass.DefaultPoweredOff;
		cbSuspended.Checked = FilterClass.DefaultSuspended;
		cbDatacenter.Text = FilterClass.DefaultDataCenter;
		cbCluster.Text = FilterClass.DefaultCluster;
		cbHost.Text = FilterClass.DefaultHost;
		textVMName.Text = FilterClass.DefaultVMName;
		rbExactly.Checked = FilterClass.DefaultVMNameExactly;
		rbStartsWith.Checked = FilterClass.DefaultVMNameStartsWith;
		rbContains.Checked = FilterClass.DefaultVMNameContains;
		textNotes.Text = FilterClass.DefaultNotes;
		rbExactlyNotes.Checked = FilterClass.DefaultNotesExactly;
		rbStartsWithNotes.Checked = FilterClass.DefaultNotesStartsWith;
		rbContainsNotes.Checked = FilterClass.DefaultNotesContains;
		cbSaveFilter.Checked = FilterClass.DefaultSaveFilter;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void btCancel_Click(object sender, EventArgs e)
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

	private void btOK_Click(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		FilterClass.VirtualMachine = cbVM.Checked;
		FilterClass.Template = cbTemplates.Checked;
		FilterClass.SRMPlaceholder = cbSRMPlaceholders.Checked;
		FilterClass.PoweredOn = cbPoweredOn.Checked;
		FilterClass.PoweredOff = cbPoweredOff.Checked;
		FilterClass.Suspended = cbSuspended.Checked;
		FilterClass.DataCenter = cbDatacenter.Text;
		FilterClass.Cluster = cbCluster.Text;
		FilterClass.Host = cbHost.Text;
		FilterClass.VMName = textVMName.Text;
		FilterClass.VMNameExactly = rbExactly.Checked;
		FilterClass.VMNameStartsWith = rbStartsWith.Checked;
		FilterClass.VMNameContains = rbContains.Checked;
		FilterClass.Notes = textNotes.Text;
		FilterClass.NotesExactly = rbExactlyNotes.Checked;
		FilterClass.NotesStartsWith = rbStartsWithNotes.Checked;
		FilterClass.NotesContains = rbContainsNotes.Checked;
		FilterClass.SaveFilter = cbSaveFilter.Checked;
		Settings.Default.FVirtualMachine = cbVM.Checked;
		Settings.Default.FTemplate = cbTemplates.Checked;
		Settings.Default.FSRMPlaceholder = cbSRMPlaceholders.Checked;
		Settings.Default.FPoweredOn = cbPoweredOn.Checked;
		Settings.Default.FPoweredOff = cbPoweredOff.Checked;
		Settings.Default.FSuspended = cbSuspended.Checked;
		Settings.Default.FDataCenter = cbDatacenter.Text;
		Settings.Default.FCluster = cbCluster.Text;
		Settings.Default.FHost = cbHost.Text;
		Settings.Default.FVMName = textVMName.Text;
		Settings.Default.FVMNameExactly = rbExactly.Checked;
		Settings.Default.FVMNameStartsWith = rbStartsWith.Checked;
		Settings.Default.FVMNameContains = rbContains.Checked;
		Settings.Default.FNotes = textNotes.Text;
		Settings.Default.FNotesExactly = rbExactlyNotes.Checked;
		Settings.Default.FNotesStartsWith = rbStartsWithNotes.Checked;
		Settings.Default.FNotesContains = rbContainsNotes.Checked;
		Settings.Default.FSaveFilter = cbSaveFilter.Checked;
		Settings.Default.Save();
		Close();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void cbVM_CheckedChanged(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		cbPoweredOn.Enabled = cbVM.Checked;
		cbPoweredOff.Enabled = cbVM.Checked;
		cbSuspended.Enabled = cbVM.Checked;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void cbDatacenter_SelectedValueChanged(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		PopulateClusterComboBox();
		cbCluster.Text = FilterClass.DefaultCluster;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void PopulateClusterComboBox()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		cbCluster.Items.Clear();
		cbCluster.Items.Add("ALL");
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if (VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i)) == cbDatacenter.Text || cbDatacenter.Text == "ALL")
			{
				string clusterName = VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i));
				if (clusterName != null && !cbCluster.Items.Contains(clusterName))
				{
					cbCluster.Items.Add(clusterName);
				}
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void cbCluster_SelectedValueChanged(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		PopulateHostComboBox();
		cbHost.Text = FilterClass.DefaultHost;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void PopulateHostComboBox()
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		cbHost.Items.Clear();
		cbHost.Items.Add("ALL");
		for (int i = 0; i < VISDK.InventoryCounter; i++)
		{
			if ((VISDK.GetDatacenterName(VISDK.GetSummaryRuntimeHost(i)) == cbDatacenter.Text || cbDatacenter.Text == "ALL") && (VISDK.GetClusterName(VISDK.GetSummaryRuntimeHost(i)) == cbCluster.Text || cbCluster.Text == "ALL"))
			{
				string hostName = VISDK.GetHostName(VISDK.GetSummaryRuntimeHost(i));
				if (hostName != null && !cbHost.Items.Contains(hostName))
				{
					cbHost.Items.Add(hostName);
				}
			}
		}
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RVTools.Filter));
		this.cbTemplates = new System.Windows.Forms.CheckBox();
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this.cbSuspended = new System.Windows.Forms.CheckBox();
		this.cbVM = new System.Windows.Forms.CheckBox();
		this.cbPoweredOff = new System.Windows.Forms.CheckBox();
		this.cbPoweredOn = new System.Windows.Forms.CheckBox();
		this.btSetDefaults = new System.Windows.Forms.Button();
		this.btOK = new System.Windows.Forms.Button();
		this.groupBox3 = new System.Windows.Forms.GroupBox();
		this.cbHost = new System.Windows.Forms.ComboBox();
		this.label3 = new System.Windows.Forms.Label();
		this.cbCluster = new System.Windows.Forms.ComboBox();
		this.label2 = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		this.cbDatacenter = new System.Windows.Forms.ComboBox();
		this.groupBox2 = new System.Windows.Forms.GroupBox();
		this.rbExactly = new System.Windows.Forms.RadioButton();
		this.rbContains = new System.Windows.Forms.RadioButton();
		this.rbStartsWith = new System.Windows.Forms.RadioButton();
		this.label4 = new System.Windows.Forms.Label();
		this.textVMName = new System.Windows.Forms.TextBox();
		this.groupBox4 = new System.Windows.Forms.GroupBox();
		this.rbExactlyNotes = new System.Windows.Forms.RadioButton();
		this.rbContainsNotes = new System.Windows.Forms.RadioButton();
		this.rbStartsWithNotes = new System.Windows.Forms.RadioButton();
		this.label5 = new System.Windows.Forms.Label();
		this.textNotes = new System.Windows.Forms.TextBox();
		this.groupBox5 = new System.Windows.Forms.GroupBox();
		this.cbSaveFilter = new System.Windows.Forms.CheckBox();
		this.cbSRMPlaceholders = new System.Windows.Forms.CheckBox();
		this.groupBox1.SuspendLayout();
		this.groupBox3.SuspendLayout();
		this.groupBox2.SuspendLayout();
		this.groupBox4.SuspendLayout();
		this.groupBox5.SuspendLayout();
		base.SuspendLayout();
		this.cbTemplates.AutoSize = true;
		this.cbTemplates.Checked = true;
		this.cbTemplates.CheckState = System.Windows.Forms.CheckState.Checked;
		this.cbTemplates.Location = new System.Drawing.Point(14, 110);
		this.cbTemplates.Name = "cbTemplates";
		this.cbTemplates.Size = new System.Drawing.Size(125, 17);
		this.cbTemplates.TabIndex = 4;
		this.cbTemplates.Text = "Templates are visible";
		this.cbTemplates.UseVisualStyleBackColor = true;
		this.groupBox1.Controls.Add(this.cbSRMPlaceholders);
		this.groupBox1.Controls.Add(this.cbSuspended);
		this.groupBox1.Controls.Add(this.cbVM);
		this.groupBox1.Controls.Add(this.cbPoweredOff);
		this.groupBox1.Controls.Add(this.cbTemplates);
		this.groupBox1.Controls.Add(this.cbPoweredOn);
		this.groupBox1.Location = new System.Drawing.Point(18, 9);
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.Size = new System.Drawing.Size(367, 160);
		this.groupBox1.TabIndex = 4;
		this.groupBox1.TabStop = false;
		this.cbSuspended.AutoSize = true;
		this.cbSuspended.Checked = true;
		this.cbSuspended.CheckState = System.Windows.Forms.CheckState.Checked;
		this.cbSuspended.Location = new System.Drawing.Point(34, 87);
		this.cbSuspended.Name = "cbSuspended";
		this.cbSuspended.Size = new System.Drawing.Size(209, 17);
		this.cbSuspended.TabIndex = 3;
		this.cbSuspended.Text = "Suspended virtual machines are visible";
		this.cbSuspended.UseVisualStyleBackColor = true;
		this.cbVM.AutoSize = true;
		this.cbVM.Checked = true;
		this.cbVM.CheckState = System.Windows.Forms.CheckState.Checked;
		this.cbVM.Location = new System.Drawing.Point(14, 19);
		this.cbVM.Name = "cbVM";
		this.cbVM.Size = new System.Drawing.Size(153, 17);
		this.cbVM.TabIndex = 0;
		this.cbVM.Text = "Virtual machines are visible";
		this.cbVM.UseVisualStyleBackColor = true;
		this.cbVM.CheckedChanged += new System.EventHandler(this.cbVM_CheckedChanged);
		this.cbPoweredOff.AutoSize = true;
		this.cbPoweredOff.Checked = true;
		this.cbPoweredOff.CheckState = System.Windows.Forms.CheckState.Checked;
		this.cbPoweredOff.Location = new System.Drawing.Point(34, 65);
		this.cbPoweredOff.Name = "cbPoweredOff";
		this.cbPoweredOff.Size = new System.Drawing.Size(214, 17);
		this.cbPoweredOff.TabIndex = 2;
		this.cbPoweredOff.Text = "Powered Off virtual machines are visible";
		this.cbPoweredOff.UseVisualStyleBackColor = true;
		this.cbPoweredOn.AutoSize = true;
		this.cbPoweredOn.Checked = true;
		this.cbPoweredOn.CheckState = System.Windows.Forms.CheckState.Checked;
		this.cbPoweredOn.Location = new System.Drawing.Point(34, 42);
		this.cbPoweredOn.Name = "cbPoweredOn";
		this.cbPoweredOn.Size = new System.Drawing.Size(214, 17);
		this.cbPoweredOn.TabIndex = 1;
		this.cbPoweredOn.Text = "Powered On virtual machines are visible";
		this.cbPoweredOn.UseVisualStyleBackColor = true;
		this.btSetDefaults.Location = new System.Drawing.Point(18, 521);
		this.btSetDefaults.Name = "btSetDefaults";
		this.btSetDefaults.Size = new System.Drawing.Size(100, 23);
		this.btSetDefaults.TabIndex = 2;
		this.btSetDefaults.Text = "&Reset";
		this.btSetDefaults.UseVisualStyleBackColor = true;
		this.btSetDefaults.Click += new System.EventHandler(this.btSetDefaults_Click);
		this.btOK.Location = new System.Drawing.Point(285, 521);
		this.btOK.Name = "btOK";
		this.btOK.Size = new System.Drawing.Size(100, 23);
		this.btOK.TabIndex = 3;
		this.btOK.Text = "&Ok";
		this.btOK.UseVisualStyleBackColor = true;
		this.btOK.Click += new System.EventHandler(this.btOK_Click);
		this.groupBox3.Controls.Add(this.cbHost);
		this.groupBox3.Controls.Add(this.label3);
		this.groupBox3.Controls.Add(this.cbCluster);
		this.groupBox3.Controls.Add(this.label2);
		this.groupBox3.Controls.Add(this.label1);
		this.groupBox3.Controls.Add(this.cbDatacenter);
		this.groupBox3.Location = new System.Drawing.Point(18, 181);
		this.groupBox3.Name = "groupBox3";
		this.groupBox3.Size = new System.Drawing.Size(367, 110);
		this.groupBox3.TabIndex = 5;
		this.groupBox3.TabStop = false;
		this.groupBox3.Text = " Hosted by ";
		this.cbHost.FormattingEnabled = true;
		this.cbHost.Items.AddRange(new object[1] { "ALL" });
		this.cbHost.Location = new System.Drawing.Point(95, 77);
		this.cbHost.Name = "cbHost";
		this.cbHost.Size = new System.Drawing.Size(257, 21);
		this.cbHost.Sorted = true;
		this.cbHost.TabIndex = 2;
		this.label3.AutoSize = true;
		this.label3.Location = new System.Drawing.Point(12, 81);
		this.label3.Name = "label3";
		this.label3.Size = new System.Drawing.Size(29, 13);
		this.label3.TabIndex = 4;
		this.label3.Text = "Host";
		this.cbCluster.FormattingEnabled = true;
		this.cbCluster.Items.AddRange(new object[1] { "ALL" });
		this.cbCluster.Location = new System.Drawing.Point(94, 49);
		this.cbCluster.Name = "cbCluster";
		this.cbCluster.Size = new System.Drawing.Size(257, 21);
		this.cbCluster.Sorted = true;
		this.cbCluster.TabIndex = 1;
		this.cbCluster.SelectedValueChanged += new System.EventHandler(this.cbCluster_SelectedValueChanged);
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(12, 53);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(39, 13);
		this.label2.TabIndex = 2;
		this.label2.Text = "Cluster";
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(12, 24);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(60, 13);
		this.label1.TabIndex = 1;
		this.label1.Text = "Datacenter";
		this.cbDatacenter.FormattingEnabled = true;
		this.cbDatacenter.Items.AddRange(new object[1] { "ALL" });
		this.cbDatacenter.Location = new System.Drawing.Point(94, 20);
		this.cbDatacenter.Name = "cbDatacenter";
		this.cbDatacenter.Size = new System.Drawing.Size(257, 21);
		this.cbDatacenter.Sorted = true;
		this.cbDatacenter.TabIndex = 0;
		this.cbDatacenter.SelectedValueChanged += new System.EventHandler(this.cbDatacenter_SelectedValueChanged);
		this.groupBox2.Controls.Add(this.rbExactly);
		this.groupBox2.Controls.Add(this.rbContains);
		this.groupBox2.Controls.Add(this.rbStartsWith);
		this.groupBox2.Controls.Add(this.label4);
		this.groupBox2.Controls.Add(this.textVMName);
		this.groupBox2.Location = new System.Drawing.Point(18, 301);
		this.groupBox2.Name = "groupBox2";
		this.groupBox2.Size = new System.Drawing.Size(367, 68);
		this.groupBox2.TabIndex = 0;
		this.groupBox2.TabStop = false;
		this.groupBox2.Text = " Virtual Machine / Template ";
		this.rbExactly.AutoSize = true;
		this.rbExactly.Location = new System.Drawing.Point(293, 45);
		this.rbExactly.Name = "rbExactly";
		this.rbExactly.Size = new System.Drawing.Size(59, 17);
		this.rbExactly.TabIndex = 4;
		this.rbExactly.TabStop = true;
		this.rbExactly.Text = "Exactly";
		this.rbExactly.UseVisualStyleBackColor = true;
		this.rbContains.AutoSize = true;
		this.rbContains.Location = new System.Drawing.Point(95, 45);
		this.rbContains.Name = "rbContains";
		this.rbContains.Size = new System.Drawing.Size(66, 17);
		this.rbContains.TabIndex = 2;
		this.rbContains.TabStop = true;
		this.rbContains.Text = "Contains";
		this.rbContains.UseVisualStyleBackColor = true;
		this.rbStartsWith.AutoSize = true;
		this.rbStartsWith.Location = new System.Drawing.Point(188, 45);
		this.rbStartsWith.Name = "rbStartsWith";
		this.rbStartsWith.Size = new System.Drawing.Size(74, 17);
		this.rbStartsWith.TabIndex = 3;
		this.rbStartsWith.TabStop = true;
		this.rbStartsWith.Text = "Starts with";
		this.rbStartsWith.UseVisualStyleBackColor = true;
		this.label4.AutoSize = true;
		this.label4.Location = new System.Drawing.Point(11, 26);
		this.label4.Name = "label4";
		this.label4.Size = new System.Drawing.Size(35, 13);
		this.label4.TabIndex = 5;
		this.label4.Text = "Name";
		this.textVMName.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
		this.textVMName.Location = new System.Drawing.Point(95, 19);
		this.textVMName.Name = "textVMName";
		this.textVMName.Size = new System.Drawing.Size(256, 20);
		this.textVMName.TabIndex = 1;
		this.groupBox4.Controls.Add(this.rbExactlyNotes);
		this.groupBox4.Controls.Add(this.rbContainsNotes);
		this.groupBox4.Controls.Add(this.rbStartsWithNotes);
		this.groupBox4.Controls.Add(this.label5);
		this.groupBox4.Controls.Add(this.textNotes);
		this.groupBox4.Location = new System.Drawing.Point(18, 380);
		this.groupBox4.Name = "groupBox4";
		this.groupBox4.Size = new System.Drawing.Size(367, 68);
		this.groupBox4.TabIndex = 1;
		this.groupBox4.TabStop = false;
		this.groupBox4.Text = " Annotation / Custom Fields  ";
		this.rbExactlyNotes.AutoSize = true;
		this.rbExactlyNotes.Location = new System.Drawing.Point(292, 45);
		this.rbExactlyNotes.Name = "rbExactlyNotes";
		this.rbExactlyNotes.Size = new System.Drawing.Size(59, 17);
		this.rbExactlyNotes.TabIndex = 1;
		this.rbExactlyNotes.TabStop = true;
		this.rbExactlyNotes.Text = "Exactly";
		this.rbExactlyNotes.UseVisualStyleBackColor = true;
		this.rbContainsNotes.AutoSize = true;
		this.rbContainsNotes.Location = new System.Drawing.Point(95, 45);
		this.rbContainsNotes.Name = "rbContainsNotes";
		this.rbContainsNotes.Size = new System.Drawing.Size(66, 17);
		this.rbContainsNotes.TabIndex = 3;
		this.rbContainsNotes.TabStop = true;
		this.rbContainsNotes.Text = "Contains";
		this.rbContainsNotes.UseVisualStyleBackColor = true;
		this.rbStartsWithNotes.AutoSize = true;
		this.rbStartsWithNotes.Location = new System.Drawing.Point(187, 45);
		this.rbStartsWithNotes.Name = "rbStartsWithNotes";
		this.rbStartsWithNotes.Size = new System.Drawing.Size(74, 17);
		this.rbStartsWithNotes.TabIndex = 2;
		this.rbStartsWithNotes.TabStop = true;
		this.rbStartsWithNotes.Text = "Starts with";
		this.rbStartsWithNotes.UseVisualStyleBackColor = true;
		this.label5.AutoSize = true;
		this.label5.Location = new System.Drawing.Point(11, 26);
		this.label5.Name = "label5";
		this.label5.Size = new System.Drawing.Size(34, 13);
		this.label5.TabIndex = 5;
		this.label5.Text = "String";
		this.textNotes.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
		this.textNotes.Location = new System.Drawing.Point(95, 19);
		this.textNotes.Name = "textNotes";
		this.textNotes.Size = new System.Drawing.Size(256, 20);
		this.textNotes.TabIndex = 0;
		this.groupBox5.Controls.Add(this.cbSaveFilter);
		this.groupBox5.Location = new System.Drawing.Point(19, 454);
		this.groupBox5.Name = "groupBox5";
		this.groupBox5.Size = new System.Drawing.Size(367, 50);
		this.groupBox5.TabIndex = 6;
		this.groupBox5.TabStop = false;
		this.groupBox5.Text = " Filter ";
		this.cbSaveFilter.AutoSize = true;
		this.cbSaveFilter.Location = new System.Drawing.Point(14, 20);
		this.cbSaveFilter.Name = "cbSaveFilter";
		this.cbSaveFilter.Size = new System.Drawing.Size(288, 17);
		this.cbSaveFilter.TabIndex = 0;
		this.cbSaveFilter.Text = "Save filter and use it next time when RVTools is started.";
		this.cbSaveFilter.UseVisualStyleBackColor = true;
		this.cbSRMPlaceholders.AutoSize = true;
		this.cbSRMPlaceholders.Checked = true;
		this.cbSRMPlaceholders.CheckState = System.Windows.Forms.CheckState.Checked;
		this.cbSRMPlaceholders.Location = new System.Drawing.Point(15, 134);
		this.cbSRMPlaceholders.Name = "cbSRMPlaceholders";
		this.cbSRMPlaceholders.Size = new System.Drawing.Size(164, 17);
		this.cbSRMPlaceholders.TabIndex = 5;
		this.cbSRMPlaceholders.Text = "SRM Placeholders are visible";
		this.cbSRMPlaceholders.UseVisualStyleBackColor = true;
		base.AcceptButton = this.btOK;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(405, 564);
		base.Controls.Add(this.groupBox5);
		base.Controls.Add(this.groupBox4);
		base.Controls.Add(this.groupBox2);
		base.Controls.Add(this.groupBox3);
		base.Controls.Add(this.btOK);
		base.Controls.Add(this.btSetDefaults);
		base.Controls.Add(this.groupBox1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "Filter";
		base.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "RVTools Filter";
		base.Load += new System.EventHandler(this.Filter_Load);
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		this.groupBox3.ResumeLayout(false);
		this.groupBox3.PerformLayout();
		this.groupBox2.ResumeLayout(false);
		this.groupBox2.PerformLayout();
		this.groupBox4.ResumeLayout(false);
		this.groupBox4.PerformLayout();
		this.groupBox5.ResumeLayout(false);
		this.groupBox5.PerformLayout();
		base.ResumeLayout(false);
	}
}
