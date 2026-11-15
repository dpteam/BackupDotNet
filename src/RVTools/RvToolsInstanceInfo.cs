using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace RVTools;

internal static class RvToolsInstanceInfo
{
	private static Dictionary<string, string> _arguments;

	private const string REGISTRY_PATH = "SOFTWARE\\Dell\\RVTools";

	private const string REGISTRY_PATH_X86 = "SOFTWARE\\WOW6432Node\\Dell\\RVTools";

	private const string REGISTRY_VALUE = "ProductImprovementOptIn";

	public static bool ProductImprovementOptIn
	{
		get
		{
			try
			{
				using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Dell\\RVTools");
				string text = registryKey?.GetValue("ProductImprovementOptIn").ToString();
				if (text == "1" || text == "0")
				{
					return "1" == text;
				}
				using RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Dell\\RVTools");
				text = registryKey2?.GetValue("ProductImprovementOptIn").ToString();
				return "0" != text;
			}
			catch (Exception)
			{
				return false;
			}
		}
		set
		{
			try
			{
				using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Dell\\RVTools", writable: true) ?? Registry.CurrentUser.CreateSubKey("SOFTWARE\\Dell\\RVTools");
				registryKey?.SetValue("ProductImprovementOptIn", value ? "1" : "0");
			}
			catch (Exception)
			{
			}
		}
	}

	public static bool IsCommandlineArgumentSet(string argName)
	{
		return CommandLineArgumentValue(argName) != null;
	}

	public static string CommandLineArgumentValue(string argName, string defaultValue = null)
	{
		if (_arguments == null)
		{
			_arguments = new Dictionary<string, string>();
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			string text = null;
			for (int i = 1; i < commandLineArgs.Length; i++)
			{
				if (commandLineArgs[i].StartsWith("-") || commandLineArgs[i].StartsWith("/"))
				{
					if (!string.IsNullOrEmpty(text))
					{
						_arguments[text.ToLower()] = string.Empty;
						text = null;
					}
					if (commandLineArgs[i].Length >= 2)
					{
						int num = ((commandLineArgs[i].Length < 4) ? (-1) : commandLineArgs[i].IndexOf('=', 2));
						if (num >= 2)
						{
							_arguments[commandLineArgs[i].Substring(1, num).ToLower()] = commandLineArgs[i].Substring(num + 1);
						}
						else
						{
							text = commandLineArgs[i].Substring(1);
						}
					}
				}
				else if (!string.IsNullOrEmpty(text))
				{
					_arguments[text.ToLower()] = commandLineArgs[i];
					text = null;
				}
			}
			if (!string.IsNullOrEmpty(text) && !_arguments.ContainsKey(text.ToLower()))
			{
				_arguments.Add(text.ToLower(), string.Empty);
			}
		}
		if (!_arguments.TryGetValue(argName.ToLower(), out var value))
		{
			return defaultValue;
		}
		return value;
	}
}
