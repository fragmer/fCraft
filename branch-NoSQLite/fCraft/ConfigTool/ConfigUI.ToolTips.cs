﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using fCraft;

namespace ConfigTool {
    public sealed partial class ConfigUI : Form {
        void FillOptionToolTips() {
            vPermissions.Items[(int)Permission.Ban].ToolTipText =
@"Ability to ban/unban other players from the server.
Affected commands:
    /ban
    /unban
Affected permissions:
    BanIP
    BanAll";

            vPermissions.Items[(int)Permission.BanAll].ToolTipText =
@"Ability to ban/unban a player, his IP, and all other players who used the IP.
BanAll/UnbanAll commands can be used on players who keep evading bans.
Affected commands:
    /banall
    /unbanall";

            vPermissions.Items[(int)Permission.BanIP].ToolTipText =
@"Ability to ban/unban players by IP.
Affected commands:
    /banip
    /unbanip";

            vPermissions.Items[(int)Permission.Bring].ToolTipText =
@"Ability to bring/summon other players to your location.
This works a bit like reverse-teleport - other player is sent to you.
Affected commands:
    /bring";

            vPermissions.Items[(int)Permission.Build].ToolTipText =
@"Ability to place blocks on maps. This is a baseline permission
that can be overriden by world-specific and zone-specific permissions.";

            vPermissions.Items[(int)Permission.Chat].ToolTipText =
@"Ability to chat and PM players. Note that players without this
permission can still type in commands, receive PMs, and read chat.
Affected commands:
    /say
    @ (pm)
    @@ (rank chat)";

            vPermissions.Items[(int)Permission.CopyAndPaste].ToolTipText =
@"Ability to copy (or cut) and paste blocks. The total number of
blocks that can be copied or pasted at a time is affected by
the draw limit.
Affected commands:
    /copy
    /cut
    /paste, /pastenot
    /rotate
    /mirror";

            vPermissions.Items[(int)Permission.Delete].ToolTipText =
@"Ability to delete blocks on maps. This is a baseline permission
that can be overriden by world-specific and zone-specific permissions.";

            vPermissions.Items[(int)Permission.DeleteAdmincrete].ToolTipText =
@"Ability to delete admincrete (aka adminium) blocks. Even if someone
has this permission, it can be overriden by world-specific and
zone-specific permissions.";

            vPermissions.Items[(int)Permission.Demote].ToolTipText =
@"Ability to demote other players to a lower rank.
Affected commands:
    /rank";

            vPermissions.Items[(int)Permission.Draw].ToolTipText =
@"Ability to use drawing tools (commands capable of affecting many blocks
at once). This permission can be overriden by world-specific and
zone-specific permissions.
Affected commands:
    /cuboid and /cuboidh
    /ellipsoid
    /replace and /replacenot
    /mark
    /cancel
    /undo";

            vPermissions.Items[(int)Permission.Freeze].ToolTipText =
@"Ability to freeze/unfreeze players. Frozen players cannot
move or build/delete.
Affected commands:
    /freeze
    /unfreeze";

            vPermissions.Items[(int)Permission.Hide].ToolTipText =
@"Ability to appear hidden from other players. You can still chat,
build/delete blocks, use all commands, and join worlds while hidden.
Hidden players are completely invisible to other players.
Affected commands:
    /hide
    /unhide";

            vPermissions.Items[(int)Permission.Import].ToolTipText =
@"Ability to import rank and ban lists from files. Useful if you
are switching from another server software.
Affected commands:
    /importranks
    /importbans";

            vPermissions.Items[(int)Permission.Kick].ToolTipText =
@"Ability to kick players from the server.
Affected commands:
    /kick";

            vPermissions.Items[(int)Permission.Lock].ToolTipText =
@"Ability to lock/unlock maps (locking puts a map into read-only state).
Affected commands:
    /lock
    /unlock
    /lockall
    /unlockall";

            vPermissions.Items[(int)Permission.ManageWorlds].ToolTipText =
@"Ability to manipulate the world list: adding, renaming, and deleting worlds,
loading/saving maps, change per-world permissions, and using the map generator.
Affected commands:
    /wload
    /wrename
    /wremove
    /wmain
    /waccess and /wbuild
    /wflush
    /gen";

            vPermissions.Items[(int)Permission.ManageZones].ToolTipText =
@"Ability to manipulate zones: adding, editing, renaming, and removing zones.
Affected commands:
    /zadd
    /zedit
    /zremove";

            vPermissions.Items[(int)Permission.Mute].ToolTipText =
@"Ability to temporarily mute players. Muted players cannot write chat or 
send PMs, but they can still type in commands, receive PMs, and read chat.
Affected commands:
    /mute
    /unmute";

