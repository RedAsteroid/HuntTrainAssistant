using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using HuntTrainAssistant;
using HuntTrainAssistant.Tasks;
using Lumina.Excel.Sheets;
using System;
using UIColor = ECommons.ChatMethods.UIColor;

namespace HuntTrainAssistant.TaskMovements;

public static class TaskMovement
{
    public static readonly VnavmeshIPC Nav = new();

    public static void PrintWhiteMessage(SeString msg)
    {
        var builder = new SeStringBuilder()
            .AddUiForeground((int)UIColor.White)
            .Append("\ue078 ")
            .Append(msg)
            .AddUiForegroundOff();

        Svc.Chat.Print(builder.Build());
    }

    private static SeString BuildSRankMapLink(IBattleNpc target)
    {
        uint territoryId = Svc.ClientState.TerritoryType;
        uint mapId = Svc.ClientState.MapId;

        var map = Svc.Data.GetExcelSheet<Map>()?.GetRow(mapId);
        if (map == null)
            return ("(坐标转换失败)");

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
            PrintWhiteMessage("当前地图未发现 S 级狩猎怪");
            return;
        }

        var blacklisted = allSRanksOnMap
            .Where(bn => P.Config.SRankBlacklist.Contains(bn.NameId))
            .ToList();

        if (blacklisted.Count > 0)
        {
            var bn = blacklisted.First();
            PrintWhiteMessage($"发现 S 级狩猎怪，但在黑名单中：{bn.Name}");
            return;
        }

        var candidates = allSRanksOnMap
            .Where(bn => !P.Config.SRankBlacklist.Contains(bn.NameId))
            .Where(IsValidNpc)
            .ToList();

        if (candidates.Count == 0)
        {
            PrintWhiteMessage("未找到可寻路的 S 级狩猎怪");
            return;
        }

        var playerPos = Svc.Objects.LocalPlayer.Position;
        var target = candidates
            .OrderBy(bn => Vector3.Distance(bn.Position, playerPos))
            .FirstOrDefault();

        if (target == null || !IsValidNpc(target))
        {
            PrintWhiteMessage("目标 S 怪已消失，无法寻路");
            return;
        }

        bool fly = !P.Config.ForceGroundPathfinding;

        var finalPos = SmartDestination.ComputeFinalDestination(
            target.Position,
            playerPos,
            fly
        );

        TaskMount.EnqueueIfEnabled();
        Nav.PathfindAndMoveTo(finalPos, fly);

        var dist = Vector3.Distance(playerPos, target.Position);
        var mapLink = BuildSRankMapLink(target);
        var random = P.Config.RandomDestinationOffset;
        var randomdistance = P.Config.RandomDestinationOffsetRadius;

        var msg = new SeStringBuilder()
            .AddUiForeground((int)UIColor.White)
            .Append($"\ue078 寻路到 S 级狩猎怪: {target.Name}\n")
            .Append("位置: ")
            .Add(mapLink.Payloads)
            .Append($"\n距离: {dist:F1}")
            .Append($"\n安全距离: {(P.Config.UseSafeStopDistance ? P.Config.SafeStopDistance.ToString("F1") : "否")}")
            .Append($"\n飞行寻路: {(fly ? "是" : "否")}")
            .Append($"\n终点偏移: {(random ? $"是\n偏移距离: {randomdistance:F1}" : "否")}")
            .AddUiForegroundOff()
            .Build();

