using Lumina.Excel.GeneratedSheets;
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
				ImGuiEx.Text($"这些以太之光将被忽略"); // These aetherytes will always be ignored
				ImGui.SetNextItemWidth(200f);
				ImGui.InputTextWithHint("##fltr", "搜索", ref Filter, 100); // Search
				ImGui.SameLine();
				ImGui.Checkbox("仅选定", ref OnlySel); // Only selected
				foreach(var x in Svc.Data.GetExcelSheet<Aetheryte>())
				{
						var name = x.PlaceName.Value.Name.ExtractText();
						if (name.IsNullOrEmpty()) continue;
						if (Filter != "" && !name.Contains(Filter, StringComparison.OrdinalIgnoreCase)) continue;
						if (OnlySel && !P.Config.AetheryteBlacklist.Contains(x.RowId)) continue;
						ImGuiEx.CollectionCheckbox($"{x.PlaceName.Value.Name}##{x.RowId}", x.RowId, P.Config.AetheryteBlacklist);
				}
		}
}
