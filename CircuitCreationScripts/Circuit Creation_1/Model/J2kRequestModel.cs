namespace Skyline.Automation.CircuitCreation.Model
{
	using System;
	using Newtonsoft.Json;

	public class J2kRequestModel : BaseRequestModel
	{
		[JsonProperty("protectionId")]
		public int? ProtectionId { get; set; }

		public bool ShouldSerializeProtectionId()
		{
			return ProtectionId != -1;
		}
	}
}
