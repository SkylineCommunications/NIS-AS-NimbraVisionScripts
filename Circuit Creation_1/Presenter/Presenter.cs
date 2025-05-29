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
	using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp;
	using Skyline.DataMiner.Utils.ConnectorAPI.NetInsight.Nimbra.Vision.InterApp.Messages;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class Presenter
	{
		private readonly View view;
		private readonly Model model;
		private readonly Settings settings;

		public Presenter(View view, Model model, Settings settings)
		{
			this.view = view ?? throw new ArgumentNullException("view");
			this.model = model ?? throw new ArgumentNullException("model");
			this.settings = settings ?? throw new ArgumentNullException("settings");
			SelectCircuitConstructor = new Dictionary<string, Func<string>>
			{
				{ "E-Line", CreateELineCircuit() },
				{ "E-Line VLAN", CreateELineVlanCircuit() },
				{ "JPEG 2000", CreateJ2KCircuit() },
				{ "JPEG 2000 1+1 Hitless", CreateJ2KCircuit() },
				{ "JPEG-XS", CreateJxsCircuit() },
				{ "JPEG-XS 1+1 Hitless", CreateJxsCircuit() },
				{ "SDI SRT", CreateSdiSrtCircuit() },
			};

			view.CircuitTypeSelector.Changed += UpdateUI;
			view.SourceNode.Changed += UpdateUI;
			view.DestinationNode.Changed += UpdateUI;
			view.NoEndTime.Changed += UpdateUI;
			view.NoStartTime.Changed += UpdateUI;

			view.AddCircuitButton.Pressed += OnCreateResourcesPressed;
		}

		private Dictionary<string, Func<string>> SelectCircuitConstructor { get; }

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
					case "E-Line VLAN":
						return intf.Capabilities == "Ethernet";

					case "JPEG 2000":
					case "JPEG 2000 1+1 Hitless":
						if (inOrOut == Utils.InterfaceType.Source)
							return intf.Capabilities.Contains("j2kEnc");

						return intf.Capabilities.Contains("j2kDec");

					case "JPEG-XS":
					case "JPEG-XS 1+1 Hitless":
						if (inOrOut == Utils.InterfaceType.Source)
							return intf.Capabilities.Contains("jxse");

						return intf.Capabilities.Contains("jxsd");
					case "SDI SRT":
						return intf.Capabilities == "SDI SRT";

					default:
						return false;
				}
			}

			if (view.CircuitTypeSelector.Selected.Contains("JPEG 2000"))
				view.Capacity.Value = 50;

			if (view.CircuitTypeSelector.Selected.Equals("JPEG-XS"))
				view.Capacity.Value = 103;

			if (view.CircuitTypeSelector.Selected.Equals("JPEG-XS 1+1 Hitless"))
				view.Capacity.Value = 125;
		}

		private static void ShowResult(IEngine engine, string result)
		{
			var dialog = new MessageDialog(engine, result);
			dialog.Show();
		}

		private void OnCreateResourcesPressed(object sender, EventArgs e)
		{
			string result;
			view.Engine.GenerateInformation("Create Circuit");

			if (view.CircuitTypeSelector.Selected != "SDI SRT" && view.SourceNode.Selected == view.DestinationNode.Selected)
			{
				view.ErrorLabel.Text = "Nodes can't be the same!";
				return;
			}

			if (view.CircuitTypeSelector.Selected == "SDI SRT" && !String.IsNullOrWhiteSpace(view.Passphrase.Text) && view.Passphrase.Text.Length < 10)
			{
				view.ErrorLabel.Text = "Passpharse must be at least 10 characters.";
				return;
			}

			try
			{
				result = SelectCircuitConstructor[view.CircuitTypeSelector.Selected].Invoke();
			}
			catch (Exception ex)
			{
				result = "Issue while creation circuit: " + ex;
			}

			view.Engine.Log(result);
			ShowResult(view.Engine, result);
			view.Engine.ExitSuccess(result);
		}

		private Func<string> CreateELineCircuit()
		{
			return () =>
			{
				view.Engine.GenerateInformation("CreateELineCircuit");
				try
				{
					var createFields = new ELineCircuitRequest
					{
						ServiceId = view.CircuitTypeSelector.Selected,
						Capacity = Convert.ToInt32(view.Capacity.Value),
						Destination = model.Interfaces.First(intf => intf.InterfaceName == view.DestinationInterface.Selected).CircuitCreationInterfaceName,
						Source = model.Interfaces.First(intf => intf.InterfaceName == view.SourceInterface.Selected).CircuitCreationInterfaceName,
						StartTime = view.NoStartTime.IsChecked ? DateTime.MinValue : view.StartTime.DateTime,
						EndTime = view.NoEndTime.IsChecked ? DateTime.MinValue : view.StopTime.DateTime,
					};

					INimbraVisionResponse nimbraVisionResponse = SendInterAppMessage(createFields);
					return nimbraVisionResponse.Success ? "Circuit request sent successfully." : nimbraVisionResponse.Message;
				}
				catch
				{
					return "Circuit request failed.";
				}
			};
		}

		private Func<string> CreateJ2KCircuit()
		{
			return () =>
			{
				try
				{
					var createFields = new J2KCircuitRequest
					{
						ServiceId = view.CircuitTypeSelector.Selected == "JPEG 2000 1+1 Hitless" ? "j2k-hitless" : "j2k",
						Capacity = Convert.ToInt32(view.Capacity.Value),
						Destination = model.Interfaces.First(intf => intf.InterfaceName == view.DestinationInterface.Selected).CircuitCreationInterfaceName,
						Source = model.Interfaces.First(intf => intf.InterfaceName == view.SourceInterface.Selected).CircuitCreationInterfaceName,
						StartTime = view.NoStartTime.IsChecked ? DateTime.MinValue : view.StartTime.DateTime,
						EndTime = view.NoEndTime.IsChecked ? DateTime.MinValue : view.StopTime.DateTime,
						ProtectionId = view.CircuitTypeSelector.Selected == "JPEG 2000 1+1 Hitless" ? 1 : -1,
					};

					INimbraVisionResponse nimbraVisionResponse = SendInterAppMessage(createFields);
					return nimbraVisionResponse.Success ? "Circuit request sent successfully." : nimbraVisionResponse.Message;
				}
				catch
				{
					return "Circuit request failed.";
				}
			};
		}

		private Func<string> CreateJxsCircuit()
		{
			return () =>
			{
				try
				{
					var createFields = new J2KCircuitRequest
					{
						ServiceId = view.CircuitTypeSelector.Selected == "JPEG-XS 1+1 Hitless" ? "jxs-hitless" : "jxs",
						Capacity = Convert.ToInt32(view.Capacity.Value),
						Destination = model.Interfaces.First(intf => intf.InterfaceName == view.DestinationInterface.Selected).CircuitCreationInterfaceName,
						Source = model.Interfaces.First(intf => intf.InterfaceName == view.SourceInterface.Selected).CircuitCreationInterfaceName,
						StartTime = view.NoStartTime.IsChecked ? DateTime.MinValue : view.StartTime.DateTime,
						EndTime = view.NoEndTime.IsChecked ? DateTime.MinValue : view.StopTime.DateTime,
						ProtectionId = view.CircuitTypeSelector.Selected == "JPEG-XS 1+1 Hitless" ? 1 : -1,
					};

					INimbraVisionResponse nimbraVisionResponse = SendInterAppMessage(createFields);
					return nimbraVisionResponse.Success ? "Circuit request sent successfully." : nimbraVisionResponse.Message;
				}
				catch
				{
					return "Circuit request failed.";
				}
			};
		}

		private Func<string> CreateELineVlanCircuit()
		{
			return () =>
			{
				try
				{
					var createFields = new ELineVlanCircuitRequest
					{
						ServiceId = view.CircuitTypeSelector.Selected,
						Capacity = Convert.ToInt32(view.Capacity.Value),
						Destination = model.Interfaces.First(intf => intf.InterfaceName == view.DestinationInterface.Selected).CircuitCreationInterfaceName,
						Source = model.Interfaces.First(intf => intf.InterfaceName == view.SourceInterface.Selected).CircuitCreationInterfaceName,
						StartTime = view.NoStartTime.IsChecked ? DateTime.MinValue : view.StartTime.DateTime,
						EndTime = view.NoEndTime.IsChecked ? DateTime.MinValue : view.StopTime.DateTime,
						ExtraInfo = new ELineVlanCircuitRequest.Extra
						{
							Common = new ELineVlanCircuitRequest.Common
							{
								FormName = view.FormName.Text,
								Vlan = Convert.ToInt32(view.Vlan.Value),
							},
						},
					};

					INimbraVisionResponse nimbraVisionResponse = SendInterAppMessage(createFields);
					return nimbraVisionResponse.Success ? "Circuit request sent successfully." : nimbraVisionResponse.Message;
				}
				catch
				{
					return "Circuit request failed.";
				}
			};
		}

		private Func<string> CreateSdiSrtCircuit()
		{
			return () =>
			{
				try
				{
					var createFields = new SdiSrtCircuitRequest
					{
						ServiceId = "VA-SRT",
						Capacity = Convert.ToInt32(view.Capacity.Value),
						Destination = model.Interfaces.First(intf => intf.InterfaceName == view.DestinationInterface.Selected).CircuitCreationInterfaceName,
						Source = model.Interfaces.First(intf => intf.InterfaceName == view.SourceInterface.Selected).CircuitCreationInterfaceName,
						StartTime = view.NoStartTime.IsChecked ? DateTime.MinValue : view.StartTime.DateTime,
						EndTime = view.NoEndTime.IsChecked ? DateTime.MinValue : view.StopTime.DateTime,
						ExtraInfo = new SdiSrtCircuitRequest.Extra
						{
							Common = new SdiSrtCircuitRequest.Common
							{
								FormName = view.FormName.Text,
								Port = Convert.ToInt32(view.StreamPort.Value),
								Passphrase = view.Passphrase.Text,
								Mode = settings.SupportedSrtModes[view.SrtMode.Selected],
							},
						},
					};

					INimbraVisionResponse nimbraVisionResponse = SendInterAppMessage(createFields);
					return nimbraVisionResponse.Success ? "Circuit request sent successfully." : nimbraVisionResponse.Message;
				}
				catch
				{
					return "Circuit request failed.";
				}
			};
		}

		private void UpdateUI(object sender, DropDown.DropDownChangedEventArgs e)
		{
			LoadFromModel();
			view.RestartUI();
		}

		private void UpdateUI(object sender, CheckBox.CheckBoxChangedEventArgs e)
		{
			LoadFromModel();
			view.RestartUI();
		}

		private INimbraVisionResponse SendInterAppMessage(INimbraVisionRequest createFields)
		{
			var nimbraVisionElement = new NimbraVisionInterAppCalls(view.Engine.GetUserConnection(), model.NimbraVisionElement.Name);

			INimbraVisionResponse nimbraVisionResponse = nimbraVisionElement.SendSingleResponseMessage(createFields, TimeSpan.FromMinutes(1));
			return nimbraVisionResponse;
		}
	}
}