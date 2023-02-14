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
using System.Threading;
using Newtonsoft.Json;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Library.Automation;
using Skyline.DataMiner.Library.Common;
using Skyline.DataMiner.Net.Helper;
using Skyline.DataMiner.Net.Serialization;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private string sourceBasic = String.Empty;
	private string destinationBasic = String.Empty;
	private string sourceVlan = String.Empty;
	private string destinationVlan = String.Empty;
	private List<EtsInterfaceData> etsInterfaceData = new List<EtsInterfaceData>();
	private List<CircuitTableData> circuitData = new List<CircuitTableData>();

	public enum TableIds
	{
		ETS = 1900,
		Circuits = 1800,
	}

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		var nimbraElementName = engine.GetScriptParam("Element Name").Value;

		var dms = engine.GetDms();
		var nimbraElement = dms.GetElement(nimbraElementName);
		if (nimbraElement == null || nimbraElement.State != Skyline.DataMiner.Library.Common.ElementState.Active)
		{
			engine.GenerateInformation("Nimbra element is null or not active.");
			engine.ExitFail("Nimbra element is null or not active.");
		}

		if (!LoadTableData(nimbraElement))
			engine.ExitFail("Couldn't load element data.");

		foreach (var etsInterface in etsInterfaceData)
		{
			if (!circuitData.Any(a => a.Source == etsInterface.InterfaceName) || circuitData.Any(a => a.Destination == etsInterface.InterfaceName))
			{
				if (SaveInterface(etsInterface.InterfaceName))
					break;
			}
		}

		// CreateBasicCircuit
		CreateBasicCircuit(engine);

		// CreateVlanCircuit
		CreateVlanCircuit(engine);

		int retries = 3;
		bool basicCircuitCreated = false;
		bool vlanCircuitCreated = false;
		for (int i = 0; i < retries; i++)
		{
			Thread.Sleep(10000);
			var circuitsTable = nimbraElement.GetTable(1800);
			var circuits = circuitsTable.GetRows();
			if (!basicCircuitCreated)
			{
				basicCircuitCreated = CheckBasicCircuitWasCreated(engine, circuits, source, destination);
			}

			if (!vlanCircuitCreated)
			{
				vlanCircuitCreated = CheckVlanCircuitWasCreated(engine, circuits, source, destination);
			}

			if (basicCircuitCreated && vlanCircuitCreated)
				break;
		}

		if (!basicCircuitCreated)
		{
			engine.GenerateInformation("Basic Circuit wasn't successfully created.");
		}

		if (!vlanCircuitCreated)
		{
			engine.GenerateInformation("VLAN Circuit wasn't successfully created.");
		}
	}

	private static void CreateVlanCircuit(Engine engine)
	{
		var createVlanCircuit = engine.PrepareSubScript("NimbraVisionVlanCircuitCreation");
		createVlanCircuit.SelectScriptParam("Source", source);
		createVlanCircuit.SelectScriptParam("Destination", destination);
		createVlanCircuit.SelectScriptParam("Capacity", "5");
		createVlanCircuit.SelectScriptParam("Service ID", "E-Line");
		createVlanCircuit.SelectScriptParam("Start Time", "-1");
		createVlanCircuit.SelectScriptParam("End Time", "-1");
		createVlanCircuit.SelectScriptParam("VLAN", "100");
		createVlanCircuit.SelectScriptParam("Form Name", "EVP-Line");

		createVlanCircuit.StartScript();
	}

	private bool SaveInterface(string interfaceName)
	{
		if (sourceBasic == String.Empty)
		{
			sourceBasic = interfaceName;
			return false;
		}

		if (destinationBasic == String.Empty)
		{
			destinationBasic = interfaceName;
			return false;
		}

		if (sourceVlan == String.Empty)
		{
			sourceVlan = interfaceName;
			return false;
		}

		if(destinationVlan == String.Empty)
		{
			destinationVlan = interfaceName;
			return true;
		}

		return true;
	}

	private void CreateBasicCircuit(Engine engine)
	{
		var createBasicCircuit = engine.PrepareSubScript("NimbraVisionBasicCircuitCreation");
		createBasicCircuit.SelectScriptParam("Source", sourceBasic);
		createBasicCircuit.SelectScriptParam("Destination", destinationBasic);
		createBasicCircuit.SelectScriptParam("Capacity", "5");
		createBasicCircuit.SelectScriptParam("Service ID", "E-Line");
		createBasicCircuit.SelectScriptParam("Start Time", "-1");
		createBasicCircuit.SelectScriptParam("End Time", "-1");

		engine.GenerateInformation("Script Data: " + JsonConvert.SerializeObject(createBasicCircuit));

		createBasicCircuit.StartScript();
	}

	private bool LoadTableData(IDmsElement nimbraElement)
	{
		try
		{
			var etsInterfacesTable = nimbraElement.GetTable(1900);
			var etsInterfacesRows = etsInterfacesTable.GetRows();

			foreach (var intfRow in etsInterfacesRows)
			{
				if (!Convert.ToString(intfRow[1]).IsNullOrEmpty())
				{
					etsInterfaceData.Add(new EtsInterfaceData
					{
						TableKey = Convert.ToString(intfRow[0]),
						FowardingFunction = Convert.ToString(intfRow[1]),
						InterfaceName = Convert.ToString(intfRow[23]),
					});
				}
			}

			var circuitsTable = nimbraElement.GetTable(1800);
			var circuitsRows = circuitsTable.GetRows();

			foreach (var row in circuitsRows)
			{
				circuitData.Add(new CircuitTableData
				{
					Source = Convert.ToString(row[8]),
					Destination = Convert.ToString(row[9]),
				});
			}

			return true;
		}
		catch
		{
			return false;
		}
	}

	private bool CheckBasicCircuitWasCreated(Engine engine, object[][] circuits, string source, string destination)
	{
		foreach (object[] circuit in circuits)
		{
			if (Convert.ToString(circuit[2]) != "E-Line")
				continue;

			if (Convert.ToString(circuit[4]) != "-1")
				continue;

			if (Convert.ToString(circuit[5]) != "-1")
				continue;

			if (Convert.ToInt32(circuit[6]) > 399)
				continue;

			if (Convert.ToString(circuit[8]) != source)
				continue;

			if(Convert.ToString(circuit[9]) != destination)
				continue;

			if (Convert.ToString(circuit[10]) != "5")
				continue;

			return true;
		}

		return false;
	}

	private bool CheckVlanCircuitWasCreated(Engine engine, object[][] circuits, string source, string destination)
	{
		foreach (object[] circuit in circuits)
		{
			if (Convert.ToString(circuit[2]) != "E-Line")
				continue;

			if (Convert.ToString(circuit[4]) != "-1")
				continue;

			if (Convert.ToString(circuit[5]) != "-1")
				continue;

			if (Convert.ToInt32(circuit[6]) > 399)
				continue;

			if (Convert.ToString(circuit[8]) != source)
				continue;

			if (Convert.ToString(circuit[9]) != destination)
				continue;

			if (Convert.ToString(circuit[10]) != "5")
				continue;

			if (Convert.ToString(circuit[11]) != "EVP-Line")
				continue;

			if (Convert.ToString(circuit[13]) != "100")
				continue;

			return true;
		}

		return false;
	}
}

public class EtsInterfaceData
{
	public string InterfaceName { get; set; }

	public string TableKey { get; set; }

	public string FowardingFunction { get; set; }
}

public class CircuitTableData
{
	public string Source{ get; set; }

	public string Destination{ get; set; }
}