using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Vss;
using Ionic.Zip;
using Ionic.Zlib;
using NLog;
using SevenZip;

namespace HyperVBackUp.Engine;

public class BackupManager
{
	private volatile bool _cancel;

	private static string _currentFile = string.Empty;

	protected bool UseWMIV2NameSpace
	{
		get
		{
			Version version = Environment.OSVersion.Version;
			if (version.Major >= 6)
			{
				return version.Minor >= 2;
			}
			return false;
		}
	}

	public event EventHandler<BackupProgressEventArgs> BackupProgress;

	public IDictionary<string, string> VssBackup(IEnumerable<string> vmNames, VmNameType nameType, Options options, ILogger logger)
	{
		_cancel = false;
		IDictionary<string, string> vMNames = GetVMNames(vmNames, nameType);
		if (vMNames.Count > 0)
		{
			if (options.SingleSnapshot)
			{
				BackupSubset(vMNames, options, logger);
			}
			else
			{
				foreach (KeyValuePair<string, string> item in vMNames)
				{
					Dictionary<string, string> vmNamesMapSubset = new Dictionary<string, string> { { item.Key, item.Value } };
					BackupSubset(vmNamesMapSubset, options, logger);
				}
			}
		}
		return vMNames;
	}

	private void BackupSubset(IDictionary<string, string> vmNamesMapSubset, Options options, ILogger logger)
	{
		using IVssBackupComponents vssBackupComponents = VssUtils.LoadImplementation().CreateVssBackupComponents();
		RaiseEvent(EventAction.InitializingVss, null, null);
		vssBackupComponents.InitializeForBackup(null);
		vssBackupComponents.SetBackupState(selectComponents: true, backupBootableSystemState: true, VssBackupType.Full, partialFileSupport: false);
		vssBackupComponents.SetContext(VssSnapshotContext.Backup);
		Guid hyperVwriterGuid = new Guid("66841cd4-6ded-4f4b-8f17-fd23f8ddc3de");
		vssBackupComponents.EnableWriterClasses(hyperVwriterGuid);
		vssBackupComponents.GatherWriterMetadata();
		IList<IVssWMComponent> list = new List<IVssWMComponent>();
		IDictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		IVssExamineWriterMetadata vssExamineWriterMetadata = vssBackupComponents.WriterMetadata.FirstOrDefault((IVssExamineWriterMetadata o) => o.WriterId.Equals(hyperVwriterGuid));
		foreach (IVssWMComponent component in vssExamineWriterMetadata.Components)
		{
			if (!vmNamesMapSubset.ContainsKey(component.ComponentName))
			{
				continue;
			}
			list.Add(component);
			vssBackupComponents.AddComponent(vssExamineWriterMetadata.InstanceId, vssExamineWriterMetadata.WriterId, component.Type, component.LogicalPath, component.ComponentName);
			foreach (VssWMFileDescriptor file in component.Files)
			{
				string volumePath;
				string volumeName;
				if (CSV.IsSupported() && CSV.IsPathOnSharedVolume(file.Path))
				{
					CSV.ClusterPrepareSharedVolumeForBackup(file.Path, out volumePath, out volumeName);
				}
				else
				{
					volumePath = Alphaleonis.Win32.Filesystem.Path.GetPathRoot(file.Path).ToUpper();
					volumeName = volumePath;
				}
				if (!dictionary.ContainsKey(volumePath))
				{
					dictionary.Add(volumePath, volumeName);
				}
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		Guid snapshotSetId = vssBackupComponents.StartSnapshotSet();
		IDictionary<string, Guid> dictionary2 = new Dictionary<string, Guid>();
		foreach (string value in dictionary.Values)
		{
			dictionary2.Add(value, vssBackupComponents.AddToSnapshotSet(value, Guid.Empty));
		}
		vssBackupComponents.PrepareForBackup();
		RaiseEvent(EventAction.StartingSnaphotSet, list, dictionary);
		vssBackupComponents.DoSnapshotSet();
		RaiseEvent(EventAction.SnapshotSetDone, list, dictionary);
		IDictionary<string, string> dictionary3 = new Dictionary<string, string>();
		foreach (KeyValuePair<string, Guid> item in dictionary2)
		{
			dictionary3.Add(item.Key, vssBackupComponents.GetSnapshotProperties(item.Value).SnapshotDeviceObject);
		}
		BackupFiles(list, dictionary, dictionary3, vmNamesMapSubset, options, logger);
		foreach (IVssWMComponent item2 in list)
		{
			vssBackupComponents.SetBackupSucceeded(vssExamineWriterMetadata.InstanceId, vssExamineWriterMetadata.WriterId, item2.Type, item2.LogicalPath, item2.ComponentName, succeeded: true);
		}
		vssBackupComponents.BackupComplete();
		RaiseEvent(EventAction.DeletingSnapshotSet, list, dictionary);
		vssBackupComponents.DeleteSnapshotSet(snapshotSetId, forceDelete: true);
	}

	private void RaiseEvent(EventAction action, IList<IVssWMComponent> components, IDictionary<string, string> volumeMap)
	{
		if (BackupProgress == null)
		{
			return;
		}
		BackupProgressEventArgs e = new BackupProgressEventArgs
		{
			Action = action
		};
		if (components != null)
		{
			e.Components = new Dictionary<string, string>();
			foreach (IVssWMComponent component in components)
			{
				e.Components.Add(component.ComponentName, component.Caption);
			}
		}
		if (volumeMap != null)
		{
			e.VolumeMap = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> item in volumeMap)
			{
				e.VolumeMap.Add(item);
			}
		}
		BackupProgress(this, e);
		if (e.Cancel)
		{
			throw new BackupCancelledException();
		}
	}

	private void BackupFiles(IList<IVssWMComponent> components, IDictionary<string, string> volumeMap, IDictionary<string, string> snapshotVolumeMap, IDictionary<string, string> vmNamesMap, Options options, ILogger logger)
	{
		List<Stream> list = new List<Stream>();
		try
		{
			foreach (IVssWMComponent component in components)
			{
				string text;
				if (options.DirectCopy)
				{
					text = Alphaleonis.Win32.Filesystem.Path.Combine(options.Output, string.Format(options.OutputFormat, vmNamesMap[component.ComponentName], component.ComponentName, DateTime.Now, ""));
				}
				else
				{
					text = Alphaleonis.Win32.Filesystem.Path.Combine(options.Output, string.Format(options.OutputFormat, vmNamesMap[component.ComponentName], component.ComponentName, DateTime.Now, options.ZipFormat ? ".zip" : ".7z"));
					Alphaleonis.Win32.Filesystem.File.Delete(text);
				}
				Dictionary<string, Stream> dictionary = new Dictionary<string, Stream>();
				foreach (VssWMFileDescriptor file in component.Files)
				{
					string path;
					if (file.IsRecursive)
					{
						path = file.Path;
					}
					else
					{
						path = Alphaleonis.Win32.Filesystem.Path.Combine(file.Path, file.FileSpecification);
					}
					string text2 = volumeMap.Keys.OrderBy((string o) => o.Length).Reverse().First((string o) => path.StartsWith(o, StringComparison.OrdinalIgnoreCase));
					string key = volumeMap[text2];
					string fileName = Alphaleonis.Win32.Filesystem.Path.GetFileName(path.Substring(text2.Length)).ToUpperInvariant();
					bool flag = !path.EndsWith("\\*");
					string[] array = path.Split(Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar);
					if (array.Length >= 2 && array[array.Length - 2].ToLowerInvariant() == "snapshots")
					{
						flag = false;
					}
					if (flag && options.VhdInclude != null && Enumerable.Count(options.VhdInclude, (string x) => string.CompareOrdinal(x.ToUpperInvariant(), fileName) == 0) == 0)
					{
						flag = false;
					}
					if (flag && options.VhdIgnore != null && Enumerable.Count(options.VhdIgnore, (string x) => string.CompareOrdinal(x.ToUpperInvariant(), fileName) == 0) != 0)
					{
						flag = false;
					}
					if (flag)
					{
						if (options.DirectCopy)
						{
							DoDirectCopy(text, snapshotVolumeMap[key], text2.Length, path);
						}
						else
						{
							AddPathToCompressionList(dictionary, list, snapshotVolumeMap[key], text2.Length, path);
						}
					}
					else
					{
						string text3 = $"Ignoring file {path}";
						logger.Info(text3);
						Console.WriteLine(text3);
					}
				}
				if (options.DirectCopy)
				{
					continue;
				}
				logger.Debug($"Start compression. File: {text}");
				if (options.ZipFormat)
				{
					if (options.CompressionLevel == -1)
					{
						options.CompressionLevel = 6;
					}
					using ZipFile zipFile = new ZipFile(text);
					zipFile.ParallelDeflateThreshold = -1L;
					zipFile.UseZip64WhenSaving = Zip64Option.Always;
					zipFile.Encryption = EncryptionAlgorithm.WinZipAes256;
					switch (options.CompressionLevel)
					{
					case 0:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
						break;
					case 1:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
						break;
					case 2:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.Level2;
						break;
					case 3:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.Level3;
						break;
					case 4:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.Level4;
						break;
					case 5:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.Level5;
						break;
					case 6:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
						break;
					case 7:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.Level7;
						break;
					case 8:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.Level8;
						break;
					case 9:
						zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
						break;
					}
					if (BackupProgress != null)
					{
						zipFile.SaveProgress += (object sender, SaveProgressEventArgs e) =>
						{
							ReportZipProgress(component, volumeMap, e);
						};
					}
					if (!string.IsNullOrEmpty(options.Password))
					{
						zipFile.Password = options.Password;
					}
					foreach (KeyValuePair<string, Stream> item in dictionary)
					{
						logger.Debug($"Adding file: {item.Key}");
						zipFile.AddEntry(item.Key, item.Value);
					}
					zipFile.Save();
				}
				else
				{
					if (options.CompressionLevel == -1)
					{
						options.CompressionLevel = 3;
					}
					SevenZipBase.SetLibraryPath(Alphaleonis.Win32.Filesystem.Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "7z.dll"));
					SevenZipCompressor sevenZipCompressor = new SevenZipCompressor
					{
						ArchiveFormat = OutArchiveFormat.SevenZip,
						CompressionMode = SevenZip.CompressionMode.Create,
						DirectoryStructure = true,
						PreserveDirectoryRoot = false
					};
					if (options.MultiThreaded)
					{
						sevenZipCompressor.CustomParameters.Add("mt", "on");
					}
					sevenZipCompressor.CustomParameters.Add("d", "24");
					switch (options.CompressionLevel)
					{
					case 0:
						sevenZipCompressor.CompressionLevel = SevenZip.CompressionLevel.None;
						break;
					case 1:
					case 2:
						sevenZipCompressor.CompressionLevel = SevenZip.CompressionLevel.Fast;
						break;
					case 3:
					case 4:
					case 5:
						sevenZipCompressor.CompressionLevel = SevenZip.CompressionLevel.Low;
						break;
					case 6:
						sevenZipCompressor.CompressionLevel = SevenZip.CompressionLevel.Normal;
						break;
					case 7:
					case 8:
						sevenZipCompressor.CompressionLevel = SevenZip.CompressionLevel.High;
						break;
					case 9:
						sevenZipCompressor.CompressionLevel = SevenZip.CompressionLevel.Ultra;
						break;
					}
					if (BackupProgress != null)
					{
						sevenZipCompressor.FileCompressionStarted += (object sender, FileNameEventArgs e) =>
						{
							BackupProgressEventArgs ebp = new BackupProgressEventArgs
							{
								AcrhiveFileName = e.FileName,
								Action = EventAction.StartingArchive
							};
							_currentFile = e.FileName;
							Report7ZipProgress(component, volumeMap, ebp);
							if (_cancel)
							{
								e.Cancel = true;
							}
						};
						sevenZipCompressor.FileCompressionFinished += (object sender, EventArgs e) =>
						{
							BackupProgressEventArgs ebp = new BackupProgressEventArgs
							{
								AcrhiveFileName = _currentFile,
								Action = EventAction.ArchiveDone
							};
							_currentFile = string.Empty;
							Report7ZipProgress(component, volumeMap, ebp);
						};
						sevenZipCompressor.Compressing += (object sender, SevenZip.ProgressEventArgs e) =>
						{
							BackupProgressEventArgs ebp = new BackupProgressEventArgs
							{
								AcrhiveFileName = _currentFile,
								Action = EventAction.PercentProgress,
								CurrentEntry = _currentFile,
								PercentDone = e.PercentDone
							};
							Report7ZipProgress(component, volumeMap, ebp);
						};
					}
					if (string.IsNullOrEmpty(options.Password))
					{
						sevenZipCompressor.CompressStreamDictionary(dictionary, text);
					}
					else
					{
						sevenZipCompressor.CompressStreamDictionary(dictionary, text, options.Password);
					}
				}
				logger.Debug("Compression finished");
				if (_cancel)
				{
					if (Alphaleonis.Win32.Filesystem.File.Exists(text))
					{
						Alphaleonis.Win32.Filesystem.File.Delete(text);
					}
					throw new BackupCancelledException();
				}
			}
		}
		finally
		{
			foreach (Stream item2 in list)
			{
				item2.Close();
			}
		}
	}

