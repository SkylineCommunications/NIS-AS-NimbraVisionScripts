using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skyline.Automation.CircuitCreation
{
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
	}

	public class Settings
	{
		public readonly List<string> SupportedCircuitTypes = new List<string>
		{
			"E-Line",
			"E-Line VLAN",
			"JPEG 2000",
			"JPEG 2000 1+1 Hitless",
		};
	}
}
