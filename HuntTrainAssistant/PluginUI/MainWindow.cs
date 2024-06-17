using ECommons.ImGuiMethods;
using ECommons.SimpleGui;
using System.Runtime.Intrinsics.X86;

namespace HuntTrainAssistant.PluginUI;

public unsafe class MainWindow : ConfigWindow
{
    public MainWindow() : base()
    {
				TitleBarButtons.Add(new()
				{
						Click = (m) => { if (m == ImGuiMouseButton.Left) S.SettingsWindow.IsOpen = true; },
						Icon = FontAwesomeIcon.Cog,
						IconOffset = new(2, 2),
						ShowTooltip = () => ImGui.SetTooltip("打开设置窗口"), // Open settings window
                });
		}

    public override void Draw()
    {
				ImGui.SetNextItemWidth(150f);
				var condIndex = 0;
				var condNames = P.Config.Conductors.Select(x => x.Name).ToArray();
				ImGuiEx.Text("当前车头:"); // Current conductors:
				ImGui.SameLine();
				if (ImGui.SmallButton("清除")) // Clear
        {
						P.Config.Conductors.Clear();
				}
				ImGui.SameLine();
				// Remove selected conductor
				if (ImGui.SmallButton("移除所选")) // Remove selected
				{
						if (condIndex >= 0 && condIndex < P.Config.Conductors.Count)
						{
								P.Config.Conductors.RemoveAt(condIndex);
						}
				}
				ImGuiEx.SetNextItemFullWidth();
				ImGui.ListBox("##conds", ref condIndex, condNames, condNames.Length, Math.Clamp(condNames.Length, 1, 3));
				ImGuiEx.Text("添加车头:"); // Add conductor:
				ImGui.SameLine();
				ImGui.SetNextItemWidth(150f);
				var newCond = "";
				if (ImGui.InputText("##newCond", ref newCond, 50, ImGuiInputTextFlags.EnterReturnsTrue))
				{
						if (newCond.Length > 0)
						{
								P.Config.Conductors.Add(new(newCond, 0));
								newCond = "";
						}
				}
				if (P.TeleportTo.Territory == 0)
				{
						ImGuiEx.Text("自动传送: 未激活"); // Autoteleport: inactive
						if (ChatMessageHandler.LastMessageLoc.Aetheryte != null && ImGui.Button($"自动传送到: {ChatMessageHandler.LastMessageLoc.Aetheryte.PlaceName.Value.Name}")) // Autoteleport to
            {
								P.TeleportTo = ChatMessageHandler.LastMessageLoc;
						}
				}
				else
				{
						ImGuiEx.Text($"自动传送: 已激活"); // Autoteleport active.
						ImGui.SameLine();
						if (ImGui.SmallButton("取消")) // Cancel
						{
								P.TeleportTo.Territory = 0;
								P.TeleportTo.Aetheryte = null;
						}
						ImGuiEx.Text($"{P.TeleportTo.Aetheryte.GetPlaceName()}@{P.TeleportTo.Territory.GetTerritoryName()}");
				}
				ImGui.Checkbox($"Sonar 自动传送", ref P.Config.AutoVisitTeleportEnabled); // Sonar Auto-teleport
				if (P.Config.AutoVisitTeleportEnabled)
				{
						if (!Utils.IsInHuntingTerritory())
						{
								ImGuiEx.HelpMarker("您不在狩猎地图内，传送已启用。", EColor.GreenBright, FontAwesomeIcon.Check.ToIconString()); // You are not in a hunting zone. Teleport enabled.
						}
						else
						{
								ImGuiEx.HelpMarker("您已经在狩猎地图内，传送已禁用。", EColor.RedBright, "\uf00d"); // You are in a hunting zone. Teleport disabled. 
						}
						ImGui.SameLine();
						ImGui.Checkbox("允许跨界传送", ref P.Config.AutoVisitCrossWorld); // C/W
						ImGui.SameLine();
						ImGui.Checkbox("允许超域传送", ref P.Config.AutoVisitCrossDC); // C/DC
				}
				if(S.SonarMonitor.Continuation != null)
				{
						ImGuiEx.Text(GradientColor.Get(EColor.RedBright, EColor.YellowBright), $"等待抵达: {S.SonarMonitor.Continuation.Value.World}/{S.SonarMonitor.Continuation.Value.Aetheryte.GetPlaceName()}"); // Waiting to arrive at:
				if (ImGui.SmallButton("取消##arrival")) // Cancel
						{
						S.SonarMonitor.Continuation = null;
						}
				}
    }

    static void Help()
    {
        ImGuiEx.TextWrapped("- Be in one of Endwalker hunt zones;");
        ImGuiEx.TextWrapped("- Assign conductors either by right-clicking them in chat/world or enter their names manually;");
    }
}
