using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using ECommons.Singletons;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HuntTrainAssistant.DataStructures;
using HuntTrainAssistant.PluginUI;
using HuntTrainAssistant.Services;
using HuntTrainAssistant.Tasks;
using HuntTrainAssistant.TaskMovements;
using Lumina.Excel.Sheets;

namespace HuntTrainAssistant;

public unsafe class HuntTrainAssistant : IDalamudPlugin
{
    internal static HuntTrainAssistant P;
    internal Config Config;
    internal ArrivalData TeleportTo = null;
    internal bool IsMoving = false;
    internal Vector3 LastPosition = Vector3.Zero;
    public TaskManager TaskManager;
    public VnavmeshIPC VnavmeshIPC;
    public int LastInstance = 0;
    public HashSet<DawntrailARank> KilledARanks = [];
    public string CommandComments;
    public string CommandCommentsBlu;
    public float TmpSafeStopDistance;

    public HuntTrainAssistant(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this, Module.DalamudReflector);
        EzConfig.Migrate<Config>();
        Config = EzConfig.Init<Config>();
        EzConfigGui.Init(new MainWindow());
        EzConfigGui.Window.RespectCloseHotkey = false;
        EzCmd.Add("/hta", OnChatCommand, "切换显示插件界面\n" +
            "/hta clear → 清除当前设置的车头\n" +
            "/hta <玩家名称> → 添加新车头\n" +
            "/hta settings → 切换显示设置窗口\n" +
            "/hta cfg → 切换显示设置窗口\n" +
            "/hta pf <自由留言内容> → 创建怪物狩猎招募(无参数则使用设置的自由留言)\n" +
            "/hta pfb <自由留言内容> → 创建青魔占位的怪物狩猎招募(无参数则使用设置的自由留言)\n" +
            "/hta mts → 寻路到当前地图的 S 级狩猎怪位置\n" +
            "/hta mtsd → 直接寻路到当前地图 S 级狩猎怪位置\n" +
            "/hta mtss <距离> → 寻路到与 S 级狩猎怪参数距离的地面位置");
        Svc.Chat.ChatMessage += ChatMessageHandler.Chat_ChatMessage;
        Svc.Framework.Update += Framework_Update;
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        SingletonServiceManager.Initialize(typeof(ServiceManager));
				EzIPC.OnSafeInvocationException += EzIPC_OnSafeInvocationException;
        TaskManager = new(new(timeLimitMS: 60*1000, showDebug:true));
        EzIPC.OnSafeInvocationException += (x) => x.LogInternal();
        VnavmeshIPC = new VnavmeshIPC();
    }

	private void EzIPC_OnSafeInvocationException(Exception obj)
	{
        InternalLog.Error($"During handling IPC call, exception has occurred: \n{obj}");
	}

	private void ClientState_TerritoryChanged(uint e)
    {
        LastInstance = 0;
        if(TeleportTo != null)
        {
            TaskManager.Abort();
            if(TeleportTo.Instance > 0 && e == TeleportTo.Territory)
            {
                TaskChangeInstanceAfterTeleport.Enqueue(TeleportTo.Instance, TeleportTo.Aetheryte.Territory.RowId);
            }
            TaskMount.EnqueueIfEnabled();
            PluginLog.Debug($"TeleportTo reset (2)");
            TeleportTo = null;
        }
        if (!Utils.IsInHuntingTerritory())
        {
            P.Config.Conductors.Clear();
        }
        KilledARanks.Clear();
        PluginLog.Debug($"Cleared killed A ranks list (cs_tt)");
    }

    private void Framework_Update(object framework)
    {
        if(P.Config.Debug)
        {
            if(EzThrottler.Throttle("InformDebug", 600000)) DuoLog.Warning("您正在使用 HuntTrainAssistant 的 Debug 模式，这会破坏插件的功能。请在您不需要调试时禁用 Debug 模式。");
        }
        if(LastInstance != UIState.Instance()->PublicInstance.InstanceId)
        {
            LastInstance = (int)UIState.Instance()->PublicInstance.InstanceId;
            //instance changed event
            KilledARanks.Clear();
            PluginLog.Debug($"Cleared killed A ranks list (inst.ch.)");
        }
        if(P.Config.Conductors.Count > 0 && Utils.IsInHuntingTerritory() && IsScreenReady())
        {
            foreach(var x in Svc.Objects)
            {
                if(x is IBattleNpc b && b.CurrentHp == 0 && Utils.IsNpcIdInARankList(b.NameId) && !KilledARanks.Contains((DawntrailARank)b.NameId))
                {
                    PluginLog.Debug($"Added killed A rank: {(DawntrailARank)b.NameId}. Killed A ranks: {KilledARanks.Print()}");
                    KilledARanks.Add((DawntrailARank)b.NameId);
                }
            }
        }
        if (Player.Interactable && TeleportTo?.Aetheryte != null && Svc.Objects.LocalPlayer.CurrentHp > 0) 
        {
            if(Utils.CheckMultiMode()) return;
            if (IsScreenReady())
            {
                if (Svc.Objects.LocalPlayer.IsCasting)
                {
                    if (Svc.Objects.LocalPlayer.CastActionId == 5)
                    {
                        if (!Svc.Condition[ConditionFlag.Casting])
                        {
                            EzThrottler.Throttle("Teleport", 2000, true);
                        }
                        else
                        {
                            EzThrottler.Throttle("Teleport", 500, true);
                        }
                    }
                    else
                    {
                        EzThrottler.Throttle("Teleport", 500, true);
                    }
                }
                if (Svc.Condition[ConditionFlag.MountOrOrnamentTransition])
                {
                    EzThrottler.Throttle("Teleport", 500, true);
                }
                if (!Svc.Condition[ConditionFlag.InCombat] && !Svc.Condition[ConditionFlag.BetweenAreas] && !Svc.Condition[ConditionFlag.BetweenAreas51] && !Svc.Condition[ConditionFlag.Casting] && !IsMoving)
                {
                    if (EzThrottler.Throttle("Teleport") && !Player.IsAnimationLocked)
                    {
                        if (S.TeleporterIPC.Teleport(TeleportTo.Aetheryte.RowId, 0))
                        {
                            PluginLog.Information($"Teleporting using Teleporter plugin");
                        }
                        else if (S.LifestreamIPC.Teleport(TeleportTo.Aetheryte.RowId))
                        {
                            PluginLog.Information($"Teleporting using Lifestream plugin");
                        }
                        else
                        {
                            PluginLog.Warning($"Failed to teleport. ");
                        }
                    }
                }
            }
            IsMoving = Svc.Objects.LocalPlayer.Position != LastPosition;
            LastPosition = Svc.Objects.LocalPlayer.Position;
        }
        if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])
        {
            if(TeleportTo != null)
            {
                TaskManager.Abort();
                if(TeleportTo.Instance > 0)
                {
                    if(Utils.CheckMultiMode()) return;
                    TaskChangeInstanceAfterTeleport.Enqueue(TeleportTo.Instance, (int)TeleportTo.Aetheryte.Territory.RowId);
                }
                TaskMount.EnqueueIfEnabled();
                PluginLog.Debug($"TeleportTo reset (1)");
                TeleportTo = null;
            }
        }
    }

    public string Name => "HuntTrainAssistant";

    public void Dispose()
    {
        Svc.Chat.ChatMessage -= ChatMessageHandler.Chat_ChatMessage;
        Svc.Framework.Update -= Framework_Update;
        Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        ECommonsMain.Dispose();
        P = null;
    }

    private void OnChatCommand(string command, string arguments)
    {
        arguments = arguments.Trim();

        if (arguments == string.Empty)
        {
            EzConfigGui.Window.Toggle();
        }
        else if (arguments.StartsWith("clear"))
        {
            P.Config.Conductors.Clear();
        }
        // 切换设置界面显示
        else if (arguments.Equals("settings", StringComparison.OrdinalIgnoreCase))
        {
            S.SettingsWindow.Toggle();
        }
        // 切换设置界面显示(简易)
        else if (arguments.Equals("cfg", StringComparison.OrdinalIgnoreCase))
        {
            S.SettingsWindow.Toggle();
        }
        // 创建青魔占位招募，自定义留言
        else if (arguments.StartsWith("pfb ", StringComparison.OrdinalIgnoreCase))
        {
            var arg = arguments[4..].Trim();
            CommandCommentsBlu = arg;
            TaskCreateHuntPF.Enqueue4();
        }
        // 创建青魔占位招募(配置)
        else if (arguments.Equals("pfb", StringComparison.OrdinalIgnoreCase))
        {
            TaskCreateHuntPF.Enqueue3();
        }
        // 创建招募，自定义留言
        else if (arguments.StartsWith("pf ", StringComparison.OrdinalIgnoreCase))
        {
            var arg = arguments[3..].Trim();
            CommandComments = arg;
            TaskCreateHuntPF.Enqueue2();
        }
        // 创建招募(配置)
        else if (arguments.Equals("pf", StringComparison.OrdinalIgnoreCase))
        {
            TaskCreateHuntPF.Enqueue();
        }
        // 移动到S怪位置，直接到S怪位置
        else if (arguments.Equals("mtsd", StringComparison.OrdinalIgnoreCase))
        {
            TaskMovement.EnqueueMoveToSRankDirect();
        }
        else if (arguments.StartsWith("mtss ", StringComparison.OrdinalIgnoreCase))
        {
            var arg = arguments[5..].Trim();
            if (float.TryParse(arg, out float dist))
            {
                P.TmpSafeStopDistance = dist;
                if (P.TmpSafeStopDistance <= 0)
                {
                    TaskMovement.PrintWhiteMessage("自定义安全距离无效，请输入大于 0 的数值");
                    return;
                }
                TaskMovement.EnqueueMoveToSRankWithCustomSafeDistance(dist);
            }
            else
            {
                TaskMovement.PrintWhiteMessage("请输入有效的数字作为安全距离");
            }
        }
        else if (arguments.Equals("mtss", StringComparison.OrdinalIgnoreCase))
        {
            TaskMovement.PrintWhiteMessage("用法: /hta mtss <距离>");
        }
        // 移动到S怪位置(配置)
        else if (arguments.Equals("mts", StringComparison.OrdinalIgnoreCase))
        {
            TaskMovement.EnqueueMoveToSRank();
        }
        else
        {
            if (arguments.StartsWith("add "))
                arguments = arguments[4..].Trim();
            if (!P.Config.Conductors.Contains(new(arguments, 0)))
                P.Config.Conductors.Add(new(arguments, 0));
            EzConfigGui.Open();
        }
    }
}
