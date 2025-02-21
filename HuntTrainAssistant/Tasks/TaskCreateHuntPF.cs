using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HuntTrainAssistant.Tasks;
public static unsafe class TaskCreateHuntPF
{
    public static void Enqueue()
    {
        P.TaskManager.Abort();
        if(!Player.Available)
        {
            Notify.Error("现在不能这么做");
            return;
        }
        if(Player.Object.OnlineStatus.RowId == 26)
        {
            Notify.Error("已经在招募队员！");
            return;
        }
        if(!QuestManager.IsQuestComplete(67099) && !QuestManager.IsQuestComplete(67100) && !QuestManager.IsQuestComplete(67101))
        {
            DuoLog.Error($"怪物狩猎还未解锁，无法创建队员招募。");
            return;
        }
        var cfg = new TaskManagerConfiguration(timeLimitMS: 2000);
        P.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd"))
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
            if(TryGetAddonMaster<AddonMaster.LookingForGroup>(out var lfg) && IsAddonReady(lfg.Base) && EzThrottler.Throttle("RMOD"))
            {
                S.LFGService.SetComment(P.Config.PfinderString);
                return lfg.RecruitMembersOrDetails();
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.Normal();
                return true;
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base))
            {
                m.SelectDutyCategory(11);
                return true;
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if(TryGetAddonMaster<AddonMaster.LookingForGroupCondition>(out var m) && IsAddonReady(m.Base) && EzThrottler.Throttle("Recruit", 1000))
            {
                return m.Recruit();
            }
            return false;
        }, cfg);
        P.TaskManager.Enqueue(() =>
        {
            if(Player.OnlineStatus == 26 && TryGetAddonByName<AtkUnitBase>("LookingForGroup", out var a) && EzThrottler.Throttle("Pfindercmd2"))
            {
                Chat.Instance.ExecuteCommand("/pfinder");
                return true;
            }
            return false;
        }, new(timeLimitMS:5000));
    }

    // command mode
    public static void Enqueue2()
    {
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
                Chat.Instance.ExecuteCommand("/pfinder");
                Chat.Instance.ExecuteCommand($"/e 已完成创建怪物狩猎招募");
                Chat.Instance.ExecuteCommand($"/e 自由留言内容：{P.CommandComments}");
                P.CommandComments = null;
                return true;
            }
            return false;
        }, new(timeLimitMS: 5000));
    }
}
