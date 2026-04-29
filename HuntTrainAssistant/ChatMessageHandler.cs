using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.CSExtensions;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using Lumina.Excel.Sheets;

namespace HuntTrainAssistant;

internal unsafe static class ChatMessageHandler
{
    internal static ArrivalData LastMessageLoc = null;

    internal static void Chat_ChatMessage(IHandleableChatMessage msg)
    {
        var type = msg.LogKind;
        var sender = msg.Sender;
        var message = msg.Message;
        var payloads = message.Payloads;

        var conductorNames = P.Config.Conductors.Select(x => x.Name).ToList();

        if (Svc.Objects.LocalPlayer != null
            && P.Config.Enabled
            && ((type.EqualsAny(XivChatType.Shout, XivChatType.Yell, XivChatType.Say,
                                XivChatType.CustomEmote, XivChatType.StandardEmote, XivChatType.Echo)
                 && Utils.IsInHuntingTerritory())
                || P.Config.Debug))
        {
            bool isMapLink = false;

            bool isConductorMessage =
                (P.Config.Debug && (sender.ToString().Contains(Svc.Objects.LocalPlayer.Name.ToString())
                                    || type == XivChatType.Echo))
                || (TryDecodeSender(sender, out var s) && conductorNames.Contains(s.Name));

            foreach (var x in payloads)
            {
                if (x is MapLinkPayload m)
                {
                    isMapLink = true;

                    if (isConductorMessage)
                    {
                        var nearestAetheryte = MapManager.GetNearestAetheryte(m);
                        if (nearestAetheryte == null)
                            break;

                        if (Utils.IsInHuntingTerritory() || P.Config.Debug)
                        {
                            if (P.Config.AutoTeleport)
                            {
                                if (m.TerritoryType.RowId != Svc.ClientState.TerritoryType)
                                {
                                    P.TeleportTo = ArrivalData.CreateOrNull(
                                        nearestAetheryte,
                                        m.TerritoryType.RowId,
                                        P.Config.AutoSwitchInstanceToOne ? 1 : 0);

                                    Utils.DelayTeleport();
                                    Notify.Info("正在激活自动传送");
                                }
                                else if (Utils.CanAutoInstanceSwitch()
                                         && P.Config.AutoSwitchInstanceTwoRanks
                                         && S.LifestreamIPC.GetCurrentInstance() < S.LifestreamIPC.GetNumberOfInstances())
                                {
                                    P.TeleportTo = ArrivalData.CreateOrNull(
                                        nearestAetheryte,
                                        m.TerritoryType.RowId,
                                        S.LifestreamIPC.GetCurrentInstance() + 1);

                                    Utils.DelayTeleport();
                                    PluginLog.Debug($"Auto-teleporting because of two A ranks killed ({P.KilledARanks.Print()})");
                                    Notify.Info("正在激活自动传送(切换分线)");
                                }
                            }

                            if (P.Config.AutoOpenMap)
                            {
                                var flag = AgentMap.Instance()->FlagMapMarker;

                                if (AgentMap.Instance()->IsFlagMarkerSet != false
                                    && flag.TerritoryId == m.TerritoryType.RowId)
                                {
                                    if (Svc.Data.GetExcelSheet<Map>()
                                            .TryGetFirst(x => x.TerritoryType.RowId == m.TerritoryType.RowId,
                                                out var place))
                                    {
                                        var pos = new Vector2(m.RawX / 1000, m.RawY / 1000);
                                        var distance = Vector2.Distance(new(flag.XFloat, flag.YFloat), pos);

                                        PluginLog.Information($"Distance between map marker and linked position is {distance}");

                                        if (distance > 10 || !P.Config.NoDuplicateFlags)
                                            Svc.GameGui.OpenMapWithMapLink(m);
                                    }
                                }
                                else
                                {
                                    Svc.GameGui.OpenMapWithMapLink(m);
                                }

                                var a = MapManager.GetNearestAetheryte(m);
                                if (a != null)
                                    LastMessageLoc = ArrivalData.CreateOrNull(a, m.TerritoryType.RowId, 0);
                            }
                        }
                    }

                    break;
                }
            }

            if (P.Config.SuppressChatOtherPlayers
                && !isMapLink
                && !isConductorMessage
                && conductorNames.Count > 0)
            {
                msg.PreventOriginal(); // 新版：屏蔽消息
            }

            if (isConductorMessage)
            {
                var builder = new SeStringBuilder();
                builder.AddUiForeground(578);

                foreach (var x in payloads)
                    builder.Add(x);

                builder.AddUiForegroundOff();

                msg.Message = builder.Build(); // 新版：修改消息

                if (P.Config.AudioAlert)
                {
                    if (Framework.Instance()->WindowInactive || !P.Config.AudioAlertOnlyMinimized)
                    {
                        if (EzThrottler.Throttle("AudioPlay", P.Config.AudioThrottle))
                        {
                            S.Notificator.PlaySound(
                                P.Config.AudioAlertPath,
                                P.Config.AudioAlertVolume,
                                false,
                                P.Config.AudioAlertOnlyMinimized);
                        }
                    }
                }

                if (Framework.Instance()->WindowInactive || P.Config.Debug)
                {
                    if (P.Config.TrayNotification)
                        S.Notificator.DisplayTrayNotification("[HTA] Conductor's message", msg.Message.GetText());

                    if (P.Config.FlashTaskbar)
                        S.Notificator.FlashTaskbarIcon();
                }

                if (P.Config.ExecuteMacroOnFlag
                    && isMapLink
                    && P.Config.MacroIndex.InRange(0, RaptureMacroModule.Instance()->Shared.Length))
                {
                    new TickScheduler(() =>
                    {
                        var macro = RaptureMacroModule.Instance()->Shared[P.Config.MacroIndex];
                        if (macro.IsNotEmpty())
                            RaptureShellModule.Instance()->ExecuteMacro(&macro);
                        else
                            PluginLog.Warning("Selected macro was empty");
                    });
                }
            }
        }
    }
}
