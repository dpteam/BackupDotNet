using System;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace RVToolsSendMail;

internal class RVToolsSendMail
{
	private static void Main(string[] args)
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string text = null;
		string value = "25";
		string text2 = null;
		string text3 = null;
		string text4 = null;
		string text5 = null;
		if (commandLineArgs.Length <= 1 || GetOption(commandLineArgs, "/help"))
		{
			Console.WriteLine("--------------------");
			Console.WriteLine("RVToolsSendMail help");
			Console.WriteLine("-------------------------------------------------------");
			Console.WriteLine("/SMTPserver <server>         - SMTP server using for sending");
			Console.WriteLine("/SMTPport <port number>      - SMTP port (default is 25)");
			Console.WriteLine("/Mailto <SMTP Address>       - Recipient address");
			Console.WriteLine("/Mailfrom <SMTP Address>     - Sender address");
			Console.WriteLine("/Mailsubject <subject>       - Mail subject");
			Console.WriteLine("/Attachment <dir/file name>  - Attachment");
			Console.WriteLine("/Help                        - This information");
			Console.WriteLine("-------------------------------------------------------");
			return;
		}
		if (GetOption(commandLineArgs, "/SMTPserver"))
		{
			text = GetOptionValue(commandLineArgs, "/SMTPserver");
		}
		if (GetOption(commandLineArgs, "/SMTPport"))
		{
			value = GetOptionValue(commandLineArgs, "/SMTPport");
		}
		if (GetOption(commandLineArgs, "/Mailto"))
		{
			text2 = GetOptionValue(commandLineArgs, "/Mailto");
		}
		if (GetOption(commandLineArgs, "/Mailfrom"))
		{
			text3 = GetOptionValue(commandLineArgs, "/Mailfrom");
		}
		if (GetOption(commandLineArgs, "/Mailsubject"))
		{
			text4 = GetOptionValue(commandLineArgs, "/Mailsubject");
		}
		if (GetOption(commandLineArgs, "/Attachment"))
		{
			text5 = GetOptionValue(commandLineArgs, "/Attachment");
		}
		if (text == null)
		{
			Console.WriteLine("RVToolsSendMail Error: SMTP server not passed!");
			return;
		}
		if (text2 == null)
		{
			Console.WriteLine("RVToolsSendMail Error: Recipient address not passed!");
			return;
		}
		if (text3 == null)
		{
			Console.WriteLine("RVToolsSendMail Error: Sender address not passed!");
			return;
		}
		if (text5 == null)
		{
			Console.WriteLine("RVToolsSendMail Error: Path to attached file not passed!");
			return;
		}
		if (!File.Exists(text5))
		{
			Console.WriteLine("RVToolsSendMail Error: Attached file doesn't exists!");
			return;
		}
		MailMessage mailMessage = new MailMessage();
		MailAddress mailAddress = new MailAddress(text3, text3, Encoding.UTF8);
		mailMessage.From = mailAddress;
		if (text4 == null)
		{
			mailMessage.Subject = "RVTools Batch";
		}
		else
		{
			mailMessage.Subject = text4;
		}
		mailMessage.BodyEncoding = Encoding.GetEncoding("utf-8");
		AlternateView item = AlternateView.CreateAlternateViewFromString(Regex.Replace("This email is sent by the batch command RVToolsSendMail. Please check the attachment", "<(.|\\n)*?>", string.Empty), null, "text/plain");
		AlternateView item2 = AlternateView.CreateAlternateViewFromString("This email is sent by the batch command RVToolsSendMail. Please check the attachment", null, "text/html");
		mailMessage.AlternateViews.Add(item);
		mailMessage.AlternateViews.Add(item2);
		Attachment item3 = new Attachment(text5);
		mailMessage.Attachments.Add(item3);
		try
		{
			string[] array = text2.Split(';');
			SmtpClient smtpClient = new SmtpClient(text, Convert.ToInt32(value));
			string[] array2 = array;
			foreach (string text6 in array2)
			{
				mailMessage.To.Add(text6);
				smtpClient.Send(mailMessage);
				mailMessage.To.Clear();
				Console.WriteLine("Mail send to: " + text6);
			}
			Console.WriteLine("RVToolsSendMail: Terminated normally.");
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
	}

	private static bool GetOption(string[] CommandLineArgs, string Option)
	{
		for (int i = 0; i < CommandLineArgs.Length; i++)
		{
			if (CommandLineArgs[i].ToUpper() == Option.ToUpper())
			{
				return true;
			}
		}
		return false;
	}

	private static string GetOptionValue(string[] CommandLineArgs, string Option)
	{
		for (int i = 0; i < CommandLineArgs.Length; i++)
		{
			if (CommandLineArgs[i].ToUpper() == Option.ToUpper())
			{
				try
				{
					return CommandLineArgs[i + 1];
				}
				catch
				{
				}
			}
		}
		return null;
	}
}
