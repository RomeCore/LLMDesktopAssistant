using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using LLMDesktopAssistant.Modules;
using RCLargeLanguageModels.Tools;
using WindowsInput;
using WindowsInput.Native;

namespace LLMDesktopAssistant.ToolModules
{
	[Module]
	public class SimulationToolModule : ToolModule
	{
		private readonly InputSimulator _inputSimulator;
		private readonly Dictionary<string, VirtualKeyCode> _keyMap;

		public SimulationToolModule()
		{
			_inputSimulator = new();

			_keyMap = new Dictionary<string, VirtualKeyCode>(StringComparer.OrdinalIgnoreCase);
			foreach (VirtualKeyCode keyCode in Enum.GetValues(typeof(VirtualKeyCode)))
			{
				var name = keyCode.ToString();
				string transformedName;

				if (name.StartsWith("VK_"))
				{
					transformedName = name.Substring(3).Replace("_", "");
				}
				else
				{
					transformedName = name.Replace("_", "");
				}

				_keyMap[transformedName] = keyCode;
			}

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(MoveMouse, "input-move_mouse", "Move mouse to a specified position."),
				Category = "input",
				Enabled = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(MoveMouseDelta, "input-move_mouse_delta", "Move mouse by a specified position delta."),
				Category = "input",
				Enabled = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(PressKey, "input-press_key", "Press a specified key"),
				Category = "input",
				Enabled = false
			});

			AddTool(new ToolInfo
			{
				Tool = FunctionTool.From(EnterText, "input-enter_text", "Enter text."),
				Category = "input",
				Enabled = false
			});
		}

		private ToolResult MoveMouse([Description("X coordinate")] int x, [Description("Y coordinate")] int y)
		{
			_inputSimulator.Mouse.MoveMouseTo(x, y);
			return new ToolResult("Success.");
		}

		private ToolResult MoveMouseDelta([Description("X coordinate delta")] int deltaX, [Description("Y coordinate delta")] int deltaY)
		{
			_inputSimulator.Mouse.MoveMouseBy(deltaX, deltaY);
			return new ToolResult("Success.");
		}

		private ToolResult PressKey([Description("Key to press, for example 'A' or 'Return'")] string key)
		{
			if (_keyMap.TryGetValue(key, out VirtualKeyCode keyCode))
			{
				_inputSimulator.Keyboard.KeyPress(keyCode);
				return new ToolResult("Success.");
			}
			return new ToolResult($"Key not found: {key}.");
		}

		private ToolResult EnterText([Description("Text to enter")] string text)
		{
			_inputSimulator.Keyboard.TextEntry(text);
			return new ToolResult("Success.");
		}
	}
}