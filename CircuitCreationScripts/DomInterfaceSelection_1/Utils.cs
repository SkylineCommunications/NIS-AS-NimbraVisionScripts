namespace Skyline.Automation.CircuitCreation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.CommunityLibrary.FlowProvisioning.Info;
	using Skyline.DataMiner.Library.Common.InterAppCalls.CallSingle;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;

	public static class Utils
	{
		public static readonly int NumberOfRetries = 60;

		public static readonly int SleepTime = 1000;

		public static readonly List<Type> KnownTypes = new List<Type> { typeof(FlowInfoMessage), typeof(DeleteCircuitMessage) };

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
		}

		public static bool ValidateArguments(DomInstanceId domInstanceId, string scriptParamValue)
		{
			if (domInstanceId == null)
			{
				return false;
			}

			if (string.IsNullOrEmpty(scriptParamValue))
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
	}

	public class Settings
	{
		private readonly List<string> supportedCircuitTypes = new List<string>
		{
			"E-Line",
			"E-Line VLAN",
			"JPEG 2000",
			"JPEG 2000 1+1 Hitless",
		};

		private readonly int labelWidth = 250;
		private readonly int componentWidth = 150;
		private readonly int buttonHeight = 25;

		public int LabelWidth => labelWidth;

		public int ComponentWidth => componentWidth;

		public int ButtonHeight => buttonHeight;

		public List<string> SupportedCircuitTypes => supportedCircuitTypes;
	}

	public class DeleteCircuitMessage : Message
	{
		public string SharedId { get; set; }
	}
}