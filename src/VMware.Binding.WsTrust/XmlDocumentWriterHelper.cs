using System;
using System.IO;
using System.Text;
using System.Xml;

namespace VMware.Binding.WsTrust;

public class XmlDocumentWriterHelper : IDisposable
{
	private readonly MemoryStream _bufferMemoryStream;

	private XmlWriter _writer;

	public XmlDocumentWriterHelper()
	{
		_bufferMemoryStream = new MemoryStream();
	}

	public XmlWriter CreateDocumentWriter()
	{
		if (_writer != null)
		{
			throw new InvalidOperationException("Cannot get a second document writer.");
		}
		_writer = XmlDictionaryWriter.CreateTextWriter(_bufferMemoryStream, Encoding.UTF8, ownsStream: false);
		return _writer;
	}

	public XmlDocument ReadDocument()
	{
		if (_writer == null)
		{
			throw new InvalidOperationException("Cannot read the document ouput until the writer was used.");
		}
		_writer.Close();
		_bufferMemoryStream.Seek(0L, SeekOrigin.Begin);
		return Util.ReadDocument(_bufferMemoryStream, ownStream: true);
	}

	public void Dispose()
	{
		if (_writer != null)
		{
			_writer.Dispose();
		}
		_bufferMemoryStream.Dispose();
	}
}
