namespace Skyline.Automation.CircuitCreation.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Library.Automation;
	using Skyline.DataMiner.Library.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;

	public class Model
	{
		public Model(IEngine engine, DomInstance domInstance, DomHelper domHelper, string transitionId)
		{
			var dms = engine.GetDms() ?? throw new NullReferenceException("dms");
			NimbraVisionElement = dms.GetElements().FirstOrDefault(
				elem => elem.Protocol.Name == "NetInsight Nimbra Vision" && elem.Protocol.Version == "Production") ?? throw new NullReferenceException("Nimbra Vision");

			Interfaces = LoadInterfacesFromElement(NimbraVisionElement);

			DomInstance = domInstance;
			DomHelper = domHelper;
			TransitionId = transitionId;
		}

		public DomInstance DomInstance { get; }

		public DomHelper DomHelper { get; }

		public string TransitionId { get; }

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

				interfaces.Add(new Interface
				{
					Capabilities = capabilities,
					CircuitCreationInterfaceName = String.Join("_", Convert.ToString(itsRow[0]).Split('-')[1], Convert.ToString(itsRow[(int)Utils.Idx.ItsInterfaceNodeName]) ),
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
