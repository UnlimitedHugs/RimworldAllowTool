using RimWorld;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Used by context menu entries to report the result of their activation.
	/// Can be displayed as a Message.
	/// </summary>
	public class ActivationResult {
		public const string SuccessIdSuffix = "_succ";
		public const string FailureIdSuffix = "_fail";

		public static ActivationResult Success(string messageKey, params NamedArgument[] translateArgs) {
			return SuccessMessage((messageKey + SuccessIdSuffix).Translate(translateArgs));
		}

		public static ActivationResult SuccessMessage(string message) {
			return new ActivationResult(message, MessageTypeDefOf.TaskCompletion);
		}

		public static ActivationResult Failure(string messageKey, params NamedArgument[] translateArgs) {
			return FailureMessage((messageKey + FailureIdSuffix).Translate(translateArgs));
		}

		public static ActivationResult FailureMessage(string message) {
			return new ActivationResult(message, MessageTypeDefOf.RejectInput);
		}

		public static ActivationResult FromCount(int designationCount, string baseMessageKey) {
			return designationCount > 0 ?
				Success(baseMessageKey, designationCount) :
				Failure(baseMessageKey);
		}

		public string Message { get; }
		public MessageTypeDef MessageType { get; }

		public ActivationResult() {
		}

		public ActivationResult(string message, MessageTypeDef messageType) {
			Message = message;
			MessageType = messageType;
		}

		public void ShowMessage() {
			if (Message != null && MessageType != null) {
				Messages.Message(Message, MessageType);
			}
		}
	}
}