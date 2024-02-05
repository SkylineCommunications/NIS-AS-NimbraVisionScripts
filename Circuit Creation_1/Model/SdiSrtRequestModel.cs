namespace Skyline.Automation.CircuitCreation.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Newtonsoft.Json;

	public class SdiSrtRequestModel : BaseRequestModel
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
			public string FormName { get; set; }

			[JsonProperty("StreamType")]
			public string Mode { get; set; }

			public string Passphrase { get; set; }

			[JsonProperty("StreamPort")]
			public int Port { get; set; }
		}
	}
}
