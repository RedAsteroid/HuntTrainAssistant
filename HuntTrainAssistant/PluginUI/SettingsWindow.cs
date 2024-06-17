using ECommons.Funding;
using ECommons.SimpleGui;
using NightmareUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuntTrainAssistant.PluginUI;
public unsafe class SettingsWindow : ConfigWindow
{
		public TabSettings TabSettings = new();
		public TabDebug TabDebug = new();

		private SettingsWindow() : base()
		{
				this.WindowName = "HuntTrainAssistant 设置"; // HuntTrainAssistant Configuration
				EzConfigGui.WindowSystem.AddWindow(this);
		}

		public override void Draw()
		{
				PatreonBanner.DrawRight();
				ImGuiEx.EzTabBar("Bar", PatreonBanner.Text,
            ("设置", TabSettings.Draw, null, true), // Settings
            ("联动", S.TabIntegrations.Draw, null, true), // Integrations
            ("以太之光黑名单", S.TabAetheryteBlacklist.Draw, null, true), // Aetheryte Blacklist
            ("Debug", TabDebug.Draw, ImGuiColors.DalamudGrey3, true),
						("Log", InternalLog.PrintImgui, ImGuiColors.DalamudGrey3, false)
						);
		}
}
