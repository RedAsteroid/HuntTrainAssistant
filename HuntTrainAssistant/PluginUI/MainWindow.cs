using Dalamud.Interface.Style;
using ECommons.ImGuiMethods;
using ECommons.SimpleGui;
using System.Runtime.Intrinsics.X86;
using HuntTrainAssistant.Tasks;

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
						ShowTooltip = () => ImGui.SetTooltip("打开设置窗口"),
				});
        TitleBarButtons.Add(new()
        {
            Click = (m) => { if(P.Config.PfinderEnable) { TaskCreateHuntPF.Enqueue(); } else { DuoLog.Warning($"创建怪物狩猎招募按钮未启用，请在设置中启用。"); } },
            Icon = FontAwesomeIcon.PeopleGroup,
            IconOffset = new(2, 2),
            ShowTooltip = () => ImGui.SetTooltip("创建 怪物狩猎招募"),
        });
		}

    public override void Draw()
    {
				ImGui.SetNextItemWidth(150f);
				var condIndex = 0;
				var condNames = P.Config.Conductors.Select(x => x.Name).ToArray();
				ImGuiEx.Text("当前车头:");
				ImGui.SameLine();
				if (ImGui.SmallButton("清除"))
				{
						P.Config.Conductors.Clear();
				}
				ImGui.SameLine();
				// Remove selected conductor
				if (ImGui.SmallButton("移除所选"))
				{
						if (condIndex >= 0 && condIndex < P.Config.Conductors.Count)
						{
								P.Config.Conductors.RemoveAt(condIndex);
						}
				}
				ImGuiEx.SetNextItemFullWidth();
				ImGui.ListBox("##conds", ref condIndex, condNames, condNames.Length, Math.Clamp(condNames.Length, 1, 3));
				ImGuiEx.Text("添加车头:");
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

				// Show KilledARanks
				if (P.Config.ShowKilledARanks)
				{
						if (P.KilledARanks.Count > 0)
						{
							ImGuiEx.Text($"当前已记录击杀A怪({P.KilledARanks.Count}): {P.KilledARanks.Print(", ")}");
						}
						if (P.KilledARanks.Count == 0)
						{
							ImGuiEx.Text($"当前已记录击杀A怪: 暂无");
						}
				}
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);


				if (P.TeleportTo == null)
				{
						ImGuiEx.Text(ImGuiColors.DalamudGrey3, "自动传送: 未激活");
						if (ChatMessageHandler.LastMessageLoc != null && ImGui.Button($"自动传送到 → {ChatMessageHandler.LastMessageLoc.Aetheryte.PlaceName.Value.Name}"))
						{
							P.TeleportTo = ChatMessageHandler.LastMessageLoc;
						}
				}
				else
				{
						ImGuiEx.Text($"自动传送: 已激活");
						ImGui.SameLine();
						if (ImGui.SmallButton("取消"))
						{
						PluginLog.Debug($"TeleportTo reset (3)");
						P.TeleportTo = null;
						}
						ImGuiEx.Text($"{P.TeleportTo.Aetheryte.GetPlaceName()}@{P.TeleportTo.Territory.GetTerritoryName()} i{P.TeleportTo.Instance}");
				}
				if(P.TaskManager.IsBusy)
				{
						ImGuiEx.Text($"{P.TaskManager.NumQueuedTasks:D2} 个任务进行中");
						ImGui.SameLine();
						if(ImGui.SmallButton("停止##tm"))
						{
							P.TaskManager.Abort();
						}
				}
				else
				{
						ImGuiEx.Text(ImGuiColors.DalamudGrey3, $"Task manager: 未激活");
				}
				ImGui.Checkbox($"Sonar 自动传送", ref P.Config.AutoVisitTeleportEnabled);
				if (P.Config.AutoVisitTeleportEnabled)
				{
						if (!Utils.IsInHuntingTerritory())
						{
								ImGuiEx.HelpMarker("您不在狩猎地图内，Sonar 自动传送已启用", EColor.GreenBright, FontAwesomeIcon.Check.ToIconString());
						}
						else
						{
								ImGuiEx.HelpMarker("您已经在狩猎地图内，Sonar 自动传送已禁用", EColor.RedBright, "\uf00d");
						}
						ImGui.SameLine();
						ImGui.Checkbox("允许跨界传送", ref P.Config.AutoVisitCrossWorld);
						ImGui.SameLine();
						ImGui.Checkbox("允许超域传送", ref P.Config.AutoVisitCrossDC);
				}
				if(S.SonarMonitor.Continuation != null)
				{
						ImGuiEx.Text(GradientColor.Get(EColor.RedBright, EColor.YellowBright), $"等待抵达: {S.SonarMonitor.Continuation.World}/{S.SonarMonitor.Continuation.Aetheryte.GetPlaceName()} i{S.SonarMonitor.Continuation.Instance}");
						if (ImGui.SmallButton("取消##arrival"))
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
