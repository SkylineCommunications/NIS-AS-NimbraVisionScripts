/*
****************************************************************************
*  Copyright (c) 2024,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

	Skyline Communications NV
	Ambachtenstraat 33
	B-8870 Izegem
	Belgium
	Tel.	: +32 51 31 35 69
	Fax.	: +32 51 31 01 29
	E-mail	: info@skyline.be
	Web		: www.skyline.be
	Contact	: Ben Vandenberghe

****************************************************************************
Revision History:

DATE		VERSION		AUTHOR			COMMENTS

dd/mm/2024	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

namespace NimbraVisionSrtCircuitCreation_1
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json;
	using Skyline.DataMiner.Automation;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			var startTime = engine.GetScriptParam("Start Time").Value;
			var endTime = engine.GetScriptParam("End Time").Value;
			var source = engine.GetScriptParam("Source").Value;
			var destination = engine.GetScriptParam("Destination").Value;
			var capacity = engine.GetScriptParam("Capacity").Value;
			var streamPort = engine.GetScriptParam("Port").Value;
			var mode = engine.GetScriptParam("Mode").Value;
			var password = engine.GetScriptParam("Password").Value;

			var fields = new CreateFieldsSdiSrt();

			fields.ServiceId = "VA-SRT";

			if (String.IsNullOrEmpty(source) || String.IsNullOrWhiteSpace(source))
			{
				engine.ExitFail("Source is null or empty. Can't create circuit.");
				return;
			}

			fields.Source = Regex.Replace(source, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);

			if (String.IsNullOrEmpty(destination) || String.IsNullOrWhiteSpace(destination))
			{
				engine.ExitFail("Destination is null or empty. Can't create circuit.");
				return;
			}

			fields.Destination = Regex.Replace(destination, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);

			capacity = Regex.Replace(capacity, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);

			if (!Int32.TryParse(capacity, out var integerCapcity))
			{
				engine.ExitFail("Capacity isn't an integer. Can't create circuit.");
				return;
			}

			fields.Capacity = integerCapcity;

			if (!DateTime.TryParseExact(startTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startTimeDate) && startTime != "-1")
			{
				engine.ExitFail("Start Time isn't in the supported format - yyyy-MM-ddTHH:mm:ssZ");
				return;
			}

			fields.StartTime = startTime;

			if (!DateTime.TryParseExact(endTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime stopTimeDate) && endTime != "-1")
			{
				engine.ExitFail("End Time isn't in the supported format - yyyy-MM-ddTHH:mm:ssZ");
				return;
			}

			fields.EndTime = endTime;

			fields.ExtraInfo = new CreateFieldsSdiSrt.Extra();
			fields.ExtraInfo.Common = new CreateFieldsSdiSrt.Common();

			streamPort = Regex.Replace(streamPort, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);

			if (!Int32.TryParse(streamPort, out var integerPort))
			{
				engine.ExitFail("Port isn't an integer. Can't create circuit.");
				return;
			}

			fields.ExtraInfo.Common.Port = integerPort;

			if (String.IsNullOrEmpty(mode) || String.IsNullOrWhiteSpace(mode))
			{
				engine.ExitFail("Mode is null or empty. Can't create circuit.");
				return;
			}

			mode = Regex.Replace(mode, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);

			fields.ExtraInfo.Common.Mode = mode;

			password = Regex.Replace(password, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);

			if (password != "-1")
			{
				if (password.Length < 10)
				{
					engine.ExitFail("Passphrase must contain at least 10 charterers");
					return;
				}

				fields.ExtraInfo.Common.Passphrase = password;
			}

			fields.ExtraInfo.Common.FormName = "vaSdiSrt";

			ValidateAndReturnElement(engine).SetParameter(125, JsonConvert.SerializeObject(fields, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

			engine.ExitSuccess("Sent request to Nimbra Vision element.");
		}

		private static string ParseParamValue(string paramValueRaw)
		{
			// Checking first characters
			var firstCharacters = "[\"";
			var paramValue = (paramValueRaw.Substring(0, 2) == firstCharacters) ?
				paramValueRaw.Substring(2, paramValueRaw.Length - 4) :
				paramValueRaw;

			return paramValue;
		}

		private static Element ValidateAndReturnElement(IEngine engine)
		{
			var paramValueRaw = engine.GetScriptParam("ElementName").Value;
			var elementName = ParseParamValue(paramValueRaw);
			var element = engine.FindElement(elementName);

			if (element == null)
			{
				engine.ExitFail("Element Nimbra Vision does not exist!");
				return null;
			}

			if (element.ElementInfo.State != Skyline.DataMiner.Net.Messages.ElementState.Active)
			{
				engine.ExitFail("Element Nimbra Vision is not in Active state");
				return null;
			}

			return element;
		}
	}

	public class CreateFieldsSdiSrt
	{
		[JsonProperty("capacity")]
		public int Capacity { get; set; }

		[JsonProperty("destination")]
		public string Destination { get; set; }

		[JsonProperty("endTime")]
		public string EndTime { get; set; }

		[JsonProperty("serviceId")]
		public string ServiceId { get; set; }

		[JsonProperty("source")]
		public string Source { get; set; }

		[JsonProperty("startTime")]
		public string StartTime { get; set; }

		[JsonProperty("extra")]
		public Extra ExtraInfo { get; set; }

		public bool ShouldSerializeEndTime()
		{
			return EndTime != "-1";
		}

		public bool ShouldSerializeStartTime()
		{
			return StartTime != "-1";
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

		public class Extra
		{
			[JsonProperty("common")]
			public Common Common { get; set; }
		}
	}
}