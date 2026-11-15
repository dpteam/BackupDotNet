using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using HyperVBackUp.Engine;
using NLog;

namespace HyperVBackup.Console;

internal class Program
{
	private class Options
	{
		[Option('f', "file", HelpText = "Text file containing a list of VMs to backup, one per line.", MutuallyExclusiveSet = "fla")]
		public string File { get; set; }

		[OptionList('l', "list", Separator = ',', HelpText = "List of VMs to backup, comma separated.", MutuallyExclusiveSet = "fla")]
		public IList<string> List { get; set; }

		[OptionList('v', "vhdinclude", Separator = ',', HelpText = "List of VHDs file names to backup, comma separated.")]
		public IList<string> VhdInclude { get; set; }

		[OptionList('i', "vhdignore", Separator = ',', HelpText = "List of VHDs file names to ignore, comma separated.")]
		public IList<string> VhdIgnore { get; set; }

		[Option('a', "all", HelpText = "Is set, backup all VMs on this server.", MutuallyExclusiveSet = "fla", DefaultValue = true)]
		public bool All { get; set; }

		[Option('n', "name", HelpText = "If set, VMs to backup are specified by name.", MutuallyExclusiveSet = "ng", DefaultValue = true)]
		public bool Name { get; set; }

		[Option('g', "guid", HelpText = "If set, VMs to backup are specified by guid.", MutuallyExclusiveSet = "ng")]
		public bool Guid { get; set; }

		[Option('o', "output", Required = true, HelpText = "Backup ouput folder.")]
		public string Output { get; set; }

		[Option('p', "password", HelpText = "Secure the backup with a password.")]
		public string Password { get; set; }

		[Option('z', "zip", HelpText = "Use the zip format to store the backup.", MutuallyExclusiveSet = "zd")]
		public bool ZipFormat { get; set; }

		[Option('d', "directcopy", HelpText = "Do not compress the output, just copy the files recreating the folder structure.", MutuallyExclusiveSet = "zd")]
		public bool DirectCopy { get; set; }

		[Option("outputformat", HelpText = "Backup archive name format. {0} is the VM's name, {1} the VM's GUID, {2} is the current date and time and {3} is the extension for the compression format (7z or zip). Default: \"{0}_{2:yyyyMMddHHmmss}{3}\"")]
		public string OutputFormat { get; set; } = "{0}_{2:yyyyMMddHHmmss}{3}";

		[Option('s', "singlevss", HelpText = "Perform one single snapshot for all the VMs.")]
		public bool SingleSnapshot { get; set; }

		[Option("compressionlevel", DefaultValue = -1, HelpText = "Compression level, between 0 (no compression, very fast) and 9 (max. compression, very slow).")]
		public int CompressionLevel { get; set; }

		[Option("cleanoutputbydays", DefaultValue = 0, HelpText = "Delete all files in the output folder older than x days. TOTALLY OPTIONAL. USE WITH CAUTION.")]
		public int CleanOutputDays { get; set; }

		[Option("cleanoutputbymb", DefaultValue = 0, HelpText = "Delete older files in the output folder if total size is bigger then x Megabytes. TOTALLY OPTIONAL. USE WITH CAUTION.")]
		public int CleanOutputMb { get; set; }

		[Option("onsuccess", HelpText = "Execute this program if backup completes correctly.")]
		public string OnSuccess { get; set; }

		[Option("onfailure", HelpText = "Execute this program if backup fails.")]
		public string OnFailure { get; set; }

		[Option("mt", HelpText = "Enable multi-threaded compression (only for 7zip format).")]
		public bool MultiThreaded { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		[HelpOption(HelpText = "Display this help screen.")]
		public string GetUsage()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("Note: short switchs use one dash (-) / long switches use two dashes (--). Example: HyperVBackup -l \"Mail Server\" --compressionlevel 0");
			stringBuilder.AppendLine();
			HelpText helpText = new HelpText(stringBuilder.ToString())
			{
				AdditionalNewLineAfterOption = false
			};
			HandleParsingErrorsInHelp(helpText);
			helpText.AddOptions(this);
			return helpText.ToString();
		}

		private void HandleParsingErrorsInHelp(HelpText help)
		{
			string text = help.RenderParsingErrorsText(this, 1);
			if (!string.IsNullOrEmpty(text))
			{
				help.AddPreOptionsLine("ERROR: " + text + Environment.NewLine);
			}
		}
	}

	private static volatile bool _cancel;

	private static int _currentWidth;

