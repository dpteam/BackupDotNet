using System;
using System.IO;
using ClosedXML.Excel;

namespace RVToolsMergeExcelFiles;

internal class Program
{
	private static string inputFiles = null;

	private static string outputFile = null;

	private static string templateFile = null;

	private static bool verbose = false;

	private static bool overwrite = false;

	private static bool firstFile = true;

	private static int Main(string[] args)
	{
		try
		{
			if (args[0] == "-help" || args[0] == "/help" || args[0] == "/?")
			{
				DisplayUsage();
				return 0;
			}
		}
		catch
		{
			DisplayUsage();
			return -1;
		}
		if (!CheckParameters(args))
		{
			DisplayUsage();
			return -1;
		}
		if (GetParameter(args, "-verbose"))
		{
			verbose = true;
		}
		if (GetParameter(args, "-overwrite"))
		{
			overwrite = true;
		}
		try
		{
			inputFiles = GetParameter(args, "-input", required: true);
		}
		catch
		{
			return -1;
		}
		string[] array = inputFiles.Split(';');
		for (int i = 0; i < array.Length; i++)
		{
			if (!CheckIfFileExists(array[i], "input", mustexist: true))
			{
				return -1;
			}
		}
		try
		{
			outputFile = GetParameter(args, "-output", required: true);
		}
		catch
		{
			return -1;
		}
		if (!CheckIfFileExists(outputFile, "output", mustexist: false))
		{
			return -1;
		}
		if (GetParameter(args, "-template"))
		{
			templateFile = GetParameter(args, "-template", required: false);
			if (!CheckIfFileExists(templateFile, "template", mustexist: true))
			{
				return -1;
			}
			inputFiles = templateFile + ";" + inputFiles;
		}
		if (!CopyInputToOutput())
		{
			return -1;
		}
		Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Done.");
		return 0;
	}

