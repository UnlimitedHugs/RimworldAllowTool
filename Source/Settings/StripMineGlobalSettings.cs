using System;
using System.Xml.Serialization;
using HugsLib.Settings;
using HugsLib.Source.Settings;
using UnityEngine;

namespace AllowTool.Settings {
	/// <summary>
	/// Stores settings for the Strip Mine designator that are common to all worlds
	/// </summary>
	[Serializable]
	public class StripMineGlobalSettings : SettingHandleConvertible, IEquatable<StripMineGlobalSettings> {
		[XmlElement]
		public Vector2 WindowPosition { get; set; }

		public override bool ShouldBeSaved {
			get { return !Equals(new StripMineGlobalSettings()); }
		}

		public override void FromString(string settingValue) {
			SettingHandleConvertibleUtility.DeserializeValuesFromString(settingValue, this);
		}

		public bool Equals(StripMineGlobalSettings other) {
			return other != null &&
					other.WindowPosition == WindowPosition;
		}

		public override string ToString() {
			return SettingHandleConvertibleUtility.SerializeValuesToString(this);
		}

		public StripMineGlobalSettings Clone() {
			return (StripMineGlobalSettings)MemberwiseClone();
		}
	}
}