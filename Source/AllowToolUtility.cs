using UnityEngine;

namespace AllowTool {
	public class AllowToolUtility {
		private const string logPrefix = "[AllowTool] ";

		public static void Log(object message) {
			Verse.Log.Message(logPrefix + message);
		}

		public static void Error(object message) {
			Verse.Log.Error(logPrefix + message);
		}

		public static bool ShiftIsHeld {
			get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
		}

		public static bool ControlIsHeld {
			get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand); }
		}
	}
}