namespace Skyline.Automation.CircuitCreation.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Helper;

	public class Model
	{
		public Model(IEngine engine, DomInstance domInstance, DomHelper domHelper, string transitionId, string nimbraVisionElementName)
		{
			var dms = engine.GetDms() ?? throw new NullReferenceException("dms");
			NimbraVisionElement = dms.GetElement(nimbraVisionElementName);

			Interfaces = LoadInterfacesFromElement(engine, NimbraVisionElement);

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

		private List<Interface> LoadInterfacesFromElement(IEngine engine, IDmsElement nimbraVisionElement)
		{
			List<Interface> interfaces = new List<Interface>();
			var etsIntfTable = nimbraVisionElement.GetTable((int)Utils.Pids.EtsInterfaceTable);
			var itsIntfTable = nimbraVisionElement.GetTable((int)Utils.Pids.ItsInterfaceTable);
			var circuitsTable = nimbraVisionElement.GetTable((int)Utils.Pids.CircuitTable);
			var vaResourcesTable = nimbraVisionElement.GetTable((int)Utils.Pids.VAResourceTable);

			var circuitRows = circuitsTable.GetRows();
			HashSet<string> j2kInterfacesInUse = new HashSet<string>();
			foreach (var row in from row in circuitRows
								where Convert.ToString(row[(int)Utils.Idx.CircuitServiceId]).Contains("j2k")
								select row)
			{
				j2kInterfacesInUse.Add(Convert.ToString(row[(int)Utils.Idx.CircuitSourceIntf]));
				j2kInterfacesInUse.Add(Convert.ToString(row[(int)Utils.Idx.CircuitDestIntf]));
			}

			HashSet<string> jxsInterfacesInUse = new HashSet<string>();
			foreach (var row in from row in circuitRows
								where Convert.ToString(row[(int)Utils.Idx.CircuitServiceId]).Contains("jxs")
								select row)
			{
				jxsInterfacesInUse.Add(Convert.ToString(row[(int)Utils.Idx.CircuitSourceIntf]));
				jxsInterfacesInUse.Add(Convert.ToString(row[(int)Utils.Idx.CircuitDestIntf]));
			}

			HashSet<string> srtInterfacesInUse = new HashSet<string>();
			foreach (var row in from row in circuitRows
								where Convert.ToString(row[(int)Utils.Idx.CircuitServiceId]).Contains("VA-SRT")
								select row)
			{
				srtInterfacesInUse.Add(Convert.ToString(row[(int)Utils.Idx.CircuitSourceIntf]));
				srtInterfacesInUse.Add(Convert.ToString(row[(int)Utils.Idx.CircuitDestIntf]));
			}

			// ETS
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

			// ITS
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

				if (jxsInterfacesInUse.Contains(circuitCreationInterfaceName))
					continue;

				interfaces.Add(new Interface
				{
					Capabilities = capabilities,
					CircuitCreationInterfaceName = circuitCreationInterfaceName,
					InterfaceName = Convert.ToString(itsRow[0]),
					NodeName = Convert.ToString(itsRow[(int)Utils.Idx.ItsInterfaceNodeName]),
				});
			}

			// VA
			var vaRows = vaResourcesTable.GetRows();
			foreach (var vaRow in vaRows)
			{
				var typeVA = Convert.ToString(vaRow[(int)Utils.Idx.VAInterfaceType]);
				if (typeVA != "0")
					continue;

				var modeVA = Convert.ToString(vaRow[(int)Utils.Idx.VAInterfaceMode]);
				if (modeVA != "6" && modeVA != "7")
					continue;

				var circuitCreationInterfaceNameVA = Convert.ToString(vaRow[(int)Utils.Idx.VAInterfaceCircuitNaming]);
				if (srtInterfacesInUse.Contains(circuitCreationInterfaceNameVA))
					continue;

				interfaces.Add(new Interface
				{
					// In this case capabilities are not used. We use Mode on VA Resource Table instead.
					Capabilities = modeVA,
					CircuitCreationInterfaceName = circuitCreationInterfaceNameVA,
					InterfaceName = Convert.ToString(vaRow[0]),
					NodeName = Convert.ToString(vaRow[(int)Utils.Idx.VAInterfaceNodeName]),
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