namespace Skyline.Automation.CircuitCreation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public static class Utils
	{
		public enum Pids
		{
			ItsInterfaceTable = 1600,
			ItsInterfaceCapabilities = 1603,
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
}