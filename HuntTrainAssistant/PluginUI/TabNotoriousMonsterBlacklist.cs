using System;
using System.Linq;
using System.Collections.Generic;
using HuntTrainAssistant;

namespace HuntTrainAssistant.PluginUI;

public class TabNotoriousMonsterBlacklist
{
    private string Filter = "";
    private bool OnlySelected = false;

    private static readonly List<string> ExpansionOrder = new()
    {
        "ARealmReborn",
        "Heavensward",
        "Stormblood",
        "Shadowbringers",
        "Endwalker",
        "Dawntrail",
    };

    private static readonly Dictionary<string, string> ExpansionDisplayName = new()
    {
        ["ARealmReborn"] = "重生之境",
        ["Heavensward"] = "苍穹之禁城",
        ["Stormblood"] = "红莲之狂潮",
        ["Shadowbringers"] = "暗影之逆焰",
        ["Endwalker"] = "晓月之终途",
        ["Dawntrail"] = "金曦之遗辉",
    };

    public void Draw()
    {
        ImGuiEx.Text("这些 S 级狩猎怪将被忽略");
        ImGui.SetNextItemWidth(200f);
        ImGui.InputTextWithHint("##sfltr", "搜索", ref Filter, 100);
        ImGui.SameLine();
        ImGui.Checkbox("仅显示选定的 S 级狩猎怪", ref OnlySelected);

        foreach (var key in ExpansionOrder)
        {
            if (!SRankNotoriousMonster.Data.TryGetValue(key, out var dict))
                continue;

            var header = ExpansionDisplayName.TryGetValue(key, out var cn)
                ? cn
                : key;

            if (ImGui.TreeNode(header))
            {
                foreach (var kv in dict)
                {
                    uint npcId = kv.Key;
                    string name = kv.Value;

                    if (!string.IsNullOrEmpty(Filter) &&
                        !name.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (OnlySelected && !P.Config.SRankBlacklist.Contains(npcId))
                        continue;

                    ImGuiEx.CollectionCheckbox($"{name}##{npcId}", npcId, P.Config.SRankBlacklist);
                }

                ImGui.TreePop();
            }
        }
    }
}
