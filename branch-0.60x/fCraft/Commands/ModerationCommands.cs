﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using fCraft.Events;

namespace fCraft {
    /// <summary>
    /// Most commands for server moderation - kick, ban, rank change, etc - are here.
    /// </summary>
    public static class ModerationCommands {
        const string BanCommonHelp = "Ban information can be viewed with &H/baninfo";

        internal static void Init() {
            CdBan.Help += BanCommonHelp;
            CdBanIP.Help += BanCommonHelp;
            CdBanAll.Help += BanCommonHelp;
            CdUnban.Help += BanCommonHelp;
            CdUnbanIP.Help += BanCommonHelp;
            CdUnbanAll.Help += BanCommonHelp;

            CommandManager.RegisterCommand( CdBan );
            CommandManager.RegisterCommand( CdBanIP );
            CommandManager.RegisterCommand( CdBanAll );
            CommandManager.RegisterCommand( CdUnban );
            CommandManager.RegisterCommand( CdUnbanIP );
            CommandManager.RegisterCommand( CdUnbanAll );

            CommandManager.RegisterCommand( CdKick );

            CommandManager.RegisterCommand( CdRank );

            CommandManager.RegisterCommand( CdHide );
            CommandManager.RegisterCommand( CdUnhide );

            CommandManager.RegisterCommand( CdSetSpawn );

            CommandManager.RegisterCommand( CdFreeze );
            CommandManager.RegisterCommand( CdUnfreeze );

            CommandManager.RegisterCommand( CdTP );
            CommandManager.RegisterCommand( CdBring );
            CommandManager.RegisterCommand( CdWorldBring );
            CommandManager.RegisterCommand( CdBringAll );

            CommandManager.RegisterCommand( CdPatrol );
            CommandManager.RegisterCommand( CdSpecPatrol );

            CommandManager.RegisterCommand( CdMute );
            CommandManager.RegisterCommand( CdUnmute );

            CommandManager.RegisterCommand( CdSpectate );
            CommandManager.RegisterCommand( CdUnspectate );
        }

        static readonly CommandDescriptor CdSpectate = new CommandDescriptor {
            Name = "spectate",
            Aliases = new[] { "follow", "spec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Handler = Spectate
        };

        internal static void Spectate( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdSpectate.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false );
            if( target == null ) return;

            if( target == player ) {
                player.Message( "You cannot spectate yourself." );
                return;
            }

            if( !player.Can( Permission.Spectate, target.Info.Rank ) ) {
                player.Message( "You can only spectate players ranked {0}&S or lower.",
                player.Info.Rank.GetLimit( Permission.Spectate ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            player.Spectate( target );
        }



        static readonly CommandDescriptor CdUnspectate = new CommandDescriptor {
            Name = "unspectate",
            Aliases = new[] { "unfollow", "unspec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Handler = Unspectate
        };

        internal static void Unspectate( Player player, Command cmd ) {
            if( !player.StopSpectating() ) {
                player.Message( "You are not currently spectating anyone." );
            }
        }



        #region Ban

        static readonly CommandDescriptor CdBan = new CommandDescriptor {
            Name = "ban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/ban PlayerName [Reason]",
            Help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = Ban
        };

        static void Ban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, false );
        }



        static readonly CommandDescriptor CdBanIP = new CommandDescriptor {
            Name = "banip",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/banip PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanIP
        };

        static void BanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, false );
        }



        static readonly CommandDescriptor CdBanAll = new CommandDescriptor {
            Name = "banall",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/banall PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanAll
        };