	private static int _consoleWidth;

	private static ILogger _logger;

	private static void Main(string[] args)
	{
		_logger = LogManager.GetCurrentClassLogger();
		Options options = new Options();
		try
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			System.Console.WriteLine("HyperVBackup 3");
			System.Console.WriteLine("Copyright (C) 2012 Cloudbase Solutions SRL");
			System.Console.WriteLine("Copyright (C) 2016/2017 Coliseo Software SRL");
			_logger.Info($"HyperVBackup started at {DateTime.Now}");
			if (new Parser(ConfigureSettings).ParseArgumentsStrict(args, options, () =>
			{
				Environment.Exit(1);
			}))
			{
				GetConsoleWidth();
				System.Console.WriteLine();
				ICollection<string> vmNames = GetVmNames(options);
				if (vmNames == null)
				{
					_logger.Info("Backing up all VMs on this server");
				}
				if (!Directory.Exists(options.Output))
				{
					throw new Exception($"The folder \"{options.Output}\" is not valid");
				}
				if (options.CleanOutputDays != 0)
				{
					CleanOutputByDays(options.Output, options.CleanOutputDays);
				}
				if (options.CleanOutputMb != 0)
				{
					CleanOutputByMegabytes(options.Output, options.CleanOutputMb);
				}
				VmNameType nameType = ((!options.Name) ? VmNameType.SystemName : VmNameType.ElementName);
				BackupManager backupManager = new BackupManager();
				backupManager.BackupProgress += MgrBackupProgress;
				System.Console.CancelKeyPress += Console_CancelKeyPress;
				HyperVBackUp.Engine.Options options2 = new HyperVBackUp.Engine.Options
				{
					CompressionLevel = options.CompressionLevel,
					Output = options.Output,
					OutputFormat = options.OutputFormat,
					SingleSnapshot = options.SingleSnapshot,
					VhdInclude = options.VhdInclude,
					VhdIgnore = options.VhdIgnore,
					Password = options.Password,
					ZipFormat = options.ZipFormat,
					DirectCopy = options.DirectCopy,
					MultiThreaded = options.MultiThreaded
				};
				IDictionary<string, string> vmNamesMap = backupManager.VssBackup(vmNames, nameType, options2, _logger);
				CheckRequiredVMs(vmNames, nameType, vmNamesMap);
				ShowElapsedTime(stopwatch);
				if (!string.IsNullOrEmpty(options.OnSuccess))
				{
					_logger.Info("Executing OnSucess program");
					ExecuteProcess(options.OnSuccess, _logger);
				}
				_logger.Info($"HyperVBackup ended at {DateTime.Now}");
			}
		}
		catch (BackupCancelledException ex)
		{
			System.Console.Error.WriteLine(string.Format(ex.Message));
			_logger.Error(ex.ToString());
			if (!string.IsNullOrEmpty(options.OnFailure))
			{
				_logger.Info("Executing OnFailure program");
				ExecuteProcess(options.OnFailure, _logger);
			}
			Environment.Exit(3);
		}
		catch (Exception ex2)
		{
			System.Console.Error.WriteLine($"Error: {ex2.Message}");
			System.Console.Error.WriteLine(ex2.StackTrace);
			_logger.Error(ex2.ToString());
			if (!string.IsNullOrEmpty(options.OnFailure))
			{
				_logger.Info("Executing OnFailure program");
				ExecuteProcess(options.OnFailure, _logger);
			}
			Environment.Exit(2);
		}
		Environment.Exit(_cancel ? 3 : 0);
	}

	private static void ExecuteProcess(string fileName, ILogger logger)
	{
		if (string.IsNullOrEmpty(fileName))
		{
			return;
		}
		using Process process = new Process();
		logger.Debug($"Executing program {fileName}");
		process.StartInfo.FileName = fileName;
		process.StartInfo.WorkingDirectory = Path.GetDirectoryName(fileName);
		process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.CreateNoWindow = true;
		process.Start();
		process.WaitForExit();
		string arg = process.StandardOutput.ReadToEnd();
		string arg2 = process.StandardError.ReadToEnd();
		int exitCode = process.ExitCode;
		process.Close();
		logger.Info($"Exit code of executing program {fileName} is {exitCode}");
		logger.Debug($"Output of executing program {fileName} is {arg}");
		logger.Debug($"Errors of executing program {fileName} are {arg2}");
	}

	private static void CleanOutputByDays(string output, int days)
	{
		string[] files = Directory.GetFiles(output);
		for (int i = 0; i < files.Length; i++)
		{
			FileInfo fileInfo = new FileInfo(files[i]);
			if (fileInfo.LastWriteTime < DateTime.Now.AddDays(days * -1))
			{
				_logger.Info($"Deleting file {fileInfo.Name}");
				fileInfo.Delete();
			}
		}
	}

	private static void CleanOutputByMegabytes(string output, int totalMb)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(output);
		long num = directoryInfo.EnumerateFiles().Sum((FileInfo file) => file.Length);
		long num2 = (long)totalMb * 1024L * 1024;
		if (num <= num2)
		{
			return;
		}
		_logger.Info($"Size of output folder is {num / 1024 / 1024} Megabytes, deleting some files ...");
		FileInfo[] files = directoryInfo.GetFiles();
		while (num > num2 && files.Length != 0)
		{
			string fullName = files.OrderBy((FileInfo f) => f.LastWriteTime).ToList()[0].FullName;
			_logger.Info($"Deleting file {fullName}");
			File.Delete(fullName);
			files = directoryInfo.GetFiles();
			num = files.Sum((FileInfo file) => file.Length);
		}
	}

	private static void ConfigureSettings(ParserSettings settings)
	{
		settings.MutuallyExclusive = true;
		settings.HelpWriter = System.Console.Out;
	}

	private static void ShowElapsedTime(Stopwatch stopwatch)
	{
		stopwatch.Stop();
		TimeSpan elapsed = stopwatch.Elapsed;
		_logger.Info($"Elapsed time: {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds:000}");
	}

	private static void CheckRequiredVMs(IEnumerable<string> vmNames, VmNameType nameType, IDictionary<string, string> vmNamesMap)
	{
		if (vmNames == null)
		{
			return;
		}
		foreach (string vmName in vmNames)
		{
			if ((nameType == VmNameType.SystemName && !vmNamesMap.Keys.Contains(vmName, StringComparer.OrdinalIgnoreCase)) || (nameType == VmNameType.ElementName && !vmNamesMap.Values.Contains(vmName, StringComparer.OrdinalIgnoreCase)))
			{
				_logger.Error($"WARNING: \"{vmName}\" not found");
			}
		}
	}

	private static void GetConsoleWidth()
	{
		try
		{
			_consoleWidth = System.Console.WindowWidth;
		}
		catch (Exception)
		{
			_consoleWidth = 80;
		}
	}

	private static ICollection<string> GetVmNames(Options options)
	{
		ICollection<string> collection = null;
		if (options.File != null)
		{
			collection = File.ReadAllLines(options.File);
		}
		else if (options.List != null)
		{
			collection = options.List;
		}
		if (collection != null)
		{
			collection = (from o in collection
				where o.Trim().Length > 0
				select o.Trim()).ToList();
		}
		return collection;
	}

	private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
	{
		System.Console.Error.WriteLine();
		System.Console.Error.WriteLine("Cancelling backup...");
		_cancel = true;
		e.Cancel = true;
	}

	private static void MgrBackupProgress(object sender, BackupProgressEventArgs e)
	{
		switch (e.Action)
		{
		case EventAction.InitializingVss:
			_logger.Info("Initializing VSS");
			break;
		case EventAction.StartingSnaphotSet:
			_logger.Info("Starting snapshot set for:");
			foreach (string value in e.Components.Values)
			{
				_logger.Info(value);
			}
			_logger.Info("");
			_logger.Info("Volumes:");
			foreach (string key in e.VolumeMap.Keys)
			{
				System.Console.WriteLine(key);
			}
			break;
		case EventAction.DeletingSnapshotSet:
			_logger.Info("Deleting snapshot set");
			break;
		case EventAction.StartingArchive:
			_logger.Info("");
			_logger.Info($"Creating archive: \"{e.AcrhiveFileName}\"");
			break;
		case EventAction.StartingEntry:
			_logger.Info($"Compressing entry: \"{e.CurrentEntry}\"");
			_currentWidth = 0;
			break;
		case EventAction.PercentProgress:
		{
			int num = e.PercentDone * _consoleWidth / 100;
			for (int i = 0; i < num - _currentWidth; i++)
			{
				System.Console.Write(".");
			}
			_currentWidth = num;
			if (e.PercentDone == 100)
			{
				System.Console.WriteLine();
			}
			_logger.Debug($"Percent done: {e.PercentDone}%");
			break;
		}
		}
		e.Cancel = _cancel;
	}
}
