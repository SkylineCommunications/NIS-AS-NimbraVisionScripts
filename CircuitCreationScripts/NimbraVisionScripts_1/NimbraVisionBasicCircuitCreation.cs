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

25/01/2023	1.0.0.1		JSV, Skyline	Initial version
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages.SLDataGateway;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	public static Element ValidateAndReturnElement(Engine engine, string elementName)
	{
		var element = engine.FindElement("Nimbra Vision");

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

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	///
	public void Run(Engine engine)
	{
		var serviceId = engine.GetScriptParam("Service ID").Value;
		var startTime = engine.GetScriptParam("Start Time").Value;
		var endTime = engine.GetScriptParam("End Time").Value;
		var source = engine.GetScriptParam("Source").Value;
		var destination = engine.GetScriptParam("Destination").Value;
		var capacity = engine.GetScriptParam("Capacity").Value;

		var fields = new CreateFields();

		if (String.IsNullOrEmpty(serviceId) || String.IsNullOrWhiteSpace(serviceId))
		{
			engine.ExitFail("Service ID is null or empty. Can't create circuit.");
			return;
		}

		fields.ServiceId = serviceId;

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

		fields.StartTime = startTime;

		if (endTime != "-1" && !DateTime.TryParseExact(endTime, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime stopTimeDate))
		{
			engine.ExitFail("Stop Time isn't in the supported format - yyyy-MM-ddTHH:mm:ssZ");
			return;
		}

		fields.EndTime = endTime;

		engine.GenerateInformation(JsonConvert.SerializeObject(fields));

		ValidateAndReturnElement(engine, "Nimbra Vision").SetParameter(125, JsonConvert.SerializeObject(fields));

		Thread.Sleep(5000);

		engine.ExitSuccess("Sent request to Nimbra Vision element.");
	}

	public class CreateFields
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
		public string StartTime { get; set; }

		[JsonProperty("endTime")]
		public string EndTime { get; set; }

		public bool ShouldSerializeStartTime()
		{
			return StartTime != "-1";
		}

		public bool ShouldSerializeEndTime()
		{
			return EndTime != "-1";
		}
	}
}