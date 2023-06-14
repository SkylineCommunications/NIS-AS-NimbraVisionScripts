namespace Skyline.Automation.CircuitCreation.View
{
	using System;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.DeveloperCommunityLibrary.InteractiveAutomationToolkit;

	public class View : Dialog
	{
		private readonly Settings _settings;

		public View(IEngine engine, Settings settings) : base(engine)
		{
			Title = "Create New Circuit";
			Width = 800;
			this._settings = settings;
			AllowOverlappingWidgets = true;

			StartTime = new DateTimePicker(DateTime.Now) { Width = _settings.ComponentWidth, Minimum = DateTime.Now};
			StopTime = new DateTimePicker(DateTime.Now.AddDays(1)) { Width = _settings.ComponentWidth, Minimum = DateTime.Now };
			CircuitTypeSelector = new DropDown(_settings.SupportedCircuitTypes) { Width = _settings.ComponentWidth };
			Capacity = new Numeric(1) { Width = _settings.ComponentWidth, Decimals = 0, Minimum = 1, Maximum = 1000, Tooltip = "Capacity in Mbps", ValidationText = "Invalid Range" };
			SourceNode = new DropDown { Width = _settings.ComponentWidth };
			DestinationNode = new DropDown { Width = _settings.ComponentWidth };
			SourceInterface = new DropDown { Width = _settings.ComponentWidth };
			DestinationInterface = new DropDown { Width = _settings.ComponentWidth };
			FormName = new TextBox { Width = _settings.ComponentWidth };
			Vlan = new Numeric(1) { Width = _settings.ComponentWidth, Decimals = 0, Minimum = 1, Maximum = 4096, ValidationText = "Invalid Range" };
			AddCircuitButton = new Button("Create Circuit") { Width = _settings.ComponentWidth * 2, Height = _settings.ButtonHeight };
			NoEndTime = new CheckBox("Undefined") { Width = _settings.ComponentWidth };
			NoStartTime = new CheckBox("Now") { Width = _settings.ComponentWidth };
			ErrorLabel = new Label(String.Empty);

			SharedInitialiation();

			AddWidget(new WhiteSpace(), RowCount + 1, 1);
			AddWidget(AddCircuitButton, RowCount + 1 , 2, 2, 2, HorizontalAlignment.Center);
			AddWidget(ErrorLabel, RowCount, 4, 1, 1);
			AddWidget(new WhiteSpace(), RowCount + 1, 1);
		}

		public Button AddCircuitButton { get; set; }

		public Label ErrorLabel { get; set; }

		public DropDown CircuitTypeSelector { get; set; }

		public DropDown SourceNode { get; set; }

		public DropDown DestinationNode { get; set; }

		public DropDown SourceInterface { get; set; }

		public DropDown DestinationInterface { get; set; }

		public Numeric Capacity { get; set; }

		public DateTimePicker StartTime { get; set; }

		public DateTimePicker StopTime { get; set; }

		public TextBox FormName { get; set; }

		public Numeric Vlan { get; set; }

		public CheckBox NoEndTime { get; set; }

		public CheckBox NoStartTime { get; set; }

		internal void RestartUI()
		{
			StopTime.IsEnabled = !NoEndTime.IsChecked;
			StartTime.IsEnabled = !NoStartTime.IsChecked;
			Clear();
			SharedInitialiation();
			if(CircuitTypeSelector.Selected == "E-Line VLAN")
			{
				AddWidget(new Label("Form Name") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, RowCount + 1, 1);
				AddWidget(FormName, RowCount, 2, 1, 1);
				AddWidget(new Label("VLAN") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, RowCount + 1, 1);
				AddWidget(Vlan, RowCount, 2, 1, 1);
			}

			AddWidget(new WhiteSpace(),RowCount + 1, 1);
			AddWidget(AddCircuitButton, RowCount + 1, 2, 2, 2, HorizontalAlignment.Center);
			AddWidget(ErrorLabel, RowCount, 4, 1, 1);
			AddWidget(new WhiteSpace(), RowCount + 1, 1);
		}

		private void SharedInitialiation()
		{
			AddWidget(new Label("Circuit Type") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 0, 1);
			AddWidget(CircuitTypeSelector, 0, 2, 1, 1);
			AddWidget(new Label("Capacity") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 1, 1);
			AddWidget(Capacity, 1, 2, 1, 1);
			AddWidget(new Label("Source Node") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 2, 1);
			AddWidget(SourceNode, 2, 2, 1, 1);
			AddWidget(new Label("Source Interface") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 2, 3, HorizontalAlignment.Right);
			AddWidget(SourceInterface, 2, 4, 1, 1);
			AddWidget(new Label("Destination Node") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 3, 1);
			AddWidget(DestinationNode, 3, 2, 1, 1);
			AddWidget(new Label("Destination Interface") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 3, 3, HorizontalAlignment.Right);
			AddWidget(DestinationInterface, 3, 4, 1, 1);
			AddWidget(new Label("Start Time") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 4, 1);
			AddWidget(StartTime, 4, 2, 1, 1);
			AddWidget(NoStartTime, 4, 3);
			AddWidget(new Label("Stop time") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 5, 1);
			AddWidget(StopTime, 5, 2, 1, 1);
			AddWidget(NoEndTime, 5, 3);
		}
	}
}