        static void BanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, false );
        }



        static readonly CommandDescriptor CdUnban = new CommandDescriptor {
            Name = "unban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/unban PlayerName [Reason]",
            Help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = Unban
        };

        static void Unban( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), false, false, true );
        }



        static readonly CommandDescriptor CdUnbanIP = new CommandDescriptor {
            Name = "unbanip",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/unbanip PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanIP
        };

        static void UnbanIP( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, false, true );
        }



        static readonly CommandDescriptor CdUnbanAll = new CommandDescriptor {
            Name = "unbanall",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/unbanall PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanAll
        };

        static void UnbanAll( Player player, Command cmd ) {
            DoBan( player, cmd.Next(), cmd.NextAll(), true, true, true );
        }



        public static void DoBan( Player player, string nameOrIP, string reason, bool banIP, bool banAll, bool unban ) {
            if( nameOrIP == null ) {
                player.Message( "Please specify player name or IP to ban." );
                return;
            }

            IPAddress address;
            Player target = Server.FindPlayerExact( nameOrIP );
            PlayerInfo targetInfo;
            if( target != null ) {
                targetInfo = target.Info;
            } else {
                targetInfo = PlayerDB.FindPlayerInfoExact( nameOrIP );
            }

            if( ConfigKey.RequireBanReason.GetBool() && string.IsNullOrEmpty( reason ) ) {
                player.Message( "&WPlease specify a ban/unban reason." );
                // freeze the target player to prevent further damage
                if( !unban && target != null && !targetInfo.IsFrozen && player.Can( Permission.Freeze ) && player.Can( Permission.Ban, target.Info.Rank ) ) {
                    player.Message( "{0}&S has been frozen while you retry.",
                                    target.ClassyName );
                    Freeze( player, new Command( "/freeze " + target.Name ) );
                }

                return;
            }

            // ban by IP address
            if( banIP && Server.IsIP( nameOrIP ) && IPAddress.TryParse( nameOrIP, out address ) ) {
                DoIPBan( player, address, reason, null, banAll, unban );

                // ban online players
            } else if( !unban && target != null ) {

                // check permissions
                if( player.Can( Permission.Ban, target.Info.Rank ) ) {
                    address = target.Info.LastIP;
                    if( banIP ) DoIPBan( player, address, reason, target.Name, banAll, false );
                    if( !banAll ) {
                        if( target.Info.ProcessBan( player, reason ) ) {
                            Server.FirePlayerBannedEvent( target.Info, player, reason );
                            Logger.Log( "{0} was banned by {1}.", LogType.UserActivity,
                                        target.Info.Name, player.Name );

                            Server.Message( target,
                                            "{0}&W was banned by {1}",
                                            target.ClassyName, player.ClassyName );
                            if( !string.IsNullOrEmpty( reason ) ) {
                                if( ConfigKey.AnnounceKickAndBanReasons.GetBool() ) {
                                    Server.Message( target, "&WBan reason: {0}", reason );
                                }
                            }
                            DoKick( player, target, reason, true, false, LeaveReason.Ban );

                            if( !banIP ) {
                                PlayerInfo[] alts = PlayerDB.FindPlayers( target.Info.LastIP );
                                PlayerInfo[] bannedAlts = alts.Where( t => (t.Banned && t != target.Info) ).ToArray();
                                if( bannedAlts.Length > 0 ) {
                                    player.Message( "Warning: {0}&S shares IP with other banned players: {1}&S. Consider adding an IP-ban.",
                                                    target.ClassyName,
                                                    bannedAlts.JoinToClassyString() );
                                }
                            }

                        } else {
                            player.Message( "{0}&S is already banned.", target.ClassyName );
                        }
                    }
                } else {
                    player.Message( "You can only ban players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Ban ).ClassyName );
                    player.Message( "{0}&S is ranked {1}",
                                    target.ClassyName, target.Info.Rank.ClassyName );
                }

                // ban or unban offline players
            } else if( targetInfo != null ) {
                if( player.Can( Permission.Ban, targetInfo.Rank ) || unban ) {
                    address = targetInfo.LastIP;
                    if( banIP ) DoIPBan( player, address, reason, targetInfo.Name, banAll, unban );
                    if( !banAll ) {
                        if( unban ) {
                            if( targetInfo.ProcessUnban( player.Name, reason ) ) {
                                Server.FirePlayerUnbannedEvent( targetInfo, player, reason );
                                Logger.Log( "{0} (offline) was unbanned by {1}", LogType.UserActivity,
                                            targetInfo.Name, player.Name );
                                Server.Message( "{0}&W (offline) was unbanned by {1}",
                                                  targetInfo.ClassyName, player.ClassyName );
                                if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !string.IsNullOrEmpty( reason ) ) {
                                    Server.Message( "&WUnban reason: {0}", reason );
                                }
                            } else {
                                player.Message( "{0}&S (offline) is not currenty banned.", targetInfo.ClassyName );
                            }
                        } else {
                            if( targetInfo.ProcessBan( player, reason ) ) {
                                Server.FirePlayerBannedEvent( targetInfo, player, reason );
                                Logger.Log( "{0} (offline) was banned by {1}.", LogType.UserActivity,
                                            targetInfo.Name, player.Name );
                                Server.Message( "{0}&W (offline) was banned by {1}",
                                                  targetInfo.ClassyName, player.ClassyName );
                                if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !string.IsNullOrEmpty( reason ) ) {
                                    Server.Message( "&WBan reason: {0}", reason );
                                }
                            } else {
                                player.Message( "{0}&S (offline) is already banned.", targetInfo.ClassyName );
                            }
                        }
                    }
                } else {
                    player.Message( "You can only ban players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Ban ).ClassyName );
                    player.Message( "{0}&S is ranked {1}",
                                    targetInfo.ClassyName, targetInfo.Rank.ClassyName );
                }

                // ban players who are not in the database yet
            } else if( Player.IsValidName( nameOrIP ) ) {
                if( !player.Can( Permission.EditPlayerDB ) ) {
                    player.Message( "Player not found. Please specify valid name or IP." );
                    return;
                }

                player.Message( "Warning: Player \"{0}\" is not in the database (possible typo)", nameOrIP );

                if( unban ) {
                    player.Message( "\"{0}\" (unrecognized) is not banned.", nameOrIP );
                } else {
                    targetInfo = PlayerDB.AddFakeEntry( nameOrIP, RankChangeType.Default );
                    targetInfo.ProcessBan( player, reason ); // this will never return false (player could not have been banned already)
                    Server.FirePlayerBannedEvent( targetInfo, player, reason );
                    player.Message( "Player \"{0}\" (unrecognized) was banned.", nameOrIP );
                    Logger.Log( "{0} (unrecognized) was banned by {1}", LogType.UserActivity,
                                targetInfo.Name, player.Name );
                    Server.Message( "{0}&W (unrecognized) was banned by {1}",
                                      targetInfo.ClassyName, player.ClassyName );

                    if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !string.IsNullOrEmpty( reason ) ) {
                        Server.Message( "&WBan reason: {0}", reason );
                    }
                }
            } else {
                player.Message( "Please specify valid player name or IP." );
            }
        }


        internal static void DoIPBan( Player player, IPAddress address, string reason, string targetName, bool banAll, bool unban ) {

            if( player == null ) throw new ArgumentNullException( "player" );
            if( address == null ) throw new ArgumentNullException( "address" );

            if( address.Equals( IPAddress.None ) || address.Equals( IPAddress.Any ) ) {
                player.Message( "Invalid IP: {0}", address );
                return;
            }

            if( unban ) {
                IPBanInfo banInfo = IPBanList.Get( address );
                if( IPBanList.Remove( address ) ) {
                    Logger.Log( "{0} unbanned {1}", LogType.UserActivity, player.Name, address );
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        player.Message( "{0} has been removed from the IP ban list.", address );
                    } else {
                        player.Message( "This IP has been removed from the ban list.", address );
                    }
                    string ipAssociatedName = targetName;
                    if( !String.IsNullOrEmpty( banInfo.PlayerName ) ) {
                        ipAssociatedName = banInfo.PlayerName;
                    }

                    var can = Server.Players.Can( Permission.ViewPlayerIPs ).Except( player );
                    var cant = Server.Players.Cant( Permission.ViewPlayerIPs ).Except( player );
                    if( !String.IsNullOrEmpty( ipAssociatedName ) ) {
                        can.Message( "&W{0} (associated with {1}) was unbanned by {2}",
                                     address, ipAssociatedName, player.ClassyName );
                        cant.Message( "&WIP associated with {0} was unbanned by {1}",
                                      ipAssociatedName, player.ClassyName );
                    } else {
                        can.Message( "&W{0} was unbanned by {1}",
                                     address, player.ClassyName );
                        can.Message( "&WAn IP was unbanned by {0}",
                                     player.ClassyName );
                    }
                    if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !String.IsNullOrEmpty( reason ) ) {
                        Server.Message( player,
                                        "&WUnban reason: {0}",
                                        player, reason );
                    }
                } else {
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        player.Message( "{0} is not currently banned.", address );
                    } else {
                        player.Message( "This IP is not currently banned." );
                    }
                }

                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayers( address ) ) {
                        if( otherInfo.ProcessUnban( player.Name, reason + "~UnBanAll" ) ) {
                            Server.FirePlayerUnbannedEvent( otherInfo, player, reason + "~UnBanAll" );
                            Server.Message( player,
                                            "{0}&W was unbanned (UnbanAll) by {1}",
                                            otherInfo.ClassyName, player.ClassyName );
                            player.Message( "{0}&S matched IP and was also unbanned.", otherInfo.ClassyName );
                        }
                    }
                }

            } else {
                if( IPBanList.Add( new IPBanInfo( address, targetName, player.Name, reason ) ) ) {
                    Logger.Log( "{0} banned {1}", LogType.UserActivity, player.Name, address );
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        player.Message( "{0} was added to the IP ban list.", address );
                    } else {
                        player.Message( "This IP was added to the ban list.", address );
                    }

                    var can = Server.Players.Can( Permission.ViewPlayerIPs ).Except( player );
                    var cant = Server.Players.Cant( Permission.ViewPlayerIPs ).Except( player );
                    if( !String.IsNullOrEmpty( targetName ) ) {
                        can.Message( "&W{0} (associated with {1}) was banned by {2}",
                                     address, targetName, player.ClassyName );
                        cant.Message( "&WIP associated with {0} was banned by {1}",
                                      targetName, player.ClassyName );
                    } else {
                        can.Message( "&W{0} was banned by {1}",
                                     address, player.ClassyName );
                        cant.Message( "&WAn IP was banned by {0}",
                                      player.ClassyName );
                    }

                    if( ConfigKey.AnnounceKickAndBanReasons.GetBool() && !String.IsNullOrEmpty( reason ) ) {
                        Server.Message( player, "&WBan reason: {0}", reason );
                    }

                    foreach( Player other in Server.FindPlayers( address ) ) {
                        DoKick( player, other, reason, true, false, LeaveReason.BanIP );
                    }

                } else {
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        player.Message( "{0} is already banned.", address );
                    } else {
                        player.Message( "This IP is already banned." );
                    }
                }

                if( banAll ) {
                    foreach( PlayerInfo otherInfo in PlayerDB.FindPlayers( address ) ) {
                        if( otherInfo.ProcessBan( player, reason + "~BanAll" ) ) {
                            Server.FirePlayerBannedEvent( otherInfo, player, reason + "~BanAll" );
                            player.Message( "{0}&S matched IP and was also banned.", otherInfo.ClassyName );
                            Server.Message( player,
                                            "{0}&W was banned (BanAll) by {1}",
                                            otherInfo.ClassyName, player.ClassyName );
                        }
                    }

                    foreach( Player other in Server.FindPlayers( address ) ) {
                        DoKick( player, other, reason, true, false, LeaveReason.BanAll );
                    }
                }
            }
        }

        #endregion


        #region Kick

        static readonly CommandDescriptor CdKick = new CommandDescriptor {
            Name = "kick",
            Aliases = new[] { "k" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Kick },
            Usage = "/kick PlayerName [Reason]",
            Help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            Handler = Kick
        };

        internal static void Kick( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                string reason = cmd.NextAll();

                Player target = Server.FindPlayerOrPrintMatches( player, name, false );
                if( target == null ) return;

                DateTime previousKickDate = target.Info.LastKickDate;
                string previousKickedBy = target.Info.LastKickBy;
                string previousKickReason = target.Info.LastKickReason;

                if( ConfigKey.RequireKickReason.GetBool() && String.IsNullOrEmpty( reason ) ) {
                    player.Message( "&WPlease specify a kick reason: &H/k PlayerName Reason" );
                    // freeze the target player to prevent further damage
                    if( !target.Info.IsFrozen && player.Can( Permission.Freeze ) && player.Can( Permission.Kick, target.Info.Rank ) ) {
                        player.Message( "{0}&S has been frozen while you retry.",
                                        target.ClassyName );
                        Freeze( player, new Command( "/freeze " + target.Name ) );
                    }
                    return;
                }

                if( DoKick( player, target, reason, false, true, LeaveReason.Kick ) ) {
                    if( target.Info.TimesKicked > 1 ) {
                        player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                        target.ClassyName, target.Info.TimesKicked - 1 );
                        if( previousKickDate != DateTime.MinValue ) {
                            player.Message( "Most recent kick was {0} ago, by {1}.",
                                            DateTime.UtcNow.Subtract( previousKickDate ).ToMiniString(),
                                            previousKickedBy );
                        }
                        if( !String.IsNullOrEmpty( previousKickReason ) ) {
                            player.Message( "Most recent kick reason was: {0}",
                                            previousKickReason );
                        }
                    }
                }
            } else {
                player.Message( "Usage: &H/kick PlayerName [Message]" );
            }
        }


        internal static bool DoKick( Player player, Player target, string reason, bool silent, bool recordToPlayerDB, LeaveReason leaveReason ) {

            if( player == null ) throw new ArgumentNullException( "player" );
            if( target == null ) throw new ArgumentNullException( "target" );

            if( player == target ) {
                player.Message( "You cannot kick yourself." );
                return false;
            }

            if( !player.Can( Permission.Kick, target.Info.Rank ) ) {
                player.Message( "You can only kick players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Kick ).ClassyName );
                player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                return false;

            } else {

                var e = new PlayerBeingKickedEventArgs( target, player, reason, silent, recordToPlayerDB, leaveReason );
                Server.RaisePlayerBeingKickedEvent( e );
                if( e.Cancel ) return false;

                if( !e.IsSilent ) {
                    Server.Message( "{0}&W was kicked by {1}",
                                      target.ClassyName, player.ClassyName );
                }

                if( e.RecordToPlayerDB ) {
                    target.Info.ProcessKick( player, reason );
                }

                Server.RaisePlayerKickedEvent( e );
                if( !string.IsNullOrEmpty( reason ) ) {
                    if( !e.IsSilent && ConfigKey.AnnounceKickAndBanReasons.GetBool() ) {
                        Server.Message( "&WKick reason: {0}", reason );
                    }
                    Logger.Log( "{0} was kicked by {1}. Reason: {2}", LogType.UserActivity,
                                target.Name, player.Name, reason );
                    target.Session.Kick( "Kicked by " + player.ClassyName + Color.White + ": " + reason, leaveReason );

                } else {
                    Logger.Log( "{0} was kicked by {1}", LogType.UserActivity,
                                target.Name, player.Name );
                    target.Session.Kick( "You were kicked by " + player.ClassyName, leaveReason );
                }
                return true;
            }
        }

        #endregion


        #region Changing Rank (Promotion / Demotion)

        static readonly CommandDescriptor CdRank = new CommandDescriptor {
            Name = "rank",
            Aliases = new[] { "user", "promote", "demote" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Usage = "/user PlayerName RankName [Reason]",
            Help = "Changes the rank of a player to a specified rank. " +
                   "Any text specified after the RankName will be saved as a memo.",
            Handler = ChangeRank
        };

        internal static void ChangeRank( Player player, Command cmd ) {
            string name = cmd.Next();
            string newRankName = cmd.Next();

            // Check arguments
            if( newRankName == null ) {
                CdRank.PrintUsage( player );
                player.Message( "See &H/ranks&S for list of ranks." );
                return;
            }

            // Parse rank name
            Rank newRank = RankManager.FindRank( newRankName );
            if( newRank == null ) {
                player.MessageNoRank( newRankName );
                return;
            }

            // Parse player name
            PlayerInfo targetInfo = PlayerDB.FindPlayerInfoExact( name );

            if( targetInfo == null ) {
                if( !player.Can( Permission.EditPlayerDB ) ) {
                    player.MessageNoPlayer( name );
                    return;
                }
                if( Player.IsValidName( name ) ) {
                    if( cmd.IsConfirmed ) {
                        targetInfo = PlayerDB.AddFakeEntry( name, (newRank > RankManager.DefaultRank ? RankChangeType.Promoted : RankChangeType.Demoted) );
                    } else {
                        player.AskForConfirmation( cmd, "Warning: Player \"{0}\" is not in the database (possible typo). Type out the full name or", name );
                        return;
                    }
                } else {
                    player.Message( "Player not found. Please specify a valid name." );
                    return;
                }
            }

            DoChangeRank( player, targetInfo, newRank, cmd.NextAll(), false, false );
        }


        /// <summary> Changes player's rank. This needs refactoring BADLY. </summary>
        /// <param name="player"> Player who originated the promotion/demotion action. Must not be null. </param>
        /// <param name="targetInfo"> PlayerInfo of the target player (the one getting promoted/demoted). Must not be null. </param>
        /// <param name="newRank"> New rank to give to target. Must not be null. </param>
        /// <param name="reason"> Reason for promotion/demotion. May be null. </param>
        /// <param name="silent"> Whether rank change should be announced or not. </param>
        /// <param name="automatic"> Whether rank change should be marked as "automatic" or manual. </param>
        public static void DoChangeRank( Player player, PlayerInfo targetInfo, Rank newRank, string reason, bool silent, bool automatic ) {

            if( player == null ) throw new ArgumentNullException( "player" );
            if( targetInfo == null ) throw new ArgumentNullException( "targetInfo" );
            if( newRank == null ) throw new ArgumentNullException( "newRank" );

            bool promote = (targetInfo.Rank < newRank);
            Player target = targetInfo.PlayerObject;

            // Make sure it's not same rank
            if( targetInfo.Rank == newRank ) {
                player.Message( "{0}&S is already ranked {1}",
                                targetInfo.ClassyName,
                                newRank.ClassyName );
                return;
            }

            // Make sure player has the general permissions
            if( promote && !player.Can( Permission.Promote ) ) {
                player.MessageNoAccess( Permission.Promote );
                return;
            } else if( !promote && !player.Can( Permission.Demote ) ) {
                player.MessageNoAccess( Permission.Demote );
                return;
            }

            // Make sure player has the specific permissions (including limits)
            if( promote && !player.Can( Permission.Promote, newRank ) ) {
                player.Message( "You can only promote players up to {0}",
                                player.Info.Rank.GetLimit( Permission.Promote ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                targetInfo.ClassyName,
                                targetInfo.Rank.ClassyName );
                return;
            } else if( !promote && !player.Can( Permission.Demote, targetInfo.Rank ) ) {
                player.Message( "You can only demote players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit( Permission.Demote ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                targetInfo.ClassyName,
                                targetInfo.Rank.ClassyName );
                return;
            }

            if( ConfigKey.RequireRankChangeReason.GetBool() && String.IsNullOrEmpty( reason ) ) {
                if( promote ) {
                    player.Message( "&WPlease specify a promotion reason." );
                } else {
                    player.Message( "&WPlease specify a demotion reason." );
                }
                CdRank.PrintUsage( player );
                return;
            }

            RankChangeType changeType;
            if( newRank >= targetInfo.Rank ) {
                changeType = (automatic ? RankChangeType.AutoPromoted : RankChangeType.Promoted);
            } else {
                changeType = (automatic ? RankChangeType.AutoDemoted : RankChangeType.Demoted);
            }

            string verb = (promote ? "promoted" : "demoted");

            // Do the rank change
            if( (promote && targetInfo.Rank < newRank) ||
                (!promote && targetInfo.Rank > newRank) ) {
                Rank oldRank = targetInfo.Rank;

                if( Server.RaisePlayerInfoRankChangingEvent( targetInfo, player, newRank, reason, changeType ) ) {
                    throw new OperationCanceledException( "Cancelled by plugin." );
                }

                if( !silent ) Logger.Log( "{0} {1} {2} from {3} to {4}.", LogType.UserActivity,
                                          player.Name, verb, targetInfo.Name, targetInfo.Rank.Name, newRank.Name );

                // if player is online, toggle visible/invisible players
                if( target != null && target.World != null ) {

                    HashSet<Player> invisiblePlayers = new HashSet<Player>();

                    Player[] worldPlayerList = target.World.Players;
                    for( int i = 0; i < worldPlayerList.Length; i++ ) {
                        if( !target.CanSee( worldPlayerList[i] ) ) {
                            invisiblePlayers.Add( worldPlayerList[i] );
                        }
                    }


                    // ==== Actual rank change happens here ====
                    targetInfo.ProcessRankChange( newRank, player, reason, changeType );
                    Server.RaisePlayerListChangedEvent();
                    Server.RaisePlayerInfoRankChangedEvent( targetInfo, player, oldRank, reason, changeType );
                    // ==== Actual rank change happens here ====


                    // change admincrete deletion permission
                    target.Send( PacketWriter.MakeSetPermission( target ) );

                    // inform the player of the rank change
                    target.Message( "You have been {0} to {1}&S by {2}",
                                    verb,
                                    newRank.ClassyName,
                                    player.ClassyName );

                    // check if player is still patrollable by others
                    target.World.CheckIfPlayerIsPatrollable( target );

                } else {
                    // ==== Actual rank change happens here (offline) ====
                    targetInfo.ProcessRankChange( newRank, player, reason, changeType );
                    Server.RaisePlayerInfoRankChangedEvent( targetInfo, player, oldRank, reason, changeType );
                    // ==== Actual rank change happens here (offline) ====
                }

                if( !silent ) {
                    if( ConfigKey.AnnounceRankChanges.GetBool() ) {
                        Server.Message( target,
                                        "{0}&S {1} {2} from {3}&S to {4}",
                                        player.ClassyName,
                                        verb,
                                        targetInfo.Name,
                                        oldRank.ClassyName,
                                        newRank.ClassyName );
                        if( ConfigKey.AnnounceRankChangeReasons.GetBool() && !String.IsNullOrEmpty( reason ) ) {
                            Server.Message( "&S{0} reason: {1}",
                                              promote ? "Promotion" : "Demotion",
                                              reason );
                        }
                    } else {
                        player.Message( "You {0} {1} from {2}&S to {3}",
                                        verb,
                                        targetInfo.Name,
                                        oldRank.ClassyName,
                                        newRank.ClassyName );
                        if( !String.IsNullOrEmpty( reason ) ) {
                            target.Message( "&S{0} reason: {1}",
                                            promote ? "Promotion" : "Demotion",
                                            reason );
                        }
                    }
                }

            } else {
                player.Message( "{0}&S is already same or {1} rank than {2}",
                                targetInfo.ClassyName,
                                (promote ? "higher" : "lower"),
                                newRank.ClassyName );
            }
        }

        #endregion


        #region Hide

        static readonly CommandDescriptor CdHide = new CommandDescriptor {
            Name = "hide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/hide [silent]",
            Help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/unhide&S to reveal yourself.",
            Handler = Hide
        };

        internal static void Hide( Player player, Command cmd ) {
            if( player.IsHidden ) {
                player.Message( "You are already hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            player.IsHidden = true;
            player.Message( "{0}You are now hidden.", Color.Gray );

            // to make it look like player just logged out in /info
            player.Info.LastSeen = DateTime.UtcNow;

            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.GetBool() ) {
                    Server.Players.CantSee( player ).Message( "&SPlayer {0}&S left the server.", player.ClassyName );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.GetBool() ) {
                    IRC.PlayerDisconnectedHandler( null, new PlayerDisconnectedEventArgs( player, LeaveReason.ClientQuit ) );
                }
            }

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is now hidden.", player.ClassyName );

            Server.RaisePlayerHideChangedEvent( player );
        }



        static readonly CommandDescriptor CdUnhide = new CommandDescriptor {
            Name = "unhide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/unhide [silent]",
            Help = "Disables the &H/hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            Handler = Unhide
        };

        internal static void Unhide( Player player, Command cmd ) {
            if( !player.IsHidden ) {
                player.Message( "You are not currently hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is no longer hidden.",
                                                     player.ClassyName );

            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.GetBool() ) {
                    Server.Players.CantSee( player ).Message( Server.MakePlayerConnectedMessage( player, false, player.World ) );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.GetBool() ) {
                    IRC.PlayerReadyHandler( null, new PlayerConnectedEventArgs( player, player.World ) );
                }
            }

            player.Message( "You are no longer hidden.", Color.Gray );
            player.IsHidden = false;
            Server.RaisePlayerHideChangedEvent( player );
        }

        #endregion


        #region Set Spawn

        static readonly CommandDescriptor CdSetSpawn = new CommandDescriptor {
            Name = "setspawn",
            Category = CommandCategory.Moderation | CommandCategory.World,
            Permissions = new[] { Permission.SetSpawn },
            Help = "Assigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            Usage = "/setspawn [PlayerName]",
            Handler = SetSpawn
        };

        internal static void SetSpawn( Player player, Command cmd ) {
            string playerName = cmd.Next();
            if( playerName == null ) {
                player.World.Map.Spawn = player.Position;
                player.Send( PacketWriter.MakeSelfTeleport( player.World.Map.Spawn ) );
                player.Send( PacketWriter.MakeAddEntity( 255, player.GetListName(), player.Position ) );
                player.Message( "New spawn point saved." );
                Logger.Log( "{0} changed the spawned point.", LogType.UserActivity,
                            player.Name );
            } else if( player.Can( Permission.Bring ) ) {
                Player[] infos = player.World.FindPlayers( player, playerName );
                if( infos.Length == 1 ) {
                    Player target = infos[0];
                    if( player.Can( Permission.Bring, target.Info.Rank ) ) {
                        target.Send( PacketWriter.MakeAddEntity( 255, target.GetListName(), player.Position ) );
                    } else {
                        player.Message( "You can only set spawn of players ranked {0}&S or lower.",
                                        player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                        player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    }

                } else if( infos.Length > 0 ) {
                    player.MessageManyMatches( "player", infos );

                } else {
                    infos = Server.FindPlayers( player, playerName );
                    if( infos.Length > 0 ) {
                        player.Message( "You can only set spawn of players on the same world as you." );
                    } else {
                        player.MessageNoPlayer( playerName );
                    }
                }
            } else {
                player.MessageNoAccess( Permission.Bring, Permission.SetSpawn );
            }
        }

        #endregion


        #region Freeze

        static readonly CommandDescriptor CdFreeze = new CommandDescriptor {
            Name = "freeze",
            Aliases = new[] { "f" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/freeze PlayerName",
            Help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            Handler = Freeze
        };

        internal static void Freeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false );
            if( target == null ) return;

            if( player.Can( Permission.Freeze, target.Info.Rank ) ) {
                if( target.Info.Freeze( player.Name ) ) {
                    Server.Message( "{0}&S has been frozen by {1}",
                                      target.ClassyName, player.ClassyName );
                } else {
                    player.Message( "{0}&S is already frozen.", target.ClassyName );
                }
            } else {
                player.Message( "You can only freeze players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit( Permission.Kick ).ClassyName );
                player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
            }
        }



        static readonly CommandDescriptor CdUnfreeze = new CommandDescriptor {
            Name = "unfreeze",
            Aliases = new[] { "uf" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/unfreeze PlayerName",
            Help = "Releases the player from a frozen state. See &H/help freeze&S for more information.",
            Handler = Unfreeze
        };

        internal static void Unfreeze( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false );
            if( target == null ) return;

            if( player.Can( Permission.Freeze, target.Info.Rank ) ) {
                if( target.Info.Unfreeze() ) {
                    Server.Message( "{0}&S is no longer frozen.", target.ClassyName );
                } else {
                    player.Message( "{0}&S is currently not frozen.", target.ClassyName );
                }
            } else {
                player.Message( "You can only unfreeze players ranked {0}&S or lower",
                                player.Info.Rank.GetLimit( Permission.Kick ).ClassyName );
                player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
            }
        }

        #endregion


        #region TP

        static readonly CommandDescriptor CdTP = new CommandDescriptor {
            Name = "tp",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Teleport },
            Usage = "/tp PlayerName&S or &H/tp X Y Z",
            Help = "Teleports you to a specified player's location. " +
                   "If coordinates are given, teleports to that location.",
            Handler = TP
        };

        internal static void TP( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdTP.PrintUsage( player );
                return;
            }

            if( cmd.Next() != null ) {
                cmd.Rewind();
                int x, y, h;
                if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out h ) ) {

                    if( x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || h <= -1024 || h >= 1024 ) {
                        player.Message( "Coordinates are outside the valid range!" );

                    } else {
                        player.StopSpectating();
                        player.Send( PacketWriter.MakeTeleport( 255, new Position {
                            X = (short)(x * 32 + 16),
                            Y = (short)(y * 32 + 16),
                            H = (short)(h * 32 + 16),
                            R = player.Position.R,
                            L = player.Position.L
                        } ) );
                    }
                } else {
                    CdTP.PrintUsage( player );
                }

            } else {
                Player[] matches = Server.FindPlayers( player, name );
                if( matches.Length == 1 ) {
                    Player target = matches[0];

                    if( target.World == player.World ) {
                        player.StopSpectating();
                        player.Send( PacketWriter.MakeSelfTeleport( target.Position ) );

                    } else {
                        switch( target.World.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                if( target.World.IsFull ) {
                                    player.Message( "Cannot teleport to {0}&S because world {1}&S is full.",
                                                    target.ClassyName,
                                                    target.World.ClassyName );
                                    return;
                                }
                                player.StopSpectating();
                                player.Session.JoinWorld( target.World, target.Position );
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "Cannot teleport to {0}&S because you are blacklisted on world {1}&S.",
                                                target.ClassyName,
                                                target.World.ClassyName );
                                break;
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                                target.ClassyName,
                                                target.World.ClassyName,
                                                target.World.AccessSecurity.MinRank.ClassyName );
                                break;
                            // TODO: case PermissionType.RankTooHigh:
                        }
                    }

                } else if( matches.Length > 1 ) {
                    player.MessageManyMatches( "player", matches );

                } else {
                    // Try to guess if player typed "/tp" instead of "/join"
                    World[] worlds = WorldManager.FindWorlds( name );
                    SearchingForWorldEventArgs e = new SearchingForWorldEventArgs( player, name, worlds.ToList(), true );
                    WorldManager.RaiseSearchingForWorldEvent( e );
                    worlds = e.Matches.ToArray();

                    if( worlds.Length == 1 ) {
                        player.StopSpectating();
                        player.ParseMessage( "/join " + name, false );
                    } else {
                        player.MessageNoPlayer( name );
                    }
                }
            }
        }

        #endregion


        #region Bring / WorldBring / BringAll

        static readonly CommandDescriptor CdBring = new CommandDescriptor {
            Name = "bring",
            IsConsoleSafe = true,
            Aliases = new[] { "summon", "fetch" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/bring PlayerName [ToPlayer]",
            Help = "Teleports another player to your location. " +
                   "If the optional second parameter is given, teleports player to another player.",
            Handler = Bring
        };

        static void Bring( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            // bringing someone to another player (instead of to self)
            string toName = cmd.Next();
            Player toPlayer = player;
            if( toName != null ) {
                toPlayer = Server.FindPlayerOrPrintMatches( player, toName, false );
                if( toPlayer == null ) return;
            } else if( player.World == null ) {
                player.Message( "When used from console, /bring requires both names to be given." );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false );
            if( target == null ) return;

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You can only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( target.World == toPlayer.World ) {
                // teleport within the same world
                target.StopSpectating();
                target.Send( PacketWriter.MakeSelfTeleport( toPlayer.Position ) );
                target.Position = toPlayer.Position;
                if( target.Info.IsFrozen ) {
                    target.Position = toPlayer.Position;
                }

            } else {
                // teleport to a different world
                BringPlayerToWorld( player, target, toPlayer.World );
            }
        }


        static readonly CommandDescriptor CdWorldBring = new CommandDescriptor {
            Name = "wbring",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/wbring PlayerName WorldName",
            Help = "Teleports a player to the given world's spawn.",
            Handler = WorldBring
        };

        static void WorldBring( Player player, Command cmd ) {
            string playerName = cmd.Next();
            string worldName = cmd.Next();
            if( playerName == null || worldName == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, playerName, false );
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );

            if( target == null || world == null ) return;

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You can only wbring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( world == target.World ) {
                player.Message( "Player {0}&S is already in world {1}",
                                target.ClassyName, world.ClassyName );
                return;
            }

            BringPlayerToWorld( player, target, world );
        }


        static readonly CommandDescriptor CdBringAll = new CommandDescriptor {
            Name = "bringall",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring, Permission.BringAll },
            Usage = "/bringall [@Rank [@AnotherRank]] [*|World [AnotherWorld]]",
            Help = "Teleports all players from your world to you. " +
                   "If any world names are given, only teleports players from those worlds. " +
                   "If any rank names are given, only teleports players of those ranks.",
            Handler = BringAll
        };

        static void BringAll( Player player, Command cmd ) {
            List<World> targetWorlds = new List<World>();
            List<Rank> targetRanks = new List<Rank>();
            bool allWorlds = false;
            bool allRanks = true;

            // Parse the list of worlds and ranks
            string arg;
            while( (arg = cmd.Next()) != null ) {
                if( arg.StartsWith( "@" ) ) {
                    Rank rank = RankManager.ParseRank( arg.Substring( 1 ) );
                    if( rank == null ) {
                        player.Message( "Unknown rank: {0}", arg.Substring( 1 ) );
                        return;
                    } else {
                        if( player.Can( Permission.Bring, rank ) ) {
                            targetRanks.Add( rank );
                        } else {
                            player.Message( "&WYou are not allowed to &H/bring&W players of rank {0}",
                                            rank.ClassyName );
                        }
                        allRanks = false;
                    }
                } else if( arg == "*" ) {
                    allWorlds = true;
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, arg );
                    if( world == null ) return;
                    targetWorlds.Add( world );
                }
            }

            // If no worlds were specified, use player's current world
            if( !allWorlds && targetWorlds.Count == 0 ) {
                targetWorlds.Add( player.World );
            }

            // Apply all the rank and world options
            HashSet<Player> targetPlayers;
            if( allRanks && allWorlds ) {
                targetPlayers = new HashSet<Player>( Server.Players );
            } else if( allWorlds ) {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( Player rankPlayer in Server.Players.Ranked( rank ) ) {
                        targetPlayers.Add( rankPlayer );
                    }
                }
            } else if( allRanks ) {
                targetPlayers = new HashSet<Player>();
                foreach( World world in targetWorlds ) {
                    Player[] worldPlayers = world.Players;
                    foreach( Player worldPlayer in worldPlayers ) {
                        targetPlayers.Add( worldPlayer );
                    }
                }
            } else {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( World world in targetWorlds ) {
                        foreach( Player rankWorldPlayer in world.Players.Ranked( rank ) ) {
                            targetPlayers.Add( rankWorldPlayer );
                        }
                    }
                }
            }

            // Remove the player him/herself
            targetPlayers.Remove( player );

            // Check if there's anyone to bring
            if( targetPlayers.Count == 0 ) {
                player.Message( "No players to bring!" );
            } else {
                player.Message( "Bringing {0} players...", targetPlayers.Count );
            }

            // Actually bring all the players
            foreach( Player targetPlayer in targetPlayers ) {
                Bring( player, new Command( "/bring " + targetPlayer.Name ) );
            }
        }




        static void BringPlayerToWorld( Player player, Player target, World world ) {
            switch( world.AccessSecurity.CheckDetailed( target.Info ) ) {
                case SecurityCheckResult.Allowed:
                case SecurityCheckResult.WhiteListed:
                    if( world.IsFull ) {
                        player.Message( "Cannot bring {0}&S because world {1}&S is full.",
                                        target.ClassyName,
                                        world.ClassyName );
                        return;
                    }
                    target.StopSpectating();
                    target.Session.JoinWorld( world );
                    break;
                case SecurityCheckResult.BlackListed:
                    player.Message( "Cannot bring {0}&S because he/she is blacklisted on world {1}",
                                    target.ClassyName,
                                    world.ClassyName );
                    break;
                case SecurityCheckResult.RankTooLow:
                    player.Message( "Cannot bring {0}&S because world {1}&S requires {2}+&S to join.",
                                    target.ClassyName,
                                    world.ClassyName,
                                    world.AccessSecurity.MinRank.ClassyName );
                    break;
                // TODO: case PermissionType.RankTooHigh:
            }
        }

        #endregion


        #region Patrol & SpecPatrol

        static readonly CommandDescriptor CdPatrol = new CommandDescriptor {
            Name = "patrol",
            Aliases = new[] { "pat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = Patrol
        };

        internal static void Patrol( Player player, Command cmd ) {
            Player target = player.World.GetNextPatrolTarget();
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            if( target == player ) {
                target = player.World.GetNextPatrolTarget();
                if( target == player ) {
                    player.Message( "Patrol: No one to patrol in this world (except yourself)." );
                    return;
                }
            }

            player.StopSpectating();
            player.Message( "Patrol: Teleporting to {0}", target.ClassyName );
            player.Send( PacketWriter.MakeSelfTeleport( target.Position ) );
        }


        static readonly CommandDescriptor CdSpecPatrol = new CommandDescriptor {
            Name = "specpatrol",
            Aliases = new[] { "spat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = SpecPatrol
        };

        internal static void SpecPatrol( Player player, Command cmd ) {
            Player target = player.World.GetNextPatrolTarget();
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            if( target == player ) {
                target = player.World.GetNextPatrolTarget();
                if( target == player ) {
                    player.Message( "Patrol: No one to patrol in this world (except yourself)." );
                    return;
                }
            }

            player.Spectate( target );
            player.Message( "SpecPatrol: Spectating {0}", target.ClassyName );
        }

        #endregion


        #region Mute / Unmute

        static readonly CommandDescriptor CdMute = new CommandDescriptor {
            Name = "mute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Mutes a player for a specified number of seconds.",
            Usage = "/mute PlayerName Seconds",
            Handler = Mute
        };

        internal static void Mute( Player player, Command cmd ) {
            string targetName = cmd.Next();
            int seconds;
            if( targetName != null && Player.IsValidName( targetName ) && cmd.NextInt( out seconds ) && seconds > 0 ) {
                Player target = Server.FindPlayerOrPrintMatches( player, targetName, false );
                if( target == null ) return;

                if( !player.Can( Permission.Mute, target.Info.Rank ) ) {
                    player.Message( "You can only mute players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Mute ).ClassyName );
                    player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    return;
                }

                if( target.Info.Mute( player.Name, TimeSpan.FromSeconds( seconds ) ) ) {
                    target.Message( "You were muted by {0}&S for {1} sec", player.ClassyName, seconds );
                    Server.Message( target,
                                    "&SPlayer {0}&S was muted by {1}&S for {2} sec",
                                    target.ClassyName, player.ClassyName, seconds );
                    Logger.Log( "Player {0} was muted by {1} for {2} seconds.", LogType.UserActivity,
                                target.Name, player.Name, seconds );
                } else {
                    player.Message( "Player {0}&S is already muted by {1}&S for {2:0} more seconds.",
                                    target.ClassyName,
                                    target.Info.MutedBy,
                                    target.Info.MutedUntil.Subtract( DateTime.UtcNow ).TotalSeconds );
                }

            } else {
                CdMute.PrintUsage( player );
            }
        }



        static readonly CommandDescriptor CdUnmute = new CommandDescriptor {
            Name = "unmute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Unmutes a player.",
            Usage = "/unmute PlayerName",
            Handler = Unmute
        };

        internal static void Unmute( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName != null && Player.IsValidName( targetName ) ) {

                Player target = Server.FindPlayerOrPrintMatches( player, targetName, false );
                if( target == null ) return;

                if( !player.Can( Permission.Mute, target.Info.Rank ) ) {
                    player.Message( "You can only unmute players ranked {0}&S or lower.",
                                    player.Info.Rank.GetLimit( Permission.Mute ).ClassyName );
                    player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    return;
                }

                if( target.Info.MutedUntil >= DateTime.UtcNow ) {
                    target.Info.Unmute();
                    target.Message( "You were unmuted by {0}", player.ClassyName );
                    Server.Message( target,
                                    "&SPlayer {0}&S was unmuted by {1}",
                                    target.ClassyName, player.ClassyName );
                    Logger.Log( "Player {0} was unmuted by {1}.", LogType.UserActivity,
                                target.Name, player.Name );
                } else {
                    player.Message( "Player {0}&S is not muted.", target.ClassyName );
                }

            } else {
                CdUnmute.PrintUsage( player );
            }
        }

        #endregion
    }
}