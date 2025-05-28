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

dd/mm/2023	1.0.0.1		XXX, Skyline	Initial version
28/05/2025	1.0.0.2		SDT, Skyline	Added support for Nimbra Vision InterApp.
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Core.DataMinerSystem.Automation;
using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp;
using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp.Messages;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	public enum Pids
	{
		CircuitsTable = 1800,
	}

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		var intfName = Regex.Replace(engine.GetScriptParam("Interface Name").Value, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);
		var element = ValidateAndReturnElement(engine);

		if (element == null)
		{
			engine.ExitFail("Couldn't find element named Nimbra Vision");
			return;
		}

		engine.GenerateInformation("Interface Name: " + intfName);

		var dms = engine.GetDms();
		var idmsElement = dms.GetElement(element.ElementName);
		var circuitsTable = idmsElement.GetTable((int)Pids.CircuitsTable);
		var rows = circuitsTable.GetRows();
		HashSet<string> sharedIds = new HashSet<string>();

		foreach (var row in rows)
		{
			if (Convert.ToString(row[8]) == intfName || Convert.ToString(row[9]) == intfName)
			{
				sharedIds.Add(Convert.ToString(row[1]));
			}
		}

		INimbraVisionInterAppCalls nimbraVisionInterAppCalls = new NimbraVisionInterAppCalls(engine.GetUserConnection(), idmsElement.DmsElementId.AgentId, idmsElement.DmsElementId.ElementId);
		List<INimbraVisionRequest> deleteMessages = new List<INimbraVisionRequest>();
		foreach (var sharedId in sharedIds)
		{
			DeleteCircuitRequest basicCircuitDeleteMessage = new DeleteCircuitRequest { SharedId = Convert.ToString(sharedId) };
			deleteMessages.Add(basicCircuitDeleteMessage);
		}

		engine.GenerateInformation(String.Join(";", sharedIds));

		nimbraVisionInterAppCalls.SendMessageNoResponse(deleteMessages.ToArray());

		Thread.Sleep(1500);
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