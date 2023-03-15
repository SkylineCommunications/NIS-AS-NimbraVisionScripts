/*
****************************************************************************
*  Copyright (c) 2023,  Skyline Communications NV  All Rights Reserved.    *
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

dd/mm/2023	1.0.0.1		XXX, Skyline	Initial version
****************************************************************************
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Skyline.Automation.CircuitCreation;
using Skyline.Automation.CircuitCreation.Model;
using Skyline.Automation.CircuitCreation.Presenter;
using Skyline.Automation.CircuitCreation.View;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.DeveloperCommunityLibrary.InteractiveAutomationToolkit;
using Skyline.DataMiner.Net.ReportsAndDashboards;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
	private Settings Settings { get; set; }
	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(Engine engine)
	{
		// engine.ShowUI(); - this comment is needed for Interactive UI to work
		var controller = new InteractiveController(engine);
		Settings = new Settings();
		var model = new Model(engine, Settings);
		engine.GenerateInformation("Interfaces:" + String.Join(",", model.Interfaces.Where(intf=>intf.Capabilities == "Ethernet").Select(a => a.CircuitCreationInterfaceName)));
		var view = new View(engine, Settings);
		var presenter = new Presenter(view, model, Settings);

		//presenter.Add += (sender, args) =>
		//{
		//	SaveResources(engine, model);
		//};

		view.Show(false);
		presenter.LoadFromModel();

		controller.Run(view);
	}


}