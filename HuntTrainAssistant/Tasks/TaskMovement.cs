using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using HuntTrainAssistant;
using Lumina.Excel.Sheets;

namespace HuntTrainAssistant.TaskMovements;

public static class TaskMovement
{
    private static readonly VnavmeshIPC Nav = new();

    public static SeString White(string text)
    {
        return new SeStringBuilder()
            .Add(new UIForegroundPayload(1))   // 白色
            .Append(text)
            .Add(RawPayload.LinkTerminator) // end
            .Build();
    }

    public static void PrintRouteMessage(SeString msg)
    {
        var builder = new SeStringBuilder()
            .Append("\ue078 ") // H 图标
            .Append(msg);

        Svc.Chat.Print(builder.Build());
    }

    private static SeString BuildSRankMapLink(IBattleNpc target)
    {
        uint territoryId = Svc.ClientState.TerritoryType;
        uint mapId = Svc.ClientState.MapId;

        var map = Svc.Data.GetExcelSheet<Map>()?.GetRow(mapId);
        if (map == null)
            return White("(坐标转换失败)");

        var mapVec = PositionHelper.WorldToMap(
            new Vector2(target.Position.X, target.Position.Z),
            map.Value
        );

        return SeString.CreateMapLink(
            territoryId,
            mapId,
            mapVec.X,
            mapVec.Y,
            0.05f
        );
    }

    public static void EnqueueMoveToSRank()
    {
        // 获取所有 S 怪 NameId
        var allSRankIds = SRankNotoriousMonster.Data
            .SelectMany(x => x.Value.Keys)
            .ToHashSet();

        // 找到当前地图 S 怪（不排除黑名单）
        var allSRanksOnMap = Svc.Objects
            .Where(o => o is IBattleNpc bn && allSRankIds.Contains(bn.NameId))
            .Cast<IBattleNpc>()
            .Where(IsValidNpc) // 过滤无效对象
            .ToList();

        // 当前地图没有 S 怪
        if (allSRanksOnMap.Count == 0)
        {
            PrintRouteMessage(White("当前地图未发现 S 级狩猎怪"));
            return;
        }

        // 检查是否存在黑名单中的 S 怪
        var blacklistedSRanks = allSRanksOnMap
            .Where(bn => P.Config.SRankBlacklist.Contains(bn.NameId))
            .ToList();

        // 存在 S 怪，但在黑名单
        if (blacklistedSRanks.Count > 0)
        {
            var bn = blacklistedSRanks.First();
            PrintRouteMessage(White($"发现 S 级狩猎怪，但在黑名单中：{bn.Name}"));
            return;
        }

        // 过滤黑名单，选择最近的 S 怪
        var candidates = allSRanksOnMap
            .Where(bn => !P.Config.SRankBlacklist.Contains(bn.NameId))
            .Where(IsValidNpc)
            .ToList();

        if (candidates.Count == 0)
        {
            PrintRouteMessage(White("未找到可寻路的 S 级狩猎怪"));
            return;
        }

        var playerPos = Svc.Objects.LocalPlayer.Position;
        var target = candidates
            .OrderBy(bn => Vector3.Distance(bn.Position, playerPos))
            .FirstOrDefault();

        if (target == null || !IsValidNpc(target))
        {
            PrintRouteMessage(White("目标 S 怪已消失，无法寻路"));
            return;
        }

        // 修正目标点（是否落地由配置控制）
        var targetPos = FixPosition(target.Position);
        if (targetPos == null)
        {
            PrintRouteMessage(White("寻路失败: 目标位置不在导航网格上"));
            return;
        }

        // 安全距离（3D）
        if (P.Config.UseSafeStopDistance)
        {
            var fixedTarget = FixPosition(target.Position) ?? target.Position;

            var dir = Vector3.Normalize(fixedTarget - playerPos);
            var safePos = fixedTarget - dir * P.Config.SafeStopDistance;

            targetPos = FixPosition(safePos) ?? safePos;
        }

        // 自动寻路
        Nav.PathfindAndMoveTo(targetPos.Value, CanFly());

        // 输出聊天信息
        var dist = Vector3.Distance(playerPos, target.Position);
        var ground = P.Config.SnapDestinationToGround ? "是" : "否";
        var mapLink = BuildSRankMapLink(target);

        var msg = new SeStringBuilder()
            .Append(White($"寻路到 S 级狩猎怪: {target.Name}\n"))
            .Append(White("位置: "))
            .Append(mapLink)
            .Append(White($"\n距离: {dist:F1}"))
            .Append(White($"\n使用安全距离: {(P.Config.UseSafeStopDistance ? P.Config.SafeStopDistance.ToString("F1") : "否")}"))
            .Append(White($"\n寻路到地面: {ground}"))
            .Build();

        PrintRouteMessage(msg);

        PluginLog.Information($"Moving to S-rank → {targetPos.Value}");
    }

    private static Vector3? FixPosition(Vector3 pos)
    {
        if (!P.Config.SnapDestinationToGround)
            return pos;

        var nearest = Nav.NearestPoint(pos, 5f, 10000f);
        if (nearest != null)
            return nearest.Value;

        for (float extent = 10; extent <= 200; extent += 10)
        {
            nearest = Nav.NearestPoint(pos, extent, 10000f);
            if (nearest != null)
                return nearest.Value;
        }

        if (Math.Abs(pos.Y - Svc.Objects.LocalPlayer.Position.Y) < 30f)
        {
            var floor = Nav.PointOnFloor(pos, true, 10f);
            if (floor != null)
                return floor.Value;
        }

        return pos;
    }

    public static bool CanFly()
    {
        return Control.GetFlightAllowedStatus() == 0 || Svc.Condition[ConditionFlag.InFlight];
    }

    private static bool IsValidNpc(IBattleNpc npc)
    {
        return npc != null
            && npc.GameObjectId != 0
            && npc.IsTargetable
            && npc.Position != default
            && npc.NameId != 0;
    }

}
