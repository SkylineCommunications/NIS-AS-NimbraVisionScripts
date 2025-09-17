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
using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp;
using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp.Messages;

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

		var fields = new J2KCircuitRequest();

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

		fields.ProtectionId = 1; // first circuit to be created

		SetDateTimeField(engine, startTime, dt => fields.StartTime = dt, "Start Time");
		SetDateTimeField(engine, endTime, dt => fields.EndTime = dt, "Stop Time");

		engine.GenerateInformation(JsonConvert.SerializeObject(fields));

		var nimbraVisionInterAppCalls = ValidateAndReturnElement(engine);

		var response = nimbraVisionInterAppCalls.SendSingleResponseMessage(fields);

		engine.Sleep(5000);

		if (response.Success)
		{
			engine.ExitSuccess("Circuit created");
		}
		else
		{
			engine.ExitFail($"Fail to create circuit: {response.Message}");
		}
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

	private static INimbraVisionInterAppCalls ValidateAndReturnElement(Engine engine)
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

		return new NimbraVisionInterAppCalls(engine.GetUserConnection(), elementName);
	}

	private static void SetDateTimeField(Engine engine, string time, Action<DateTime?> setField, string fieldName)
	{
		if (time == "-1")
		{
			setField(null);
		}
		else if (DateTime.TryParseExact(time, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
		{
			setField(parsedDate);
		}
		else
		{
			engine.ExitFail($"{fieldName} isn't in the supported format - yyyy-MM-ddTHH:mm:ssZ");
		}
	}
}