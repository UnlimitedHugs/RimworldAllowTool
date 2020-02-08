using HugsLib.Settings;
using HugsLib.Source.Settings;
using UnityEngine;

namespace AllowTool.Settings {
	/// <summary>
	/// Stores settings for the Strip Mine designator that are common to all worlds
	/// </summary>
	public class StripMineGlobalSettings : SettingHandleConvertible {
		[SerializeField]
		public bool ShowWindow { get; set; }
		[SerializeField]
		public Vector2 WindowPosition { get; set; }

		public override void FromString(string settingValue) {
			SettingHandleConvertibleUtility.DeserializeValuesFromString(settingValue, this);
		}

		public override string ToString() {
			return SettingHandleConvertibleUtility.SerializeValuesToString(this);
		}
	}
}