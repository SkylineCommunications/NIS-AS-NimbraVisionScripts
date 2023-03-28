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
using Skyline.DataMiner.Net.MasterSync;
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
		engine.ExitFail("This script should be executed using the 'OnDomAction' entry point");
	}

	[AutomationEntryPoint(AutomationEntryPointType.Types.OnDomAction)]
	public void OnDomActionMethod(IEngine engine, ExecuteScriptDomActionContext context)
	{
		var instanceId = context.ContextId as DomInstanceId;
		var _action = engine.GetScriptParam("Action")?.Value;

		if (!Utils.ValidateArguments(instanceId, _action))
		{
			engine.ExitFail("Input is not valid");
		}

		var domHelper = new DomHelper(engine.SendSLNetMessages, instanceId.ModuleId);
		string transitionId = string.Empty;
		DomInstance domInstance = Utils.GetDomInstance(domHelper, instanceId);

		switch (_action)
		{
			case "Select Interfaces":
				ScheduleReservation(engine, domInstance);
				transitionId = "draft_to_waiting for approval";
				break;
			case "Approve":
				break;
			case "Reject":
				break;
			default:
				throw new InvalidOperationException($"Action {_action} not supported.");
		}

		//if (!string.IsNullOrEmpty(transitionId))
		//{
		//	domHelper.DomInstances.DoStatusTransition(instanceId, transitionId);
		//}
	}

	private static void ScheduleReservation(IEngine engine, DomInstance domInstance)
	{
		Utils.CircuitType circuitType =(Utils.CircuitType)Convert.ToInt32(GetFieldValue(domInstance, "Circuit Type"));
		//engine.FindInteractiveClient("Run Automation", 60);
		// engine.ShowUI(); - this comment is needed for Interactive UI to work
		var controller = new InteractiveController(engine);
		var settings = new Settings();
		var model = new Model(engine);
		var view = new View(engine, settings, circuitType);
		var presenter = new Presenter(view, model);

		view.Show(false);
		presenter.LoadFromModel();

		controller.Run(view);
	}

	private static object GetFieldValue(DomInstance domInstance, string fieldName)
	{
		foreach (var section in domInstance.Sections)
		{
			foreach (var fieldValue in section.FieldValues)
			{
				var fieldDescriptor = fieldValue.GetFieldDescriptor();
				if (fieldDescriptor.Name.Equals(fieldName))
				{
					return fieldValue.Value.Value;
				}
			}
		}

		return null;
	}
}