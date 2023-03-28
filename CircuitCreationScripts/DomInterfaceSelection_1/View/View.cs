namespace Skyline.Automation.CircuitCreation.View
{
	using System;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.DeveloperCommunityLibrary.InteractiveAutomationToolkit;

	public class View : Dialog
	{
		private readonly Settings _settings;

		public View(IEngine engine, Settings settings, Utils.CircuitType circuitType) : base(engine)
		{
			Title = "Select Interfaces";
			Width = 800;
			this._settings = settings;
			AllowOverlappingWidgets = true;

			string circuitTypeStr = String.Empty;

			switch (circuitType)
			{
				case Utils.CircuitType.Eline:
					circuitTypeStr = "E-Line";
					break;
				case Utils.CircuitType.J2k:
					circuitTypeStr = "JPEG 2000";
					break;
				case Utils.CircuitType.J2kHitless:
					circuitTypeStr = "JPEG 2000 1+1 Hitless";
					break;
				default:

					break;
			}

			CircuitTypeSelector = new DropDown(_settings.SupportedCircuitTypes) { Width = _settings.ComponentWidth, IsEnabled = false, Selected = circuitTypeStr };
			SourceNode = new DropDown { Width = _settings.ComponentWidth };
			DestinationNode = new DropDown { Width = _settings.ComponentWidth };
			SourceInterface = new DropDown { Width = _settings.ComponentWidth };
			DestinationInterface = new DropDown { Width = _settings.ComponentWidth };
			AddCircuitButton = new Button("Schedule Circuit") { Width = _settings.ComponentWidth * 2, Height = _settings.ButtonHeight };
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

		internal void RestartUI()
		{
			Clear();
			SharedInitialiation();

			AddWidget(new WhiteSpace(),RowCount + 1, 1);
			AddWidget(AddCircuitButton, RowCount + 1, 2, 2, 2, HorizontalAlignment.Center);
			AddWidget(ErrorLabel, RowCount, 4, 1, 1);
			AddWidget(new WhiteSpace(), RowCount + 1, 1);
		}

		private void SharedInitialiation()
		{
			AddWidget(new Label("Circuit Type") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 0, 1);
			AddWidget(CircuitTypeSelector, 0, 2, 1, 1);
			AddWidget(new Label("Source Node") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 2, 1);
			AddWidget(SourceNode, 2, 2, 1, 1);
			AddWidget(new Label("Source Interface") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 2, 3, HorizontalAlignment.Right);
			AddWidget(SourceInterface, 2, 4, 1, 1);
			AddWidget(new Label("Destination Node") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 3, 1);
			AddWidget(DestinationNode, 3, 2, 1, 1);
			AddWidget(new Label("Destination Interface") { Width = _settings.LabelWidth, Style = TextStyle.Bold }, 3, 3, HorizontalAlignment.Right);
			AddWidget(DestinationInterface, 3, 4, 1, 1);
		}
	}
}