	private static void AddPathToCompressionList(IDictionary<string, Stream> files, ICollection<Stream> streams, string snapshotPath, int volumePathLength, string vmPath)
	{
		string text = Alphaleonis.Win32.Filesystem.Path.Combine(snapshotPath, vmPath.Substring(volumePathLength));
		if (Alphaleonis.Win32.Filesystem.Directory.Exists(text))
		{
			string[] fileSystemEntries = Alphaleonis.Win32.Filesystem.Directory.GetFileSystemEntries(text);
			for (int i = 0; i < fileSystemEntries.Length; i++)
			{
				string text2 = fileSystemEntries[i].Substring(snapshotPath.EndsWith(Alphaleonis.Win32.Filesystem.Path.PathSeparator.ToString(), StringComparison.CurrentCultureIgnoreCase) ? snapshotPath.Length : (snapshotPath.Length + 1));
				string vmPath2 = Alphaleonis.Win32.Filesystem.Path.Combine(vmPath.Substring(0, volumePathLength), text2);
				AddPathToCompressionList(files, streams, snapshotPath, volumePathLength, vmPath2);
			}
		}
		else if (Alphaleonis.Win32.Filesystem.File.Exists(text))
		{
			FileStream fileStream = Alphaleonis.Win32.Filesystem.File.OpenRead(text);
			files.Add(vmPath.Substring(volumePathLength), fileStream);
			streams.Add(fileStream);
		}
		else
		{
			string text3 = text.ToLowerInvariant();
			if (!text3.EndsWith(".avhdx") && !text3.EndsWith(".vmrs") && !text3.EndsWith(".bin") && !text3.EndsWith(".vsv"))
			{
				throw new Exception($"Entry \"{text}\" not found in snapshot");
			}
		}
	}

