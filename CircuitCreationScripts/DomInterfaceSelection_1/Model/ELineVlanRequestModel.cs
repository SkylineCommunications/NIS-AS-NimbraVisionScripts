namespace Skyline.Automation.CircuitCreation.Model
{
	using Newtonsoft.Json;

	public class ELineVlanRequestModel : BaseRequestModel
	{
		[JsonProperty("extra")]
		public Extra ExtraInfo { get; set; }

		public class Extra
		{
			[JsonProperty("common")]
			public Common Common { get; set; }
		}

		public class Common
		{
			[JsonProperty("VLANs")]
			public int VLAN { get; set; }

			public string FormName { get; set; }
		}
	}
}
