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

30/05/2025	1.0.0.1		SDT, Skyline	Initial version
****************************************************************************
*/

// Ignore Spelling: Pids
namespace CircuitEdit_1
{
	using System;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Automation;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp;
	using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp.Messages;

	public enum Pids
	{
		CircuitsTable = 1800,
	}

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{
		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public static void Run(IEngine engine)
		{
			try
			{
				RunSafe(engine);
			}
			catch (ScriptAbortException)
			{
				// Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
				throw; // Comment if it should be treated as a normal exit of the script.
			}
			catch (ScriptForceAbortException)
			{
				// Catch forced abort exceptions, caused via external maintenance messages.
				throw;
			}
			catch (ScriptTimeoutException)
			{
				// Catch timeout exceptions for when a script has been running for too long.
				throw;
			}
			catch (InteractiveUserDetachedException)
			{
				// Catch a user detaching from the interactive script by closing the window.
				// Only applicable for interactive scripts, can be removed for non-interactive scripts.
				throw;
			}
			catch (Exception e)
			{
				engine.ExitFail("Run|Something went wrong: " + e);
			}
		}

		private static void RunSafe(IEngine engine)
		{
			var element = ValidateAndReturnElement(engine);
			var circuitId = ParseParamValue(engine.GetScriptParam("Circuit ID").Value);
			var action = ParseParamValue(engine.GetScriptParam("Action").Value);

			switch (action.ToLower())
			{
				case "stop":
					StopCircuit(engine, element, circuitId);
					break;

				case "delete":
					DeleteCirtcuit(engine, element, circuitId);
					break;

				default:
					engine.ExitFail($"Action '{action}' is not supported. Supported actions are: 'stop', 'delete'.");
					break;
			}
		}

		private static void DeleteCirtcuit(IEngine engine, Element element, string circuitId)
		{
			var dms = engine.GetDms();
			var idmsElement = dms.GetElement(element.ElementName);
			var circuitsTable = idmsElement.GetTable((int)Pids.CircuitsTable);
			var row = circuitsTable.GetRow(circuitId);

			if (row == null || row.Length < 2)
			{
				engine.ExitFail($"Circuit ID '{circuitId}' does not exist in the element.");
			}

			INimbraVisionInterAppCalls nimbraVisionInterAppCalls = new NimbraVisionInterAppCalls(engine.GetUserConnection(), element.DmaId, element.ElementId);
			DeleteCircuitRequest circuitDeleteMessage = new DeleteCircuitRequest { SharedId = Convert.ToString(row[1]) };
			var response = nimbraVisionInterAppCalls.SendSingleResponseMessage(circuitDeleteMessage);

			if (response.Success)
			{
				engine.ExitSuccess("Circuit deleted");
			}
			else
			{
				engine.ExitFail($"Failed to delete circuit: {response.Message}");
			}
		}

		private static void StopCircuit(IEngine engine, Element element, string circuitId)
		{
			INimbraVisionInterAppCalls nimbraVisionInterAppCalls = new NimbraVisionInterAppCalls(engine.GetUserConnection(), element.DmaId, element.ElementId);
			var response = nimbraVisionInterAppCalls.SendSingleResponseMessage(new EditCircuitRequest { CircuitId = circuitId, EndTime = DateTime.Now.AddMinutes(1) });
			if (response.Success)
			{
				engine.ExitSuccess("Circuit stopped");
			}
			else
			{
				engine.ExitFail($"Failed to stop circuit: {response.Message}");
			}
		}

		private static string ParseParamValue(string paramValueRaw)
		{
			return paramValueRaw.Trim('[', '\"');
		}

		private static Element ValidateAndReturnElement(IEngine engine)
		{
			var paramValueRaw = engine.GetScriptParam("ElementName").Value;
			var elementName = ParseParamValue(paramValueRaw);
			var element = engine.FindElement(elementName);

			if (element == null)
			{
				engine.ExitFail("Element Nimbra Vision does not exist!");
				return null;
			}

			if (element.ElementInfo.State != Skyline.DataMiner.Net.Messages.ElementState.Active)
			{
				engine.ExitFail("Element Nimbra Vision is not in Active state");
				return null;
			}

			return element;
		}
	}
}