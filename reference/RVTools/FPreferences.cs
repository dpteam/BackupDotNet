using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using RVTools.Properties;

namespace RVTools;

public class FPreferences : Form
{
	private static readonly ILog logger = LogManager.GetLogger(typeof(Filter));

	private IContainer components;

	private CheckBox cbvInfoShowFolder;

	private CheckBox cbvInfoShowAppName;

	private GroupBox groupBox1;

	private Button btnCancel;

	private Button btnOk;

	private GroupBox groupBox2;

	private CheckBox cbxAutomaticRefresh;

	private Label label1;

	private NumericUpDown numRefreshInterval;

	private GroupBox groupBox3;

	private CheckBox cbxExcludeAnnotations;

	private CheckBox cbxExcludeTags;

	private GroupBox groupBox4;

	private CheckBox cbxGetFriendlyNames;

	private GroupBox groupBox5;

	private CheckBox cbxGetFileInfo;

	private Label label2;

	private GroupBox groupBox6;

	private CheckBox checkBoxProductImprovement;

	public FPreferences()
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

	private void FPreferences_Load(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		cbvInfoShowFolder.Checked = PreferencesClass.vInfoShowFolder;
		cbvInfoShowAppName.Checked = PreferencesClass.vInfoShowAppName;
		cbxAutomaticRefresh.Checked = PreferencesClass.AutoRefresh;
		cbxGetFriendlyNames.Checked = PreferencesClass.GetFriendlyNames;
		cbxGetFileInfo.Checked = PreferencesClass.GetFileInfo;
		cbxExcludeAnnotations.Checked = PreferencesClass.ExcludeAnnotations;
		cbxExcludeTags.Checked = PreferencesClass.ExcludeTags;
		numRefreshInterval.Value = PreferencesClass.AutoRefreshInterval;
		checkBoxProductImprovement.Checked = RvToolsInstanceInfo.ProductImprovementOptIn;
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void btnOk_Click(object sender, EventArgs e)
	{
		MethodBase currentMethod = MethodBase.GetCurrentMethod();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Started");
		}
		PreferencesClass.vInfoShowFolder = cbvInfoShowFolder.Checked;
		PreferencesClass.vInfoShowAppName = cbvInfoShowAppName.Checked;
		PreferencesClass.AutoRefresh = cbxAutomaticRefresh.Checked;
		PreferencesClass.AutoRefreshInterval = (int)numRefreshInterval.Value;
		PreferencesClass.ExcludeAnnotations = cbxExcludeAnnotations.Checked;
		PreferencesClass.GetFriendlyNames = cbxGetFriendlyNames.Checked;
		PreferencesClass.GetFileInfo = cbxGetFileInfo.Checked;
		PreferencesClass.ExcludeTags = cbxExcludeTags.Checked;
		Settings.Default.vInfoShowFolder = cbvInfoShowFolder.Checked;
		Settings.Default.vInfoShowAppName = cbvInfoShowAppName.Checked;
		Settings.Default.AutoRefresh = cbxAutomaticRefresh.Checked;
		Settings.Default.AutoRefreshInterval = (int)numRefreshInterval.Value;
		Settings.Default.GetFriendlyNames = cbxGetFriendlyNames.Checked;
		Settings.Default.GetFileInfo = cbxGetFileInfo.Checked;
		Settings.Default.ExcludeAnnotations = cbxExcludeAnnotations.Checked;
		Settings.Default.ExcludeTags = cbxExcludeTags.Checked;
		if (RvToolsInstanceInfo.ProductImprovementOptIn != checkBoxProductImprovement.Checked)
		{
			RvToolsInstanceInfo.ProductImprovementOptIn = checkBoxProductImprovement.Checked;
			if (!checkBoxProductImprovement.Checked)
			{
				FMain.Telemetry.AddEvent("ProductImprovementOptInChange", "Out");
			}
			FMain.ProgramImprovementOptInChanged();
			if (checkBoxProductImprovement.Checked)
			{
				FMain.Telemetry.AddEvent("ProductImprovementOptInChange", "In");
			}
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " vInfoShowFolder = " + Settings.Default.vInfoShowFolder);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " vInfoShowAppName = " + Settings.Default.vInfoShowAppName);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " AutoRefresh = " + Settings.Default.AutoRefresh);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " AutoRefreshInterval = " + Settings.Default.AutoRefreshInterval);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exclude Annotations = " + Settings.Default.ExcludeAnnotations);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exclude Tags = " + Settings.Default.ExcludeTags);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Get Friendly Names = " + Settings.Default.GetFriendlyNames);
		}
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Get File Info = " + Settings.Default.GetFileInfo);
		}
		Settings.Default.Save();
		Close();
		if (logger.IsDebugEnabled)
		{
			logger.Debug(currentMethod.Name + " Exit");
		}
	}

	private void btnCancel_Click(object sender, EventArgs e)
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RVTools.FPreferences));
		this.cbvInfoShowFolder = new System.Windows.Forms.CheckBox();
		this.cbvInfoShowAppName = new System.Windows.Forms.CheckBox();
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this.btnCancel = new System.Windows.Forms.Button();
		this.btnOk = new System.Windows.Forms.Button();
		this.groupBox2 = new System.Windows.Forms.GroupBox();
		this.label1 = new System.Windows.Forms.Label();
		this.numRefreshInterval = new System.Windows.Forms.NumericUpDown();
		this.cbxAutomaticRefresh = new System.Windows.Forms.CheckBox();
		this.groupBox3 = new System.Windows.Forms.GroupBox();
		this.cbxExcludeTags = new System.Windows.Forms.CheckBox();
		this.cbxExcludeAnnotations = new System.Windows.Forms.CheckBox();
		this.groupBox4 = new System.Windows.Forms.GroupBox();
		this.cbxGetFriendlyNames = new System.Windows.Forms.CheckBox();
		this.groupBox5 = new System.Windows.Forms.GroupBox();
		this.label2 = new System.Windows.Forms.Label();
		this.cbxGetFileInfo = new System.Windows.Forms.CheckBox();
		this.groupBox6 = new System.Windows.Forms.GroupBox();
		this.checkBoxProductImprovement = new System.Windows.Forms.CheckBox();
		this.groupBox1.SuspendLayout();
		this.groupBox2.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.numRefreshInterval).BeginInit();
		this.groupBox3.SuspendLayout();
		this.groupBox4.SuspendLayout();
		this.groupBox5.SuspendLayout();
		this.groupBox6.SuspendLayout();
		base.SuspendLayout();
		this.cbvInfoShowFolder.AutoSize = true;
		this.cbvInfoShowFolder.Location = new System.Drawing.Point(9, 27);
		this.cbvInfoShowFolder.Margin = new System.Windows.Forms.Padding(4);
		this.cbvInfoShowFolder.Name = "cbvInfoShowFolder";
		this.cbvInfoShowFolder.Size = new System.Drawing.Size(135, 21);
		this.cbvInfoShowFolder.TabIndex = 0;
		this.cbvInfoShowFolder.Text = "Show Folder info";
		this.cbvInfoShowFolder.UseVisualStyleBackColor = true;
		this.cbvInfoShowAppName.AutoSize = true;
		this.cbvInfoShowAppName.Location = new System.Drawing.Point(9, 55);
		this.cbvInfoShowAppName.Margin = new System.Windows.Forms.Padding(4);
		this.cbvInfoShowAppName.Name = "cbvInfoShowAppName";
		this.cbvInfoShowAppName.Size = new System.Drawing.Size(139, 21);
		this.cbvInfoShowAppName.TabIndex = 1;
		this.cbvInfoShowAppName.Text = "Show vApp name";
		this.cbvInfoShowAppName.UseVisualStyleBackColor = true;
		this.groupBox1.Controls.Add(this.cbvInfoShowFolder);
		this.groupBox1.Controls.Add(this.cbvInfoShowAppName);
		this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.groupBox1.Location = new System.Drawing.Point(29, 27);
		this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox1.Size = new System.Drawing.Size(388, 89);
		this.groupBox1.TabIndex = 2;
		this.groupBox1.TabStop = false;
		this.groupBox1.Text = " vInfo tabpage - slows down the performance";
		this.btnCancel.Location = new System.Drawing.Point(603, 380);
		this.btnCancel.Margin = new System.Windows.Forms.Padding(4);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(100, 28);
		this.btnCancel.TabIndex = 3;
		this.btnCancel.Text = "Cancel";
		this.btnCancel.UseVisualStyleBackColor = true;
		this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
		this.btnOk.Location = new System.Drawing.Point(730, 380);
		this.btnOk.Margin = new System.Windows.Forms.Padding(4);
		this.btnOk.Name = "btnOk";
		this.btnOk.Size = new System.Drawing.Size(100, 28);
		this.btnOk.TabIndex = 4;
		this.btnOk.Text = "Ok";
		this.btnOk.UseVisualStyleBackColor = true;
		this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
		this.groupBox2.Controls.Add(this.label1);
		this.groupBox2.Controls.Add(this.numRefreshInterval);
		this.groupBox2.Controls.Add(this.cbxAutomaticRefresh);
		this.groupBox2.Location = new System.Drawing.Point(445, 27);
		this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox2.Name = "groupBox2";
		this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox2.Size = new System.Drawing.Size(388, 89);
		this.groupBox2.TabIndex = 5;
		this.groupBox2.TabStop = false;
		this.groupBox2.Text = " Auto Refresh Settings ";
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(28, 58);
		this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(162, 16);
		this.label1.TabIndex = 2;
		this.label1.Text = "Refresh interval in minutes";
		this.numRefreshInterval.Location = new System.Drawing.Point(216, 54);
		this.numRefreshInterval.Margin = new System.Windows.Forms.Padding(4);
		this.numRefreshInterval.Maximum = new decimal(new int[4] { 600, 0, 0, 0 });
		this.numRefreshInterval.Minimum = new decimal(new int[4] { 1, 0, 0, 0 });
		this.numRefreshInterval.Name = "numRefreshInterval";
		this.numRefreshInterval.Size = new System.Drawing.Size(89, 22);
		this.numRefreshInterval.TabIndex = 1;
		this.numRefreshInterval.Value = new decimal(new int[4] { 10, 0, 0, 0 });
		this.cbxAutomaticRefresh.AutoSize = true;
		this.cbxAutomaticRefresh.Location = new System.Drawing.Point(9, 27);
		this.cbxAutomaticRefresh.Margin = new System.Windows.Forms.Padding(4);
		this.cbxAutomaticRefresh.Name = "cbxAutomaticRefresh";
		this.cbxAutomaticRefresh.Size = new System.Drawing.Size(183, 20);
		this.cbxAutomaticRefresh.TabIndex = 0;
		this.cbxAutomaticRefresh.Text = "Automatic refresh the data";
		this.cbxAutomaticRefresh.UseVisualStyleBackColor = true;
		this.groupBox3.Controls.Add(this.cbxExcludeTags);
		this.groupBox3.Controls.Add(this.cbxExcludeAnnotations);
		this.groupBox3.Location = new System.Drawing.Point(29, 197);
		this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox3.Name = "groupBox3";
		this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox3.Size = new System.Drawing.Size(388, 87);
		this.groupBox3.TabIndex = 6;
		this.groupBox3.TabStop = false;
		this.groupBox3.Text = " Annotation Fields  and Tags ";
		this.cbxExcludeTags.AutoSize = true;
		this.cbxExcludeTags.Location = new System.Drawing.Point(9, 55);
		this.cbxExcludeTags.Margin = new System.Windows.Forms.Padding(4);
		this.cbxExcludeTags.Name = "cbxExcludeTags";
		this.cbxExcludeTags.Size = new System.Drawing.Size(112, 20);
		this.cbxExcludeTags.TabIndex = 0;
		this.cbxExcludeTags.Text = "Exclude Tags";
		this.cbxExcludeTags.UseVisualStyleBackColor = true;
		this.cbxExcludeAnnotations.AutoSize = true;
		this.cbxExcludeAnnotations.Location = new System.Drawing.Point(9, 27);
		this.cbxExcludeAnnotations.Margin = new System.Windows.Forms.Padding(4);
		this.cbxExcludeAnnotations.Name = "cbxExcludeAnnotations";
		this.cbxExcludeAnnotations.Size = new System.Drawing.Size(178, 20);
		this.cbxExcludeAnnotations.TabIndex = 0;
		this.cbxExcludeAnnotations.Text = "Exclude Annotation fields";
		this.cbxExcludeAnnotations.UseVisualStyleBackColor = true;
		this.groupBox4.Controls.Add(this.cbxGetFriendlyNames);
		this.groupBox4.Location = new System.Drawing.Point(29, 121);
		this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox4.Name = "groupBox4";
		this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox4.Size = new System.Drawing.Size(388, 68);
		this.groupBox4.TabIndex = 7;
		this.groupBox4.TabStop = false;
		this.groupBox4.Text = " Use vSAN Friendly names";
		this.cbxGetFriendlyNames.AutoSize = true;
		this.cbxGetFriendlyNames.Location = new System.Drawing.Point(9, 27);
		this.cbxGetFriendlyNames.Margin = new System.Windows.Forms.Padding(4);
		this.cbxGetFriendlyNames.Name = "cbxGetFriendlyNames";
		this.cbxGetFriendlyNames.Size = new System.Drawing.Size(342, 20);
		this.cbxGetFriendlyNames.TabIndex = 0;
		this.cbxGetFriendlyNames.Text = "Wait for \"Get Friendly vSAN Names\" thread at startup";
		this.cbxGetFriendlyNames.UseVisualStyleBackColor = true;
		this.groupBox5.Controls.Add(this.label2);
		this.groupBox5.Controls.Add(this.cbxGetFileInfo);
		this.groupBox5.Location = new System.Drawing.Point(445, 137);
		this.groupBox5.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox5.Name = "groupBox5";
		this.groupBox5.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox5.Size = new System.Drawing.Size(388, 223);
		this.groupBox5.TabIndex = 8;
		this.groupBox5.TabStop = false;
		this.groupBox5.Text = "  Populate vFileInfo  tab page ";
		this.label2.ForeColor = System.Drawing.Color.Red;
		this.label2.Location = new System.Drawing.Point(9, 57);
		this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(359, 119);
		this.label2.TabIndex = 1;
		this.label2.Text = resources.GetString("label2.Text");
		this.cbxGetFileInfo.AutoSize = true;
		this.cbxGetFileInfo.Location = new System.Drawing.Point(9, 27);
		this.cbxGetFileInfo.Margin = new System.Windows.Forms.Padding(4);
		this.cbxGetFileInfo.Name = "cbxGetFileInfo";
		this.cbxGetFileInfo.Size = new System.Drawing.Size(200, 20);
		this.cbxGetFileInfo.TabIndex = 0;
		this.cbxGetFileInfo.Text = "Get FileInfo detail information";
		this.cbxGetFileInfo.UseVisualStyleBackColor = true;
		this.groupBox6.Controls.Add(this.checkBoxProductImprovement);
		this.groupBox6.Location = new System.Drawing.Point(29, 292);
		this.groupBox6.Margin = new System.Windows.Forms.Padding(4);
		this.groupBox6.Name = "groupBox6";
		this.groupBox6.Padding = new System.Windows.Forms.Padding(4);
		this.groupBox6.Size = new System.Drawing.Size(388, 68);
		this.groupBox6.TabIndex = 9;
		this.groupBox6.TabStop = false;
		this.groupBox6.Text = " Product Improvement Program";
		this.checkBoxProductImprovement.AutoSize = true;
		this.checkBoxProductImprovement.Checked = true;
		this.checkBoxProductImprovement.CheckState = System.Windows.Forms.CheckState.Checked;
		this.checkBoxProductImprovement.Location = new System.Drawing.Point(9, 27);
		this.checkBoxProductImprovement.Margin = new System.Windows.Forms.Padding(4);
		this.checkBoxProductImprovement.Name = "checkBoxProductImprovement";
		this.checkBoxProductImprovement.Size = new System.Drawing.Size(291, 20);
		this.checkBoxProductImprovement.TabIndex = 0;
		this.checkBoxProductImprovement.Text = "Participate in Product Improvement Program";
		this.checkBoxProductImprovement.UseVisualStyleBackColor = true;
		base.AutoScaleDimensions = new System.Drawing.SizeF(8f, 16f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(864, 425);
		base.Controls.Add(this.groupBox6);
		base.Controls.Add(this.groupBox5);
		base.Controls.Add(this.groupBox4);
		base.Controls.Add(this.groupBox3);
		base.Controls.Add(this.groupBox2);
		base.Controls.Add(this.btnOk);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.groupBox1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.Margin = new System.Windows.Forms.Padding(4);
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "FPreferences";
		base.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
		this.Text = "RVTools Preferences";
		base.Load += new System.EventHandler(this.FPreferences_Load);
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		this.groupBox2.ResumeLayout(false);
		this.groupBox2.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this.numRefreshInterval).EndInit();
		this.groupBox3.ResumeLayout(false);
		this.groupBox3.PerformLayout();
		this.groupBox4.ResumeLayout(false);
		this.groupBox4.PerformLayout();
		this.groupBox5.ResumeLayout(false);
		this.groupBox5.PerformLayout();
		this.groupBox6.ResumeLayout(false);
		this.groupBox6.PerformLayout();
		base.ResumeLayout(false);
	}
}
