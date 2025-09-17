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
30/05/2025	1.0.0.2		SDT, Skyline	Added support for Nimbra Vision InterApp.
****************************************************************************
*/

namespace Scheduler_DOM_CRUD_1
{
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;
	using Skyline.Automation.CircuitCreation;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp;
	using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp.Messages;

	using static Skyline.Automation.CircuitCreation.Utils;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		public const string NimbraVisionElementName = "NetInsight Nimbra Vision";

		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
		{
			var id = new Guid(Regex.Replace(engine.GetScriptParam("DomInstanceId").Value, @"[\[\]]", String.Empty).Split(',')[0].Replace("\"", String.Empty));
			var domHelper = new DomHelper(engine.SendSLNetMessages, "(netinsight)circuitjobs");
			var domInstances = domHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(id));
			domHelper.StitchDomInstances(domInstances);
			var domInstance = domInstances.First();

			engine.GenerateInformation("Status ID : " + domInstance.StatusId);

			if (domInstance.StatusId != "ongoing" && domInstance.StatusId != "confirmed")
			{
				domHelper.DomInstances.Delete(domInstance);
				return;
			}

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
			else if (circuitType == CircuitType.SdiSrt)
			{
				translatedDestinationIntf = GetCircuitNamedVAInterface(Convert.ToString(destinationInterface));
				translatedSourceIntf = GetCircuitNamedVAInterface(Convert.ToString(sourceInterface));
			}
			else
			{
				translatedDestinationIntf = GetCircuitNamedItsInterface(Convert.ToString(destinationInterface));
				translatedSourceIntf = GetCircuitNamedItsInterface(Convert.ToString(sourceInterface));
			}

			var dms = engine.GetDms() ?? throw new NullReferenceException("dms");
			var nimbraElement = dms.GetElement(NimbraVisionElementName);

			var circuitTable = nimbraElement.GetTable((int)Pids.CircuitTable);

			var rowToDelete = circuitTable.GetData()
								.FirstOrDefault(kv => Convert.ToString(kv.Value[(int)Idx.CircuitSourceIntf]) == translatedSourceIntf && Convert.ToString(kv.Value[(int)Idx.CircuitDestIntf]) == translatedDestinationIntf);

			INimbraVisionInterAppCalls nimbraVisionInterApp = new NimbraVisionInterAppCalls(engine.GetUserConnection(), nimbraElement.DmsElementId.AgentId, nimbraElement.DmsElementId.ElementId);
			DeleteCircuitRequest circuitDeleteMessage = new DeleteCircuitRequest { SharedId = Convert.ToString(rowToDelete.Value[(int)Utils.Idx.CircuitsSharedId]) };
			nimbraVisionInterApp.SendMessageNoResponse(circuitDeleteMessage);
			domHelper.DomInstances.Delete(domInstance);
		}
	}
}

namespace Skyline.Automation.CircuitCreation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;

	public static class Utils
	{
		public static readonly int NumberOfRetries = 60;

		public static readonly int SleepTime = 1000;

		public enum InterfaceType
		{
			Source = 0,
			Destination = 1,
		}

		public enum Pids
		{
			ItsInterfaceTable = 1600,
			ItsInterfaceCapabilities = 1603,
			CircuitTable = 1800,
			EtsInterfaceTable = 1900,
			EtsInterfaceCircuitNaming = 1924,
			EtsInterfaceNodeName = 1922,
			VAResourceTable = 2200,
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
			CircuitsSharedId = 1,
		}

		public enum CircuitType
		{
			Eline = 0,
			J2k = 1,
			J2kHitless = 2,
			Jxs = 3,
			JxsHitless = 4,
			SdiSrt = 5,
		}

		public static bool ValidateArguments(DomInstanceId domInstanceId, string scriptParamValue)
		{
			if (domInstanceId == null)
			{
				return false;
			}

			if (String.IsNullOrEmpty(scriptParamValue))
			{
				return false;
			}

			return true;
		}

		public static DomInstance GetDomInstance(DomHelper domHelper, DomInstanceId instanceId)
		{
			FilterElement<DomInstance> domInstanceFilter = DomInstanceExposers.Id.Equal(instanceId);

			List<DomInstance> domInstances = domHelper.DomInstances.Read(domInstanceFilter);
			domHelper.StitchDomInstances(domInstances);

			return domInstances.First();
		}

		public static object GetFieldValue(DomInstance domInstance, string fieldName)
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

		public static FieldDescriptorID GetFiledId(DomInstance domInstance, string fieldName)
		{
			foreach (var section in domInstance.Sections)
			{
				foreach (var fieldValue in section.FieldValues)
				{
					var fieldDescriptor = fieldValue.GetFieldDescriptor();
					if (fieldDescriptor.Name.Equals(fieldName))
					{
						return fieldValue.FieldDescriptorID;
					}
				}
			}

			return null;
		}

		public static string GetCircuitNamedEtsInterface(string interfaceName)
		{
			var splittedInterfaceName = interfaceName.Split('_');
			var node = splittedInterfaceName[0];
			var interfaceNumbering = splittedInterfaceName[1].Replace("eth", String.Empty);
			return String.Join("_", interfaceNumbering, node);
		}

		public static string GetCircuitNamedItsInterface(string interfaceName)
		{
			var splittedInterfaceName = interfaceName.Split('_');
			var node = splittedInterfaceName[0];
			var interfaceNumbering = splittedInterfaceName[1].Split('-')[1];
			return String.Join("_", interfaceNumbering, node);
		}

		public static string GetCircuitNamedVAInterface(string interfaceName)
		{
			var splittedInterfaceName = interfaceName.Split('_');
			var node = splittedInterfaceName[0];
			var interfaceNumbering = splittedInterfaceName[1].Replace("av", String.Empty);
			return String.Join("_", interfaceNumbering, node);
		}
	}

	public class Settings
	{
		private readonly List<string> supportedCircuitTypes = new List<string>
		{
			"E-Line",
			"E-Line VLAN",
			"JPEG 2000",
			"JPEG 2000 1+1 Hitless",
			"JPEG-XS",
			"JPEG-XS 1+1 Hitless",
			"SDI SRT",
		};

		private readonly int labelWidth = 250;
		private readonly int componentWidth = 150;
		private readonly int buttonHeight = 25;

		public int LabelWidth => labelWidth;

		public int ComponentWidth => componentWidth;

		public int ButtonHeight => buttonHeight;

		public List<string> SupportedCircuitTypes => supportedCircuitTypes;
	}
}