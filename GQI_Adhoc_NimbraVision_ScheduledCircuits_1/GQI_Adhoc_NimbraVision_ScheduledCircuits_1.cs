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

	using Skyline.DataMiner.Analytics.GenericInterface;

	[GQIMetaData(Name = "GQI Nimbra Vision Circuits")]
	public class GetElementsByServiceName : IGQIDataSource, IGQIOnInit, IGQIInputArguments
	{
		private static readonly object _lock = new object();
		private readonly GQIStringArgument _elementArgument = new GQIStringArgument("Element Name") { IsRequired = true };
		private readonly GQIDateTimeArgument _startArgument = new GQIDateTimeArgument("Start Time") { IsRequired = true };
		private readonly GQIDateTimeArgument _stopArgument = new GQIDateTimeArgument("End Time") { IsRequired = true };
		private GQIDMS _dms;
		private string _elementName;
		private IGQILogger _logger;
		private DateTime _startTime;
		private DateTime _stopTime;

		/// <summary>
		/// Columns for the data source.
		/// </summary>
		/// <returns></returns>
		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("ID"),
				new GQIStringColumn("Type"),
				new GQIStringColumn("State"),
				new GQIDateTimeColumn("Start"),
				new GQIDateTimeColumn("End"),
				new GQIIntColumn("Capacity"),
				new GQIStringColumn("Source Node"),
				new GQIStringColumn("Source Interface"),
				new GQIStringColumn("Destination Node"),
				new GQIStringColumn("Destination Interface"),
				new GQIStringColumn("Status Description"),
			};
		}

		/// <summary>
		/// Input arguments for the data source.
		/// </summary>
		/// <returns></returns>
		public GQIArgument[] GetInputArguments() => new GQIArgument[] { _elementArgument, _startArgument, _stopArgument };

		/// <summary>
		/// Define the data source.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			_logger.Information("Fetching next page");
			List<GQIRow> rows = new List<GQIRow>();

			lock (_lock)
			{
				try
				{
					var circuits = Circuits.Instance(_dms, _logger, _elementName);

					foreach (var circuit in circuits.CircuitsTable)
					{
						if (circuit.Start < _stopTime && (circuit.Start >= _startTime || circuit.End > _startTime))
						{
							rows.Add(new GQIRow(new[]
							{
								new GQICell { Value = circuit.Id}, // IO ID SRC
								new GQICell { Value = circuit.Type}, // IO Name SRC
								new GQICell { Value = circuit.State}, // IO SRC
								new GQICell { Value = circuit.Start}, // IO State SRC
								new GQICell { Value = circuit.End, DisplayValue = circuit.End.ToOADate() >= 2593223 ? "Undefined" : Convert.ToString(circuit.End) }, // IO Type SRC
								new GQICell { Value = circuit.Capacity, DisplayValue = $"{circuit.Capacity} Mpbs"}, // Bitrate SRC
								new GQICell { Value = circuit.SourceNode}, // Stream Name SRC
								new GQICell { Value = circuit.SourceInterface}, // IO ID SRC
								new GQICell { Value = circuit.DestinationNode}, // Edge Name SRC
								new GQICell { Value = circuit.DestinationInterface}, // IO Name SRC
								new GQICell { Value = circuit.StatusDescription}, // IO State SRC
							}));
						}
					}
				}
				catch (Exception ex)
				{
					_logger.Information($"GetNextPage|Exception: {ex}");
					rows = new List<GQIRow>(0);
				}
			}

			return new GQIPage(rows.ToArray())
			{
				HasNextPage = false,
			};
		}

		/// <summary>
		/// Process the input arguments.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			_elementName = args.GetArgumentValue(_elementArgument);
			_startTime = args.GetArgumentValue(_startArgument);
			_stopTime = args.GetArgumentValue(_stopArgument);
			return default;
		}

		/// <summary>
		/// Initialize the data source.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			_logger = args.Logger;
			_dms = args.DMS;
			return default;
		}
	}
}