namespace AllowTool {
	public class AllowToolUtility {
		private const string logPrefix = "[AllowTool] ";

		public static void Log(object message) {
			Verse.Log.Message(logPrefix + message);
		}

		public static void Error(object message) {
			Verse.Log.Error(logPrefix + message);
		}
	}
}