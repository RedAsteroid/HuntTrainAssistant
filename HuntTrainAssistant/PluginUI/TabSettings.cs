using Dalamud.Memory.Exceptions;
using NightmareUI.PrimaryUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuntTrainAssistant.PluginUI;
public class TabSettings
{
		public void Draw()
		{
				new NuiBuilder().
						Section("通用设置")
						.Widget(() =>
						{
								ImGui.Checkbox("启用插件", ref P.Config.Enabled);
								ImGui.SameLine();
								ImGui.Checkbox("调试模式", ref P.Config.Debug);
								ImGui.Checkbox("自动传送到不同的地图", ref P.Config.AutoTeleport);
								ImGui.Checkbox("检测到新的坐标链接后自动打开地图", ref P.Config.AutoOpenMap);
								ImGui.Checkbox("设置车头后，屏蔽其他人的聊天信息", ref P.Config.SuppressChatOtherPlayers);
								ImGui.Checkbox("补偿一些以太之光的位置判断", ref P.Config.DistanceCompensationHack);
						})

						.Section("联动")
						.Widget(() =>
						{
								ImGui.Checkbox("启用 Sonar 联动", ref P.Config.SonarIntegration);
								ImGuiEx.PluginAvailabilityIndicator([new("SonarPlugin", "Sonar")]);
								ImGui.Indent();
								ImGuiEx.TextWrapped("检测到聊天中的狩猎标记通知时，自动传送到目标服务器与地图");
								ImGui.Unindent();
								ImGui.Checkbox($"接收到通知时，自动传送到最近的以太之光", ref P.Config.AutoVisitTeleportEnabled);
								ImGuiEx.PluginAvailabilityIndicator([new("TeleporterPlugin", "Teleporter")]);
								ImGui.Checkbox("允许跨界传送", ref P.Config.AutoVisitCrossWorld);
								ImGuiEx.PluginAvailabilityIndicator([new("TeleporterPlugin", "Teleporter"), new("Lifestream", new Version("2.1.1.0"))]);
								ImGui.Checkbox("允许超域传送", ref P.Config.AutoVisitCrossDC);
								ImGuiEx.PluginAvailabilityIndicator([new("TeleporterPlugin", "Teleporter"), new("Lifestream", new Version("2.1.1.0"))]);
								ImGui.Checkbox("在聊天信息中添加点击传送按钮", ref P.Config.AutoVisitModifyChat);
						})

						.Draw();
		}
}
