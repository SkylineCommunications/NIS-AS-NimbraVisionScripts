namespace Skyline.Automation.CircuitCreation.Presenter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Skyline.Automation.CircuitCreation.Model;
    using Skyline.Automation.CircuitCreation.View;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Library.Automation;
    using Skyline.DataMiner.Library.Common;

	public class Presenter
	{
		private readonly View view;
		private readonly Model model;
		private readonly Settings _settings;

		public Presenter(View view, Model model, Settings settings)
		{
			view = view ?? throw new ArgumentNullException("view");
			model = model ?? throw new ArgumentNullException("model");
			_settings = settings;
		}

		public void LoadFromModel()
		{
			view.Source.Options = model.Interfaces.Where(intf => intf.Capabilities == "Ethernet").Select(intf => intf.InterfaceName);
			view.Destination.Options = model.Interfaces.Where(intf => intf.Capabilities == "Ethernet").Select(intf => intf.InterfaceName);
		}
	}
}
