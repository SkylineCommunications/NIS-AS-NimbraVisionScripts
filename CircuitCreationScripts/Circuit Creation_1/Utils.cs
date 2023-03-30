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

		public enum Pids
		{
			ItsInterfaceTable = 1600,
			ItsInterfaceCapabilities = 1603,
			EtsInterfaceTable = 1900,
			EtsInterfaceCircuitNaming = 1924,
			EtsInterfaceNodeName = 1922,
			CircuitsTable = 1800,
		}

		public enum Idx
		{
			ItsInterfaceNodeName = 1,
			ItsInterfaceCapabilities = 2,
			ItsInterfaceName = 4,
			EtsInterfaceNodeName = 21,
			EtsInterfaceCircuitNaming = 23,
			CircuitSource = 8,
			CircuitDestination = 9,
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

		private readonly int labelWidth = 160;
		private readonly int componentWidth = 150;
		private readonly int buttonHeight = 25;

		public int LabelWidth => labelWidth;

		public int ComponentWidth => componentWidth;

		public int ButtonHeight => buttonHeight;

		public List<string> SupportedCircuitTypes => supportedCircuitTypes;
	}
}
