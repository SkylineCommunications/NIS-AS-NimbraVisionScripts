
namespace Skyline.Automation.CircuitCreation.View
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.DeveloperCommunityLibrary.InteractiveAutomationToolkit;
	using Skyline.DataMiner.Net.Profiles;

	public class View : Dialog
	{
		private readonly Settings _settings;

		public View(IEngine engine, Settings settings) : base(engine)
		{
			Title = "Create New Circuit";
			Width = 800;
			this._settings = settings;

			CircuitTypeSelector = new DropDown(_settings.SupportedCircuitTypes);
			Capacity = new Numeric(1);
			Source = new DropDown();
			Destination = new DropDown();

			AddWidget(new Label("Circuit Type"), 0, 1);
			AddWidget(CircuitTypeSelector, 0, 2, 1, 2);
			AddWidget(new Label("Capacity"), 1, 1);
			AddWidget(Capacity, 1, 2, 1, 2);
			AddWidget(new Label("Source"), 2, 1);
			AddWidget(Source, 2, 2, 1, 2);
			AddWidget(new Label("Destination"), 3, 1);
			AddWidget(Destination, 3, 2, 1, 2);
			AddCircuitButton = new Button("Create Resources");
			AddWidget(AddCircuitButton, 4, 2, 1, 2);

		}

		public Button AddCircuitButton { get; set; }

		public Label ErrorLabel { get; set; }

		public DropDown CircuitTypeSelector { get; set; }

		public DropDown Source { get; set; }

		public DropDown Destination { get; set; }

		public Numeric Capacity { get; set; }
	}
}
