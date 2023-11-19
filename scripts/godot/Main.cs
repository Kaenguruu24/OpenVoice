using System;
using System.Net.Http.Headers;
using Godot;

#nullable enable
namespace OpenVoice
{
	public partial class Main : Node2D
	{
		private Theme? LastTheme;
		private User? ActiveUser;
		AudioEffectRecord? MicRecord;

		public override void _Ready()
		{
			LoadServerSidebar();
			MicRecord = (AudioEffectRecord) AudioServer.GetBusEffect(AudioServer.GetBusIndex("Record"), 0);
			MicRecord.SetRecordingActive(true);
			GD.Print(AudioServer.GetInputDeviceList());
		}

        public override void _Process(double delta)
        {
			AudioEffectSpectrumAnalyzerInstance AnalyzerInstance = (AudioEffectSpectrumAnalyzerInstance) AudioServer.GetBusEffectInstance(AudioServer.GetBusIndex("Record"), 1);
			GD.Print(AnalyzerInstance.GetMagnitudeForFrequencyRange(0f, 50000f, AudioEffectSpectrumAnalyzerInstance.MagnitudeMode.Max));
			//GetNode<TextureProgressBar>("UserPanel/MicStatus/MicLevel").Value = MicRecord.GetRecording().Data[MicRecord.GetRecording().Data.Length - 1];
        }

        private void LoadUserPanel()
		{
			if (ActiveUser == null) return;

		}

		private void LoadServerSidebar()
		{
			string[] Servers = FileAccess.Open("user://servers.dat", FileAccess.ModeFlags.Read).GetAsText().Split("\n");
			foreach (string Server in Servers)
			{
				string[] Parameters = Server.Split("/"); // Format: ip=0.0.0.0/logo="absolute_path"

				Control NewServerListInstance = (Control) GD.Load<PackedScene>("res://scenes/interactables/ServerListItem.tscn").Instantiate();

				NewServerListInstance.GetNode<Sprite2D>("Logo").Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(Parameters[1].Replace("logo=","").Replace("\"","")));
				NewServerListInstance.GetNode<Sprite2D>("Logo").Scale = new Vector2(50f / NewServerListInstance.GetNode<Sprite2D>("Logo").Texture.GetWidth(), 50f / NewServerListInstance.GetNode<Sprite2D>("Logo").Texture.GetHeight());

				Action OpenServerPressed = () => { LoadServer(Parameters[0].Replace("ip=", "")); };
				NewServerListInstance.GetNode<Button>("OpenServer").Connect("pressed", Callable.From(OpenServerPressed));
				
				GetNode<VBoxContainer>("ServerList/VBox").AddChild(NewServerListInstance);
			}

			// Repositioning AddServer Button to be at the bottom
			GetNode<VBoxContainer>("ServerList/VBox").MoveChild(GetNode<Control>("ServerList/VBox/AddServerItem"), GetNode<VBoxContainer>("ServerList/VBox").GetChildCount() - 1);
		}

		private void LoadServer(string IpAdress, bool tryReconnectIfFailed = true)
		{
			RequestHandler.RequestError Error = RequestHandler.SubscribeToServer(IpAdress);
			if (Error == RequestHandler.RequestError.Ok) { GD.Print("Successfully connected to: " + IpAdress); }
			else if (Error == RequestHandler.RequestError.AlreadySubscribed) { RequestHandler.Unsubscribe(); if (tryReconnectIfFailed) LoadServer(IpAdress, false); return; }
			
			foreach (Channel ServerChannel in RequestHandler.GetSubscribed().GetChannels())
			{
				ChannelListItem NewChannelListItem = GD.Load<PackedScene>("res://scenes/interactables/ChannelListItem.tscn").Instantiate<ChannelListItem>();
				GetNode<VBoxContainer>("ChannelList/VBox").AddChild(NewChannelListItem);
				NewChannelListItem.UpdateTheme(LastTheme);
			}
		}

		private void LoadChennel(int ChannelID)
		{
			if (RequestHandler.GetSubscribed()?.GetChannel(ChannelID) == null) return;
			if (RequestHandler.GetSubscribed()?.GetChannel(ChannelID) != null) return; // ! Implement Channel Loading
		}

		public void UpdateTheme(Theme NewTheme)
		{
			foreach (Button Secondaries in GetNode<Node2D>("SecondaryBackgrounds").GetChildren())
			{
				Secondaries.AddThemeStyleboxOverride("disabled", NewTheme.GenerateStyleBoxFromTheme(Theme.StyleBoxType.FLAT, Theme.StyleTarget.BUTTON, Theme.Palette.SECONDARY, new Vector4I(5, 5, 5, 5)));
			}
			foreach (Button Tertiaries in GetNode<Node2D>("TertiaryBackgrounds").GetChildren())
			{
				Tertiaries.AddThemeStyleboxOverride("disabled", NewTheme.GenerateStyleBoxFromTheme(Theme.StyleBoxType.FLAT, Theme.StyleTarget.BUTTON, Theme.Palette.BACKGROUND, new Vector4I(5, 5, 5, 5)));
			}
			GetNode<Button>("ServerList/VBox/AddServerItem/AddServer").AddThemeStyleboxOverride("normal", NewTheme.GenerateStyleBoxFromTheme(Theme.StyleBoxType.FLAT, Theme.StyleTarget.BUTTON, Theme.Palette.PRIMARY, new Vector4I(8, 8, 8, 8)));
			GetNode<Button>("ServerList/VBox/AddServerItem/AddServer").AddThemeStyleboxOverride("hover", NewTheme.GenerateStyleBoxFromTheme(Theme.StyleBoxType.FLAT, Theme.StyleTarget.BUTTON, Theme.Palette.ACCENT, new Vector4I(8, 8, 8, 8)));
			GetNode<Button>("ServerList/VBox/AddServerItem/AddServer").AddThemeStyleboxOverride("pressed", NewTheme.GenerateStyleBoxFromTheme(Theme.StyleBoxType.FLAT, Theme.StyleTarget.BUTTON, Theme.Palette.PRIMARY, new Vector4I(8, 8, 8, 8)));
			GetNode<Button>("ServerList/VBox/AddServerItem/AddServer").AddThemeColorOverride("font_color", NewTheme.GetColor(Theme.Palette.TEXT));
			LastTheme = NewTheme;
		}
	}
}