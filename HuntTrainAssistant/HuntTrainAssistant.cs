using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ECommons.EzIpcManager;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Reflection;
using ECommons.SimpleGui;
using ECommons.Singletons;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HuntTrainAssistant.DataStructures;
using HuntTrainAssistant.PluginUI;
using HuntTrainAssistant.Services;
using HuntTrainAssistant.Tasks;
using Lumina.Excel.GeneratedSheets;

namespace HuntTrainAssistant;

public unsafe class HuntTrainAssistant : IDalamudPlugin
{
    internal static HuntTrainAssistant P;
    internal Config Config;
    internal (Aetheryte Aetheryte, uint Territory, int Instance) TeleportTo = default;
    internal bool IsMoving = false;
    internal Vector3 LastPosition = Vector3.Zero;
    public TaskManager TaskManager;
    public int LastInstance = 0;
    public HashSet<DawntrailARank> KilledARanks = [];
    public string CommandComments;

    public HuntTrainAssistant(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, this, Module.DalamudReflector);
        EzConfig.Migrate<Config>();
        Config = EzConfig.Init<Config>();
        EzConfigGui.Init(new MainWindow());
        EzConfigGui.Window.RespectCloseHotkey = false;
        EzCmd.Add("/hta", OnChatCommand, "切换显示插件界面\n/hta clear → 清除当前设置的车头\n/hta <玩家名称> → 添加新车头\n/hta pf <自由留言内容> → 创建怪物狩猎招募(参数不区分大小写，无参数则使用设置的自由留言, 需要启用设置)");
        Svc.Chat.ChatMessage += ChatMessageHandler.Chat_ChatMessage;
        Svc.Framework.Update += Framework_Update;
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        SingletonServiceManager.Initialize(typeof(ServiceManager));
				EzIPC.OnSafeInvocationException += EzIPC_OnSafeInvocationException;
        TaskManager = new(new(timeLimitMS: 60*1000, showDebug:true));
        EzIPC.OnSafeInvocationException += (x) => x.LogInternal();
    }

		private void EzIPC_OnSafeInvocationException(Exception obj)
		{
        InternalLog.Error($"During handling IPC call, exception has occurred: \n{obj}");
		}

		private void ClientState_TerritoryChanged(ushort e)
    {
        LastInstance = 0;
        if(TeleportTo.Instance > 0 && e == TeleportTo.Territory)
        {
            TaskChangeInstanceAfterTeleport.Enqueue(TeleportTo.Instance, (int)TeleportTo.Aetheryte.Territory.Row);
        }
        TeleportTo = default;
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
            if(EzThrottler.Throttle("InformDebug", 600000)) DuoLog.Warning("您正在使用 HuntTrainAssistant 的 Debug 模式，这会破坏插件的功能。请在您不需要调试时禁用 Debug 模式。"); // You are using debug mode in HuntTrainAssistant which will break functions of the plugin. Please disable debug mode once you don't need it.
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
        if (Player.Interactable && TeleportTo.Aetheryte != null && Svc.ClientState.LocalPlayer.CurrentHp > 0) 
        {
            if (IsScreenReady())
            {
                if (Svc.ClientState.LocalPlayer.IsCasting)
                {
                    if (Svc.ClientState.LocalPlayer.CastActionId == 5)
                    {
                        if (!Svc.Condition[ConditionFlag.Casting]) // 在上坐骑即将完成时发起传送，将会锁死在传送状态无法传送，需要切换地图(比如返回、进入副本)解除。虽然通过增加延迟可以一定程度上避免，但在计算机性能不佳(狩猎玩家人数过多cpu性能不足等原因)并且插件处于自动传送时上坐骑，仍有可能发生锁死的情况。
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
                if (Svc.Condition[ConditionFlag.Unknown57])
                {
                    EzThrottler.Throttle("Teleport", 500, true);
                }
                if (!Svc.Condition[ConditionFlag.InCombat] && !Svc.Condition[ConditionFlag.BetweenAreas] && !Svc.Condition[ConditionFlag.BetweenAreas51] && !Svc.Condition[ConditionFlag.Casting] && !IsMoving)
                {
                    if (EzThrottler.Throttle("Teleport"))
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
            IsMoving = Svc.ClientState.LocalPlayer.Position != LastPosition;
            LastPosition = Svc.ClientState.LocalPlayer.Position;
        }
        if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])
        {
            if(TeleportTo.Instance > 0)
            {
                TaskChangeInstanceAfterTeleport.Enqueue(TeleportTo.Instance, (int)TeleportTo.Aetheryte.Territory.Row);
            }
            TeleportTo = default;
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
        // 创建怪物狩猎招募
        else if (arguments.Equals("pf", StringComparison.OrdinalIgnoreCase))
        {
            if (P.Config.PfinderEnable)
            {
                TaskCreateHuntPF.Enqueue();
            }
            else
            {
                DuoLog.Warning($"创建怪物狩猎招募按钮未启用，请在设置中启用。");
            }
        }
        // 创建怪物狩猎招募，自定义自由留言
        else if (arguments.StartsWith("pf ", StringComparison.OrdinalIgnoreCase)) 
        {
            arguments = arguments[3..].Trim();
            if (P.Config.PfinderEnable)
            {
                CommandComments = arguments;
                TaskCreateHuntPF.Enqueue2();
            }
            else
            {
                DuoLog.Warning($"创建怪物狩猎招募按钮未启用，请在设置中启用。");
            }
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
