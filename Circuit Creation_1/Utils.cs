namespace Skyline.Automation.CircuitCreation
{
	using System;
	using System.Collections.Generic;

	public static class Utils
	{
		public enum InterfaceType
		{
			Source = 0,
			Destination = 1,
		}

		public enum VaResourceType
		{
			NA = -1,
			VaInterface = 0,
			VaEncoderPipe = 1,
			VaDecoderPipe = 2,
		}

		public enum Pids
		{
			ItsInterfaceTable = 1600,
			ItsInterfaceCapabilities = 1603,
			EtsInterfaceTable = 1900,
			EtsInterfaceCircuitNaming = 1924,
			EtsInterfaceNodeName = 1922,
			CircuitTable = 1800,
			VaResourcesTable = 2200,
			VaResourcesNodeName = 2203,
			VaResourcesCircuitNaming = 2217,
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
			VaResourcesNodeName = 2,
			VaResourcesType = 3,
			VaResourcesCircuitNaming = 16,
		}

		public static string GetCircuitNamedEtsInterface(string interfaceName)
		{
			var splittedInterfaceName = interfaceName.Split('_');
			var node = splittedInterfaceName[0];
			var interfaceNumbering = splittedInterfaceName[1].Replace("eth", String.Empty);
			return String.Join("_", interfaceNumbering, node );
		}

		public static string GetCircuitNamedItsInterface(string interfaceName)
		{
			var splittedInterfaceName = interfaceName.Split('_');
			var node = splittedInterfaceName[0];
			var interfaceNumbering = splittedInterfaceName[1].Split('-')[1];
			return String.Join("_", interfaceNumbering, node );
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

		private readonly Dictionary<string, string> supportedSrtModes = new Dictionary<string, string>
		{
			{"Caller -> Listener", "push" },
			{"Listener -> Caller", "pull" },
			{"Rendezvous -> Rendezvous", "rendezvous" },
		};

		private readonly int labelWidth = 160;
		private readonly int componentWidth = 150;
		private readonly int buttonHeight = 25;

		public int LabelWidth => labelWidth;

		public int ComponentWidth => componentWidth;

		public int ButtonHeight => buttonHeight;

		public List<string> SupportedCircuitTypes => supportedCircuitTypes;

		public Dictionary<string, string> SupportedSrtModes => supportedSrtModes;
	}
}
