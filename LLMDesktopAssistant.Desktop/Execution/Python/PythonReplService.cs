using DocumentFormat.OpenXml.Wordprocessing;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace LLMDesktopAssistant.Desktop.Execution.Python
{
	[ChatService]
	public class PythonReplService : Disposable
	{
		private readonly Chat _chat;

		private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private string _serverScript;
		private Process _pythonProcess = null!;
		private StreamWriter _stdin = null!;
		private StreamReader _stdout = null!;

		public PythonReplService(Chat chat)
		{
			_chat = chat;

			using var serverScriptResource = typeof(PythonReplService).Assembly
				.GetManifestResourceStream("LLMDesktopAssistant.Desktop.Execution.Python.repl_server.py")
				?? throw new FileNotFoundException($"Python script not found in resources (LLMDesktopAssistant.Desktop.Execution.Python.repl_server.py) in the assembly (LLMDesktopAssistant.Desktop.dll).");
			using var serverScriptReader = new StreamReader(serverScriptResource);
			_serverScript = serverScriptReader.ReadToEnd();
		}

		public async Task EnsureServerAsync(CancellationToken cancellationToken = default)
		{
			if (_pythonProcess != null && !_pythonProcess.HasExited && _stdin != null && _stdout != null)
				return;

			var tempPyFile = Path.GetFullPath(Path.Combine(Directories.TempScripts, $"{Guid.NewGuid()}.py"));
			File.WriteAllText(tempPyFile, _serverScript);

			string? cmd, venvActivatePath = _chat.Settings.Environment.PythonVenvActivateScriptPath;
			if (!string.IsNullOrWhiteSpace(venvActivatePath))
				cmd = $"call \"{venvActivatePath}\" && python \"{tempPyFile}\"";
			else
				cmd = $"python \"{tempPyFile}\"";

			var startInfo = new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = $"/c \"{cmd}\"",
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = _chat.Settings.Environment.GetWorkingDirectory()
			};

			_pythonProcess = new Process { StartInfo = startInfo };
			_pythonProcess.Start();

			_stdin = _pythonProcess.StandardInput;
			_stdout = _pythonProcess.StandardOutput;

			var pong = await SendCommandAsync("{\"action\": \"ping\"}", cancellationToken);
			Log.Information($"Python REPL started: {pong}");
		}

		public async Task ShutDownAsync(CancellationToken cancellationToken = default)
		{
			await _semaphore.WaitAsync(cancellationToken);

			try
			{
				if (_stdin != null)
				{
					_stdin.WriteLine("{\"action\": \"exit\"}");
					_stdin.Flush();
					_stdin = null!;
				}

				if (_pythonProcess != null && !_pythonProcess.HasExited)
				{
					await _pythonProcess.WaitForExitAsync(cancellationToken);
					_pythonProcess.Kill();
					_pythonProcess.Dispose();
					_pythonProcess = null!;
				}
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public async Task<string> ExecuteAsync(string code, CancellationToken cancellationToken = default)
		{
			await EnsureServerAsync();

			var response = await SendCommandAsync(new
			{
				action = "execute",
				code = code
			}, cancellationToken);

			if (response["data"]?["status"] is JsonValue status && status.ToString() == "error")
				throw new Exception(response["data"]?["output"]?.ToString() ?? "Unknown error");

			var output = response["data"]?["output"];
			return output?.ToString() ?? string.Empty;
		}

		public async Task<string> EvaluateAsync(string expression, CancellationToken cancellationToken = default)
		{
			await EnsureServerAsync();

			var response = await SendCommandAsync(new
			{
				action = "eval",
				code = expression
			}, cancellationToken);

			if (response["data"]?["status"] is JsonValue status && status.ToString() == "error")
				throw new Exception(response["data"]?["output"]?.ToString() ?? "Unknown error");

			var output = response["data"]?["output"];
			return output?.ToString() ?? string.Empty;
		}

		public async Task SetVariableAsync(string name, string valueCode, CancellationToken cancellationToken = default)
		{
			await EnsureServerAsync(cancellationToken);

			await SendCommandAsync(new
			{
				action = "set_var",
				name = name,
				code = valueCode
			}, cancellationToken);
		}

		public async Task<string> GetVariableAsync(string name, CancellationToken cancellationToken = default)
		{
			await EnsureServerAsync(cancellationToken);

			var response = await SendCommandAsync(new
			{
				action = "get_var",
				name = name
			}, cancellationToken);

			if (response["data"]?["error"] is JsonValue error)
				throw new Exception(error?.ToString() ?? "Unknown error");

			return response["data"]?["value"]?.ToString() ?? string.Empty;
		}

		public async Task<string> ResetAsync(CancellationToken cancellationToken = default)
		{
			await EnsureServerAsync(cancellationToken);

			var response = await SendCommandAsync(new
			{
				action = "reset"
			}, cancellationToken);

			if (response["data"]?["status"] is JsonValue status && status.ToString() == "error")
				throw new Exception(response["data"]?["output"]?.ToString() ?? "Unknown error");

			var output = response["data"]?["output"];
			return output?.ToString() ?? string.Empty;
		}

		private async Task<JsonNode> SendCommandAsync<T>(T command, CancellationToken cancellationToken = default)
		{
			await _semaphore.WaitAsync(cancellationToken);

			try
			{
				var jsonCommand = JsonSerializer.Serialize(command);
				_stdin.WriteLine(jsonCommand);
				_stdin.Flush();

				var response = await _stdout.ReadLineAsync(cancellationToken);
				return JsonNode.Parse(response ?? "{}") ?? throw new Exception("Invalid JSON response from Python REPL server.");
			}
			finally
			{
				_semaphore.Release();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_ = ShutDownAsync();
		}
	}
}