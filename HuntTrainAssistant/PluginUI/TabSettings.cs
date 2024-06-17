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
						Section("基本设置") // General settings
                        .Widget(() =>
						{
								ImGui.Checkbox("启用插件", ref P.Config.Enabled); // Plugin enabled
								ImGui.SameLine();
								ImGui.Checkbox("调试模式", ref P.Config.Debug); // Debug mode
								ImGui.Checkbox("自动传送到不同的地图", ref P.Config.AutoTeleport); // Autoteleport to different zone
								ImGui.Checkbox("检测到新的坐标链接后自动打开地图", ref P.Config.AutoOpenMap); // Auto-open map when new location is linked
								ImGui.Checkbox("设置车头后，屏蔽其他人的聊天信息", ref P.Config.SuppressChatOtherPlayers); // When conductor is set, suppress other people's messages
								ImGui.Checkbox("补偿一些以太之光的位置判断", ref P.Config.DistanceCompensationHack); // Compensate for some aetherytes' position
                        })
						.Draw();
		}
}
