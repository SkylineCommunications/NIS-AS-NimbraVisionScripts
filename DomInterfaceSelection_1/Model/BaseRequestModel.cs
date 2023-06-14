namespace Skyline.Automation.CircuitCreation.Model
{
	using System;
	using Newtonsoft.Json;

	public class BaseRequestModel
	{
		[JsonProperty("serviceId")]
		public string ServiceId { get; set; }

		[JsonProperty("source")]
		public string Source { get; set; }

		[JsonProperty("destination")]
		public string Destination { get; set; }

		[JsonProperty("capacity")]
		public int Capacity { get; set; }

		[JsonProperty("startTime")]
		public DateTime StartTime { get; set; }

		[JsonProperty("endTime")]
		public DateTime EndTime { get; set; }

		public bool ShouldSerializeStartTime()
		{
			return StartTime != DateTime.MinValue;
		}

		public bool ShouldSerializeEndTime()
		{
			return EndTime != DateTime.MinValue;
		}
	}
}
