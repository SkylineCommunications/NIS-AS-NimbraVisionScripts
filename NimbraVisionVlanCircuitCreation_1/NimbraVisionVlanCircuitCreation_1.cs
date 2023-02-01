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

01/02/2023	1.0.0.1		JSV, Skyline	Initial version
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;

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
		var serviceId = engine.GetScriptParam("Service ID").Value;
		var startTime = engine.GetScriptParam("Start Time").Value;
		var endTime = engine.GetScriptParam("End Time").Value;
		var source = engine.GetScriptParam("Source").Value;
		var destination = engine.GetScriptParam("Destination").Value;
		var capacity = engine.GetScriptParam("Capacity").Value;
		var vlan = engine.GetScriptParam("VLAN").Value;
		var formName = engine.GetScriptParam("Form Name").Value;

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

		fields.Source = source;

		if (String.IsNullOrEmpty(destination) || String.IsNullOrWhiteSpace(destination))
		{
			engine.ExitFail("Destination is null or empty. Can't create circuit.");
			return;
		}

		fields.Destination = destination;

		if (!Int32.TryParse(capacity, out var integerCapcity))
		{
			engine.ExitFail("Capcity isn't an integer. Can't create circuit.");
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

		engine.GenerateInformation("Here");

		if (!Int32.TryParse(vlan, out var integerVlan))
		{
			engine.ExitFail("VLAN isn't an integer. Can't create circuit.");
			return;
		}

		fields.ExtraInfo = new CreateFields.Extra();
		fields.ExtraInfo.Common = new CreateFields.Common();
		fields.ExtraInfo.Common.VLAN = integerVlan;

		if (String.IsNullOrEmpty(formName) || String.IsNullOrWhiteSpace(formName))
		{
			engine.ExitFail("Form Name is null or empty. Can't create circuit.");
			return;
		}

		fields.ExtraInfo.Common.FormName = formName;

		ValidateAndReturnElement(engine, "Nimbra Vision").SetParameter(125, JsonConvert.SerializeObject(fields));

		engine.ExitSuccess("Sent request to Nimbra Vision element.");
	}

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

		[JsonProperty("extra")]
		public Extra ExtraInfo { get; set; }

		public class Extra
		{
			[JsonProperty("common")]
			public Common Common { get; set; }
		}

		public class Common
		{
			public int VLAN { get; set; }
			public string FormName { get; set; }
		}

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