	private static void DoDirectCopy(string vmBackupPath, string snapshotPath, int volumePathLength, string vmPath)
	{
		string text = Alphaleonis.Win32.Filesystem.Path.Combine(snapshotPath, vmPath.Substring(volumePathLength));
		if (Alphaleonis.Win32.Filesystem.Directory.Exists(text))
		{
			string[] fileSystemEntries = Alphaleonis.Win32.Filesystem.Directory.GetFileSystemEntries(text);
			for (int i = 0; i < fileSystemEntries.Length; i++)
			{
				string text2 = fileSystemEntries[i].Substring(snapshotPath.EndsWith(Alphaleonis.Win32.Filesystem.Path.PathSeparator.ToString(), StringComparison.CurrentCultureIgnoreCase) ? snapshotPath.Length : (snapshotPath.Length + 1));
				string vmPath2 = Alphaleonis.Win32.Filesystem.Path.Combine(vmPath.Substring(0, volumePathLength), text2);
				DoDirectCopy(vmBackupPath, snapshotPath, volumePathLength, vmPath2);
			}
			return;
		}
		if (Alphaleonis.Win32.Filesystem.File.Exists(text))
		{
			string path = Alphaleonis.Win32.Filesystem.Path.Combine(vmBackupPath, vmPath.Substring(volumePathLength));
			using FileStream fileStream = Alphaleonis.Win32.Filesystem.File.OpenRead(text);
			string directoryName = Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(path);
			if (!Alphaleonis.Win32.Filesystem.Directory.Exists(directoryName))
			{
				Alphaleonis.Win32.Filesystem.Directory.CreateDirectory(directoryName);
			}
			using FileStream destination = Alphaleonis.Win32.Filesystem.File.Create(path);
			fileStream.CopyTo(destination);
			return;
		}
		string text3 = text.ToLowerInvariant();
		if (text3.EndsWith(".avhdx") || text3.EndsWith(".vmrs") || text3.EndsWith(".bin") || text3.EndsWith(".vsv"))
		{
			return;
		}
		throw new Exception($"Entry \"{text}\" not found in snapshot");
	}

