using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Utils
{
	public class SearXSharpLoggerAdapter : SearXSharp.ILogger
	{
		private ILogger _serilogLogger;

		public SearXSharpLoggerAdapter(ILogger serilogLogger)
		{
			_serilogLogger = serilogLogger;
		}

		public void Debug(string template, params object?[] args)
		{
			_serilogLogger.Debug(template, args);
		}

		public void Debug(Exception ex, string template, params object?[] args)
		{
			_serilogLogger.Debug(ex, template, args);
		}

		public void Error(string template, params object?[] args)
		{
			_serilogLogger.Error(template, args);
		}

		public void Error(Exception ex, string template, params object?[] args)
		{
			_serilogLogger.Error(ex, template, args);
		}

		public void Fatal(string template, params object?[] args)
		{
			_serilogLogger.Fatal(template, args);
		}

		public void Fatal(Exception ex, string template, params object?[] args)
		{
			_serilogLogger.Fatal(ex, template, args);
		}

		public void Information(string template, params object?[] args)
		{
			_serilogLogger.Information(template, args);
		}

		public void Verbose(string template, params object?[] args)
		{
			_serilogLogger.Verbose(template, args);
		}

		public void Verbose(Exception ex, string template, params object?[] args)
		{
			_serilogLogger.Verbose(ex, template, args);
		}

		public void Warning(string template, params object?[] args)
		{
			_serilogLogger.Warning(template, args);
		}

		public void Warning(Exception ex, string template, params object?[] args)
		{
			_serilogLogger.Warning(ex, template, args);
		}
	}
}