using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using HuntTrainAssistant;
using HuntTrainAssistant.Tasks;
using Lumina.Excel.Sheets;

namespace HuntTrainAssistant.TaskMovements;

public static class TaskMovement
{
    private static readonly VnavmeshIPC Nav = new();

    public static SeString White(string text)
    {
        return new SeStringBuilder()
            .Add(new UIForegroundPayload(1))
            .Append(text)
            .Add(RawPayload.LinkTerminator)
            .Build();
    }

    public static void PrintRouteMessage(SeString msg)
    {
        var builder = new SeStringBuilder()
            .Append("\ue078 ")
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
        var allSRankIds = SRankNotoriousMonster.Data
            .SelectMany(x => x.Value.Keys)
            .ToHashSet();

        var allSRanksOnMap = Svc.Objects
            .Where(o => o is IBattleNpc bn && allSRankIds.Contains(bn.NameId))
            .Cast<IBattleNpc>()
            .Where(IsValidNpc)
            .ToList();

        if (allSRanksOnMap.Count == 0)
        {
            PrintRouteMessage(White("当前地图未发现 S 级狩猎怪"));
            return;
        }

        var blacklisted = allSRanksOnMap
            .Where(bn => P.Config.SRankBlacklist.Contains(bn.NameId))
            .ToList();

        if (blacklisted.Count > 0)
        {
            var bn = blacklisted.First();
            PrintRouteMessage(White($"发现 S 级狩猎怪，但在黑名单中：{bn.Name}"));
            return;
        }

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

        // 计算最终终点（SmartDestination）
        var finalPos = SmartDestination.ComputeFinalDestination(target.Position, playerPos);

        // 上坐骑
        TaskMount.EnqueueIfEnabled();

        // 默认飞行寻路
        bool fly = !P.Config.ForceGroundPathfinding;

        Nav.PathfindAndMoveTo(finalPos, fly);

        // 输出聊天信息
        var dist = Vector3.Distance(playerPos, target.Position);
        var mapLink = BuildSRankMapLink(target);

        var msg = new SeStringBuilder()
            .Append(White($"寻路到 S 级狩猎怪: {target.Name}\n"))
            .Append(White("位置: "))
            .Append(mapLink)
            .Append(White($"\n距离: {dist:F1}"))
            .Append(White($"\n安全距离: {(P.Config.UseSafeStopDistance ? P.Config.SafeStopDistance.ToString("F1") : "否")}"))
            .Append(White($"\n终点落地: {(P.Config.SnapDestinationToGround ? "是" : "否")}"))
            .Append(White($"\n飞行寻路: {(fly ? "是" : "否")}"))
            .Build();

        PrintRouteMessage(msg);

        PluginLog.Information($"[HuntTrainAssistant] Moving to S-rank → {finalPos} (fly={fly})");
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

public static class SmartDestination
{
    private static readonly VnavmeshIPC Nav = new();

    public static Vector3 ComputeFinalDestination(Vector3 targetPos, Vector3 playerPos)
    {
        targetPos = FixHighPlatform(targetPos, playerPos);

        if (P.Config.UseSafeStopDistance)
            targetPos = ApplySafeDistance(targetPos, playerPos);

        targetPos = FixWallAndObstacles(targetPos, playerPos);

        return targetPos;
    }

    private static Vector3 FixHighPlatform(Vector3 targetPos, Vector3 playerPos)
    {
        float heightDiff = targetPos.Y - playerPos.Y;

        bool isHigh = heightDiff > 6f;
        if (!isHigh)
            return targetPos;

        if (TaskMovement.CanFly())
            return targetPos;

        var edge = Nav.NearestPoint(targetPos, 10f, 10000f);
        if (edge != null)
            return edge.Value;

        return targetPos;
    }

    private static Vector3 ApplySafeDistance(Vector3 targetPos, Vector3 playerPos)
    {
        var dir = Vector3.Normalize(targetPos - playerPos);
        return targetPos - dir * P.Config.SafeStopDistance;
    }

    private static Vector3 FixWallAndObstacles(Vector3 targetPos, Vector3 playerPos)
    {
        var meshPoint = Nav.NearestPoint(targetPos, 50f, 10000f);
        if (meshPoint == null)
            return targetPos;

        var retreatDir = Vector3.Normalize(meshPoint.Value - playerPos);
        var retreatPoint = meshPoint.Value - retreatDir * 2f;

        var final = Nav.NearestPoint(retreatPoint, 20f, 10000f);
        if (final == null)
            final = meshPoint.Value;

        // raycast 检测
        if (Nav.Raycast != null && Nav.Raycast(playerPos, final.Value))
        {
            var safer = final.Value - retreatDir * 2f;

            var saferFinal = Nav.NearestPoint(safer, 20f, 10000f);
            if (saferFinal != null && !Nav.Raycast(playerPos, saferFinal.Value))
                return saferFinal.Value;

            return meshPoint.Value;
        }

        return final.Value;
    }
}
