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
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Skyline.Automation.CircuitCreation;
using Skyline.Automation.CircuitCreation.Model;
using Skyline.Automation.CircuitCreation.Presenter;
using Skyline.Automation.CircuitCreation.View;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.DeveloperCommunityLibrary.InteractiveAutomationToolkit;
using Skyline.DataMiner.Library.Common.InterAppCalls.CallBulk;
using Skyline.DataMiner.Library.Common.InterAppCalls.CallSingle;
using Skyline.DataMiner.Library.Common.Serializing;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Actions;
using Skyline.DataMiner.Net.Correlation;
using Skyline.DataMiner.Net.MasterSync;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.ReportsAndDashboards;
using Skyline.DataMiner.Net.Sections;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	public SectionDefinition SectionDefinition { get; set; }

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		engine.ExitFail("This script should be executed using the 'OnDomAction' entry point");
	}

	[AutomationEntryPoint(AutomationEntryPointType.Types.OnDomAction)]
	public void OnDomActionMethod(IEngine engine, ExecuteScriptDomActionContext context)
	{
		var instanceId = context.ContextId as DomInstanceId;
		var action = engine.GetScriptParam("Action")?.Value;

		if (!Utils.ValidateArguments(instanceId, action))
		{
			engine.ExitFail("Input is not valid");
		}

		var domHelper = new DomHelper(engine.SendSLNetMessages, instanceId.ModuleId);
		string transitionId = string.Empty;
		DomInstance domInstance = Utils.GetDomInstance(domHelper, instanceId);
		var sectionDefinitionLinks = domInstance.GetDomDefinition().SectionDefinitionLinks;
		FilterElement<SectionDefinition> sectionDefintionfilter = SectionDefinitionExposers.ID.Equal(sectionDefinitionLinks.First().SectionDefinitionID);
		SectionDefinition = domHelper.SectionDefinitions.Read(sectionDefintionfilter).First(sd => sd.GetName() == "Circuit Info");

		switch (action)
		{
			case "Select Interfaces":
				transitionId = "draft_to_waiting for approval";
				ScheduleReservation(engine, domInstance, domHelper, transitionId);
				break;
			case "Approve":
				var requestSent = ConfirmReservationAndCreateCircuit(engine, domInstance);
				domHelper.DomInstances.Update(domInstance);
				transitionId = requestSent ? "waiting for approval_to_confirmed" : "waiting for approval_to_rejected";
				break;
			case "Reject":
				transitionId = "waiting for approval_to_rejected";
				break;
			default:
				throw new InvalidOperationException($"Action {action} not supported.");
		}

		if (!string.IsNullOrEmpty(transitionId))
		{
			domHelper.DomInstances.DoStatusTransition(instanceId, transitionId);
		}
	}

	private static void ScheduleReservation(IEngine engine, DomInstance domInstance, DomHelper domHelper, string transitionId)
	{
		Utils.CircuitType circuitType = (Utils.CircuitType)Convert.ToInt32(Utils.GetFieldValue(domInstance, "Circuit Type"));

		// engine.ShowUI(); - this comment is needed for Interactive UI to work
		var controller = new InteractiveController(engine);
		var settings = new Settings();
		var model = new Model(engine, domInstance, domHelper, transitionId);
		var view = new View(engine, settings, circuitType);
		var presenter = new Presenter(view, model);

		view.Show(false);
		presenter.LoadFromModel();

		controller.Run(view);
	}

	private static void CreateJ2KCircuit(IEngine engine, DateTime startTime, DateTime endTime, string sourceIntf, string destinationIntf, long newCapacity, Utils.CircuitType circuitType)
	{
		var now = DateTime.Now;
		var subscriptJ2k = engine.PrepareSubScript("NimbraVisionJ2000CircuitCreation");
		subscriptJ2k.SelectScriptParam("Capacity", newCapacity.ToString());
		subscriptJ2k.SelectScriptParam("Start Time", startTime < now ? "-1" : startTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
		subscriptJ2k.SelectScriptParam("End Time", endTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
		subscriptJ2k.SelectScriptParam("Source", Utils.GetCircuitNamedItsInterface(sourceIntf));
		subscriptJ2k.SelectScriptParam("Destination", Utils.GetCircuitNamedItsInterface(destinationIntf));
		subscriptJ2k.SelectScriptParam("1+1", circuitType == Utils.CircuitType.J2kHitless ? "enabled" : "no");
		engine.GenerateInformation("ScriptData: " + JsonConvert.SerializeObject(subscriptJ2k));
		subscriptJ2k.StartScript();
	}

	private static void CreateELineCircuit(IEngine engine, DateTime startTime, DateTime endTime, string sourceIntf, string destinationIntf, long newCapacity)
	{
		var now = DateTime.Now;
		var subscriptEline = engine.PrepareSubScript("NimbraVisionBasicCircuitCreation");
		subscriptEline.SelectScriptParam("Service ID", "E-Line");
		subscriptEline.SelectScriptParam("Capacity", newCapacity.ToString());
		subscriptEline.SelectScriptParam("Start Time", startTime < now ? "-1" : startTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
		subscriptEline.SelectScriptParam("End Time", endTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
		subscriptEline.SelectScriptParam("Source", Utils.GetCircuitNamedEtsInterface(sourceIntf));
		subscriptEline.SelectScriptParam("Destination", Utils.GetCircuitNamedEtsInterface(destinationIntf));
		subscriptEline.StartScript();
	}

	private static bool ConfirmReservationAndCreateCircuit(IEngine engine, DomInstance domInstance)
	{
		Utils.CircuitType circuitType = (Utils.CircuitType)Convert.ToInt32(Utils.GetFieldValue(domInstance, "Circuit Type"));
		var startTime = Convert.ToDateTime(Utils.GetFieldValue(domInstance, "Start time"));
		var endTime = Convert.ToDateTime(Utils.GetFieldValue(domInstance, "End time"));
		var sourceIntf = Convert.ToString(Utils.GetFieldValue(domInstance, "Source Interface"));
		var destinationIntf = Convert.ToString(Utils.GetFieldValue(domInstance, "Destination Interface"));
		long capacity = Convert.ToInt64(Utils.GetFieldValue(domInstance, "Capacity"));

		var now = DateTime.Now;

		if (endTime < now || endTime < startTime)
		{
			engine.GenerateInformation("EndTime can't be before present or before Start Time.");
			return false;
		}

		switch (circuitType)
		{
			case Utils.CircuitType.Eline:
				CreateELineCircuit(engine, startTime, endTime, sourceIntf, destinationIntf, capacity);
				return true;
			case Utils.CircuitType.J2k:
			case Utils.CircuitType.J2kHitless:
				CreateJ2KCircuit(engine, startTime, endTime, sourceIntf, destinationIntf, capacity, circuitType);
				return true;
			default:
				engine.GenerateInformation($"Circuit type {circuitType} not supported.");
				return false;
		}
	}
}