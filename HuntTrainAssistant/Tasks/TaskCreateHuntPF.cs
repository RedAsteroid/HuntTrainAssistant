using Dalamud.Game.Text.SeStringHandling;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HuntTrainAssistant.Services;
using static FFXIVClientStructs.Havok.Animation.Deform.Skinning.hkaMeshBinding;
using HuntTrainAssistant.TaskMovements;

namespace HuntTrainAssistant.Tasks;
public static unsafe class TaskCreateHuntPF
{
    public static void Enqueue()
    {
        var waitStart = DateTime.UtcNow;

        P.TaskManager.Abort();
        if (!Player.Available)
        {
            Notify.Error("现在不能这么做");
            return;
        }
        if (Player.Object.OnlineStatus.RowId == 26)
        {
            Notify.Error("已经在招募队员！");
            return;
        }
        if (!QuestManager.IsQuestComplete(67099) && !QuestManager.IsQuestComplete(67100) && !QuestManager.IsQuestComplete(67101))
        {
            DuoLog.Error($"怪物狩猎还未解锁，无法创建队员招募。");
            return;
        }
        var cfg = new TaskManagerConfiguration(timeLimitMS: 2000);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd"))
            {
                Chat.Instance.ExecuteCommand("/pfinder");
            }
        }, cfg);
        P.TaskManager.Enqueue(() => !TryGetAddonByName<AtkUnitBase>("LookingForGroup", out _), cfg);
        P.TaskManager.Enqueue(() =>
        {
            Chat.Instance.ExecuteCommand("/pfinder");
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroup>(out var lfg) && IsAddonReady(lfg.Base) && EzThrottler.Throttle("RMOD"))
            {
                S.LFGService.SetComment(P.Config.PfinderString);
                return lfg.RecruitMembersOrDetails();
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.Normal();
                return true;
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.SelectDutyCategory(11);
                return true;
            }
            return false;
        }, cfg);
        // 如果 SE 后续更新修改 UI 结构，游戏会爆炸，但以后再说
        // 1. 启用平均品级
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
                {
                    var node = addon->UldManager.NodeList[25];
                    var compNode = (AtkComponentNode*)node;
                    var checkbox = (AtkComponentCheckBox*)compNode->Component;

                    checkbox->SetChecked(true);
                    return true;
                }
                return false;
            }
            return true;
        }, cfg);
        // 2. 设置平均品级 531(青魔进不来)
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
                {
                    var node = addon->UldManager.NodeList[24];
                    var compNode = (AtkComponentNode*)node;
                    var numeric = (AtkComponentNumericInput*)compNode->Component;

                    // 写入品级 531
                    numeric->SetValue(531);

                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }, cfg);
        // 3. 点击第8槽位
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
                {
                    var button = addon->GetComponentButtonById(58);
                    if (button != null)
                    {
                        ECommons.Automation.Callback.Fire(addon, true, 24, 7, 0);
                        return true;
                    }
                }
                return false;
            }
            return true;
        }, cfg);
        // 4. 等待 LookingForGroupSelectRole 出现（5 秒超时）
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if ((DateTime.UtcNow - waitStart).TotalMilliseconds > 5000)
                {
                    PluginLog.Debug("职业选择界面未出现，跳过");
                    return true;
                }

                if (!TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
                    return false;

                if (!IsAddonReady(addon))
                    return false;
            }
            return true;
        }, cfg);
        // 5.1 禁用槽位
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
                {
                    ECommons.Automation.Callback.Fire(addon, true, 11, 0);
                    return true;
                }
                return false;
            }
            return true;
        }, cfg);

        // 5.2 选择青魔
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
                {
                    ECommons.Automation.Callback.Fire(addon, true, 12, 25);
                    return true;
                }
                return false;
            }
            return true;
        }, cfg);
        // 6. 确认选择
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon) && IsAddonReady(addon))
                {
                    ECommons.Automation.Callback.Fire(addon, true, 0);
                    return true;
                }
                return false;
            }
            return true;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base) && EzThrottler.Throttle("Recruit", 1000))
            {
                return m.Recruit();
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (ECommons.GameHelpers.Player.OnlineStatus.RowId == 26 && TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd2"))
            {
                var BluPlaceholder = P.Config.BluPlaceholder ? "是" : "否";
                var msg = new SeStringBuilder()
                    .Append(TaskMovement.White($"已完成创建怪物狩猎招募"))
                    .Append(TaskMovement.White($"\n自由留言内容: {P.Config.PfinderString}"))
                    .Append(TaskMovement.White($"\n使用青魔占位: {BluPlaceholder}"))
                    .Build();

                Chat.Instance.ExecuteCommand("/pfinder");
                TaskMovement.PrintRouteMessage(msg);
                return true;
            }
            return false;
        }, new(timeLimitMS: 5000));
    }

    // command mode
    public static void Enqueue2()
    {
        var waitStart = DateTime.UtcNow;

        P.TaskManager.Abort();
        if (!Player.Available)
        {
            Notify.Error("现在不能这么做"); // Can't do that now
            return;
        }
        if (Player.Object.OnlineStatus.RowId == 26)
        {
            Notify.Error("已经在招募队员！"); // Already recruiting!
            return;
        }
        if (!QuestManager.IsQuestComplete(67099) && !QuestManager.IsQuestComplete(67100) && !QuestManager.IsQuestComplete(67101))
        {
            DuoLog.Error($"怪物狩猎还未解锁，无法创建队员招募。"); // Hunt is not unlocked. Can not create party finder.
            return;
        }
        var cfg = new TaskManagerConfiguration(timeLimitMS: 2000);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd"))
            {
                Chat.Instance.ExecuteCommand("/pfinder");
            }
        }, cfg);
        P.TaskManager.Enqueue(() => !TryGetAddonByName<AtkUnitBase>("LookingForGroup", out _), cfg);
        P.TaskManager.Enqueue(() =>
        {
            Chat.Instance.ExecuteCommand("/pfinder");
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroup>(out var lfg) && IsAddonReady(lfg.Base) && EzThrottler.Throttle("RMOD"))
            {
                S.LFGService.SetComment(P.CommandComments);
                return lfg.RecruitMembersOrDetails();
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.Normal();
                return true;
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.SelectDutyCategory(11);
                return true;
            }
            return false;
        }, cfg);
        // 1. 启用平均品级
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
                {
                    var node = addon->UldManager.NodeList[25];
                    var compNode = (AtkComponentNode*)node;
                    var checkbox = (AtkComponentCheckBox*)compNode->Component;

                    checkbox->SetChecked(true);
                    return true;
                }
                return false;
            }
            return true;
        }, cfg);
        // 2. 设置平均品级 531(青魔进不来)
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
                {
                    var node = addon->UldManager.NodeList[24];
                    var compNode = (AtkComponentNode*)node;
                    var numeric = (AtkComponentNumericInput*)compNode->Component;

                    // 写入品级 531
                    numeric->SetValue(531);

                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
        }, cfg);
        // 3. 点击第8槽位
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
                {
                    var button = addon->GetComponentButtonById(58);
                    if (button != null)
                    {
                        ECommons.Automation.Callback.Fire(addon, true, 24, 7, 0);
                        return true;
                    }
                }
                return false;
            }
            return true;
        }, cfg);
        // 4. 等待 LookingForGroupSelectRole 出现（5 秒超时）
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if ((DateTime.UtcNow - waitStart).TotalMilliseconds > 5000)
                {
                    Notify.Error("职业选择界面未出现，跳过");
                    return true;
                }

                if (!TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
                    return false;

                if (!IsAddonReady(addon))
                    return false;
            }
            return true;
        }, cfg);
        // 5.1 禁用槽位
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
                {
                    ECommons.Automation.Callback.Fire(addon, true, 11, 0);
                    return true;
                }
                return false;
            }
            return true;
        }, cfg);

        // 5.2 选择青魔
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
                {
                    ECommons.Automation.Callback.Fire(addon, true, 12, 25);
                    return true;
                }
                return false;
            }
            return true;
        }, cfg);
        // 6. 确认选择
        P.TaskManager.Enqueue(() =>
        {
            if (P.Config.BluPlaceholder)
            {
                if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon) && IsAddonReady(addon))
                {
                    ECommons.Automation.Callback.Fire(addon, true, 0);
                    return true;
                }
                return false;
            }
            return true;
        }, cfg);

        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base) && EzThrottler.Throttle("Recruit", 1000))
            {
                return m.Recruit();
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (ECommons.GameHelpers.Player.OnlineStatus.RowId == 26 && TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd2"))
            {
                var BluPlaceholder = P.Config.BluPlaceholder ? "是" : "否";
                var msg = new SeStringBuilder()
                    .Append(TaskMovement.White($"已完成创建怪物狩猎招募"))
                    .Append(TaskMovement.White($"\n自由留言内容: {P.CommandComments}"))
                    .Append(TaskMovement.White($"\n使用青魔占位: {BluPlaceholder}"))
                    .Build();

                Chat.Instance.ExecuteCommand("/pfinder");
                TaskMovement.PrintRouteMessage(msg);
                P.CommandComments = null;
                return true;
            }
            return false;
        }, new(timeLimitMS: 5000));
    }

    public static void Enqueue3()
    {
        var waitStart = DateTime.UtcNow;

        P.TaskManager.Abort();
        if (!Player.Available)
        {
            Notify.Error("现在不能这么做");
            return;
        }
        if (Player.Object.OnlineStatus.RowId == 26)
        {
            Notify.Error("已经在招募队员！");
            return;
        }
        if (!QuestManager.IsQuestComplete(67099) && !QuestManager.IsQuestComplete(67100) && !QuestManager.IsQuestComplete(67101))
        {
            DuoLog.Error($"怪物狩猎还未解锁，无法创建队员招募。");
            return;
        }

        var cfg = new TaskManagerConfiguration(timeLimitMS: 2000);

        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd"))
            {
                Chat.Instance.ExecuteCommand("/pfinder");
            }
        }, cfg);
        P.TaskManager.Enqueue(() => !TryGetAddonByName<AtkUnitBase>("LookingForGroup", out _), cfg);
        P.TaskManager.Enqueue(() =>
        {
            Chat.Instance.ExecuteCommand("/pfinder");
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroup>(out var lfg) && IsAddonReady(lfg.Base) && EzThrottler.Throttle("RMOD"))
            {
                S.LFGService.SetComment(P.Config.PfinderString);
                return lfg.RecruitMembersOrDetails();
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.Normal();
                return true;
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.SelectDutyCategory(11);
                return true;
            }
            return false;
        }, cfg);

        // 1. 启用平均品级（强制青魔占位，不看配置）
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
            {
                var node = addon->UldManager.NodeList[25];
                var compNode = (AtkComponentNode*)node;
                var checkbox = (AtkComponentCheckBox*)compNode->Component;

                checkbox->SetChecked(true);
                return true;
            }
            return false;
        }, cfg);

        // 2. 设置平均品级 531
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
            {
                var node = addon->UldManager.NodeList[24];
                var compNode = (AtkComponentNode*)node;
                var numeric = (AtkComponentNumericInput*)compNode->Component;

                numeric->SetValue(531);
                return true;
            }
            return false;
        }, cfg);

        // 3. 点击第8槽位
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
            {
                var button = addon->GetComponentButtonById(58);
                if (button != null)
                {
                    ECommons.Automation.Callback.Fire(addon, true, 24, 7, 0);
                    return true;
                }
            }
            return false;
        }, cfg);

        // 4. 等待职业选择界面
        P.TaskManager.Enqueue(() =>
        {
            if ((DateTime.UtcNow - waitStart).TotalMilliseconds > 5000)
            {
                PluginLog.Debug("职业选择界面未出现，跳过");
                return true;
            }

            if (!TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
                return false;

            if (!IsAddonReady(addon))
                return false;

            return true;
        }, cfg);

        // 5.1 禁用槽位
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
            {
                ECommons.Automation.Callback.Fire(addon, true, 11, 0);
                return true;
            }
            return false;
        }, cfg);

        // 5.2 选择青魔
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
            {
                ECommons.Automation.Callback.Fire(addon, true, 12, 25);
                return true;
            }
            return false;
        }, cfg);

        // 6. 确认选择
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon) && IsAddonReady(addon))
            {
                ECommons.Automation.Callback.Fire(addon, true, 0);
                return true;
            }
            return false;
        }, cfg);

        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base) && EzThrottler.Throttle("Recruit", 1000))
            {
                return m.Recruit();
            }
            return false;
        }, cfg);

        P.TaskManager.Enqueue(() =>
        {
            if (Player.Object.OnlineStatus.RowId == 26 && TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd2"))
            {
                var msg = new SeStringBuilder()
                    .Append(TaskMovement.White($"已完成创建怪物狩猎招募"))
                    .Append(TaskMovement.White($"\n自由留言内容: {P.Config.PfinderString}"))
                    .Append(TaskMovement.White($"\n使用青魔占位: 是"))
                    .Build();

                Chat.Instance.ExecuteCommand("/pfinder");
                TaskMovement.PrintRouteMessage(msg);
                return true;
            }
            return false;
        }, new(timeLimitMS: 5000));
    }

    public static void Enqueue4()
    {
        var waitStart = DateTime.UtcNow;

        P.TaskManager.Abort();
        if (!Player.Available)
        {
            Notify.Error("现在不能这么做");
            return;
        }
        if (Player.Object.OnlineStatus.RowId == 26)
        {
            Notify.Error("已经在招募队员！");
            return;
        }
        if (!QuestManager.IsQuestComplete(67099) && !QuestManager.IsQuestComplete(67100) && !QuestManager.IsQuestComplete(67101))
        {
            DuoLog.Error($"怪物狩猎还未解锁，无法创建队员招募。");
            return;
        }

        var cfg = new TaskManagerConfiguration(timeLimitMS: 2000);

        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd"))
            {
                Chat.Instance.ExecuteCommand("/pfinder");
            }
        }, cfg);
        P.TaskManager.Enqueue(() => !TryGetAddonByName<AtkUnitBase>("LookingForGroup", out _), cfg);
        P.TaskManager.Enqueue(() =>
        {
            Chat.Instance.ExecuteCommand("/pfinder");
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroup>(out var lfg) && IsAddonReady(lfg.Base) && EzThrottler.Throttle("RMOD"))
            {
                S.LFGService.SetComment(P.CommandCommentsBlu);
                return lfg.RecruitMembersOrDetails();
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.Normal();
                return true;
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.SelectDutyCategory(11);
                return true;
            }
            return false;
        }, cfg);

        // 1. 启用平均品级（强制青魔占位，不看配置）
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
            {
                var node = addon->UldManager.NodeList[25];
                var compNode = (AtkComponentNode*)node;
                var checkbox = (AtkComponentCheckBox*)compNode->Component;

                checkbox->SetChecked(true);
                return true;
            }
            return false;
        }, cfg);

        // 2. 设置平均品级 531
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
            {
                var node = addon->UldManager.NodeList[24];
                var compNode = (AtkComponentNode*)node;
                var numeric = (AtkComponentNumericInput*)compNode->Component;

                numeric->SetValue(531);
                return true;
            }
            return false;
        }, cfg);

        // 3. 点击第8槽位
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupCondition", out var addon) && IsAddonReady(addon))
            {
                var button = addon->GetComponentButtonById(58);
                if (button != null)
                {
                    ECommons.Automation.Callback.Fire(addon, true, 24, 7, 0);
                    return true;
                }
            }
            return false;
        }, cfg);

        // 4. 等待职业选择界面
        P.TaskManager.Enqueue(() =>
        {
            if ((DateTime.UtcNow - waitStart).TotalMilliseconds > 5000)
            {
                Notify.Error("职业选择界面未出现，跳过");
                return true;
            }

            if (!TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
                return false;

            if (!IsAddonReady(addon))
                return false;

            return true;
        }, cfg);

        // 5.1 禁用槽位
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
            {
                ECommons.Automation.Callback.Fire(addon, true, 11, 0);
                return true;
            }
            return false;
        }, cfg);

        // 5.2 选择青魔
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon))
            {
                ECommons.Automation.Callback.Fire(addon, true, 12, 25);
                return true;
            }
            return false;
        }, cfg);

        // 6. 确认选择
        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupSelectRole", out var addon) && IsAddonReady(addon))
            {
                ECommons.Automation.Callback.Fire(addon, true, 0);
                return true;
            }
            return false;
        }, cfg);

        P.TaskManager.Enqueue(() =>
        {
            if (TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base) && EzThrottler.Throttle("Recruit", 1000))
            {
                return m.Recruit();
            }
            return false;
        }, cfg);

        P.TaskManager.Enqueue(() =>
        {
            if (Player.Object.OnlineStatus.RowId == 26 && TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd2"))
            {
                var msg = new SeStringBuilder()
                    .Append(TaskMovement.White($"已完成创建怪物狩猎招募"))
                    .Append(TaskMovement.White($"\n自由留言内容: {P.CommandCommentsBlu}"))
                    .Append(TaskMovement.White($"\n使用青魔占位: 是"))
                    .Build();

                Chat.Instance.ExecuteCommand("/pfinder");
                TaskMovement.PrintRouteMessage(msg);

                P.CommandCommentsBlu = null;
                return true;
            }
            return false;
        }, new(timeLimitMS: 5000));
    }

}
