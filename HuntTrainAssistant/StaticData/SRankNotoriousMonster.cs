using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System.Collections.Generic;

public static class SRankNotoriousMonster
{
    public static readonly Dictionary<string, Dictionary<uint, string>> Data
        = new()
        {
            ["Dawntrail"] = new()
            {
                { 13360u, "厌忌之人 奇里格" },
                { 13444u, "伊努索奇" },
                { 12754u, "内尤佐缇" },
                { 13399u, "山谢亚" },
                { 13437u, "天气预报机器人" },
                { 13156u, "先驱勇士 阿提卡斯" },
                { 13406u, "水晶化身之王" }
            },

            ["Endwalker"] = new()
            {
                { 10619u, "阿姆斯特朗" },
                { 10617u, "布弗鲁" },
                { 10615u, "克尔" },
                { 10622u, "狭缝" },
                { 10621u, "俄菲翁尼厄斯" },
                { 10620u, "沉思之物" },
                { 10618u, "颇胝迦" }
            },

            ["Shadowbringers"] = new()
            {
                { 8653u, "阿格拉俄珀" },
                { 8910u, "得到宽恕的炫学" },
                { 8915u, "得到宽恕的叛乱" },
                { 8895u, "顾尼图" },
                { 8890u, "伊休妲" },
                { 8900u, "多智兽" },
                { 8905u, "戾虫" }
            },

            ["Stormblood"] = new()
            {
                { 5984u, "巨大鳐" },
                { 5989u, "盐和光" },
                { 5988u, "爬骨怪龙" },
                { 5987u, "优昙婆罗花" },
                { 5985u, "伽马" },
                { 5986u, "兀鲁忽乃朝鲁" }
            },

            ["Heavensward"] = new()
            {
                { 4374u, "凯撒贝希摩斯" },
                { 4378u, "极乐鸟" },
                { 4375u, "神穆尔鸟" },
                { 4377u, "刚德瑞瓦" },
                { 4376u, "苍白骑士" },
                { 4380u, "卢克洛塔" }
            },

            ["ARealmReborn"] = new()
            {
                { 2962u, "护土精灵" },
                { 2963u, "咕尔呱洛斯" },
                { 2964u, "伽洛克" },
                { 2965u, "火愤牛" },
                { 2966u, "南迪" },
                { 2967u, "牛头黑神" },
                { 2953u, "雷德罗巨蛇" },
                { 2954u, "乌尔伽鲁" },
                { 2955u, "夺心魔" },
                { 2956u, "千竿口花希达" },
                { 2958u, "布隆特斯" },
                { 2960u, "努纽努维" },
                { 2961u, "蚓螈巨虫" },
                { 2957u, "虚无探索者" },
                { 2959u, "巴拉乌尔" },
                { 2968u, "萨法特" },
                { 2969u, "阿格里帕" },
                //{ 13108u, "故障航空机" } // Debug 遗产之地 雷转质广场
            }
        };

    public static bool TryGetName(string expansion, uint id, out string name)
    {
        name = null;

        if (!Data.TryGetValue(expansion, out var dict))
            return false;

        return dict.TryGetValue(id, out name);
    }
}