        Svc.Chat.Print(msg);
    }

    public static void EnqueueMoveToSRankDirect()
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
            PrintWhiteMessage("当前地图未发现 S 级狩猎怪");
            return;
        }

        var target = allSRanksOnMap
            .OrderBy(bn => Vector3.Distance(bn.Position, Svc.Objects.LocalPlayer.Position))
            .FirstOrDefault();

        if (target == null)
        {
            PrintWhiteMessage("目标 S 怪已消失，无法寻路");
            return;
        }

        bool fly = !P.Config.ForceGroundPathfinding;

        TaskMount.EnqueueIfEnabled();
        Nav.PathfindAndMoveTo(target.Position, fly);

        var mapLink = BuildSRankMapLink(target);
        var random = P.Config.RandomDestinationOffset;
        var randomdistance = P.Config.RandomDestinationOffsetRadius;

        var msg = new SeStringBuilder()
            .AddUiForeground((int)UIColor.White)
            .Append($"\ue078 直接寻路到 S 级狩猎怪: {target.Name}\n")
            .Append("位置: ")
            .Add(mapLink.Payloads)
            .Append($"\n飞行寻路: {(fly ? "是" : "否")}")
            .Append($"\n终点偏移: {(random ? $"是\n偏移距离: {randomdistance:F1}" : "否")}")
            .AddUiForegroundOff()
            .Build();

        Svc.Chat.Print(msg);
    }

    public static void EnqueueMoveToSRankWithCustomSafeDistance(float customSafeDistance)
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
            PrintWhiteMessage("当前地图未发现 S 级狩猎怪");
            return;
        }

        var target = allSRanksOnMap
            .OrderBy(bn => Vector3.Distance(bn.Position, Svc.Objects.LocalPlayer.Position))
            .FirstOrDefault();

        if (target == null)
        {
            PrintWhiteMessage("目标 S 怪已消失，无法寻路");
            return;
        }

        var playerPos = Svc.Objects.LocalPlayer.Position;

        bool fly = !P.Config.ForceGroundPathfinding;

        var finalPos = SmartDestination.ComputeFinalDestinationForCustomSafeDistance(
            target.Position,
            playerPos,
            customSafeDistance,
            fly
        );

        TaskMount.EnqueueIfEnabled();
        Nav.PathfindAndMoveTo(finalPos, fly);

        var mapLink = BuildSRankMapLink(target);
        var random = P.Config.RandomDestinationOffset;
        var randomdistance = P.Config.RandomDestinationOffsetRadius;

        var msg = new SeStringBuilder()
            .AddUiForeground((int)UIColor.White)
            .Append($"\ue078 寻路到 S 级狩猎怪: {target.Name}\n")
            .Append("位置: ")
            .Add(mapLink.Payloads)
            .Append($"\n安全距离: {customSafeDistance:F1}")
            .Append($"\n飞行寻路: {(fly ? "是" : "否")}")
            .Append($"\n终点偏移: {(random ? $"是\n偏移距离: {randomdistance:F1}" : "否")}")
            .AddUiForegroundOff()
            .Build();

        Svc.Chat.Print(msg);
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
    private static VnavmeshIPC Nav => TaskMovement.Nav;

    public static Vector3 ComputeFinalDestination(Vector3 monsterPos, Vector3 playerPos, bool isFlying)
    {
        float safeDistance = P.Config.UseSafeStopDistance ? P.Config.SafeStopDistance : 0f;

        return isFlying
            ? ComputeFlyingGroundDestination(monsterPos, playerPos, safeDistance)
            : ComputeGroundDestination(monsterPos, playerPos, safeDistance);
    }

    public static Vector3 ComputeFinalDestinationForCustomSafeDistance(
        Vector3 monsterPos,
        Vector3 playerPos,
        float safeDistance,
        bool isFlying
    )
    {
        return isFlying
            ? ComputeFlyingGroundDestination(monsterPos, playerPos, safeDistance)
            : ComputeGroundDestination(monsterPos, playerPos, safeDistance);
    }

    private static Vector3 ComputeFlyingGroundDestination(Vector3 monsterPos, Vector3 playerPos, float safeDistance)
    {
        var flatDir = new Vector3(monsterPos.X - playerPos.X, 0, monsterPos.Z - playerPos.Z);
        if (flatDir == Vector3.Zero)
            flatDir = Vector3.UnitX;

        var dir = Vector3.Normalize(flatDir);
        var targetFlat = monsterPos - dir * safeDistance;

        var basePos = new Vector3(targetFlat.X, monsterPos.Y, targetFlat.Z);

        var reachable = Nav.NearestPointReachable(basePos, 20f, 10000f);
        Vector3 point = reachable ?? basePos;

        if (Math.Abs(point.Y - basePos.Y) > 6f)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = i * (MathF.Tau / 12f);
                var offset = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * 3f;

                var candidateBase = basePos + offset;
                var candidateReachable = Nav.NearestPointReachable(candidateBase, 20f, 10000f);
                if (candidateReachable == null)
                    continue;

                var candidate = candidateReachable.Value;

                if (Math.Abs(candidate.Y - basePos.Y) <= 6f)
                {
                    point = candidate;
                    break;
                }
            }
        }

        if (P.Config.RandomDestinationOffset && P.Config.RandomDestinationOffsetRadius > 0)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = Random.Shared.NextSingle() * MathF.Tau;
                float dist = Random.Shared.NextSingle() * P.Config.RandomDestinationOffsetRadius;

                var offset = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * dist;
                var candidateBase = basePos + offset;

                var candidateReachable = Nav.NearestPointReachable(candidateBase, 20f, 10000f);
                if (candidateReachable == null)
                    continue;

                var candidate = candidateReachable.Value;

                if (Math.Abs(candidate.Y - basePos.Y) > 6f)
                    continue;

                var flatCandidate = new Vector3(candidate.X, monsterPos.Y, candidate.Z);
                var flatMonster = new Vector3(monsterPos.X, monsterPos.Y, monsterPos.Z);

                if (Vector3.Distance(flatCandidate, flatMonster) >= safeDistance - 1f)
                    return candidate;
            }
        }

        return point;
    }

    private static Vector3 ComputeGroundDestination(Vector3 monsterPos, Vector3 playerPos, float safeDistance)
    {
        var safePoint = ApplySafeDistance(monsterPos, playerPos, safeDistance);
        safePoint = FixHighPlatform(safePoint, playerPos);
        safePoint = FixWallAndObstacles(monsterPos, safePoint, safeDistance);

        // 安全距离启用且为0时，不偏移
        if (safeDistance > 0f && P.Config.RandomDestinationOffset)
            safePoint = ApplyRandomOffset(monsterPos, safePoint, safeDistance, P.Config.RandomDestinationOffsetRadius);

        return FinalizeLanding(safePoint);
    }

    private static Vector3 ApplySafeDistance(Vector3 monsterPos, Vector3 playerPos, float safeDistance)
    {
        if (safeDistance <= 0f)
            return monsterPos;

        var dir = Vector3.Normalize(monsterPos - playerPos);
        return monsterPos - dir * safeDistance;
    }

    private static Vector3 FixHighPlatform(Vector3 point, Vector3 playerPos)
    {
        float heightDiff = point.Y - playerPos.Y;

        if (heightDiff <= 6f)
            return point;

        if (TaskMovement.CanFly())
            return point;

        var edge = Nav.NearestPoint(point, 10f, 10000f);
        return edge ?? point;
    }

    private static Vector3 FixWallAndObstacles(Vector3 monsterPos, Vector3 safePoint, float safeDistance)
    {
        var mesh = Nav.NearestPoint(safePoint, 50f, 10000f);
        if (mesh == null)
            return safePoint;

        var point = mesh.Value;

        if (safeDistance <= 0f)
            return IsReachable(point) ? point : safePoint;

        if (IsReachable(point) && DistanceToMonster(point, monsterPos) >= safeDistance - 1f)
            return point;

        return FindSafeLanding(monsterPos, safeDistance);
    }

    private static Vector3 FindSafeLanding(Vector3 monsterPos, float safeDistance)
    {
        const int sampleCount = 32;
        const float bandWidth = 3f;

        for (float r = safeDistance - bandWidth; r <= safeDistance + bandWidth; r += 0.5f)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                float angle = (float)(i * (Math.PI * 2 / sampleCount));

                var dir = new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle));
                var candidate = monsterPos - dir * r;

                var mesh = Nav.NearestPoint(candidate, 20f, 10000f);
                if (mesh == null)
                    continue;

                var point = mesh.Value;

                if (!IsReachable(point))
                    continue;

                float dist = Vector3.Distance(point, monsterPos);
                if (dist < safeDistance - 1f)
                    continue;

                return point;
            }
        }

        var fallback = Nav.NearestPoint(monsterPos, safeDistance + 5f, 10000f);
        if (fallback != null && IsReachable(fallback.Value))
            return fallback.Value;

        return monsterPos;
    }

    private static Vector3 ApplyRandomOffset(Vector3 monsterPos, Vector3 finalPos, float safeDistance, float radius)
    {
        if (radius <= 0f)
            return finalPos;

        for (int i = 0; i < 10; i++)
        {
            float angle = Random.Shared.NextSingle() * MathF.Tau;
            float dist = Random.Shared.NextSingle() * radius;

            var offset = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * dist;
            var candidate = finalPos + offset;

            var mesh = Nav.NearestPoint(candidate, 20f, 10000f);
            if (mesh == null)
                continue;

            var point = mesh.Value;

            if (!IsReachable(point))
                continue;

            if (DistanceToMonster(point, monsterPos) < safeDistance - 1f)
                continue;

            return point;
        }

        return finalPos;
    }

    private static Vector3 FinalizeLanding(Vector3 point)
    {
        var mesh = Nav.NearestPoint(point, 30f, 10000f);
        if (mesh == null)
            return point;

        var p = mesh.Value;

        if (!IsReachable(p))
            return point;

        return p;
    }

    private static bool IsReachable(Vector3 point)
        => Nav.NearestPointReachable(point, 5f, 10000f) != null;

    private static float DistanceToMonster(Vector3 point, Vector3 monsterPos)
        => Vector3.Distance(point, monsterPos);
}
