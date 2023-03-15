using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Library.Automation;
using Skyline.DataMiner.Library.Common;
using Skyline.DataMiner.Net.Helper;

namespace Skyline.Automation.CircuitCreation.Model
{
	public class Model
	{
		private readonly IDms dms;
		private readonly Settings _settings;

		public Model(Engine engine, Settings settings)
		{
			dms = engine.GetDms() ?? throw new ArgumentNullException("dms");
			_settings= settings;
			var nimbraVisionElement = dms.GetElements().Where(elem => elem.Protocol.Name == "NetInsight Nimbra Vision" && elem.Protocol.Version == "Production").FirstOrDefault() ?? throw new NullReferenceException("Nimbra Vision");

			Interfaces = LoadInterfacesFromElement(nimbraVisionElement);


		}

		public string Source { get; set; }

		public string Destination { get; set; }

		public int Capacity { get; set; }

		public DateTime StartTime { get; set; }

		public DateTime StopTime { get; set; }

		public List<Interface> Interfaces { get; }

		private List<Interface> LoadInterfacesFromElement(IDmsElement nimbraVisionElement)
		{
			List<Interface> interfaces = new List<Interface>();
			var etsIntfTable = nimbraVisionElement.GetTable((int)Utils.Pids.EtsInterfaceTable);
			var itsIntfTable = nimbraVisionElement.GetTable((int)Utils.Pids.ItsInterfaceTable);

			var etsRows = etsIntfTable.GetRows();
			foreach (var etsRow in etsRows)
			{
				interfaces.Add( new Interface
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
					CircuitCreationInterfaceName = String.Join("_", new[] { Convert.ToString(itsRow[0]).Split('-')[1], Convert.ToString(itsRow[(int)Utils.Idx.ItsInterfaceNodeName]) }),
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
