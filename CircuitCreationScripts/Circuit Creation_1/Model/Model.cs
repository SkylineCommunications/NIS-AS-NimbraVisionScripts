namespace Skyline.Automation.CircuitCreation.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Library.Automation;
	using Skyline.DataMiner.Library.Common;
	using Skyline.DataMiner.Net.Helper;

	public class Model
	{
		public Model(Engine engine)
		{
			var dms = engine.GetDms() ?? throw new NullReferenceException("dms");
			NimbraVisionElement = dms.GetElements().FirstOrDefault(
				elem => elem.Protocol.Name == "NetInsight Nimbra Vision" && elem.Protocol.Version == "Production") ?? throw new NullReferenceException("Nimbra Vision");

			Interfaces = LoadInterfacesFromElement(NimbraVisionElement);
		}

		public string Source { get; set; }

		public string Destination { get; set; }

		public int Capacity { get; set; }

		public DateTime StartTime { get; set; }

		public DateTime StopTime { get; set; }

		public List<Interface> Interfaces { get; }

		public IDmsElement NimbraVisionElement { get; }

		private List<Interface> LoadInterfacesFromElement(IDmsElement nimbraVisionElement)
		{
			List<Interface> interfaces = new List<Interface>();
			var etsIntfTable = nimbraVisionElement.GetTable((int)Utils.Pids.EtsInterfaceTable);
			var itsIntfTable = nimbraVisionElement.GetTable((int)Utils.Pids.ItsInterfaceTable);
			var circuitsTable = nimbraVisionElement.GetTable((int)Utils.Pids.CircuitTable);


			var circuitRows = circuitsTable.GetRows();
			HashSet<string> j2kInterfacesInUse = new HashSet<string>();
			foreach (var row in from row in circuitRows
								where Convert.ToString(row[(int)Utils.Idx.CircuitServiceId]).Contains("j2k")
								select row)
			{
				j2kInterfacesInUse.Add(Convert.ToString(row[(int)Utils.Idx.CircuitSourceIntf]));
				j2kInterfacesInUse.Add(Convert.ToString(row[(int)Utils.Idx.CircuitDestIntf]));
			}

			var etsRows = etsIntfTable.GetRows();
			foreach (var etsRow in etsRows)
			{
				interfaces.Add(new Interface
				{
					Capabilities = "Ethernet",
					CircuitCreationInterfaceName = Convert.ToString(etsRow[(int)Utils.Idx.EtsInterfaceCircuitNaming]),
					InterfaceName = Convert.ToString(etsRow[0]),
					NodeName = Convert.ToString(etsRow[(int)Utils.Idx.EtsInterfaceNodeName]),
				});
			}

			var itsRows = itsIntfTable.GetRows();
			string capabilities;

			foreach (var itsRow in itsRows)
			{
				capabilities = Convert.ToString(itsRow[(int)Utils.Idx.ItsInterfaceCapabilities]);
				if (capabilities.IsNullOrEmpty())
					continue;

				var circuitCreationInterfaceName = Utils.GetCircuitNamedItsInterface(Convert.ToString(itsRow[0]));

				if (j2kInterfacesInUse.Contains(circuitCreationInterfaceName))
					continue;

				interfaces.Add(new Interface
				{
					Capabilities = capabilities,
					CircuitCreationInterfaceName = circuitCreationInterfaceName,
					InterfaceName = Convert.ToString(itsRow[0]),
					NodeName = Convert.ToString(itsRow[(int)Utils.Idx.ItsInterfaceNodeName]),
				});
			}

			return interfaces;
		}
	}

	public class Interface
	{
		public string InterfaceName { get; set; }

		public string NodeName { get; set; }

		public string Capabilities { get; set; }

		public string CircuitCreationInterfaceName { get; set; }
	}
}