	protected string GetWMIScope(string host = "localhost")
	{
		string format = ((!UseWMIV2NameSpace) ? "\\\\{0}\\root\\virtualization" : "\\\\{0}\\root\\virtualization\\v2");
		return string.Format(format, host);
	}

	private IDictionary<string, string> GetVMNames(IEnumerable<string> vmNames, VmNameType nameType)
	{
		IDictionary<string, string> dictionary = new Dictionary<string, string>();
		string text;
		string text2;
		if (UseWMIV2NameSpace)
		{
			text = "SELECT VirtualSystemIdentifier, ElementName FROM Msvm_VirtualSystemSettingData WHERE VirtualSystemType = 'Microsoft:Hyper-V:System:Realized'";
			text2 = "VirtualSystemIdentifier";
		}
		else
		{
			text = "SELECT SystemName, ElementName FROM Msvm_VirtualSystemSettingData WHERE SettingType = 3";
			text2 = "SystemName";
		}
		string fieldName = ((nameType == VmNameType.ElementName) ? "ElementName" : text2);
		ManagementScope scope = new ManagementScope(GetWMIScope());
		if (vmNames != null && vmNames.Any())
		{
			text += $" AND ({GetORStr(fieldName, vmNames)})";
		}
		using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(scope, new ObjectQuery(text));
		using ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
		foreach (ManagementBaseObject item in managementObjectCollection)
		{
			using (item)
			{
				dictionary.Add((string)item[text2], (string)item["ElementName"]);
			}
		}
		return dictionary;
	}

