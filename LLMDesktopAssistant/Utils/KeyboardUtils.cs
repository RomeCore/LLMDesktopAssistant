using System.Runtime.InteropServices;
using System.Windows.Input;

namespace LLMDesktopAssistant.Utils
{
	public static class KeyboardUtils
	{
		[DllImport("user32.dll")]
		public static extern short GetAsyncKeyState(int vKey);

		public static bool IsKeyDown(System.Windows.Forms.Keys key)
		{
			return (GetAsyncKeyState((int)key) & 0x8000) != 0;
		}
	}
}