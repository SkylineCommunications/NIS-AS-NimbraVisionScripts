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
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.CommunityLibrary.FlowProvisioning.Info;
using Skyline.DataMiner.Library.Automation;
using Skyline.DataMiner.Library.Common.InterAppCalls.CallBulk;
using Skyline.DataMiner.Library.Common.InterAppCalls.CallSingle;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private static readonly List<Type> KnownTypes = new List<Type> { typeof(FlowInfoMessage), typeof(DeleteCircuitMessage) };

	public enum Pids
	{
		CircuitsTable = 1800,
	}

	public static Element ValidateAndReturnElement(Engine engine, string elementName)
	{
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

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		var intfName = Regex.Replace(engine.GetScriptParam("Interface Name").Value, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);
		var element = ValidateAndReturnElement(engine, "NetInsight Nimbra Vision");
		if(element == null)
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
			if(Convert.ToString(row[8]) == intfName || Convert.ToString(row[9]) == intfName)
			{
				sharedIds.Add(Convert.ToString(row[1]));
			}
		}

		IInterAppCall deleteCommand = InterAppCallFactory.CreateNew();
		foreach (var sharedId in sharedIds)
		{
			DeleteCircuitMessage basicCircuitDeleteMessage = new DeleteCircuitMessage { SharedId = Convert.ToString(sharedId) };
			deleteCommand.Messages.Add(basicCircuitDeleteMessage);
		}

		engine.GenerateInformation(String.Join(";", sharedIds));

		deleteCommand.Send(Engine.SLNetRaw, idmsElement.DmsElementId.AgentId, idmsElement.DmsElementId.ElementId, 9000000, KnownTypes);

		Thread.Sleep(1500);
	}
}

public class DeleteCircuitMessage : Message
{
	public string SharedId { get; set; }
}