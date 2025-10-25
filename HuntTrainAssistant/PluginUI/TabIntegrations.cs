using ECommons.ExcelServices;
using HuntTrainAssistant.DataStructures;
using NightmareUI.PrimaryUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuntTrainAssistant.PluginUI;
public class TabIntegrations
{
    private TabIntegrations() { }
    public void Draw()
    {
        new NuiBuilder()
        .Section("插件")
        .Widget(() =>
        {
            ImGui.Checkbox("启用 Sonar 联动", ref P.Config.SonarIntegration);
            ImGuiEx.PluginAvailabilityIndicator([new("SonarPlugin", "Sonar")]);
            ImGui.Indent();
            ImGuiEx.TextWrapped("检测到聊天中的 Sonar 狩猎标记通知时，自动传送到目标服务器与地图");
            ImGui.Checkbox("在聊天信息中添加点击传送按钮", ref P.Config.AutoVisitModifyChat);
            ImGui.Checkbox("在传送后切换副本区", ref P.Config.EnableSonarInstanceSwitching);
            ImGui.Checkbox("使用 快捷跨界传送指令(Daily Routines) 进行超域传送", ref P.Config.UseDRWorldTravelCommand);
            ImGuiEx.PluginAvailabilityIndicator([new("DailyRoutines")]);
            ImGuiEx.PluginAvailabilityIndicator([new("DCTraveler", "DCTraveler"), new("DCTravelerX", "DCTravelerX")], "需要安装并启用以下插件之一:", false);
            ImGui.Unindent();
            ImGui.Separator();
            ImGui.Checkbox("启用 HuntAlerts 联动", ref P.Config.HuntAlertsIntegration);
            ImGuiEx.PluginAvailabilityIndicator([new("HuntAlerts", new Version("1.2.1.3"))]);
            ImGuiEx.TextWrapped("接收到服务器发送的狩猎标记时，自动传送到目标服务器与地图");
        })

        .Section("通用设置")
        .Widget(() =>
        {
            ImGuiEx.TextWrapped($"以下为所有插件联动的通用选项");
            ImGui.Separator();
            ImGui.Checkbox($"接收到通知时，自动传送到最近的以太之光", ref P.Config.AutoVisitTeleportEnabled);
            ImGuiEx.PluginAvailabilityIndicator([new("TeleporterPlugin", "Teleporter")]);
            ImGuiEx.PluginAvailabilityIndicator([new("Lifestream")]);
            ImGui.Checkbox("允许跨界传送", ref P.Config.AutoVisitCrossWorld);
            ImGuiEx.PluginAvailabilityIndicator([new("TeleporterPlugin", "Teleporter"), new("Lifestream")]);
            ImGuiEx.PluginAvailabilityIndicator([new("Lifestream")]);
            ImGui.Checkbox("允许超域传送", ref P.Config.AutoVisitCrossDC);
            ImGuiEx.PluginAvailabilityIndicator([new("TeleporterPlugin", "Teleporter"), new("Lifestream")]);
            ImGuiEx.PluginAvailabilityIndicator([new("Lifestream")]);
            ImGuiEx.TreeNodeCollapsingHeader($"黑名单服务器 (当前有 {P.Config.WorldBlacklist.Count} 个在黑名单)###blworlds", DrawWorldBlacklist);
        })

        .Section("触发过滤")
        .Widget(() =>
        {
            foreach(var rank in Enum.GetValues<Rank>())
            {
                if (rank == Rank.Unknown) continue;
                ImGui.PushID($"{rank}");
                if (!P.Config.AutoVisitExpansionsBlacklist.TryGetValue(rank, out var list))
                {
                    list = [];
                    P.Config.AutoVisitExpansionsBlacklist[rank] = list;
                }
                ImGuiEx.CollectionCheckbox($"{rank}", Enum.GetValues<Expansion>(), list, true);
                ImGui.Indent();
                foreach(var ex in Enum.GetValues<Expansion>())
                {
                    if(ex == Expansion.Unknown) continue;
                    ImGuiEx.CollectionCheckbox($"{ex}", ex, list, true);
                }
                ImGui.Unindent();
                ImGui.PopID();
            }
        })

        .Draw();
    }

    void DrawWorldBlacklist()
    {
        ImGuiEx.TextWrapped($"以下所选的服务器不会启用自动传送，但是您仍然可以点击聊天中的传送链接手动前往。");
        foreach(var r in Enum.GetValues<ExcelWorldHelper.Region>())
        {
            ImGuiEx.CollectionCheckbox($"地区 {r}", ExcelWorldHelper.GetPublicWorlds(r).Select(x => x.RowId), P.Config.WorldBlacklist);
            ImGui.Indent();
            foreach(var dc in ExcelWorldHelper.GetDataCenters(r))
            {
                ImGuiEx.CollectionCheckbox($"{dc.Name} 数据中心", ExcelWorldHelper.GetPublicWorlds(dc.RowId).Select(x => x.RowId), P.Config.WorldBlacklist);
                ImGui.Indent();
                foreach(var w in ExcelWorldHelper.GetPublicWorlds(dc.RowId))
                {
                    ImGuiEx.CollectionCheckbox($"{w.Name}", w.RowId, P.Config.WorldBlacklist);
                }
                ImGui.Unindent();
            }
            ImGui.Unindent();
        }
    }
}