            vPermissions.Items[(int)Permission.Patrol].ToolTipText =
@"Ability to patrol lower-ranked players. ""Patrolling"" means teleporting
to other players to check on them, usually while hidden.
Affected commands:
    /patrol";

            vPermissions.Items[(int)Permission.PlaceAdmincrete].ToolTipText =
@"Ability to place admincrete/adminium. This also affects draw commands.
Affected commands:
    /solid
    /bind";

            vPermissions.Items[(int)Permission.PlaceGrass].ToolTipText =
@"Ability to place grass blocks. This also affects draw commands.
Affected commands:
    /grass
    /bind";

            vPermissions.Items[(int)Permission.PlaceLava].ToolTipText =
@"Ability to place lava blocks. This also affects draw commands.
Affected commands:
    /lava
    /bind";

            vPermissions.Items[(int)Permission.PlaceWater].ToolTipText =
@"Ability to place water blocks. This also affects draw commands.
Affected commands:
    /water
    /bind";

            vPermissions.Items[(int)Permission.Promote].ToolTipText =
@"Ability to promote players to a higher rank.
Affected commands:
    /rank";

            vPermissions.Items[(int)Permission.ReloadConfig].ToolTipText =
@"Ability to reload the configuration file without restarting.
Affected commands:
    /reloadconfig";

            vPermissions.Items[(int)Permission.Say].ToolTipText =
@"Ability to use /say command to show announcements.
Affected commands:
    /say";

            vPermissions.Items[(int)Permission.SetSpawn].ToolTipText =
@"Ability to change the spawn point of a world or a player.
Affected commands:
    /setspawn";

            vPermissions.Items[(int)Permission.ShutdownServer].ToolTipText =
@"Ability to initiate a graceful shutdown remotely.
Useful for servers that are run on dedicated machines.
Affected commands:
    /shutdown";

            vPermissions.Items[(int)Permission.Teleport].ToolTipText =
@"Ability to teleport to other players.
Affected commands:
    /tp";

            vPermissions.Items[(int)Permission.UseSpeedHack].ToolTipText =
@"Ability to move at a faster-than-normal rate (using hacks).
WARNING: Speedhack detection is experimental, and may produce many
false positives - especially on laggy servers.";

            vPermissions.Items[(int)Permission.ViewOthersInfo].ToolTipText =
@"Ability to view extended information about other players.
Affected commands:
    /info
    /baninfo
    /where";



            vLogFileOptions.Items[(int)LogType.ConsoleInput].ToolTipText = "Commands typed in from the server console.";
            vLogFileOptions.Items[(int)LogType.ConsoleOutput].ToolTipText =
@"Things sent directly in response to console input,
e.g. output of commands called from console.";
            vLogFileOptions.Items[(int)LogType.Debug].ToolTipText = "Technical information that may be useful to find bugs.";
            vLogFileOptions.Items[(int)LogType.Error].ToolTipText = "Major errors and problems.";
            vLogFileOptions.Items[(int)LogType.FatalError].ToolTipText = "Errors that prevent server from starting or result in crashes.";
            vLogFileOptions.Items[(int)LogType.GlobalChat].ToolTipText = "Normal chat messages written by players.";
            vLogFileOptions.Items[(int)LogType.IRC].ToolTipText = 
@"IRC-related status and error messages.
Does not include IRC chatter (see IRCChat).";
            vLogFileOptions.Items[(int)LogType.PrivateChat].ToolTipText = "PMs (Private Messages) exchanged between players (@player message).";
            vLogFileOptions.Items[(int)LogType.RankChat].ToolTipText = "Rank-wide messages (@@rank message).";
            vLogFileOptions.Items[(int)LogType.SuspiciousActivity].ToolTipText = "Suspicious activity - hack attempts, failed logins, unverified names.";
            vLogFileOptions.Items[(int)LogType.SystemActivity].ToolTipText = "Status messages regarding normal system activity.";
            vLogFileOptions.Items[(int)LogType.UserActivity].ToolTipText = "Status messages regarding players' actions.";
            vLogFileOptions.Items[(int)LogType.UserCommand].ToolTipText = "Commands types in by players.";
            vLogFileOptions.Items[(int)LogType.Warning].ToolTipText = "Minor, recoverable errors and problems.";

            foreach( LogType type in Enum.GetValues( typeof( LogType ) ) ) {
                vConsoleOptions.Items[(int)type].ToolTipText = vLogFileOptions.Items[(int)type].ToolTipText;
            }
        }
    }
}