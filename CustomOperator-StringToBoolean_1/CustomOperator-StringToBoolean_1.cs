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
using Skyline.DataMiner.Analytics.GenericInterface;

[GQIMetaData(Name = "String to Boolean")]
public class MyCustomOperator : IGQIColumnOperator, IGQIRowOperator, IGQIOnInit, IGQIInputArguments
{
	private GQIColumnDropdownArgument _firstColumnArg = new GQIColumnDropdownArgument("Input Column") { IsRequired = true, Types = new GQIColumnType[] { GQIColumnType.String } };
	private GQIStringArgument _nameArg1 = new GQIStringArgument("Output Column Name") { IsRequired = true };

	private GQIColumn _value1;
	private GQIBooleanColumn _newColumn1;

	private GQIDMS _dms;

	public OnInitOutputArgs OnInit(OnInitInputArgs args)
	{
		_dms = args.DMS;
		return default;
	}

	public GQIArgument[] GetInputArguments()
	{
		return new GQIArgument[] { _firstColumnArg, _nameArg1 };
	}

	public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
	{
		_value1 = args.GetArgumentValue(_firstColumnArg);
		_newColumn1 = new GQIBooleanColumn(args.GetArgumentValue(_nameArg1));

		return new OnArgumentsProcessedOutputArgs();
	}

	public void HandleColumns(GQIEditableHeader header)
	{
		header.AddColumns(new GQIColumn[] { _newColumn1 });
	}

	public void HandleRow(GQIEditableRow row)
	{
		var firstValue = "";

		try
		{
			firstValue = row.GetValue<string>(_value1);
		}
		catch
		{
			row.SetValue(_newColumn1, false);
		}

		bool returnValue = false;
		if (!String.IsNullOrEmpty(firstValue))
		{
			returnValue = true;
		}

		row.SetValue(_newColumn1, returnValue);
	}
}