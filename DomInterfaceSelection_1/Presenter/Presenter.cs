namespace Skyline.Automation.CircuitCreation.Presenter
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Newtonsoft.Json;
	using Skyline.Automation.CircuitCreation.Model;
	using Skyline.Automation.CircuitCreation.View;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.LogHelpers;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class Presenter
	{
		private readonly View view;
		private readonly Model model;

		public Presenter(View view, Model model)
		{
			this.view = view ?? throw new ArgumentNullException("view");
			this.model = model ?? throw new ArgumentNullException("model");

			view.CircuitTypeSelector.Changed += UpdateUI;
			view.SourceNode.Changed += UpdateUI;
			view.DestinationNode.Changed += UpdateUI;

			view.AddCircuitButton.Pressed += OnScheduleResourcePressed;
		}

		public void LoadFromModel()
		{
			view.SourceNode.Options = model.Interfaces.Where(intf => CheckInterfaceCapabilities(intf, Utils.InterfaceType.Source)).Select(intf => intf.NodeName);
			view.DestinationNode.Options = model.Interfaces.Where(intf => CheckInterfaceCapabilities(intf, Utils.InterfaceType.Destination)).Select(intf => intf.NodeName);

			view.SourceInterface.Options = model.Interfaces.Where(intf => CheckInterfaceCapabilities(intf, Utils.InterfaceType.Source) && intf.NodeName == view.SourceNode.Selected).Select(intf => intf.InterfaceName);
			view.DestinationInterface.Options = model.Interfaces.Where(intf => CheckInterfaceCapabilities(intf, Utils.InterfaceType.Destination) && intf.NodeName == view.DestinationNode.Selected).Select(intf => intf.InterfaceName);

			bool CheckInterfaceCapabilities(CircuitCreation.Model.Interface intf, Utils.InterfaceType inOrOut)
			{
				switch (view.CircuitTypeSelector.Selected)
				{
					case "E-Line":
						return intf.Capabilities == "Ethernet";
					case "JPEG 2000":
					case "JPEG 2000 1+1 Hitless":
						if(inOrOut == Utils.InterfaceType.Source)
							return intf.Capabilities.Contains("j2kEnc");

						return intf.Capabilities.Contains("j2kDec");
					case "JPEG-XS":
					case "JPEG-XS 1+1 Hitless":
						if(inOrOut == Utils.InterfaceType.Source)
							return intf.Capabilities.Contains("jxse");

						return intf.Capabilities.Contains("jxsd");
					default:
						return false;
				}
			}
		}

		private void OnScheduleResourcePressed(object sender, EventArgs e)
		{
			if (view.SourceNode.Selected == view.DestinationNode.Selected)
			{
				view.ErrorLabel.Text = "Nodes can't be the same!";
				return;
			}

			var sectionDefinitionLinks = model.DomInstance.GetDomDefinition().SectionDefinitionLinks;
			FilterElement<SectionDefinition> sectionDefintionfilter = SectionDefinitionExposers.ID.Equal(sectionDefinitionLinks.First().SectionDefinitionID);
			var sectionDefinition = model.DomHelper.SectionDefinitions.Read(sectionDefintionfilter).First(sd => sd.GetName() == "Circuit Info");
			var capacity = Convert.ToInt64(Utils.GetFieldValue(model.DomInstance, "Capacity"));
			long newCapacity = capacity;
			if (capacity < 0)
				newCapacity = 50;

			if (view.CircuitTypeSelector.Selected.Contains("JPEG") && capacity < 50)
				newCapacity = 50;

			model.DomInstance.AddOrUpdateFieldValue(sectionDefinition, sectionDefinition.GetAllFieldDescriptors().First(fd => fd.Name == "Source Node"), view.SourceNode.Selected);
			model.DomInstance.AddOrUpdateFieldValue(sectionDefinition, sectionDefinition.GetAllFieldDescriptors().First(fd => fd.Name == "Source Interface"), view.SourceInterface.Selected);
			model.DomInstance.AddOrUpdateFieldValue(sectionDefinition, sectionDefinition.GetAllFieldDescriptors().First(fd => fd.Name == "Destination Node"), view.DestinationNode.Selected);
			model.DomInstance.AddOrUpdateFieldValue(sectionDefinition, sectionDefinition.GetAllFieldDescriptors().First(fd => fd.Name == "Destination Interface"), view.DestinationInterface.Selected);
			if(newCapacity != capacity)
			{
				model.DomInstance.AddOrUpdateFieldValue(sectionDefinition, sectionDefinition.GetAllFieldDescriptors().First(fd => fd.Name == "Capacity"), newCapacity);
			}

			model.DomHelper.DomInstances.Update(model.DomInstance);
			model.DomHelper.DomInstances.DoStatusTransition(model.DomInstance.ID, model.TransitionId);
			view.Engine.ExitSuccess("Completed Scheduling.");
		}

		private void UpdateUI(object sender, DropDown.DropDownChangedEventArgs e)
		{
			LoadFromModel();
			view.RestartUI();
		}
	}
}
