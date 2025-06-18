using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuntTrainAssistant.PluginUI;
public class TabAetheryteBlacklist
{
		private TabAetheryteBlacklist() { }	
		private string Filter = "";
		private bool OnlySel = false;

		public void Draw()
		{
				ImGuiEx.Text($"这些以太之光将被忽略");
				ImGui.SetNextItemWidth(200f);
				ImGui.InputTextWithHint("##fltr", "搜索", ref Filter, 100);
				ImGui.SameLine();
				ImGui.Checkbox("仅显示选定的以太之光", ref OnlySel);
				foreach(var x in Svc.Data.GetExcelSheet<Aetheryte>())
				{
						var name = x.PlaceName.Value.Name.GetText();
						if (name.IsNullOrEmpty()) continue;
						if (Filter != "" && !name.Contains(Filter, StringComparison.OrdinalIgnoreCase)) continue;
						if (OnlySel && !P.Config.AetheryteBlacklist.Contains(x.RowId)) continue;
						ImGuiEx.CollectionCheckbox($"{x.PlaceName.Value.Name}##{x.RowId}", x.RowId, P.Config.AetheryteBlacklist);
				}
		}
}
