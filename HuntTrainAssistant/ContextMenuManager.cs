using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Network.Structures.InfoProxy;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.ChatMethods;
using ECommons.ExcelServices;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using HuntTrainAssistant.Services;

namespace HuntTrainAssistant;

public class ContextMenuManager : IDisposable
{
    private static CharacterSearchInfo? _TargetChara;


    /*private static readonly string[] ValidAddons = new string[]
    {
                                 null,
                                "PartyMemberList",
                                "FriendList",
                                "FreeCompany",
                                "LinkShell",
                                "CrossWorldLinkshell",
                                "_PartyList",
                                "ChatLog",
                                "LookingForGroup",
                                "BlackList",
                                "ContentMemberList",
                                "SocialList",
                                "ContactList",

    };*/

    private MenuItem MenuItemAddConductor;

    private ContextMenuManager()
    {

        MenuItemAddConductor = new MenuItem()
        {
            Name = new SeStringBuilder().AddUiForeground("添加为车头", 578).Build(),
            Prefix = Dalamud.Game.Text.SeIconChar.BoxedLetterH,
            PrefixColor = 578,
            OnClicked = AssignConductor,
        };
        Svc.ContextMenu.OnMenuOpened += OpenContextMenu;
    }

    private void OpenContextMenu(IMenuOpenedArgs args)
    {
        if (!P.Config.ContextMenu) return;
        if (!IsValidAddon(args)) return;
        else if ((Utils.IsInHuntingTerritory() || P.Config.Debug))
        {
            args.AddMenuItem(MenuItemAddConductor);
        }
    }

    private static unsafe bool IsValidAddon(IMenuArgs args)
    {
        if (args.Target is MenuTargetInventory) return false;
        var menuTarget = (MenuTargetDefault)args.Target;
        if (menuTarget == null) return false;

        var agent = Svc.GameGui.FindAgentInterface("ChatLog");
        if (agent != nint.Zero && *(uint*)(agent + 0x948 + 8) == 3) return false;

        var judgeCriteria0 = menuTarget.TargetCharacter != null;
        var judgeCriteria1 = !string.IsNullOrWhiteSpace(menuTarget.TargetName) &&
                             menuTarget.TargetHomeWorld.ValueNullable != null &&
                             menuTarget.TargetHomeWorld.Value.RowId != 0;

        var judgeCriteria2 = menuTarget.TargetObject is ICharacter && judgeCriteria1;

        return GeneralJudge();

        bool GeneralJudge()
        {
            if (judgeCriteria0)
                _TargetChara = menuTarget.TargetCharacter.ToCharacterSearchInfo();
            else if (menuTarget.TargetObject is CharacterData chara && judgeCriteria1)
                _TargetChara = chara.ToCharacterSearchInfo();
            else if (judgeCriteria1)
            {
                _TargetChara = new()
                {
                    Name = menuTarget.TargetName,
                    World = menuTarget.TargetHomeWorld.ValueNullable?.Name.ExtractText() ?? string.Empty,
                    WorldID = menuTarget.TargetHomeWorld.RowId
                };
            }

            return judgeCriteria0 || judgeCriteria2 || judgeCriteria1;
        }
    }

    public void Dispose()
    {
        Svc.ContextMenu.OnMenuOpened -= OpenContextMenu;
    }

    private void AssignConductor(IMenuItemClickedArgs args)
    {
        if (args.Target is MenuTargetDefault mt)
        {
            var player = mt.TargetName.ToString();
            var world = mt.TargetHomeWorld;
            var s = new Sender(player, world);
            P.Config.Conductors.Add(s);
            EzConfigGui.Open();
        }
    }

    public class CharacterSearchInfo
    {
        public string Name { get; set; } = null!;
        public string World { get; set; } = null!;
        public uint WorldID { get; set; }
    }
}

public static class ExpandPlayerMenuSearchExtensions
{
    public static ContextMenuManager.CharacterSearchInfo ToCharacterSearchInfo(this CharacterData chara)
    {
        var info = new ContextMenuManager.CharacterSearchInfo()
        {
            Name = chara.Name,
            World = chara.HomeWorld.ValueNullable?.Name.ExtractText(),
            WorldID = chara.HomeWorld.RowId
        };
        return info;
    }
}

