using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using HuntTrainAssistant.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuntTrainAssistant;
public static class Utils
{
		public static bool IsNpcIdInARankList(uint npcId)
		{
				if(P.Config.Debug) return true;
				return Enum.GetValues<DawntrailARank>().Contains((DawntrailARank)npcId);
    }

		public static bool IsInHuntingTerritory()
		{
				if (ExcelTerritoryHelper.Get(Svc.ClientState.TerritoryType).TerritoryIntendedUse == (int)TerritoryIntendedUseEnum.Open_World) return true;
        if (Svc.ClientState.TerritoryType.EqualsAny((ushort[])[
            1024, //mare <-> garlemard gateway
						682, 739, 759, //doman enclave
						635, 659, //rhalgr's reach
            ])) return true; 
        if (Svc.ClientState.TerritoryType == MainCities.Idyllshire) return true;
				return false;
		}

		public static bool CanAutoInstanceSwitch()
		{
				if(P.KilledARanks.Count >= 2) return true;
				if(P.KilledARanks.Count == 1)
				{
					// 处理异常: 第一只A怪血量=0时实体仍未消失，在这段时间内固定返回true，导致错误的自动换线
					bool inCombat = Svc.Condition[ConditionFlag.InCombat];
					bool hasWeakARank = Svc.Objects.OfType<IBattleNpc>().Any(x => Utils.IsNpcIdInARankList(x.NameId) && (float)x.CurrentHp / (float)x.MaxHp < 0.5f && !P.KilledARanks.Contains((DawntrailARank)x.NameId));
					if (inCombat && hasWeakARank) 
					{
						return true;
					}
				}
				return false;
		}
}