	private static string GetORStr(string fieldName, IEnumerable<string> vmNames)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string vmName in vmNames)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(" OR ");
			}
			stringBuilder.Append($"{fieldName} = '{EscapeWMIStr(vmName)}'");
		}
		return stringBuilder.ToString();
	}

	private static string EscapeWMIStr(string str)
	{
		return str?.Replace("'", "''");
	}

	private void Report7ZipProgress(IVssWMComponent component, IDictionary<string, string> volumeMap, BackupProgressEventArgs ebp)
	{
		if (ebp == null)
		{
			return;
		}
		ebp.Components = new Dictionary<string, string> { { component.ComponentName, component.Caption } };
		ebp.VolumeMap = new Dictionary<string, string>();
		foreach (KeyValuePair<string, string> item in volumeMap)
		{
			ebp.VolumeMap.Add(item.Key, item.Value);
		}
		BackupProgress(this, ebp);
		if (ebp.Cancel)
		{
			_cancel = true;
		}
	}

	private void ReportZipProgress(IVssWMComponent component, IDictionary<string, string> volumeMap, ZipProgressEventArgs e)
	{
		BackupProgressEventArgs e2 = null;
		if (e.EventType == ZipProgressEventType.Saving_Started)
		{
			e2 = new BackupProgressEventArgs
			{
				AcrhiveFileName = e.ArchiveName,
				Action = EventAction.StartingArchive
			};
		}
		else if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry || e.EventType == ZipProgressEventType.Saving_EntryBytesRead)
		{
			EventAction action = ((e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry) ? EventAction.StartingEntry : EventAction.SavingEntry);
			e2 = new BackupProgressEventArgs
			{
				AcrhiveFileName = e.ArchiveName,
				Action = action,
				CurrentEntry = e.CurrentEntry.FileName,
				EntriesTotal = e.EntriesTotal,
				TotalBytesToTransfer = e.TotalBytesToTransfer,
				BytesTransferred = e.BytesTransferred
			};
		}
		else if (e.EventType == ZipProgressEventType.Saving_Completed)
		{
			e2 = new BackupProgressEventArgs
			{
				AcrhiveFileName = e.ArchiveName,
				Action = EventAction.ArchiveDone
			};
		}
		if (e2 == null)
		{
			return;
		}
		e2.Components = new Dictionary<string, string> { { component.ComponentName, component.Caption } };
		e2.VolumeMap = new Dictionary<string, string>();
		foreach (KeyValuePair<string, string> item in volumeMap)
		{
			e2.VolumeMap.Add(item.Key, item.Value);
		}
		BackupProgress(this, e2);
		e.Cancel = e2.Cancel;
	}
}
