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

07/04/2023	1.0.0.1		JSV, Skyline	Initial version
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Skyline.Automation.CircuitCreation;
using Skyline.Automation.CircuitCreation.Model;
using Skyline.Automation.CircuitCreation.Presenter;
using Skyline.Automation.CircuitCreation.View;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.DeveloperCommunityLibrary.InteractiveAutomationToolkit;
using Skyline.DataMiner.Library.Automation;
using Skyline.DataMiner.Library.Common;
using Skyline.DataMiner.Library.Common.InterAppCalls.CallBulk;
using Skyline.DataMiner.Library.Common.InterAppCalls.CallSingle;
using Skyline.DataMiner.Library.Common.Serializing;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Actions;
using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
using Skyline.DataMiner.Net.Correlation;
using Skyline.DataMiner.Net.History;
using Skyline.DataMiner.Net.LogHelpers;
using Skyline.DataMiner.Net.ManagerStore;
using Skyline.DataMiner.Net.MasterSync;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.ReportsAndDashboards;
using Skyline.DataMiner.Net.Sections;
using static Skyline.Automation.CircuitCreation.Utils;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	public SectionDefinition SectionDefinition { get; set; }

	public DomHelper DomHelper { get; set; }

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		DomHelper = new DomHelper(engine.SendSLNetMessages, "(netinsight)circuitjobs");
		var domInstancesCrudHelper = DomHelper.DomInstances;
		var allDomInstances = domInstancesCrudHelper.ReadAll();
		DomHelper.StitchDomInstances(allDomInstances);

		var dateTimeNowServer = DateTime.Now;

		// Check End and Start Times to do status transitions.
		foreach (var domInstance in allDomInstances)
		{
			if(domInstance.StatusId == "ongoing")
			{
				var endTime = Convert.ToDateTime(GetFieldValue(domInstance, "End time")).ToLocalTime();
				if(endTime < dateTimeNowServer)
				{
					var transitionId = "ongoing_to_completed";
					DomHelper.DomInstances.DoStatusTransition(domInstance.ID, transitionId);
				}

				continue;
			}

			if(domInstance.StatusId == "confirmed")
			{
				var startTime = Convert.ToDateTime(GetFieldValue(domInstance, "Start time")).ToLocalTime();
				if (startTime < dateTimeNowServer)
				{
					var transitionId = "confirmed_to_ongoing";
					DomHelper.DomInstances.DoStatusTransition(domInstance.ID, transitionId);
					continue;
				}

				var endTime = Convert.ToDateTime(GetFieldValue(domInstance, "End time"));
				if (endTime < dateTimeNowServer)
				{
					var transitionId = "confirmed_to_completed";
					DomHelper.DomInstances.DoStatusTransition(domInstance.ID, transitionId);
				}
			}
		}
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

		DomHelper = new DomHelper(engine.SendSLNetMessages, instanceId.ModuleId);
		string transitionId = string.Empty;
		DomInstance domInstance = Utils.GetDomInstance(DomHelper, instanceId);
		var sectionDefinitionLinks = domInstance.GetDomDefinition().SectionDefinitionLinks;
		FilterElement<SectionDefinition> sectionDefintionfilter = SectionDefinitionExposers.ID.Equal(sectionDefinitionLinks.First().SectionDefinitionID);
		SectionDefinition = DomHelper.SectionDefinitions.Read(sectionDefintionfilter).First(sd => sd.GetName() == "Circuit Info");

		switch (action)
		{
			case "Select Interfaces":
				transitionId = "draft_to_waiting for approval";
				ScheduleReservation(engine, domInstance, DomHelper, transitionId);
				break;
			case "Approve":
				var requestSent = ConfirmReservationAndCreateCircuit(engine, domInstance);
				if(!requestSent)
				{
					transitionId = "waiting for approval_to_rejected";
					domInstance.AddOrUpdateFieldValue(SectionDefinition, SectionDefinition.GetAllFieldDescriptors().First(fd => fd.Name == "Circuit Notes"), "Circuit Creation wasn't succssful");
					var ui = new UIBuilder
					{
						Title = "Circuit Creation",
						RequireResponse = true,
						RowDefs = "50;50",
						ColumnDefs = "*;200;*",
						Height = 150,
						Width = 300,
					};

					ui.AppendBlock(new UIBlockDefinition
					{
						Column = 1,
						ColumnSpan = 1,
						Row = 0,
						RowSpan = 1,
						Type = UIBlockType.StaticText,
						Text = "Circuit Creation wasn't succssful" + "\r\n",
					});

					ui.AppendBlock(new UIBlockDefinition
					{
						Column = 1,
						ColumnSpan = 1,
						Row = 1,
						RowSpan = 1,
						Text = "Dismiss",
						Type = UIBlockType.Button,
					});

					engine.ShowUI(ui);
				}
				else
				{
					transitionId = "waiting for approval_to_confirmed";
				}

				DomHelper.DomInstances.Update(domInstance);
				break;
			case "Reject":
				transitionId = "waiting for approval_to_rejected";
				break;
			case "Terminate":
				transitionId = "ongoing_to_completed";
				DeleteReservationCircuits(engine, domInstance, transitionId);
				DomHelper.DomInstances.Update(domInstance);
				break;
			case "Cancel":
				transitionId = "confirmed_to_cancelled";
				DeleteReservationCircuits(engine, domInstance, transitionId);
				DomHelper.DomInstances.Update(domInstance);
				break;
			default:
				throw new InvalidOperationException($"Action {action} not supported.");
		}

		if (!string.IsNullOrEmpty(transitionId))
		{
			DomHelper.DomInstances.DoStatusTransition(instanceId, transitionId);
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

	private static bool CreateJ2KCircuit(IEngine engine, DateTime startTime, DateTime endTime, string sourceIntf, string destinationIntf, long newCapacity, Utils.CircuitType circuitType)
	{
		try
		{
			var now = DateTime.Now;
			var subscriptJ2k = engine.PrepareSubScript("NimbraVisionJ2000CircuitCreation");
			subscriptJ2k.SelectScriptParam("Capacity", newCapacity.ToString());
			subscriptJ2k.SelectScriptParam("Start Time", startTime < now ? "-1" : startTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
			subscriptJ2k.SelectScriptParam("End Time", endTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
			subscriptJ2k.SelectScriptParam("Source", GetCircuitNamedItsInterface(sourceIntf));
			subscriptJ2k.SelectScriptParam("Destination", GetCircuitNamedItsInterface(destinationIntf));
			subscriptJ2k.SelectScriptParam("1+1", circuitType == CircuitType.J2kHitless ? "enabled" : "no");
			subscriptJ2k.StartScript();

			return ValidateCircuitCreation(engine, circuitType, sourceIntf, destinationIntf);
		}
		catch
		{
			return false;
		}
	}

	private static bool CreateELineCircuit(IEngine engine, DateTime startTime, DateTime endTime, string sourceIntf, string destinationIntf, long newCapacity)
	{
		try
		{
			var now = DateTime.Now;
			var subscriptEline = engine.PrepareSubScript("NimbraVisionBasicCircuitCreation");
			subscriptEline.SelectScriptParam("Service ID", "E-Line");
			subscriptEline.SelectScriptParam("Capacity", newCapacity.ToString());
			subscriptEline.SelectScriptParam("Start Time", startTime < now ? "-1" : startTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
			subscriptEline.SelectScriptParam("End Time", endTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
			subscriptEline.SelectScriptParam("Source", GetCircuitNamedEtsInterface(sourceIntf));
			subscriptEline.SelectScriptParam("Destination", GetCircuitNamedEtsInterface(destinationIntf));
			subscriptEline.StartScript();
			return ValidateCircuitCreation(engine, CircuitType.Eline, sourceIntf, destinationIntf);
		}
		catch
		{
			return false;
		}
	}

	private static bool CreateJxsCircuit(IEngine engine, DateTime startTime, DateTime endTime, string sourceIntf, string destinationIntf, long newCapacity, Utils.CircuitType circuitType)
	{
		try
		{
			var now = DateTime.Now;
			var subscriptJ2k = engine.PrepareSubScript("NimbraVisionJxsCircuitCreation");
			subscriptJ2k.SelectScriptParam("Capacity", newCapacity.ToString());
			subscriptJ2k.SelectScriptParam("Start Time", startTime < now ? "-1" : startTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
			subscriptJ2k.SelectScriptParam("End Time", endTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
			subscriptJ2k.SelectScriptParam("Source", GetCircuitNamedItsInterface(sourceIntf));
			subscriptJ2k.SelectScriptParam("Destination", GetCircuitNamedItsInterface(destinationIntf));
			subscriptJ2k.SelectScriptParam("1+1", circuitType == CircuitType.JxsHitless ? "enabled" : "no");
			subscriptJ2k.StartScript();

			return ValidateCircuitCreation(engine, circuitType, sourceIntf, destinationIntf);
		}
		catch
		{
			return false;
		}
	}

	private static bool ValidateCircuitCreation(IEngine engine, CircuitType circuitType, string sourceIntf, string destinationIntf)
	{
		var dms = engine.GetDms() ?? throw new NullReferenceException("dms");
		var nimbraElement = dms.GetElements()
						 .FirstOrDefault(elem => elem.Protocol.Name == "NetInsight Nimbra Vision" && elem.Protocol.Version == "Production") ?? throw new NullReferenceException("Nimbra Vision");

		var circuitTable = nimbraElement.GetTable((int)Pids.CircuitTable);
		string translatedSourceIntf = String.Empty;
		string translatedDestinationIntf = String.Empty;

		if (circuitType == CircuitType.Eline)
		{
			translatedDestinationIntf = GetCircuitNamedEtsInterface(Convert.ToString(destinationIntf));
			translatedSourceIntf = GetCircuitNamedEtsInterface(Convert.ToString(sourceIntf));
		}
		else
		{
			translatedDestinationIntf = GetCircuitNamedItsInterface(Convert.ToString(destinationIntf));
			translatedSourceIntf = GetCircuitNamedItsInterface(Convert.ToString(sourceIntf));
		}

		for (int i = 0; i < NumberOfRetries; i++)
		{
			var row = circuitTable.GetData()
						 .FirstOrDefault(kv => Convert.ToString(kv.Value[(int)Idx.CircuitSourceIntf]) == translatedSourceIntf && Convert.ToString(kv.Value[(int)Idx.CircuitDestIntf]) == translatedDestinationIntf);
			if (row.Key != null)
				return true;

			Thread.Sleep(SleepTime);
			circuitTable = nimbraElement.GetTable((int)Pids.CircuitTable);
		}

		return false;
	}

	private static bool ConfirmReservationAndCreateCircuit(IEngine engine, DomInstance domInstance)
	{
		CircuitType circuitType = (CircuitType)Convert.ToInt32(GetFieldValue(domInstance, "Circuit Type"));
		var startTime = Convert.ToDateTime(GetFieldValue(domInstance, "Start time"));
		var endTime = Convert.ToDateTime(GetFieldValue(domInstance, "End time"));
		var sourceIntf = Convert.ToString(GetFieldValue(domInstance, "Source Interface"));
		var destinationIntf = Convert.ToString(GetFieldValue(domInstance, "Destination Interface"));
		long capacity = Convert.ToInt64(GetFieldValue(domInstance, "Capacity"));

		var now = DateTime.UtcNow;

		if (endTime < now || endTime < startTime)
		{
			engine.GenerateInformation("EndTime can't be before present or before Start Time.");
			return false;
		}

		switch (circuitType)
		{
			case CircuitType.Eline:
				if(CreateELineCircuit(engine, startTime, endTime, sourceIntf, destinationIntf, capacity))
				{
					return true;
				}

				return false;
			case CircuitType.J2k:
			case CircuitType.J2kHitless:
				if (CreateJ2KCircuit(engine, startTime, endTime, sourceIntf, destinationIntf, capacity, circuitType))
				{
					return true;
				}

				return false;
			case CircuitType.Jxs:
			case CircuitType.JxsHitless:
				if(CreateJxsCircuit(engine, startTime, endTime, sourceIntf, destinationIntf, capacity, circuitType))
				{
					return true;
				}

				return false;
			default:
				engine.GenerateInformation($"Circuit type {circuitType} not supported.");
				return false;
		}
	}

	private void DeleteReservationCircuits(IEngine engine, DomInstance domInstance, string transitionId)
	{
		try
		{
			var sourceInterface = GetFieldValue(domInstance, "Source Interface");
			var destinationInterface = GetFieldValue(domInstance, "Destination Interface");
			string translatedSourceIntf = String.Empty;
			string translatedDestinationIntf = String.Empty;
			var circuitType = (CircuitType)Convert.ToInt32(GetFieldValue(domInstance, "Circuit Type"));

			if (circuitType == CircuitType.Eline)
			{
				translatedDestinationIntf = GetCircuitNamedEtsInterface(Convert.ToString(destinationInterface));
				translatedSourceIntf = GetCircuitNamedEtsInterface(Convert.ToString(sourceInterface));
			}
			else
			{
				translatedDestinationIntf = GetCircuitNamedItsInterface(Convert.ToString(destinationInterface));
				translatedSourceIntf = GetCircuitNamedItsInterface(Convert.ToString(sourceInterface));
			}

			var dms = engine.GetDms() ?? throw new NullReferenceException("dms");
			var nimbraElement = dms.GetElements()
							 .FirstOrDefault(elem => elem.Protocol.Name == "NetInsight Nimbra Vision" && elem.Protocol.Version == "Production") ?? throw new NullReferenceException("Nimbra Vision");

			var circuitTable = nimbraElement.GetTable((int)Pids.CircuitTable);

			var rowToDelete = circuitTable.GetData()
								.FirstOrDefault(kv => Convert.ToString(kv.Value[(int)Idx.CircuitSourceIntf]) == translatedSourceIntf && Convert.ToString(kv.Value[(int)Idx.CircuitDestIntf]) == translatedDestinationIntf);

			IInterAppCall deleteCommand = InterAppCallFactory.CreateNew();
			DeleteCircuitMessage basicCircuitDeleteMessage = new DeleteCircuitMessage { SharedId = Convert.ToString(rowToDelete.Value[(int)Utils.Idx.CircuitsSharedId]) };
			deleteCommand.Messages.Add(basicCircuitDeleteMessage);
			deleteCommand.Send(Engine.SLNetRaw, nimbraElement.DmsElementId.AgentId, nimbraElement.DmsElementId.ElementId, 9000000, Utils.KnownTypes);

			var sectionDefinitionLinks = domInstance.GetDomDefinition().SectionDefinitionLinks;
			FilterElement<SectionDefinition> sectionDefintionfilter = SectionDefinitionExposers.ID.Equal(sectionDefinitionLinks.First().SectionDefinitionID);
			var sectionDefinition = DomHelper.SectionDefinitions.Read(sectionDefintionfilter).First(sd => sd.GetName() == "Circuit Info");

			if(transitionId == "ongoing_to_completed")
			{
				domInstance.AddOrUpdateFieldValue(sectionDefinition, sectionDefinition.GetAllFieldDescriptors().First(fd => fd.Name == "End time"), DateTime.Now);
			}
			else
			{
				domInstance.AddOrUpdateFieldValue(sectionDefinition, sectionDefinition.GetAllFieldDescriptors().First(fd => fd.Name == "End time"), Utils.GetFieldValue(domInstance, "Start time"));
			}
		}
		catch(Exception e)
		{
			engine.GenerateInformation("Couldn't complete deletion. Exception: " + e);
		}
	}
}