	private static bool CheckParameters(string[] args)
	{
		bool flag = true;
		if (args.Length != 0)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-") && !(args[i] == "-input") && !(args[i] == "-output") && !(args[i] == "-template") && !(args[i] == "-verbose") && !(args[i] == "-overwrite"))
				{
					Console.WriteLine("Unknown parameter: " + args[i]);
					flag = false;
				}
			}
		}
		if (!flag)
		{
			Console.WriteLine("Exit program");
		}
		return flag;
	}

	private static bool GetParameter(string[] args, string arg)
	{
		bool result = false;
		if (args.Length != 0)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == arg)
				{
					result = true;
				}
			}
		}
		return result;
	}

	private static string GetParameter(string[] args, string arg, bool required)
	{
		string text = null;
		if (args.Length != 0)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == arg)
				{
					try
					{
						text = args[i + 1];
					}
					catch
					{
					}
				}
			}
		}
		if (required && text == null)
		{
			Console.WriteLine("Parameter " + arg + " not found! Exit program");
			throw new Exception();
		}
		return text;
	}

	private static void DisplayUsage()
	{
		Console.WriteLine();
		Console.WriteLine("USAGE");
		Console.WriteLine("RVToolsMergeExcelFiles.exe -input file_a;file_b -output file_c [-template file_d] [-overwrite] [-verbose]");
		Console.WriteLine("-input\t\tfull path to input xlsx file(s)");
		Console.WriteLine("-output\t\tfull path to output xlsx file");
		Console.WriteLine("-template\tfull path to template xlsx file");
		Console.WriteLine("-overwrite\toverwrite output file if it exists");
		Console.WriteLine("-verbose\tdisplay informational messages");
	}

	private static bool CheckIfFileExists(string filename, string filetype, bool mustexist)
	{
		if (verbose)
		{
			Console.WriteLine("Check " + filetype + " " + filename);
		}
		try
		{
			if (mustexist)
			{
				if (File.Exists(filename))
				{
					if (verbose)
					{
						Console.WriteLine("Ok");
					}
					return true;
				}
				Console.WriteLine(filename + " Not found. Exit program!");
				return false;
			}
			if (!mustexist)
			{
				if (File.Exists(filename))
				{
					if (!overwrite)
					{
						Console.WriteLine("output file already exists. Exit program!");
						return false;
					}
					if (verbose)
					{
						Console.WriteLine("Ok (overwrite = true)");
					}
					return true;
				}
				if (verbose)
				{
					Console.WriteLine("Ok");
				}
				return true;
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	private static bool CopyInputToOutput()
	{
		XLWorkbook xLWorkbook = null;
		XLWorkbook xLWorkbook2 = null;
		string[] array = inputFiles.Split(';');
		foreach (string text in array)
		{
			if (firstFile)
			{
				firstFile = false;
				try
				{
					if (verbose)
					{
						Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Copy " + text + " to " + outputFile);
					}
					File.Copy(text, outputFile, overwrite: true);
					if (verbose)
					{
						Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Open XLWorkbook " + outputFile);
					}
					xLWorkbook2 = new XLWorkbook(outputFile, XLEventTracking.Disabled);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Cannot copy first input file to output file. Exit program!");
					Console.WriteLine(ex.Message);
					return false;
				}
				continue;
			}
			try
			{
				if (verbose)
				{
					Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Open XLWorkbook " + text);
				}
				xLWorkbook = new XLWorkbook(text, XLEventTracking.Disabled);
				if (verbose)
				{
					Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Merge XLWorkbooks");
				}
				foreach (IXLWorksheet worksheet in xLWorkbook2.Worksheets)
				{
					if (verbose)
					{
						Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Processing worksheet " + worksheet.Name + " from " + text);
					}
					int row = worksheet.LastRowUsed().RowNumber() + 1;
					for (int j = 1; j <= worksheet.LastColumnUsed().ColumnNumber(); j++)
					{
						IXLRange column = getColumn(xLWorkbook, worksheet.Name, worksheet.Column(j).FirstCell().Value.ToString());
						if (column != null)
						{
							worksheet.Row(row).Cell(j).Value = column;
						}
					}
				}
				if (verbose)
				{
					Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Dispose XLWorkbook " + text);
				}
				xLWorkbook.Dispose();
				GC.Collect();
			}
			catch (Exception ex2)
			{
				Console.WriteLine("Copy input to output file failed! Exit program!");
				Console.WriteLine("Is this a valid xlsx file? " + text);
				Console.WriteLine();
				Console.WriteLine("Exception message");
				Console.WriteLine(ex2.Message);
				Console.WriteLine();
				Console.WriteLine("Stack trace");
				Console.WriteLine(ex2.StackTrace);
				return false;
			}
		}
		try
		{
			if (verbose)
			{
				Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Write merged XLWorkbooks to " + outputFile);
			}
			xLWorkbook2.SaveAs(outputFile, validate: false);
			if (verbose)
			{
				Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Write merged XLWorkbooks done ");
			}
			xLWorkbook2.Dispose();
			if (verbose)
			{
				Console.WriteLine(DateTime.Now.ToString("hh:mm:ss") + " Dispose XLWorkbook ");
			}
			GC.Collect();
		}
		catch (Exception ex3)
		{
			Console.WriteLine("Save XLWorkbook failed! Exit program!");
			Console.WriteLine();
			Console.WriteLine("Exception message");
			Console.WriteLine(ex3.Message);
			Console.WriteLine();
			Console.WriteLine("Stack trace");
			Console.WriteLine(ex3.StackTrace);
			return false;
		}
		return true;
	}

	private static IXLRange getColumn(IXLWorkbook wbi, string sheetName, string columnName)
	{
		IXLRange result = null;
		foreach (IXLWorksheet worksheet in wbi.Worksheets)
		{
			if (!(worksheet.Name == sheetName))
			{
				continue;
			}
			int num = worksheet.LastRowUsed().RowNumber();
			if (num == 1)
			{
				return result;
			}
			for (int i = 1; i <= worksheet.LastColumnUsed().ColumnNumber(); i++)
			{
				if (worksheet.Column(i).FirstCell().Value.ToString() == columnName)
				{
					result = worksheet.Range(2, i, num, i);
				}
			}
		}
		return result;
	}
}
