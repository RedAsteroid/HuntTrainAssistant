using Dalamud.Memory;
using Dalamud.Memory.Exceptions;
using ECommons.Automation;
using ECommons.Interop;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using NightmareUI.PrimaryUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuntTrainAssistant.PluginUI;
public unsafe class TabSettings
{
		public void Draw()
		{
				if(OpenFileDialog.IsSelecting())
				{
						ImGuiEx.Text("正在等待选择文件...");
						return;
				}
				new NuiBuilder().
						Section("基本设置")
						.Widget(() =>
						{
								ImGui.Checkbox("启用插件", ref P.Config.Enabled);
								ImGui.SameLine();
								ImGui.Checkbox("Debug 模式", ref P.Config.Debug);
								ImGui.Checkbox("自动传送到不同的地图", ref P.Config.AutoTeleport);
								ImGui.Indent();
								ImGui.Checkbox("在传送后自动切换到号副本区", ref P.Config.AutoSwitchInstanceToOne);
								ImGui.Unindent();
								ImGui.Checkbox("检测到新的旗帜标记后自动打开地图", ref P.Config.AutoOpenMap);
								ImGui.Indent();
								ImGui.Checkbox("禁止在上一个标记位置相同时重复打开地图", ref P.Config.NoDuplicateFlags);
								ImGui.Unindent();
								ImGui.Checkbox("设置车头后，屏蔽其他人的聊天信息", ref P.Config.SuppressChatOtherPlayers);
								ImGui.Checkbox("补偿一些以太之光的位置判断", ref P.Config.DistanceCompensationHack);
								ImGui.Checkbox("在2只A怪被击杀后，自动传送将前往下一个副本区", ref P.Config.AutoSwitchInstanceTwoRanks);
								ImGui.Checkbox("添加右键菜单选项", ref P.Config.ContextMenu);
								ImGui.Checkbox("启用 一键创建队员招募按钮", ref P.Config.PfinderEnable);
								ImGui.Checkbox("显示 记录A怪击杀信息在主窗口", ref P.Config.ShowKilledARanks);
								ImGui.Indent();
								ImGuiEx.Text($"队员招募自由留言");
								ImGuiEx.SetNextItemFullWidth();
								ImGui.InputText($"##pfindercommenr", ref P.Config.PfinderString, 150);
								ImGui.Unindent();
								ImGui.Checkbox("启用 自动传送随机延迟", ref P.Config.TeleportDelayEnabled);
								ImGui.Indent();
								ImGui.SetNextItemWidth(150f);
								ImGuiEx.SliderIntAsFloat("最小延迟(ms)", ref P.Config.TeleportDelayMin, 0, 1000);
								ImGui.SetNextItemWidth(150f);
								ImGuiEx.SliderIntAsFloat("最大延迟(ms)", ref P.Config.TeleportDelayMax, 0, 1000);
								ImGui.Unindent();
						})
						.Section("通知")
						.Widget(() =>
						{
								ImGuiEx.Text("需要安装 NotificationMaster 插件");
								ImGuiEx.PluginAvailabilityIndicator([new("NotificationMaster"), new("NotificationMaster.NXIV", "NotificationMaster (from NightmareXIV repo)")], "", false);
								ImGui.Checkbox("车头发送信息时，播放音频", ref P.Config.AudioAlert);
								ImGui.Indent();
								ImGuiEx.InputWithRightButtonsArea(() => ImGui.InputTextWithHint("##pathToAudio", "音频文件路径", ref P.Config.AudioAlertPath, 500), () =>
								{
										if(ImGui.Button("选择..."))
										{
												OpenFileDialog.SelectFile((x) =>
												{
														if(x != null) new TickScheduler(() => P.Config.AudioAlertPath = x.file);
												});
										}
										ImGui.SameLine();
										if(ImGuiEx.IconButton(FontAwesomeIcon.Play))
										{
												S.Notificator.PlaySound(P.Config.AudioAlertPath, P.Config.AudioAlertVolume, false, false);
										}
								});
								ImGui.SetNextItemWidth(150f);
								ImGui.SliderFloat("音量", ref P.Config.AudioAlertVolume, 0f, 1f);
								ImGui.Checkbox("只在游戏最小化时播放", ref P.Config.AudioAlertOnlyMinimized);
								ImGui.SetNextItemWidth(150f);
								ImGuiEx.SliderIntAsFloat("音频通知的最小间隔时间", ref P.Config.AudioThrottle, 0, 10000);
								ImGui.Unindent();
								ImGui.Checkbox("在任务栏闪烁车头的聊天信息", ref P.Config.FlashTaskbar);
								ImGui.Checkbox("显示车头聊天信息的托盘弹出通知", ref P.Config.TrayNotification);
						})
						.Section("触发器")
						.Widget(() =>
						{
								ImGui.Checkbox("接收到带有坐标的车头聊天信息后执行宏命令", ref P.Config.ExecuteMacroOnFlag);
								if(P.Config.ExecuteMacroOnFlag)
								{
										ImGui.Indent();
										var m = RaptureMacroModule.Instance();
										var macroName = "无";
										if(P.Config.MacroIndex >= 0 && P.Config.MacroIndex < m->Shared.Length)
										{
												var macro = m->Shared[P.Config.MacroIndex];
												if(macro.IsNotEmpty())
												{
														macroName = MemoryHelper.ReadSeString(&macro.Name).ToString();
												}
										}
										if(ImGui.BeginCombo("选择用户宏指令", macroName, ImGuiComboFlags.HeightLarge))
										{
												for(int i = 0; i < m->Shared.Length; i++)
												{
														if(m->Shared[i].IsNotEmpty())
														{
																var macro = m->Shared[i];
																macroName = MemoryHelper.ReadSeString(&macro.Name).ToString();
																if(ImGui.Selectable($"#{i + 1}: {macroName}", i == P.Config.MacroIndex))
																{
																		P.Config.MacroIndex = i;
																}
														}
												}
												ImGui.EndCombo();
										}
										ImGui.Unindent();
								}
						})
						.Draw();
		}
}
