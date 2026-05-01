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

        var finalPos = SmartDestination.ComputeFinalDestination(
            target.Position,
            playerPos,
            fly
        );

        TaskMount.EnqueueIfEnabled();
        Nav.PathfindAndMoveTo(finalPos, fly);
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

        var playerPos = Svc.Objects.LocalPlayer.Position;
        var target = allSRanksOnMap
            .OrderBy(bn => Vector3.Distance(bn.Position, Svc.Objects.LocalPlayer.Position))
            .FirstOrDefault();

        if (target == null)
        {
            PrintWhiteMessage("目标 S 怪已消失，无法寻路");
            return;
        }

        bool fly = !P.Config.ForceGroundPathfinding;

        var dist = Vector3.Distance(playerPos, target.Position);
        var mapLink = BuildSRankMapLink(target);
        var random = P.Config.RandomDestinationOffset;
        var randomdistance = P.Config.RandomDestinationOffsetRadius;

        var msg = new SeStringBuilder()
            .AddUiForeground((int)UIColor.White)
            .Append($"\ue078 直接寻路到 S 级狩猎怪: {target.Name}\n")
            .Append("位置: ")
            .Add(mapLink.Payloads)
            .Append($"\n距离: {dist:F1}")
            .Append($"\n飞行寻路: {(fly ? "是" : "否")}")
            .Append($"\n终点偏移: {(random ? $"是\n偏移距离: {randomdistance:F1}" : "否")}")
            .AddUiForegroundOff()
            .Build();

        Svc.Chat.Print(msg);

        TaskMount.EnqueueIfEnabled();
        Nav.PathfindAndMoveTo(target.Position, fly);
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
            .Append($"\n安全距离: {customSafeDistance:F1}")
            .Append($"\n飞行寻路: {(fly ? "是" : "否")}")
            .Append($"\n终点偏移: {(random ? $"是\n偏移距离: {randomdistance:F1}" : "否")}")
            .AddUiForegroundOff()
            .Build();

        Svc.Chat.Print(msg);

        var finalPos = SmartDestination.ComputeFinalDestinationForCustomSafeDistance(
            target.Position,
            playerPos,
            customSafeDistance,
            fly
        );

        TaskMount.EnqueueIfEnabled();
        Nav.PathfindAndMoveTo(finalPos, fly);
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
        return ComputeUnifiedDestination(monsterPos, playerPos, safeDistance, isFlying);
    }

    public static Vector3 ComputeFinalDestinationForCustomSafeDistance(
        Vector3 monsterPos,
        Vector3 playerPos,
        float safeDistance,
        bool isFlying
    )
    {
        return ComputeUnifiedDestination(monsterPos, playerPos, safeDistance, isFlying);
    }

    private static Vector3 ComputeUnifiedDestination(Vector3 monsterPos, Vector3 playerPos, float safeDistance, bool isFlying)
    {
        var dir = Vector3.Normalize(new Vector3(monsterPos.X - playerPos.X, 0, monsterPos.Z - playerPos.Z));
        if (dir == Vector3.Zero)
            dir = Vector3.UnitX;

        bool safeDistanceEnabled = P.Config.UseSafeStopDistance && safeDistance > 0f;
        if (!safeDistanceEnabled)
            safeDistance = 0f;

        var targetFlat = monsterPos - dir * safeDistance;
        var basePos = new Vector3(targetFlat.X, 1024f, targetFlat.Z);

        var point = SnapToFloor(basePos);

        const float cliffThreshold = 40f;

        if (isFlying && safeDistanceEnabled)
        {
            float distToMonster = Vector3.Distance(
                new(point.X, monsterPos.Y, point.Z),
                new(monsterPos.X, monsterPos.Y, monsterPos.Z)
            );

            if (distToMonster > safeDistance + 1f)
            {
                const float adjustStep = 1.5f;
                const int adjustMax = 30;

                for (int i = 1; i <= adjustMax; i++)
                {
                    float dist = Math.Max(0.1f, distToMonster - i * adjustStep);

                    var flat = monsterPos - dir * dist;
                    var baseP = new Vector3(flat.X, 1024f, flat.Z);

                    var corrected = SnapToFloor(baseP);

                    float correctedDist = Vector3.Distance(
                        new(corrected.X, monsterPos.Y, corrected.Z),
                        new(monsterPos.X, monsterPos.Y, monsterPos.Z)
                    );

                    if (MathF.Abs(correctedDist - safeDistance) <= 1f)
                    {
                        point = corrected;
                        break;
                    }
                }
            }
        }

        if (MathF.Abs(point.Y - monsterPos.Y) > cliffThreshold)
        {
            if (!safeDistanceEnabled)
                return monsterPos;

            const float step = 3f;
            const int maxAttempts = 20;

            for (int i = 1; i <= maxAttempts; i++)
            {
                float dist = safeDistance + i * step;

                var flat = monsterPos - dir * dist;
                var baseP = new Vector3(flat.X, 1024f, flat.Z);

                var corrected = SnapToFloor(baseP);

                if (MathF.Abs(corrected.Y - monsterPos.Y) <= 20f)
                {
                    TaskMovement.PrintWhiteMessage("悬崖修正: 向外");
                    return corrected;
                }
            }

            for (int i = 1; i <= maxAttempts; i++)
            {
                float dist = Math.Max(0.1f, safeDistance - i * step);

                var flat = monsterPos - dir * dist;
                var baseP = new Vector3(flat.X, 1024f, flat.Z);

                var corrected = SnapToFloor(baseP);

                if (MathF.Abs(corrected.Y - monsterPos.Y) <= 20f)
                {
                    TaskMovement.PrintWhiteMessage("悬崖修正: 向内");
                    return corrected;
                }
            }

            TaskMovement.PrintWhiteMessage("悬崖修正失败，寻路中止。");
            return playerPos;
        }

        if (P.Config.RandomDestinationOffset && P.Config.RandomDestinationOffsetRadius > 0f)
        {
            float radius = Math.Max(0f, P.Config.RandomDestinationOffsetRadius);

            for (int i = 0; i < 10; i++)
            {
                float angle = Random.Shared.NextSingle() * MathF.Tau;
                float dist = Random.Shared.NextSingle() * radius;

                var offset = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * dist;
                var candidate = SnapToFloor(basePos + offset);

                if (MathF.Abs(candidate.Y - monsterPos.Y) > cliffThreshold)
                    continue;

                if (!safeDistanceEnabled)
                    return candidate;

                if (Vector3.Distance(new(candidate.X, monsterPos.Y, candidate.Z),
                                     new(monsterPos.X, monsterPos.Y, monsterPos.Z))
                    >= safeDistance - 1f)
                {
                    return candidate;
                }
            }
        }

        return point;
    }

    private static Vector3 SnapToFloor(Vector3 pos)
    {
        var floor = Nav.PointOnFloor(pos, true, 5f);
        if (floor != null)
            return floor.Value;

        var reachable = Nav.NearestPointReachable(pos, 20f, 10000f);
        if (reachable != null)
            return reachable.Value;

        return pos;
    }
}
