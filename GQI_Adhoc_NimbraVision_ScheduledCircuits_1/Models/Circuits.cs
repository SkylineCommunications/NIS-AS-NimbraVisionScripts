/*
****************************************************************************
*  Copyright (c) 2025,  Skyline Communications NV  All Rights Reserved.    *
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

dd/mm/2025	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

namespace GQI_Adhoc_NimbraVision_ScheduledCircuits_1
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	internal sealed class Circuits
	{
		private const double _cachingTime = 30;
		private static readonly object _padlock = new object();
		private static Dictionary<string, ElementCache> _instances = new Dictionary<string, ElementCache>();
		private readonly GQIDMS _dms;

		private Circuits(GQIDMS dms, IGQILogger logger, string elementName)
		{
			_dms = dms;

			var responseElement = dms.SendMessage(new GetElementByNameMessage(elementName)) as ElementInfoEventMessage;

			CircuitsTable = GetCircuitsTable(responseElement, logger);

			if (!CircuitsTable.Any())
			{
				throw new ArgumentException("Circuits table empty or not found!");
			}
		}

		public IEnumerable<CircuitsTable> CircuitsTable { get; }

		public static Circuits Instance(GQIDMS dms, IGQILogger logger, string elementName)
		{
			var now = DateTime.UtcNow;
			ElementCache instance;
			logger.Debug($"Element: {elementName}");

			lock (_padlock)
			{
				if (!_instances.TryGetValue(elementName, out instance) ||
					(now - instance.LastRun) > TimeSpan.FromSeconds(_cachingTime))
				{
					instance = new ElementCache(new Circuits(dms, logger, elementName), now);
					_instances[elementName] = instance;
				}

				return instance.Instance;
			}
		}

		private IEnumerable<CircuitsTable> GetCircuitsTable(ElementInfoEventMessage responseElement, IGQILogger logger)
		{
			string ExtractNodeName(string iface)
			{
				var values = iface.Split('_');
				if (values.Length < 2)
				{
					return iface;
				}

				return values[1];
			}

			string ExtractStatus(string description)
			{
				int startIndex = description.IndexOf('(') + 1;
				int endIndex = description.IndexOf(')');

				if (startIndex < 1 ||
					endIndex < startIndex)
				{
					return description;
				}

				var result = description.Substring(startIndex, endIndex - startIndex);

				return $"{result.Substring(0, 1).ToUpper()}{result.Substring(1)}";
			}

			var responseEdgesTable = _dms.SendMessage(new GetPartialTableMessage
			{
				DataMinerID = responseElement.DataMinerID,
				ElementID = responseElement.ElementID,
				ParameterID = 1800, // circuits,
				Filters = new[] { "forceFullTable=true" /*, "column=xx,yy"*/ },
			}) as ParameterChangeEventMessage;

			if (!responseEdgesTable.NewValue.IsArray)
			{
				return Enumerable.Empty<CircuitsTable>();
			}

			var table = new List<CircuitsTable>();

			var cols = responseEdgesTable.NewValue.ArrayValue[0].ArrayValue;
			for (int idxRow = 0; idxRow < cols.Length; idxRow++)
			{
				try
				{
					// logger.Information($"Start: {DateTime.FromOADate(Convert.ToDouble(responseEdgesTable.NewValue.GetTableCell(idxRow, 4)?.CellValue.GetAsStringValue()))}");
					// logger.Information($"End: {DateTime.FromOADate(Convert.ToDouble(responseEdgesTable.NewValue.GetTableCell(idxRow, 5)?.CellValue.GetAsStringValue()))}");
					logger.Information($"Capacity: {responseEdgesTable.NewValue.GetTableCell(idxRow, 10)?.CellValue.GetAsStringValue()}");

					var srcIface = responseEdgesTable.NewValue.GetTableCell(idxRow, 8)?.CellValue.GetAsStringValue();
					var dstIface = responseEdgesTable.NewValue.GetTableCell(idxRow, 9)?.CellValue.GetAsStringValue();
					var statusDescription = responseEdgesTable.NewValue.GetTableCell(idxRow, 7)?.CellValue.GetAsStringValue();

					// start of row 'idxRow'
					table.Add(new CircuitsTable
					{
						Id = responseEdgesTable.NewValue.GetTableCell(idxRow, 0)?.CellValue.GetAsStringValue(),
						Type = responseEdgesTable.NewValue.GetTableCell(idxRow, 2)?.CellValue.GetAsStringValue(),
						State = ExtractStatus(statusDescription),
						Start = DateTime.FromOADate(Convert.ToDouble(responseEdgesTable.NewValue.GetTableCell(idxRow, 4)?.CellValue.GetAsStringValue())).ToUniversalTime(),
						End = DateTime.FromOADate(Convert.ToDouble(responseEdgesTable.NewValue.GetTableCell(idxRow, 5)?.CellValue.GetAsStringValue())).ToUniversalTime(),
						Capacity = Convert.ToInt32(responseEdgesTable.NewValue.GetTableCell(idxRow, 10)?.CellValue.GetAsStringValue()),
						SourceNode = ExtractNodeName(srcIface),
						SourceInterface = srcIface,
						DestinationNode = ExtractNodeName(dstIface),
						DestinationInterface = dstIface,
						StatusDescription = statusDescription,
					});
				}
				catch (Exception ex)
				{
					logger.Error($"GetCircuitsTable|Exception: {ex}");
				}
			}

			return table;
		}
	}
}