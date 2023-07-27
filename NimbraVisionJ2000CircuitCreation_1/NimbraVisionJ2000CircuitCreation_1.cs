/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
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

14/03/2023	1.0.0.1		JSV, Skyline	Initial version
****************************************************************************
*/

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Skyline.DataMiner.Automation;

public class CreateFieldsJ2K
{
	[JsonProperty("capacity")]
	public int Capacity { get; set; }

	[JsonProperty("destination")]
	public string Destination { get; set; }

	[JsonProperty("endTime")]
	public string EndTime { get; set; }

	[JsonProperty("protectionId")]
	public int ProtectionId { get; set; }

	[JsonProperty("serviceId")]
	public string ServiceId { get; set; }

	[JsonProperty("source")]
	public string Source { get; set; }

	[JsonProperty("startTime")]
	public string StartTime { get; set; }

	public bool ShouldSerializeEndTime()
	{
		return EndTime != "-1";
	}

	public bool ShouldSerializeStartTime()
	{
		return StartTime != "-1";
	}
}

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		var startTime = engine.GetScriptParam("Start Time").Value;
		var endTime = engine.GetScriptParam("End Time").Value;
		var source = engine.GetScriptParam("Source").Value;
		var destination = engine.GetScriptParam("Destination").Value;
		var capacity = engine.GetScriptParam("Capacity").Value;
		var hitless = engine.GetScriptParam("1+1").Value;

		var fields = new CreateFieldsJ2K();

		fields.ServiceId = hitless.ToLower() == "true" || hitless.ToLower() == "enabled" ? "j2k-hitless" : "j2k";

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

		if (!Int32.TryParse(capacity, out var integerCapcity))
		{
			engine.ExitFail("Capcity isn't an integer. Can't create circuit.");
			return;
		}

		fields.Capacity = integerCapcity;

		if (startTime != "-1" && !DateTime.TryParseExact(startTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startTimeDate))
		{
			engine.ExitFail("Start Time isn't in the supported format - yyyy-MM-ddTHH:mm:ssZ");
			return;
		}

		if (startTime == "-1")
			startTime = DateTime.UtcNow.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

		fields.StartTime = startTime;

		if (endTime != "-1" && !DateTime.TryParseExact(endTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime stopTimeDate))
		{
			engine.ExitFail("Stop Time isn't in the supported format - yyyy-MM-ddTHH:mm:ssZ");
			return;
		}

		fields.EndTime = endTime;

		fields.ProtectionId = 1; // first circuit to be created

		engine.GenerateInformation(JsonConvert.SerializeObject(fields));

		ValidateAndReturnElement(engine).SetParameter(125, JsonConvert.SerializeObject(fields));

		Thread.Sleep(5000);

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

	private static Element ValidateAndReturnElement(Engine engine)
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