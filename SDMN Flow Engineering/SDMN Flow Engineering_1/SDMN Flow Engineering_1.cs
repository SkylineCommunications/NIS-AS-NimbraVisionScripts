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
using Skyline.DataMiner.CommunityLibrary.FlowEngineering;
using Skyline.DataMiner.CommunityLibrary.FlowEngineering.Dom;
using Skyline.DataMiner.CommunityLibrary.FlowEngineering.InputData;
using Skyline.DataMiner.CommunityLibrary.FlowEngineering.Path;
using Skyline.DataMiner.Library.Automation;
using Skyline.DataMiner.Library.Common;
using Skyline.DataMiner.Library.Common.InterAppCalls.Shared;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private readonly IFlowId flowId = new FlowId
	{
		Guid = System.Guid.Parse("33647ebc-454c-46c3-ad70-b73231cbc9cc"),
	};

	private IDms dms;
	private FlowManager flowManager;
	private Logger logger;

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		dms = engine.GetDms();
		logger = new Logger(engine);
		flowManager = new FlowManager(Engine.SLNetRaw, logger);

		var source = engine.GetScriptParam("Source Element").Value;
		var destination = engine.GetScriptParam("Destination Element").Value;

		CalcFlow(engine, source, destination);
	}

	private void CalcFlow(Engine engine, string source, string destination)
	{
		var sourceElement = dms.GetElement(source);
		var destinationElement = dms.GetElement(destination);

		var inputData = new ElementInputData(sourceElement, destinationElement);

		var pathsCreated = flowManager.TryCalculatePath(inputData, out IReadOnlyList<IFlowPath> paths);

		if (paths == null || paths.Count == 0)
		{
			logger.Log(String.Format("Unable to calculate path between {0} and {1}.", sourceElement.Name, destinationElement.Name));
		}

		logger.Log("Calculated Paths: " + JsonConvert.SerializeObject(paths));
	}
}

public class Logger : ILogger
{
	private readonly IEngine _engine;

	public Logger(IEngine engine)
	{
		_engine = engine;
	}

	public void Log(string message)
	{
		_engine.GenerateInformation("### FLOW ENGINEERING ### | " + message);
	}
}