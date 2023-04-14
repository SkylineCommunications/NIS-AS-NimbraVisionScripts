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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Library.Automation;
using Skyline.DataMiner.Library.Common;
using Skyline.DataMiner.Net.Helper;
using Skyline.DataMiner.Net.Messages;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private readonly string nimbraVisionElementName = "Nimbra Vision";
	private readonly string nimbraEdgeElementName = "Nimbra Edge";
	private readonly string edgeDisconnectInputStr = "<not connected>";
	private readonly string tableElementName = "E2E Paths";

	private readonly int numberOfRetries = 60;
	private readonly int sleepTime = 1000;
	private readonly int defaultCapacity = 150;

	private string visionDestIntf;
	private string edgeInput;
	private string edgeOutput;
	private Engine _engine;
	private IDms _dms;
	private IDmsElement nimbraVisionElement;
	private IDmsElement nimbraEdgeElement;
	private IDmsElement connectionsTableElement;

	public enum Action
	{
		Create = 1,
		Delete = 2,
	}

	public enum Pids
	{
		ItsInterfaceTable = 1600,
		ItsInterfaceCapabilities = 1603,
		EtsInterfaceTable = 1900,
		EtsInterfaceCircuitNaming = 1924,
		EtsInterfaceNodeName = 1922,
		CircuitTable = 1800,
		EdgeOutputsTable = 15000,
		GenericTable = 100,
		DtmInterfaceTable = 1700,
	}

	public enum Idx
	{
		ItsInterfaceNodeName = 1,
		ItsInterfaceCapabilities = 2,
		ItsInterfaceName = 4,
		EtsInterfaceNodeName = 21,
		EtsInterfaceCircuitNaming = 23,
		CircuitServiceId = 2,
		CircuitSourceIntf = 8,
		CircuitDestIntf = 9,
		CircuitPath = 12,
		DtmNodeName = 16,
		DtmInterfaceName = 14,
		DtmPeerNode = 29,
		DtmPeerInterface = 34,
	}

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		_engine = engine;
		_dms = engine.GetDms();
		var visionSourceIntf = Regex.Replace(engine.GetScriptParam("Nimbra Vision Source Interface").Value, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);
		edgeOutput = Regex.Replace(engine.GetScriptParam("Nimbra Edge Output").Value, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);
		var actionStr = Regex.Replace(engine.GetScriptParam("Action").Value, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);
		edgeInput = Regex.Replace(engine.GetScriptParam("Nimbra Edge Input").Value, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);
		visionDestIntf = Regex.Replace(engine.GetScriptParam("Nimbra Vision Destination Interface").Value, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty);

		nimbraVisionElement = _dms.GetElement(nimbraVisionElementName);
		nimbraEdgeElement = _dms.GetElement(nimbraEdgeElementName);
		connectionsTableElement = _dms.GetElement(tableElementName);

		if (Enum.TryParse(actionStr, true, out Action action))
		{
			if (action == Action.Create)
			{
				CreateEndToEndFlow(visionSourceIntf, edgeOutput);
			}
			else
			{
				DeleteEndToEndFlow(visionSourceIntf, edgeOutput);
			}
		}
		else
		{
			_engine.ShowUI($"Action {actionStr} not supported.");
			engine.GenerateInformation($"Action {actionStr} not supported.");
		}

		Thread.Sleep(1000); // used to make sure all processes have been updated for Low-Code App.
	}

	private void DeleteEndToEndFlow(string visionSourceIntf, string edgeOutput)
	{
		DeleteVisionCircuit(visionSourceIntf);
		DeleteEdgeConnection(edgeOutput);

		// Clear Connections Table
		var table = connectionsTableElement.GetTable((int)Pids.GenericTable);
		table.DeleteRows(table.GetPrimaryKeys());
	}

	private bool CreateEndToEndFlow(string visionSourceIntf, string edgeOutput)
	{
		if (!ValidateVisionInterfacesInUse(visionSourceIntf))
		{
			return false;
		}

		if (!ValidateEdgeInOutInUse(edgeOutput))
		{
			_engine.ShowUI($"Selected output {edgeOutput} is already in use.");
			return false;
		}

		if (!CreateJ2KCircuit(visionSourceIntf))
		{
			// Clear Connections Table
			var table = connectionsTableElement.GetTable((int)Pids.GenericTable);
			table.DeleteRows(table.GetPrimaryKeys());
			_engine.ShowUI("J2K circuit wasn't successfully created.");
			return false;
		}

		CreateEdgeConnection(edgeOutput);

		return true;
	}

	private bool ValidateEdgeInOutInUse(string edgeOutput)
	{
		var outputsTable = nimbraEdgeElement.GetTable((int)Pids.EdgeOutputsTable);
		var outputRows = outputsTable.GetRows();
		return outputRows.Any(row => (Convert.ToString(row[8]) == edgeInput) || (Convert.ToString(row[1]) == edgeOutput && !Convert.ToString(row[8]).IsNotNullOrEmpty()));
	}

	private bool ValidateVisionInterfacesInUse(string visionSourceIntf)
	{
		var circuitsTable = nimbraVisionElement.GetTable((int)Pids.CircuitTable);

		var circuitRows = circuitsTable.GetRows();
		HashSet<string> j2kInterfacesInUse = new HashSet<string>();
		foreach (var row in from row in circuitRows
							where Convert.ToString(row[(int)Idx.CircuitServiceId]).Contains("j2k")
							select row)
		{
			j2kInterfacesInUse.Add(Convert.ToString(row[(int)Idx.CircuitSourceIntf]));
			j2kInterfacesInUse.Add(Convert.ToString(row[(int)Idx.CircuitDestIntf]));
		}

		if (j2kInterfacesInUse.Contains(visionSourceIntf))
		{
			_engine.ShowUI($"Source Interface {visionSourceIntf} is in use.");
			return false;
		}

		if (j2kInterfacesInUse.Contains(visionDestIntf))
		{
			_engine.ShowUI($"Destination Interface {visionDestIntf} is in use.");
			return false;
		}

		return true;
	}

	private bool CreateJ2KCircuit(string sourceIntf)
	{
		try
		{
			var subscriptJ2k = _engine.PrepareSubScript("NimbraVisionJ2000CircuitCreation");
			subscriptJ2k.SelectScriptParam("Capacity", defaultCapacity.ToString());
			subscriptJ2k.SelectScriptParam("Start Time", "-1");
			subscriptJ2k.SelectScriptParam("End Time", "-1");
			subscriptJ2k.SelectScriptParam("Source", sourceIntf);
			subscriptJ2k.SelectScriptParam("Destination", visionDestIntf);
			subscriptJ2k.SelectScriptParam("1+1", "no");
			subscriptJ2k.StartScript();

			return ValidateCircuitCreation(sourceIntf, visionDestIntf);
		}
		catch
		{
			return false;
		}
	}

	private bool ValidateCircuitCreation(string sourceIntf, string destinationIntf)
	{
		var nimbraElement = _dms.GetElements()
						 .FirstOrDefault(elem => elem.Protocol.Name == "NetInsight Nimbra Vision" && elem.Protocol.Version == "Production") ?? throw new NullReferenceException("Nimbra Vision");

		var circuitTable = nimbraElement.GetTable((int)Pids.CircuitTable);
		string translatedSourceIntf = String.Empty;
		string translatedDestinationIntf = String.Empty;

		translatedDestinationIntf = destinationIntf;
		translatedSourceIntf = sourceIntf;

		for (int i = 0; i < numberOfRetries; i++)
		{
			var row = circuitTable.GetData()
						 .FirstOrDefault(kv => Convert.ToString(kv.Value[(int)Idx.CircuitSourceIntf]) == translatedSourceIntf && Convert.ToString(kv.Value[(int)Idx.CircuitDestIntf]) == translatedDestinationIntf);
			if (row.Key != null)
			{
				AddToConnectionTable(row.Value);
				return true;
			}

			Thread.Sleep(sleepTime);
			circuitTable = nimbraElement.GetTable((int)Pids.CircuitTable);
		}

		return false;
	}

	private void AddToConnectionTable(object[] row)
	{
		try
		{
			var input = row[(int)Idx.CircuitSourceIntf].ToString().Split('_');
			var output = row[(int)Idx.CircuitDestIntf].ToString().Split('_');
			var path = row[(int)Idx.CircuitPath].ToString().Trim('[', ']').Replace("\"", String.Empty).Split(',');

			var dtmInterfaces = nimbraVisionElement.GetTable((int)Pids.DtmInterfaceTable).GetRows();
			var connectionsTable = connectionsTableElement.GetTable((int)Pids.GenericTable);

			ParseEachConnection(path, dtmInterfaces, connectionsTable, input, output);
		}
		catch (Exception e)
		{
			_engine.ShowUI("Exception " + e);
		}
	}

	private void ParseEachConnection(string[] path, object[][] dtmInterfaces, IDmsTable connectionsTable, string[] input, string[] output)
	{
		var inputNode = input[1];
		var inputIntf = input[0];
		var outputNode = output[1];
		var outputIntf = output[0];

		for (int i = 0; i < path.Length; i++)
		{
			// check if it's not the last hop
			if (i <= path.Length - 2)
			{
				var currentIntfOfPath = path[i];
				var nextIntfOfPath = path[i + 1];

				var currentNode = currentIntfOfPath.Split('_')[1];
				var nextNode = nextIntfOfPath.Split('_')[1].Trim();

				var dtmRow = dtmInterfaces.FirstOrDefault(r => Convert.ToString(r[(int)Idx.DtmNodeName]) == currentNode && Convert.ToString(r[(int)Idx.DtmPeerNode]) == nextNode);

				if (dtmRow is null)
				{
					_engine.ShowUI("Couldn't find Trunk connection!");
					return;
				}

				string nextHopIntf;
				string nextHopNode;

				nextHopIntf = dtmRow[(int)Idx.DtmPeerInterface].ToString().Split('_')[0].Replace("dtm", String.Empty);
				nextHopNode = dtmRow[(int)Idx.DtmPeerNode].ToString();

				// Check if it's first hop
				if (i == 0)
				{
					var sourceIntf = inputIntf + " SDI / " + currentIntfOfPath.Split('_')[0] + " DTM";
					_engine.GenerateInformation("sourceIntf: " + sourceIntf);
					connectionsTable.AddRow(new object[]
					{
							Guid.NewGuid().ToString(),
							sourceIntf,
							nextHopIntf + " DTM",
							inputNode,
							nextHopNode,
							null,
							null,
							null,
							null,
							null,
							null,
					});
				}
				else
				{
					connectionsTable.AddRow(new object[]
					{
							Guid.NewGuid().ToString(),
							currentIntfOfPath.Split('_')[0] + " DTM",
							nextHopIntf + " DTM",
							currentIntfOfPath.Split('_')[1],
							nextHopNode,
							null,
							null,
							null,
							null,
							null,
							null,
					});
				}
			}
			else
			{
				AddLastHop(path[i], dtmInterfaces, outputNode, outputIntf, connectionsTable);
			}
		}
	}

	private void AddLastHop(string hop, object[][] dtmInterfaces, string outputNode, string outputIntf, IDmsTable connectionsTable)
	{
		var currentNode = hop.Split('_')[1];
		var dtmRow = dtmInterfaces.FirstOrDefault(r => Convert.ToString(r[(int)Idx.DtmNodeName]) == currentNode && Convert.ToString(r[(int)Idx.DtmPeerNode]) == outputNode);

		if (dtmRow is null)
		{
			_engine.ShowUI("Couldn't find Trunk connection!");
			return;
		}

		string nextHopIntf;
		string nextHopNode;

		nextHopIntf = dtmRow[(int)Idx.DtmPeerInterface].ToString().Split('_')[0].Replace("dtm", String.Empty);
		nextHopNode = dtmRow[(int)Idx.DtmPeerNode].ToString();

		connectionsTable.AddRow(new object[]
		{
						Guid.NewGuid().ToString(),
						hop.Split('_')[0] + " DTM",
						nextHopIntf + " DTM",
						hop.Split('_')[1],
						nextHopNode,
						null,
						null,
						null,
						null,
						null,
						null,
		});

		connectionsTable.AddRow(new object[]
		{
						Guid.NewGuid().ToString(),
						hop.Split('_')[0] + " DTM / " + outputIntf + " SDI",
						edgeInput,
						outputNode,
						"Nimbra Edge",
						null,
						null,
						null,
						null,
						null,
						null,
		});

		connectionsTable.AddRow(new object[]
		{
						Guid.NewGuid().ToString(),
						edgeInput,
						edgeOutput,
						"Nimbra Edge",
						"Nimbra Edge",
						null,
						null,
						null,
						null,
						null,
						null,
		});
	}

	private void CreateEdgeConnection(string edgeOutput)
	{
		try
		{
			var subscriptEdge = _engine.PrepareSubScript("NimbraEdge-ConnectInput");
			subscriptEdge.SelectScriptParam("Input", edgeInput);
			subscriptEdge.SelectScriptParam("Output", edgeOutput);
			subscriptEdge.SelectDummy("dummy1", nimbraEdgeElement.AgentId, nimbraEdgeElement.DmsElementId.ElementId);
			subscriptEdge.StartScript();
		}
		catch (Exception e)
		{
			_engine.ShowUI($"Coulnd't connect input to output on Nimbra Edge. Exception: {e}");
		}
	}

	private void DeleteEdgeConnection(string edgeOutput)
	{
		try
		{
			var subscriptEdge = _engine.PrepareSubScript("NimbraEdge-ConnectInput");
			subscriptEdge.SelectScriptParam("Input", edgeDisconnectInputStr);
			subscriptEdge.SelectScriptParam("Output", edgeOutput);
			subscriptEdge.SelectDummy("dummy1", nimbraEdgeElement.AgentId, nimbraEdgeElement.DmsElementId.ElementId);
			subscriptEdge.PerformChecks = false;
			subscriptEdge.StartScript();
		}
		catch (Exception e)
		{
			_engine.ShowUI($"Coulnd't disconnect input from output on Nimbra Edge. Exception: {e}");
		}
	}

	private void DeleteVisionCircuit(string visionInput)
	{
		try
		{
			var subscriptDelete = _engine.PrepareSubScript("CircuitDeletion");
			subscriptDelete.SelectScriptParam("Interface Name", visionInput);
			subscriptDelete.StartScript();
		}
		catch (Exception e)
		{
			_engine.ShowUI($"Failed to remove circuit from Nimbra Vision. Exception: {e}");
		}
	}
}