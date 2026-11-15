using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using log4net;

namespace RVTools;

public class FWait : Form
{
	private static readonly ILog logger = LogManager.GetLogger(typeof(FWait));

	private IContainer components;

	public FWait()
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RVTools.FWait));
		base.SuspendLayout();
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(644, 23);
		base.ControlBox = false;
		base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "FWait";
		base.ShowInTaskbar = false;
		base.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "RVTools: Loading Inventory. Please wait.....";
		base.ResumeLayout(false);
	}
}
