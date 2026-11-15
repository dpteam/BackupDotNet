using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace RVTools;

internal static class Program
{
	[STAThread]
	private static int Main()
	{
		Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Assembly.GetExecutingAssembly().Location));
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.Run(new FMain());
		return VISDK.ExitCode;
	}
}
