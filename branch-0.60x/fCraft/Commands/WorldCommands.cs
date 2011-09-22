﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fCraft.MapConversion;

namespace fCraft {
    /// <summary>
    /// Contains commands related to world management.
    /// </summary>
    static class WorldCommands {
        internal static void Init() {
            CommandManager.RegisterCommand( CdBlockDB );
            CommandManager.RegisterCommand( CdBlockInfo );

            CdGenerate.Help = "Generates a new map. If no dimensions are given, uses current world's dimensions. " +
                              "If no filename is given, loads generated world into current world.\n" +
                              "Available themes: Grass, " + Enum.GetNames( typeof( MapGenTheme ) ).JoinToString() + '\n' +
                              "Available terrain types: " + Enum.GetNames( typeof( MapGenTemplate ) ).JoinToString() + '\n' +
                              "NOTE: Map is saved TO FILE ONLY, use /wload to load it.";
            CommandManager.RegisterCommand( CdGenerate );

            CommandManager.RegisterCommand( CdJoin );

            CommandManager.RegisterCommand( CdWorldLock );
            CommandManager.RegisterCommand( CdWorldUnlock );

            CommandManager.RegisterCommand( CdSpawn );

            CommandManager.RegisterCommand( CdWorlds );
            CommandManager.RegisterCommand( CdWorldAccess );
            CommandManager.RegisterCommand( CdWorldBuild );
            CommandManager.RegisterCommand( CdWorldFlush );

            CommandManager.RegisterCommand( CdWorldHide );
            CommandManager.RegisterCommand( CdWorldUnhide );

            CommandManager.RegisterCommand( CdWorldInfo );
            CommandManager.RegisterCommand( CdWorldLoad );
            CommandManager.RegisterCommand( CdWorldMain );
            CommandManager.RegisterCommand( CdWorldRename );
            CommandManager.RegisterCommand( CdWorldSave );
            CommandManager.RegisterCommand( CdWorldUnload );
        }


        #region BlockDB

        static readonly CommandDescriptor CdBlockDB = new CommandDescriptor {
            Name = "blockdb",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageBlockDB },
            Usage = "/blockdb WorldName <Operation>",
            Help = "Manages BlockDB on a given world. " +
                   "Operations are: on, off, clear, limit, timelimit, and preload. " +
                   "See &H/help blockdb <Operation>&S for operation-specific help. " +
                   "If no operation is given, BlockDB status is shown.",
            HelpSections = new Dictionary<string, string>{
                { "auto",       "Allows BlockDB to decide whether it should be enabled or disabled based on each world's permissions (default)." },
                { "on",         "Enables block tracking. Information will only be available for blocks that changed while BlockDB was enabled." },
                { "off",        "Disables block tracking. Block changes will NOT be recorded while BlockDB is disabled. " +
                                "Note that disabling BlockDB does not delete the existing data. Use &Hclear&S for that." },
                { "clear",      "Clears all recorded data from the BlockDB. Erases all changes from memory and deletes the .fbdb file." },
                { "limit",      "Sets the limit on the maximum number of changes to store for a given world. " +
                                "Oldest changes will be deleted once the limit is reached. " +
                                "Put \"None\" to disable limiting. " +
                                "Unless a Limit or a TimeLimit it specified, all changes will be stored indefinitely." },
                { "timelimit",  "Sets the age limit for stored changes. " +
                                "Oldest changes will be deleted once the limit is reached. " +
                                "Use \"None\" to disable time limiting. " +
                                "Unless a Limit or a TimeLimit it specified, all changes will be stored indefinitely." },
                { "preload",    "Enabled or disables preloading. When BlockDB is preloaded, all changes are stored in memory as well as in a file. " +
                                "This reduces CPU and disk use for busy maps, but may not be suitable for large maps due to increased memory use." },
            },
            Handler = BlockDBHandler
        };

        static void BlockDBHandler( Player player, Command cmd ) {
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }

            string worldName = cmd.Next();
            if( worldName == null ) {
                int total = 0;
                World[] autoEnabledWorlds = WorldManager.WorldList.Where( w => (w.BlockDB.EnabledState == YesNoAuto.Auto) && w.BlockDB.IsEnabled ).ToArray();
                if( autoEnabledWorlds.Length > 0 ) {
                    total += autoEnabledWorlds.Length;
                    player.Message( "BlockDB is auto-enabled on: {0}",
                                    autoEnabledWorlds.JoinToClassyString() );
                }

                World[] manuallyEnabledWorlds = WorldManager.WorldList.Where( w => w.BlockDB.EnabledState == YesNoAuto.Yes ).ToArray();
                if( manuallyEnabledWorlds.Length > 0 ) {
                    total += manuallyEnabledWorlds.Length;
                    player.Message( "BlockDB is manually enabled on: {0}",
                                    manuallyEnabledWorlds.JoinToClassyString() );
                }

                World[] manuallyDisabledWorlds = WorldManager.WorldList.Where( w => w.BlockDB.EnabledState == YesNoAuto.No ).ToArray();
                if( manuallyDisabledWorlds.Length > 0 ) {
                    player.Message( "BlockDB is manually disabled on: {0}",
                                    manuallyDisabledWorlds.JoinToClassyString() );
                }

                if( total == 0 ) {
                    player.Message( "BlockDB is not enabled on any world." );
                }
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;
            BlockDB db = world.BlockDB;

            lock( db.SyncRoot ) {
                string op = cmd.Next();
                if( op == null ) {
                    if( !db.IsEnabled ) {
                        if( db.EnabledState == YesNoAuto.Auto ) {
                            player.Message( "BlockDB is disabled (auto) on world {0}", world.ClassyName );
                        } else {
                            player.Message( "BlockDB is disabled on world {0}", world.ClassyName );
                        }
                    } else {
                        if( db.IsPreloaded ) {
                            if( db.EnabledState == YesNoAuto.Auto ) {
                                player.Message( "BlockDB is enabled (auto) and preloaded on world {0}", world.ClassyName );
                            } else {
                                player.Message( "BlockDB is enabled and preloaded on world {0}", world.ClassyName );
                            }
                        } else {
                            if( db.EnabledState == YesNoAuto.Auto ) {
                                player.Message( "BlockDB is enabled (auto) on world {0}", world.ClassyName );
                            } else {
                                player.Message( "BlockDB is enabled on world {0}", world.ClassyName );
                            }
                        }
                        player.Message( "    Change limit: {0}    Time limit: {1}",
                                        db.Limit == 0 ? "none" : db.Limit.ToString(),
                                        db.TimeLimit == TimeSpan.Zero ? "none" : db.TimeLimit.ToMiniString() );
                    }
                    return;
                }

                switch( op.ToLower() ) {
                    case "on":
                        // enables BlockDB
                        if( db.EnabledState == YesNoAuto.Yes ) {
                            player.Message( "BlockDB is already manually enabled on world {0}", world.ClassyName );

                        } else if( db.EnabledState == YesNoAuto.Auto && db.IsEnabled ) {
                            db.EnabledState = YesNoAuto.Yes;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB was auto-enabled, and is now manually enabled on world {0}", world.ClassyName );

                        } else {
                            db.EnabledState = YesNoAuto.Yes;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB is now manually enabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "off":
                        // disables BlockDB
                        if( db.EnabledState == YesNoAuto.No ) {
                            player.Message( "BlockDB is already manually disabled on world {0}", world.ClassyName );

                        } else if( db.IsEnabled ) {
                            if( cmd.IsConfirmed ) {
                                db.EnabledState = YesNoAuto.No;
                                WorldManager.SaveWorldList();
                                player.Message( "BlockDB is now manually disabled on world {0}&S. Use &H/blockdb {1} clear&S to delete all the data.",
                                                world.ClassyName, world.Name );
                            } else {
                                player.Confirm( cmd,
                                                "Disable BlockDB on world {0}&S? Block changes will stop being recorded.",
                                                world.ClassyName );
                            }
                        } else {
                            db.EnabledState = YesNoAuto.No;
                            WorldManager.SaveWorldList();
                            player.Message( "BlockDB is was auto-disabled, and is now manually disabled on world {0}&S.",
                                            world.ClassyName, world.Name );
                        }
                        break;

                    case "auto":
                        if( db.EnabledState == YesNoAuto.Auto ) {
                            player.Message( "BlockDB is already set to automatically enable/disable itself on world {0}", world.ClassyName );
                        } else {
                            if( db.IsEnabled ) {
                                db.EnabledState = YesNoAuto.Auto;
                                WorldManager.SaveWorldList();
                                player.Message( "BlockDB is now automatically enabled on world {0}&S.",
                                                world.ClassyName );
                            } else {
                                db.EnabledState = YesNoAuto.Auto;
                                WorldManager.SaveWorldList();
                                player.Message( "BlockDB is now automatically disabled on world {0}&S.",
                                                world.ClassyName );
                            }
                        }
                        break;

                    case "limit":
                        // sets or resets limit on the number of changes to store
                        if( db.IsEnabled ) {
                            string limitString = cmd.Next();
                            int limitNumber;

                            if( limitString == null ) {
                                player.Message( "BlockDB: Limit for world {0}&S is {1}",
                                                world.ClassyName,
                                                (db.Limit == 0 ? "none" : db.Limit.ToString()) );
                                return;
                            }

                            if( limitString.Equals( "none", StringComparison.OrdinalIgnoreCase ) ) {
                                limitNumber = 0;

                            } else if( !Int32.TryParse( limitString, out limitNumber ) ) {
                                CdBlockDB.PrintUsage( player );
                                return;

                            } else if( limitNumber < 0 ) {
                                player.Message( "BlockDB: Limit must be non-negative." );
                                return;
                            }

                            if( !cmd.IsConfirmed && limitNumber != 0 ) {
                                player.Confirm( cmd, "BlockDB: Change limit? Some old data for world {0}&S may be discarded.", world.ClassyName );

                            } else {
                                string limitDisplayString = (limitNumber == 0 ? "none" : limitNumber.ToString());
                                if( db.Limit == limitNumber ) {
                                    player.Message( "BlockDB: Limit for world {0}&S is already set to {1}",
                                                   world.ClassyName, limitDisplayString );

                                } else {
                                    db.Limit = limitNumber;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB: Limit for world {0}&S set to {1}",
                                                   world.ClassyName, limitDisplayString );
                                }
                            }

                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "timelimit":
                        // sets or resets limit on the age of changes to store
                        if( db.IsEnabled ) {
                            string limitString = cmd.Next();

                            if( limitString == null ) {
                                if( db.TimeLimit == TimeSpan.Zero ) {
                                    player.Message( "BlockDB: There is no time limit for world {0}",
                                                    world.ClassyName );
                                } else {
                                    player.Message( "BlockDB: Time limit for world {0}&S is {1}",
                                                    world.ClassyName, db.TimeLimit.ToMiniString() );
                                }
                                return;
                            }

                            TimeSpan limit;
                            if( limitString.Equals( "none", StringComparison.OrdinalIgnoreCase ) ) {
                                limit = TimeSpan.Zero;

                            } else if( !limitString.TryParseMiniTimespan( out limit ) ) {
                                CdBlockDB.PrintUsage( player );
                                return;
                            }

                            if( !cmd.IsConfirmed && limit != TimeSpan.Zero ) {
                                player.Confirm( cmd, "BlockDB: Change time limit? Some old data for world {0}&S may be discarded.", world.ClassyName );

                            } else {

                                if( db.TimeLimit == limit ) {
                                    if( db.TimeLimit == TimeSpan.Zero ) {
                                        player.Message( "BlockDB: There is already no time limit for world {0}",
                                                        world.ClassyName );
                                    } else {
                                        player.Message( "BlockDB: Time limit for world {0}&S is already set to {1}",
                                                        world.ClassyName, db.TimeLimit.ToMiniString() );
                                    }
                                } else {
                                    db.TimeLimit = limit;
                                    WorldManager.SaveWorldList();
                                    if( db.TimeLimit == TimeSpan.Zero ) {
                                        player.Message( "BlockDB: Time limit removed for world {0}",
                                                        world.ClassyName );
                                    } else {
                                        player.Message( "BlockDB: Time limit for world {0}&S set to {1}",
                                                        world.ClassyName, db.TimeLimit.ToMiniString() );
                                    }
                                }
                            }

                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    case "clear":
                        // wipes BlockDB data
                        bool hasData = (db.IsEnabled || File.Exists( db.FileName ));
                        if( hasData ) {
                            if( cmd.IsConfirmed ) {
                                db.Clear();
                                player.Message( "BlockDB: Cleared all data for {0}", world.ClassyName );
                            } else {
                                player.Confirm( cmd, "Clear BlockDB data for world {0}&S? This cannot be undone.",
                                                world.ClassyName );
                            }
                        } else {
                            player.Message( "BlockDB: No data to clear for world {0}", world.ClassyName );
                        }
                        break;

                    case "preload":
                        // enables/disables BlockDB preloading
                        if( db.IsEnabled ) {
                            string param = cmd.Next();
                            if( param == null ) {
                                // shows current preload setting
                                player.Message( "BlockDB preloading is {0} for world {1}",
                                                (db.IsPreloaded ? "ON" : "OFF"),
                                                world.ClassyName );

                            } else if( param.Equals( "on", StringComparison.OrdinalIgnoreCase ) ) {
                                // turns preload on
                                if( db.IsPreloaded ) {
                                    player.Message( "BlockDB preloading is already enabled on world {0}", world.ClassyName );
                                } else {
                                    db.IsPreloaded = true;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB preloading is now enabled on world {0}", world.ClassyName );
                                }

                            } else if( param.Equals( "off", StringComparison.OrdinalIgnoreCase ) ) {
                                // turns preload off
                                if( !db.IsPreloaded ) {
                                    player.Message( "BlockDB preloading is already disabled on world {0}", world.ClassyName );
                                } else {
                                    db.IsPreloaded = false;
                                    WorldManager.SaveWorldList();
                                    player.Message( "BlockDB preloading is now disabled on world {0}", world.ClassyName );
                                }

                            } else {
                                CdBlockDB.PrintUsage( player );
                            }
                        } else {
                            player.Message( "Block tracking is disabled on world {0}", world.ClassyName );
                        }
                        break;

                    default:
                        // unknown operand
                        CdBlockDB.PrintUsage( player );
                        return;
                }
            }
        }

        #endregion


        #region BlockInfo

        static readonly CommandDescriptor CdBlockInfo = new CommandDescriptor {
            Name = "binfo",
            Category = CommandCategory.World,
            Aliases = new[] { "bi", "whodid" },
            Permissions = new[] { Permission.ViewOthersInfo },
            Help = "Checks edit history for a given block.",
            Handler = BlockInfoHandler
        };

        static void BlockInfoHandler( Player player, Command cmd ) {
            if( !BlockDB.IsEnabledGlobally ) {
                player.Message( "&WBlockDB is disabled on this server." );
                return;
            }

            World world = player.World;
            if( !world.BlockDB.IsEnabled ) {
                player.Message( "&WBlockDB is disabled in this world." );
                return;
            }

            player.Message( "BInfo: Click a block to look it up." );

            player.SelectionStart( 1, BlockInfoSelectionCallback, null, CdBlockInfo.Permissions );
        }

        static void BlockInfoSelectionCallback( Player player, Position[] marks, object tag ) {
            var args = new BlockInfoLookupArgs {
                Player = player,
                World = player.World,
                X = marks[0].X,
                Y = marks[0].Y,
                Z = marks[0].Z
            };

            Scheduler.NewBackgroundTask( BlockInfoSchedulerCallback, args ).RunOnce();
        }


        sealed class BlockInfoLookupArgs {
            public Player Player;
            public World World;
            public short X, Y, Z;
        }

        const int MaxBlockChangesToList = 15;
        static void BlockInfoSchedulerCallback( SchedulerTask task ) {
            BlockInfoLookupArgs args = (BlockInfoLookupArgs)task.UserState;
            if( !args.World.BlockDB.IsEnabled ) {
                args.Player.Message( "&WBlockDB is disabled in this world." );
                return;
            }
            BlockDBEntry[] results = args.World.BlockDB.Lookup( args.X, args.Y, args.Z );
            if( results.Length > 0 ) {
                int startIndex = Math.Max( 0, results.Length - MaxBlockChangesToList );
                for( int i = startIndex; i < results.Length; i++ ) {
                    BlockDBEntry entry = results[i];
                    string date = DateTime.UtcNow.Subtract( DateTimeUtil.ToDateTime( entry.Timestamp ) ).ToMiniString();

                    PlayerInfo info = PlayerDB.FindPlayerInfoByID( entry.PlayerID );
                    string playerName;
                    if( info == null ) {
                        playerName = "?";
                    } else {
                        Player target = info.PlayerObject;
                        if( target != null && args.Player.CanSee( target ) ) {
                            playerName = info.ClassyName;
                        } else {
                            playerName = info.ClassyName + "&S (offline)";
                        }
                    }
                    if( entry.OldBlock == (byte)Block.Air ) {
                        args.Player.Message( "&S  {0} ago: {1}&S placed {2}.", date, playerName, entry.NewBlock );
                    } else if( entry.NewBlock == (byte)Block.Air ) {
                        args.Player.Message( "&S  {0} ago: {1}&S deleted {2}.", date, playerName, entry.OldBlock );
                    } else {
                        args.Player.Message( "&S  {0} ago: {1}&S replaced {2} with {3}.", date, playerName, entry.OldBlock, entry.NewBlock );
                    }
                }
            } else {
                args.Player.Message( "BlockInfo: No results for ({0},{1},{2}).",
                                     args.X, args.Y, args.Z );
            }
        }

        #endregion


        #region Gen

        static readonly CommandDescriptor CdGenerate = new CommandDescriptor {
            Name = "gen",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/gen ThemeName TemplateName [X Y Height [FileName]]",
            //Help is assigned by WorldCommands.Init
            Handler = GenHandler
        };

        static void GenHandler( Player player, Command cmd ) {

            string themeName = cmd.Next();
            string templateName = cmd.Next();

            if( templateName == null ) {
                CdGenerate.PrintUsage( player );
                return;
            }

            MapGenTemplate template;
            MapGenTheme theme;

            int wx, wy, height;
            if( !(cmd.NextInt( out wx ) && cmd.NextInt( out wy ) && cmd.NextInt( out height )) ) {
                if( player.World != null ) {
                    wx = player.World.Map.Width;
                    wy = player.World.Map.Length;
                    height = player.World.Map.Height;
                } else {
                    player.Message( "When used from console, /gen requires map dimensions." );
                    CdGenerate.PrintUsage( player );
                    return;
                }
                cmd.Rewind();
                cmd.Next();
                cmd.Next();
            }

            const string dimensionRecommendation = "Dimensions must be between 1 and 2047. " +
                                                   "Recommended values: 16, 32, 64, 128, 256, 512, and 1024.";
            if( !Map.IsValidDimension( wx ) ) {
                player.Message( "Cannot make map with width {0}. {1}", wx, dimensionRecommendation );
                return;
            } else if( !Map.IsValidDimension( wy ) ) {
                player.Message( "Cannot make map with length {0}. {1}", wy, dimensionRecommendation );
                return;
            } else if( !Map.IsValidDimension( height ) ) {
                player.Message( "Cannot make map with height {0}. {1}", height, dimensionRecommendation );
                return;
            }

            if( !cmd.IsConfirmed && (!Map.IsRecommendedDimension( wx ) || !Map.IsRecommendedDimension( wy )) ) {
                player.Message( "&WThe map will have non-standard dimensions. " +
                                "You may see glitched blocks or visual artifacts. " +
                                "The only recommended map dimensions are: 16, 32, 64, 128, 256, 512, and 1024." );
            }

            string fileName = cmd.Next();
            string fullFileName = null;

            if( fileName == null ) {
                if( player.World == null ) {
                    player.Message( "When used from console, /gen requires FileName." );
                    CdGenerate.PrintUsage( player );
                    return;
                }
                if( !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "Replace this world's map with a generated one?" );
                    return;
                }
            } else {
                fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
                if( !fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                    fileName += ".fcm";
                }
                fullFileName = Path.Combine( Paths.MapPath, fileName );
                if( !Paths.IsValidPath( fullFileName ) ) {
                    player.Message( "Invalid filename." );
                    return;
                }
                if( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                    player.MessageUnsafePath();
                    return;
                }
                string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
                if( !Directory.Exists( dirName ) ) {
                    Directory.CreateDirectory( dirName );
                }
                if( !cmd.IsConfirmed && File.Exists( fullFileName ) ) {
                    player.Confirm( cmd, "The mapfile \"{0}\" already exists. Overwrite?", fileName );
                    return;
                }
            }

            bool noTrees;
            if( themeName.Equals( "grass", StringComparison.OrdinalIgnoreCase ) ) {
                theme = MapGenTheme.Forest;
                noTrees = true;
            } else {
                try {
                    theme = (MapGenTheme)Enum.Parse( typeof( MapGenTheme ), themeName, true );
                    noTrees = (theme != MapGenTheme.Forest);
                } catch( Exception ) {
                    player.MessageNow( "Unrecognized theme \"{0}\". Available themes are: Grass, {1}",
                                       themeName,
                                       String.Join( ", ", Enum.GetNames( typeof( MapGenTheme ) ) ) );
                    return;
                }
            }

            try {
                template = (MapGenTemplate)Enum.Parse( typeof( MapGenTemplate ), templateName, true );
            } catch( Exception ) {
                player.Message( "Unrecognized template \"{0}\". Available templates are: {1}",
                                templateName,
                                String.Join( ", ", Enum.GetNames( typeof( MapGenTemplate ) ) ) );
                return;
            }

            if( !Enum.IsDefined( typeof( MapGenTheme ), theme ) || !Enum.IsDefined( typeof( MapGenTemplate ), template ) ) {
                CdGenerate.PrintUsage( player );
                return;
            }

            MapGeneratorArgs args = MapGenerator.MakeTemplate( template );
            args.MapWidth = wx;
            args.MapLength = wy;
            args.MapHeight = height;
            args.MaxHeight = (int)(args.MaxHeight / 80d * height);
            args.MaxDepth = (int)(args.MaxDepth / 80d * height);
            args.Theme = theme;
            args.AddTrees = !noTrees;

            Map map;
            try {
                if( theme == MapGenTheme.Forest && noTrees ) {
                    player.MessageNow( "Generating Grass {0}...", template );
                } else {
                    player.MessageNow( "Generating {0} {1}...", theme, template );
                }
                if( theme == MapGenTheme.Forest && noTrees && template == MapGenTemplate.Flat ) {
                    map = MapGenerator.GenerateFlatgrass( args.MapWidth, args.MapLength, args.MapHeight );
                } else {
                    MapGenerator generator = new MapGenerator( args );
                    map = generator.Generate();
                }

            } catch( Exception ex ) {
                Logger.Log( "MapGenerator: Generation failed: {0}", LogType.Error,
                            ex );
                player.MessageNow( "&WAn error occured while generating the map." );
                return;
            }

            if( fileName != null ) {
                if( map.Save( fullFileName ) ) {
                    player.MessageNow( "Generation done. Saved to {0}", fileName );
                } else {
                    player.Message( "&WAn error occured while saving generated map to {0}", fileName );
                }
            } else {
                player.MessageNow( "Generation done. Changing map..." );
                player.World.ChangeMap( map );
                player.World.MapChangedBy = player.Name;
                player.World.MapChangedOn = DateTime.UtcNow;
            }
        }

        #endregion


        #region Join

        static readonly CommandDescriptor CdJoin = new CommandDescriptor {
            Name = "join",
            Aliases = new[] { "j", "load", "goto", "map" },
            Category = CommandCategory.World,
            Usage = "/join WorldName",
            Help = "Teleports the player to a specified world. You can see the list of available worlds by using &H/worlds",
            Handler = JoinHandler
        };

        static void JoinHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdJoin.PrintUsage( player );
                return;
            }

            World[] worlds = WorldManager.FindWorlds( player, worldName );

            if( worlds.Length > 1 ) {
                player.MessageManyMatches( "world", worlds );

            } else if( worlds.Length == 1 ) {
                World world = worlds[0];
                switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.Allowed:
                    case SecurityCheckResult.WhiteListed:
                        if( world.IsFull ) {
                            player.Message( "Cannot join {0}&S: world is full.", world.ClassyName );
                            return;
                        }
                        player.StopSpectating();
                        if( !player.JoinWorldNow( world, true, WorldChangeReason.ManualJoin ) ) {
                            player.Message( "ERROR: Failed to join world. See log for details." );
                        }
                        break;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "Cannot join world {0}&S: you are blacklisted",
                                        world.ClassyName );
                        break;
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "Cannot join world {0}&S: must be {1}+",
                                        world.ClassyName, world.AccessSecurity.MinRank.ClassyName );
                        break;
                }

            } else {
                // no worlds found - see if player meant to type in "/join" and not "/tp"
                Player[] players = Server.FindPlayers( player, worldName, true );
                if( players.Length == 1 ) {
                    player.StopSpectating();
                    player.ParseMessage( "/tp " + players[0].Name, false );
                } else {
                    player.MessageNoWorld( worldName );
                }
            }
        }

        #endregion


        #region WLock, WUnlock

        static readonly CommandDescriptor CdWorldLock = new CommandDescriptor {
            Name = "wlock",
            Aliases = new[] { "lock" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/wlock [*|WorldName]",
            Help = "Puts the world into a locked, read-only mode. " +
                   "No one can place or delete blocks during lockdown. " +
                   "By default this locks the world you're on, but you can also lock any world by name. " +
                   "Put an asterisk (*) for world name to lock ALL worlds at once. " +
                   "Call &H/unlock&S to release lock on a world.",
            Handler = WorldLockHandler
        };

        static void WorldLockHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            World world;
            if( worldName != null ) {
                if( worldName == "*" ) {
                    int locked = 0;
                    World[] worldListCache = WorldManager.WorldList;
                    for( int i = 0; i < worldListCache.Length; i++ ) {
                        if( !worldListCache[i].IsLocked ) {
                            worldListCache[i].Lock( player );
                            locked++;
                        }
                    }
                    player.Message( "Unlocked {0} worlds.", locked );
                    return;
                } else {
                    world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world == null ) return;
                }

            } else if( player.World != null ) {
                world = player.World;

            } else {
                player.Message( "When called from console, /wlock requires a world name." );
                return;
            }

            if( !world.Lock( player ) ) {
                player.Message( "The world is already locked." );
            } else if( player.World != world ) {
                player.Message( "Locked world {0}", world );
            }
        }


        static readonly CommandDescriptor CdWorldUnlock = new CommandDescriptor {
            Name = "wunlock",
            Aliases = new[] { "unlock" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Lock },
            Usage = "/wunlock [*|WorldName]",
            Help = "Removes the lockdown set by &H/lock&S. See &H/help lock&S for more information.",
            Handler = WorldUnlockHandler
        };

        static void WorldUnlockHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            World world;
            if( worldName != null ) {
                if( worldName == "*" ) {
                    World[] worldListCache = WorldManager.WorldList;
                    int unlocked = 0;
                    for( int i = 0; i < worldListCache.Length; i++ ) {
                        if( worldListCache[i].IsLocked ) {
                            worldListCache[i].Unlock( player );
                            unlocked++;
                        }
                    }
                    player.Message( "Unlocked {0} worlds.", unlocked );
                    return;
                } else {
                    world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world == null ) return;
                }

            } else if( player.World != null ) {
                world = player.World;

            } else {
                player.Message( "When called from console, /lock requires a world name." );
                return;
            }

            if( !world.Unlock( player ) ) {
                player.Message( "The world is already unlocked." );
            } else if( player.World != world ) {
                player.Message( "Unlocked world {0}", world );
            }
        }

        #endregion


        #region Spawn

        static readonly CommandDescriptor CdSpawn = new CommandDescriptor {
            Name = "spawn",
            Category = CommandCategory.World,
            Usage = "/spawn",
            Help = "Teleports you to the current map's spawn.",
            Handler = SpawnHandler
        };

        static void SpawnHandler( Player player, Command cmd ) {
            player.TeleportTo( player.World.Map.Spawn );
        }

        #endregion


        #region Worlds

        static readonly CommandDescriptor CdWorlds = new CommandDescriptor {
            Name = "worlds",
            Category = CommandCategory.World | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Aliases = new[] { "maps", "levels" },
            Usage = "/worlds [all|hidden|loaded]",
            Help = "Shows a list of worlds available for you to join. " +
                   "If the optional \"all\" is added, also shows unavailable (restricted) worlds. " +
                   "If \"hidden\" is added, shows only hidden and inaccessible worlds.",
            Handler = WorldsHandler
        };

        static void WorldsHandler( Player player, Command cmd ) {
            string param = cmd.Next();
            bool listVisible = true,
                 listHidden = false,
                 listAllLoaded = false;
            if( !String.IsNullOrEmpty( param ) ) {
                switch( Char.ToLower( param[0] ) ) {
                    case 'a':
                        listHidden = true;
                        break;
                    case 'h':
                        listVisible = false;
                        listHidden = true;
                        break;
                    case 'l':
                        listAllLoaded = true;
                        listVisible = false;
                        break;
                    default:
                        CdWorlds.PrintUsage( player );
                        return;
                }
            }

            StringBuilder sb = new StringBuilder();
            bool first = true;
            int count = 0;

            World[] worldListCache = WorldManager.WorldList;
            foreach( World world in worldListCache ) {
                bool visible = player.CanJoin( world ) && !world.IsHidden;
                if( (world.IsLoaded && listAllLoaded) || (visible && listVisible) || (!visible && listHidden) ) {
                    if( !first ) {
                        sb.Append( "  " );
                    }
                    sb.Append( world.ClassyName );
                    count++;
                    first = false;
                }
            }

            string listName;
            if( listAllLoaded ) {
                listName = "loaded worlds";
            } else if( listVisible && !listHidden ) {
                listName = "available worlds";
            } else if( !listVisible ) {
                listName = "hidden worlds";
            } else {
                listName = "worlds total";
            }

            player.MessagePrefixed( "&S   ", "&SThere are {0} {1}: {2}", count, listName, sb );
        }

        #endregion


        #region WorldAccess

        static readonly CommandDescriptor CdWorldAccess = new CommandDescriptor {
            Name = "waccess",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/waccess [WorldName [RankName]]",
            Help = "Shows access permission for player's current world. " +
                   "If optional WorldName parameter is given, shows access permission for another world. " +
                   "If RankName parameter is also given, sets access permission for specified world.",
            Handler = WorldAccessHandler
        };

        static void WorldAccessHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "When calling /waccess from console, you must specify a world name." );
                } else {
                    player.Message( player.World.AccessSecurity.GetDescription( player.World, "world", "accessed" ) );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;


            string name = cmd.Next();
            if( name == null ) {
                player.Message( world.AccessSecurity.GetDescription( world, "world", "accessed" ) );
                return;
            }
            if( world == WorldManager.MainWorld ) {
                player.Message( "The main world cannot have access restrictions." );
                return;
            }

            bool changesWereMade = false;
            do {
                if( name.Length < 2 ) continue;
                // Whitelisting individuals
                if( name.StartsWith( "+" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        continue;

                    } else if( info == null ) {
                        player.MessageNoPlayer( name.Substring( 1 ) );
                        continue;
                    }

                    // prevent players from whitelisting themselves to bypass protection
                    if( player.Info == info && !player.Info.Rank.AllowSecurityCircumvention ) {
                        switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the access whitelist of {1}",
                                                world.AccessSecurity.MinRank.ClassyName,
                                                world.ClassyName );
                                continue;
                            // TODO: RankTooHigh
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the access blacklist of {0}",
                                                world.ClassyName );
                                continue;
                        }
                    }

                    if( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to access {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.AccessSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.AccessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from accessing {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You can now access world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the access blacklist of {1}&S. " +
                                                "Player is still NOT allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the access blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to join (by rank).",
                                                    player.ClassyName, world.ClassyName );
                                }
                            }
                            Logger.Log( "{0} removed {1} from the access blacklist of {2}", LogType.UserActivity,
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now allowed to access {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "You can now access world {0}&S (whitelisted by {1}&S).",
                                                world.ClassyName, player.ClassyName );
                            }
                            Logger.Log( "{0} added {1} to the access whitelist on world {2}", LogType.UserActivity,
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already on the access whitelist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;
                    }

                    // Blacklisting individuals
                } else if( name.StartsWith( "-" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        continue;
                    } else if( info == null ) {
                        player.MessageNoPlayer( name.Substring( 1 ) );
                        continue;
                    }

                    if( world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.AccessSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from accessing {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.AccessSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on access blacklist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from accessing {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from accessing world {1}",
                                                player.ClassyName, world.ClassyName );
                            }
                            Logger.Log( "{0} added {1} to the access blacklist on world {2}", LogType.UserActivity,
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.AccessSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the access whitelist of {1}&S. " +
                                                "Player is still allowed to join (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the access whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to join (by rank).",
                                                    player.ClassyName, world.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to access {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "&WYou can no longer access world {0}&W (removed from whitelist by {1}&W).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( "{0} removed {1} from the access whitelist on world {2}", LogType.UserActivity,
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankManager.FindRank( name );
                    if( rank == null ) {
                        player.MessageNoRank( name );

                    } else if( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.AccessSecurity.MinRank > rank &&
                               world.AccessSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower the access rank for world {1}",
                                        world.AccessSecurity.MinRank.ClassyName, world.ClassyName );

                    } else {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.AccessSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted to be barred from {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString() );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted to access {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString() );
                        }

                        // apply changes
                        world.AccessSecurity.MinRank = rank;
                        changesWereMade = true;
                        if( world.AccessSecurity.MinRank == RankManager.LowestRank ) {
                            Server.Message( "{0}&S made the world {1}&S accessible to everyone.",
                                              player.ClassyName, world.ClassyName );
                        } else {
                            Server.Message( "{0}&S made the world {1}&S accessible only by {2}+",
                                              player.ClassyName, world.ClassyName,
                                              world.AccessSecurity.MinRank.ClassyName );
                        }
                        Logger.Log( "{0} set access rank for world {1} to {2}+", LogType.UserActivity,
                                    player.Name, world.Name, world.AccessSecurity.MinRank.Name );
                    }
                }
            } while( (name = cmd.Next()) != null );

            if( changesWereMade ) {
                var playersWhoCantStay = world.Players.Where( p => !p.CanJoin( world ) );
                foreach( Player p in playersWhoCantStay ) {
                    p.Message( "&WYou are no longer allowed to join world {0}", world.ClassyName );
                    p.JoinWorld( WorldManager.MainWorld, WorldChangeReason.PermissionChanged );
                }
                WorldManager.SaveWorldList();
            }
        }

        #endregion


        #region WorldBuild

        static readonly CommandDescriptor CdWorldBuild = new CommandDescriptor {
            Name = "wbuild",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wbuild [WorldName [RankName]]",
            Help = "Shows build permissions for player's current world. " +
                   "If optional WorldName parameter is given, shows build permission for another world. " +
                   "If RankName parameter is also given, sets build permission for specified world.",
            Handler = WorldBuildHandler
        };

        static void WorldBuildHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();

            // Print information about the current world
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "When calling /wbuild from console, you must specify a world name." );
                } else {
                    player.Message( player.World.BuildSecurity.GetDescription( player.World, "world", "modified" ) );
                }
                return;
            }

            // Find a world by name
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;


            string name = cmd.Next();
            if( name == null ) {
                player.Message( world.BuildSecurity.GetDescription( world, "world", "modified" ) );
                return;
            }

            bool changesWereMade = false;
            do {
                if( name.Length < 2 ) continue;
                // Whitelisting individuals
                if( name.StartsWith( "+" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        continue;
                    } else if( info == null ) {
                        player.MessageNoPlayer( name.Substring( 1 ) );
                        continue;
                    }

                    // prevent players from whitelisting themselves to bypass protection
                    if( player.Info == info && !player.Info.Rank.AllowSecurityCircumvention ) {
                        switch( world.BuildSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "&WYou must be {0}&W+ to add yourself to the build whitelist of {1}",
                                                world.BuildSecurity.MinRank.ClassyName,
                                                world.ClassyName );
                                continue;
                            // TODO: RankTooHigh
                            case SecurityCheckResult.BlackListed:
                                player.Message( "&WYou cannot remove yourself from the build blacklist of {0}",
                                                world.ClassyName );
                                continue;
                        }
                    }

                    if( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.Allowed ) {
                        player.Message( "{0}&S is already allowed to build in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.BuildSecurity.Include( info ) ) {
                        case PermissionOverride.Deny:
                            if( world.BuildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer barred from building in {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You can now build in world {0}&S (removed from blacklist by {1}&S).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S was removed from the build blacklist of {1}&S. " +
                                                "Player is still NOT allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the build blacklist of world {0}&S by {1}&S. " +
                                                    "You are still NOT allowed to build (by rank).",
                                                    player.ClassyName, world.ClassyName );
                                }
                            }
                            Logger.Log( "{0} removed {1} from the build blacklist of {2}", LogType.UserActivity,
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now allowed to build in {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "You can now build in world {0}&S (whitelisted by {1}&S).",
                                                world.ClassyName, player.ClassyName );
                            }
                            Logger.Log( "{0} added {1} to the build whitelist on world {2}", LogType.UserActivity,
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            player.Message( "{0}&S is already on the build whitelist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;
                    }

                    // Blacklisting individuals
                } else if( name.StartsWith( "-" ) ) {
                    PlayerInfo info;
                    if( !PlayerDB.FindPlayerInfo( name.Substring( 1 ), out info ) ) {
                        player.Message( "More than one player found matching \"{0}\"", name.Substring( 1 ) );
                        continue;
                    } else if( info == null ) {
                        player.MessageNoPlayer( name.Substring( 1 ) );
                        continue;
                    }

                    if( world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooHigh ||
                        world.BuildSecurity.CheckDetailed( info ) == SecurityCheckResult.RankTooLow ) {
                        player.Message( "{0}&S is already barred from building in {1}&S (by rank)",
                                        info.ClassyName, world.ClassyName );
                        continue;
                    }

                    Player target = info.PlayerObject;
                    if( target == player ) target = null; // to avoid duplicate messages

                    switch( world.BuildSecurity.Exclude( info ) ) {
                        case PermissionOverride.Deny:
                            player.Message( "{0}&S is already on build blacklist of {1}",
                                            info.ClassyName, world.ClassyName );
                            break;

                        case PermissionOverride.None:
                            player.Message( "{0}&S is now barred from building in {1}",
                                            info.ClassyName, world.ClassyName );
                            if( target != null ) {
                                target.Message( "&WYou were barred by {0}&W from building in world {1}",
                                                player.ClassyName, world.ClassyName );
                            }
                            Logger.Log( "{0} added {1} to the build blacklist on world {2}", LogType.UserActivity,
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;

                        case PermissionOverride.Allow:
                            if( world.BuildSecurity.Check( info ) ) {
                                player.Message( "{0}&S is no longer on the build whitelist of {1}&S. " +
                                                "Player is still allowed to build (by rank).",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "You were removed from the build whitelist of world {0}&S by {1}&S. " +
                                                    "You are still allowed to build (by rank).",
                                                    player.ClassyName, world.ClassyName );
                                }
                            } else {
                                player.Message( "{0}&S is no longer allowed to build in {1}",
                                                info.ClassyName, world.ClassyName );
                                if( target != null ) {
                                    target.Message( "&WYou can no longer build in world {0}&W (removed from whitelist by {1}&W).",
                                                    world.ClassyName, player.ClassyName );
                                }
                            }
                            Logger.Log( "{0} removed {1} from the build whitelist on world {2}", LogType.UserActivity,
                                        player.Name, info.Name, world.Name );
                            changesWereMade = true;
                            break;
                    }

                    // Setting minimum rank
                } else {
                    Rank rank = RankManager.FindRank( name );
                    if( rank == null ) {
                        player.MessageNoRank( name );
                    } else if( !player.Info.Rank.AllowSecurityCircumvention &&
                               world.BuildSecurity.MinRank > rank &&
                               world.BuildSecurity.MinRank > player.Info.Rank ) {
                        player.Message( "&WYou must be ranked {0}&W+ to lower build restrictions for world {1}",
                                        world.BuildSecurity.MinRank.ClassyName, world.ClassyName );
                    } else {
                        // list players who are redundantly blacklisted
                        var exceptionList = world.BuildSecurity.ExceptionList;
                        PlayerInfo[] noLongerExcluded = exceptionList.Excluded.Where( excludedPlayer => excludedPlayer.Rank < rank ).ToArray();
                        if( noLongerExcluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be blacklisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerExcluded.JoinToClassyString() );
                        }

                        // list players who are redundantly whitelisted
                        PlayerInfo[] noLongerIncluded = exceptionList.Included.Where( includedPlayer => includedPlayer.Rank >= rank ).ToArray();
                        if( noLongerIncluded.Length > 0 ) {
                            player.Message( "Following players no longer need to be whitelisted on world {0}&S: {1}",
                                            world.ClassyName,
                                            noLongerIncluded.JoinToClassyString() );
                        }

                        // apply changes
                        world.BuildSecurity.MinRank = rank;
                        world.BlockDB.CheckIfShouldBeAutoEnabled();
                        changesWereMade = true;
                        if( world.BuildSecurity.MinRank == RankManager.LowestRank ) {
                            Server.Message( "{0}&S allowed anyone to build on world {1}",
                                              player.ClassyName, world.ClassyName );
                        } else {
                            Server.Message( "{0}&S allowed only {1}+&S to build in world {2}",
                                              player.ClassyName, world.BuildSecurity.MinRank.ClassyName, world.ClassyName );
                        }
                        Logger.Log( "{0} set build rank for world {1} to {2}+", LogType.UserActivity,
                                    player.Name, world.Name, world.BuildSecurity.MinRank.Name );
                    }
                }
            } while( (name = cmd.Next()) != null );

            if( changesWereMade ) {
                WorldManager.SaveWorldList();
            }
        }

        #endregion


        #region WorldFlush

        static readonly CommandDescriptor CdWorldFlush = new CommandDescriptor {
            Name = "wflush",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wflush [WorldName]",
            Help = "Flushes the update buffer on specified map by causing players to rejoin. " +
                   "Makes cuboids and other draw commands finish REALLY fast.",
            Handler = WorldFlushHandler
        };

        static void WorldFlushHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            World world = player.World;

            if( worldName != null ) {
                world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                if( world == null ) return;

            } else if( player.World == null ) {
                player.Message( "When using /wflush from console, you must specify a world name." );
                return;
            }

            Map map = world.Map;
            if( map == null ) {
                player.MessageNow( "WFlush: {0}&S has no updates to process.",
                                   world.ClassyName );
            } else {
                player.MessageNow( "WFlush: Flushing {0}&S ({1} blocks, {2} commands)...",
                                   world.ClassyName,
                                   map.UpdateQueueLength,
                                   map.DrawQueueLength );
                world.Flush();
            }
        }

        #endregion


        #region WorldHide / WorldUnhide

        static readonly CommandDescriptor CdWorldHide = new CommandDescriptor {
            Name = "whide",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/whide WorldName",
            Help = "Hides the specified world from the &H/worlds&S list. " +
                   "Hidden worlds can be seen by typing &H/worlds all",
            Handler = WorldHideHandler
        };

        static void WorldHideHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdWorldAccess.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            if( world.IsHidden ) {
                player.Message( "World \"{0}&S\" is already hidden.", world.ClassyName );
            } else {
                player.Message( "World \"{0}&S\" is now hidden.", world.ClassyName );
                world.IsHidden = true;
                WorldManager.SaveWorldList();
            }
        }


        static readonly CommandDescriptor CdWorldUnhide = new CommandDescriptor {
            Name = "wunhide",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wunhide WorldName",
            Help = "Unhides the specified world from the &H/worlds&S list. " +
                   "Hidden worlds can be listed by typing &H/worlds all",
            Handler = WorldUnhideHandler
        };

        static void WorldUnhideHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdWorldAccess.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            if( world.IsHidden ) {
                player.Message( "World \"{0}&S\" is no longer hidden.", world.ClassyName );
                world.IsHidden = false;
                WorldManager.SaveWorldList();
            } else {
                player.Message( "World \"{0}&S\" is not hidden.", world.ClassyName );
            }
        }

        #endregion


        #region WorldInfo

        static readonly CommandDescriptor CdWorldInfo = new CommandDescriptor {
            Name = "winfo",
            Aliases = new[] { "mapinfo" },
            Category = CommandCategory.World | CommandCategory.Info,
            IsConsoleSafe = true,
            UsableByFrozenPlayers = true,
            Usage = "/winfo [WorldName]",
            Help = "Shows information about a world: player count, map dimensions, permissions, etc." +
                   "If no WorldName is given, shows info for current world.",
            Handler = WorldInfoHandler
        };

        static void WorldInfoHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                if( player.World == null ) {
                    player.Message( "Please specify a world name when calling /winfo form console." );
                    return;
                } else {
                    worldName = player.World.Name;
                }
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            player.Message( "World {0}&S has {1} player(s) on.",
                            world.ClassyName,
                            world.CountVisiblePlayers( player ) );

            Map map = world.Map;

            // If map is not currently loaded, grab its header from disk
            if( map == null ) {
                try {
                    map = MapUtility.LoadHeader( Path.Combine( Paths.MapPath, world.MapFileName ) );
                } catch( Exception ex ) {
                    player.Message( "Map information could not be loaded: {0}: {1}",
                                    ex.GetType().Name, ex.Message );
                }
            }

            if( map != null ) {
                player.Message( "Map dimensions are {0} x {1} x {2}",
                                map.Width, map.Length, map.Height );
            }

            // Print access/build limits
            player.Message( world.AccessSecurity.GetDescription( world, "world", "accessed" ) );
            player.Message( world.BuildSecurity.GetDescription( world, "world", "modified" ) );

            // Print lock/unlock information
            if( world.IsLocked ) {
                player.Message( "{0}&S was locked {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.LockedDate ).ToMiniString(),
                                world.LockedBy );
            } else if( world.UnlockedBy != null ) {
                player.Message( "{0}&S was unlocked {1} ago by {2}",
                                world.ClassyName,
                                DateTime.UtcNow.Subtract( world.UnlockedDate ).ToMiniString(),
                                world.UnlockedBy );
            }

            if( !String.IsNullOrEmpty( world.LoadedBy ) && world.LoadedOn != DateTime.MinValue ) {
                player.Message( "  {0}&S was created/loaded {0} ago by {1}",
                                DateTime.UtcNow.Subtract( world.LoadedOn ).ToMiniString(),
                                world.LoadedBy );
            }

            if( !String.IsNullOrEmpty( world.MapChangedBy ) && world.MapChangedOn != DateTime.MinValue ) {
                player.Message( "  Map was last changed {0} ago by {1}",
                                DateTime.UtcNow.Subtract( world.MapChangedOn ).ToMiniString(),
                                world.MapChangedBy );
            }

            if( world.BlockDB.IsEnabled ) {
                if( world.BlockDB.EnabledState == YesNoAuto.Auto ) {
                    player.Message( "  BlockDB is enabled (auto) on {0}", world.ClassyName );
                } else {
                    player.Message( "  BlockDB is enabled on {0}", world.ClassyName );
                }
            } else {
                player.Message( "  BlockDB is disabled on {0}", world.ClassyName );
            }
        }

        #endregion


        #region WorldLoad

        static readonly CommandDescriptor CdWorldLoad = new CommandDescriptor {
            Name = "wload",
            Aliases = new[] { "wadd" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wload FileName [WorldName [BuildRank [AccessRank]]]",
            Help = "If WorldName parameter is not given, replaces the current world's map with the specified map. The old map is overwritten. " +
                   "If the world with the specified name exists, its map is replaced with the specified map file. " +
                   "Otherwise, a new world is created using the given name and map file. " +
                   "NOTE: For security reasons, you may only load files from the map folder. " +
                   "For a list of supported formats, see &H/help wload formats",
            HelpSections = new Dictionary<string, string>{
                { "formats",    "Supported formats: fCraft FCM (versions 2, 3, and 4), MCSharp/MCZall/MCLawl (.lvl), " +
                                "D3 (.map), Classic (.dat), InDev (.mclevel), MinerCPP/LuaCraft (.dat), " +
                                "JTE (.gz), iCraft/Myne (directory-based), Opticraft (.save)." }
            },
            Handler = WorldLoadHandler
        };


        static void WorldLoadHandler( Player player, Command cmd ) {
            string fileName = cmd.Next();
            string worldName = cmd.Next();

            if( worldName == null && player.World == null ) {
                player.Message( "When using /wload from console, you must specify the world name." );
                return;
            }

            if( fileName == null ) {
                // No params given at all
                CdWorldLoad.PrintUsage( player );
                return;
            }

            string fullFileName = WorldManager.FindMapFile( player, fileName );
            if( fullFileName == null ) return;

            // Loading map into current world
            if( worldName == null ) {
                if( !cmd.IsConfirmed ) {
                    player.Confirm( cmd, "About to replace THIS MAP with \"{0}\".", fileName );
                    return;
                }
                Map map;
                try {
                    map = MapUtility.Load( fullFileName );
                } catch( Exception ex ) {
                    player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                    return;
                }

                // Loading to current world
                player.World.ChangeMap( map );
                player.World.MapChangedBy = player.Name;
                player.World.MapChangedOn = DateTime.UtcNow;

                player.World.Players.Message( player, "{0}&S loaded a new map for this world.",
                                              player.ClassyName );
                player.MessageNow( "New map loaded for the world {0}", player.World.ClassyName );

                Logger.Log( "{0} loaded new map for world \"{1}\" from {2}", LogType.UserActivity,
                            player.Name, player.World.Name, fileName );


            } else {
                // Loading to some other (or new) world
                if( !World.IsValidName( worldName ) ) {
                    player.MessageNow( "Invalid world name: \"{0}\".", worldName );
                    return;
                }

                string buildRankName = cmd.Next();
                string accessRankName = cmd.Next();
                Rank buildRank = RankManager.DefaultBuildRank;
                Rank accessRank = null;
                if( buildRankName != null ) {
                    buildRank = RankManager.FindRank( buildRankName );
                    if( buildRank == null ) {
                        player.MessageNoRank( buildRankName );
                        return;
                    }
                    if( accessRankName != null ) {
                        accessRank = RankManager.FindRank( accessRankName );
                        if( accessRank == null ) {
                            player.MessageNoRank( accessRankName );
                            return;
                        }
                    }
                }

                lock( WorldManager.WorldListLock ) {
                    World world = WorldManager.FindWorldExact( worldName );
                    if( world != null ) {
                        // Replacing existing world's map
                        if( !cmd.IsConfirmed ) {
                            player.Confirm( cmd, "About to replace map for {0}&S with \"{1}\".",
                                                       world.ClassyName, fileName );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( fullFileName );
                        } catch( Exception ex ) {
                            player.MessageNow( "Could not load specified file: {0}: {1}", ex.GetType().Name, ex.Message );
                            return;
                        }

                        try {
                            world.ChangeMap( map );
                            world.MapChangedBy = player.Name;
                            world.MapChangedOn = DateTime.UtcNow;
                        } catch( WorldOpException ex ) {
                            Logger.Log( "Could not complete WorldLoad operation: {0}", LogType.Error, ex.Message );
                            player.Message( "&WWLoad: {0}", ex.Message );
                            return;
                        }

                        world.Players.Message( player, "{0}&S loaded a new map for the world {1}",
                                               player.ClassyName, world.ClassyName );
                        player.MessageNow( "New map for the world {0}&S has been loaded.", world.ClassyName );
                        Logger.Log( "{0} loaded new map for world \"{1}\" from {2}", LogType.UserActivity,
                                    player.Name, world.Name, fullFileName );

                    } else {
                        // Adding a new world
                        string targetFullFileName = Path.Combine( Paths.MapPath, worldName + ".fcm" );
                        if( !cmd.IsConfirmed &&
                            File.Exists( targetFullFileName ) && // target file already exists
                            !Paths.Compare( targetFullFileName, fullFileName ) ) { // and is different from sourceFile
                            player.Confirm( cmd, "A map named \"{0}\" already exists, and will be overwritten with \"{1}\".",
                                                       Path.GetFileName( targetFullFileName ), Path.GetFileName( fullFileName ) );
                            return;
                        }

                        Map map;
                        try {
                            map = MapUtility.Load( fullFileName );
                        } catch( Exception ex ) {
                            player.MessageNow( "Could not load \"{0}\": {1}: {2}",
                                               fileName, ex.GetType().Name, ex.Message );
                            return;
                        }

                        World newWorld;
                        try {
                            newWorld = WorldManager.AddWorld( player, worldName, map, false );
                        } catch( WorldOpException ex ) {
                            player.Message( "WLoad: {0}", ex.Message );
                            return;
                        }

                        if( newWorld != null ) {
                            newWorld.BuildSecurity.MinRank = buildRank;
                            newWorld.AccessSecurity.MinRank = accessRank;
                            newWorld.BlockDB.CheckIfShouldBeAutoEnabled();
                            newWorld.LoadedBy = player.Name;
                            newWorld.LoadedOn = DateTime.UtcNow;
                            Server.Message( "{0}&S created a new world named {1}",
                                              player.ClassyName, newWorld.ClassyName );
                            Logger.Log( "{0} created a new world named \"{1}\" (loaded from \"{2}\")", LogType.UserActivity,
                                        player.Name, worldName, fileName );
                            WorldManager.SaveWorldList();
                            player.MessageNow( "Reminder: New world's access permission is {0}+&S, and build permission is {1}+",
                                // ReSharper disable PossibleNullReferenceException
                                               newWorld.AccessSecurity.MinRank.ClassyName,
                                // ReSharper restore PossibleNullReferenceException
                                               newWorld.BuildSecurity.MinRank.ClassyName );
                        } else {
                            player.MessageNow( "Failed to create a new world." );
                        }
                    }
                }
            }

            Server.RequestGC();
        }

        #endregion


        #region WorldMain

        static readonly CommandDescriptor CdWorldMain = new CommandDescriptor {
            Name = "wmain",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wmain [@RankName] [WorldName]",
            Help = "Sets the specified world as the new main world. " +
                   "Main world is what newly-connected players join first. " +
                   "You can specify a rank name to set a different starting world for that particular rank.",
            Handler = WorldMainHandler
        };

        static void WorldMainHandler( Player player, Command cmd ) {
            string param = cmd.Next();
            if( param == null ) {
                player.Message( "Main world is {0}", WorldManager.MainWorld.ClassyName );
                var mainedRanks = RankManager.Ranks.Where( r => r.MainWorld != null && r.MainWorld != WorldManager.MainWorld );
                if( mainedRanks.Count() > 0 ) {
                    player.Message( "Rank mains: {0}",
                                    mainedRanks.JoinToString( r => String.Format( "{0}&S for {1}&S",
                                                                                  r.MainWorld.ClassyName,
                                                                                  r.ClassyName ) ) );
                }
                return;
            }

            if( param.StartsWith( "@" ) ) {
                string rankName = param.Substring( 1 );
                Rank rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }
                string worldName = cmd.Next();
                if( worldName == null ) {
                    if( rank.MainWorld != null ) {
                        player.Message( "Main world for rank {0}&S is {1}",
                                        rank.ClassyName,
                                        rank.MainWorld.ClassyName );
                    } else {
                        player.Message( "Main world for rank {0}&S is {1}&S (default)",
                                        rank.ClassyName,
                                        WorldManager.MainWorld.ClassyName );
                    }
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
                    if( world != null ) {
                        SetRankMainWorld( player, rank, world );
                    }
                }

            } else {
                World world = WorldManager.FindWorldOrPrintMatches( player, param );
                if( world != null ) {
                    SetMainWorld( player, world );
                }
            }
        }


        static void SetRankMainWorld( Player player, Rank rank, World world ) {
            if( world == rank.MainWorld ) {
                player.Message( "World {0}&S is already set as main for {1}&S.",
                                world.ClassyName, rank.ClassyName );
                return;
            }

            if( world == WorldManager.MainWorld ) {
                if( rank.MainWorld == null ) {
                    player.Message( "The main world for rank {0}&S is already {1}&S (default).",
                                    rank.ClassyName, world.ClassyName );
                } else {
                    rank.MainWorld = null;
                    WorldManager.SaveWorldList();
                    Server.Message( "&SPlayer {0}&S has reset the main world for rank {1}&S.",
                                    player.ClassyName, rank.ClassyName );
                    Logger.Log( "{0} reset the main world for rank {1}.", LogType.UserActivity,
                                player.Name, rank.Name );
                }
                return;
            }

            if( world.AccessSecurity.MinRank > rank ) {
                player.Message( "World {0}&S requires {1}+&S to join, so it cannot be used as the main world for rank {2}&S.",
                                world.ClassyName, world.AccessSecurity.MinRank, rank.ClassyName );
                return;
            }

            rank.MainWorld = world;
            WorldManager.SaveWorldList();
            Server.Message( "&SPlayer {0}&S designated {1}&S to be the main world for rank {2}",
                            player.ClassyName, world.ClassyName, rank.ClassyName );
            Logger.Log( "{0} set {1} to be the main world for rank {2}.", LogType.UserActivity,
                        player.Name, world.Name, rank.Name );
        }


        static void SetMainWorld( Player player, World world ) {
            if( world == WorldManager.MainWorld ) {
                player.Message( "World {0}&S is already set as main.", world.ClassyName );

            } else if( !player.Info.Rank.AllowSecurityCircumvention && !player.CanJoin( world ) ) {
                // Prevent players from exploiting /wmain to gain access to restricted maps
                switch( world.AccessSecurity.CheckDetailed( player.Info ) ) {
                    case SecurityCheckResult.RankTooHigh:
                    case SecurityCheckResult.RankTooLow:
                        player.Message( "You are not allowed to set {0}&S as the main world (by rank).", world.ClassyName );
                        return;
                    case SecurityCheckResult.BlackListed:
                        player.Message( "You are not allowed to set {0}&S as the main world (blacklisted).", world.ClassyName );
                        return;
                }

            } else {
                if( world.AccessSecurity.HasRestrictions ) {
                    world.AccessSecurity.Reset();
                    player.Message( "The main world cannot have access restrictions. " +
                                    "All access restrictions were removed from world {0}",
                                    world.ClassyName );
                }

                try {
                    WorldManager.MainWorld = world;
                } catch( WorldOpException ex ) {
                    player.Message( ex.Message );
                    return;
                }

                WorldManager.SaveWorldList();

                Server.Message( "{0}&S set {1}&S to be the main world.",
                                  player.ClassyName, world.ClassyName );
                Logger.Log( "{0} set {1} to be the main world.", LogType.UserActivity,
                            player.Name, world.Name );
            }
        }

        #endregion


        #region WorldRename

        static readonly CommandDescriptor CdWorldRename = new CommandDescriptor {
            Name = "wrename",
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wrename OldName NewName",
            Help = "Changes the name of a world. Does not require any reloading.",
            Handler = WorldRenameHandler
        };

        static void WorldRenameHandler( Player player, Command cmd ) {
            string oldName = cmd.Next();
            string newName = cmd.Next();
            if( oldName == null || newName == null ) {
                CdWorldRename.PrintUsage( player );
                return;
            }

            World oldWorld = WorldManager.FindWorldOrPrintMatches( player, oldName );
            if( oldWorld == null ) return;
            oldName = oldWorld.Name;

            if( !cmd.IsConfirmed && File.Exists( Path.Combine( Paths.MapPath, newName + ".fcm" ) ) ) {
                player.Confirm( cmd, "Renaming this world will overwrite an existing map file \"{0}.fcm\".", newName );
                return;
            }

            try {
                WorldManager.RenameWorld( oldWorld, newName, true );
            } catch( WorldOpException ex ) {
                switch( ex.ErrorCode ) {
                    case WorldOpExceptionCode.NoChangeNeeded:
                        player.MessageNow( "WRename: World is already named \"{0}\"", oldName );
                        return;
                    case WorldOpExceptionCode.DuplicateWorldName:
                        player.MessageNow( "WRename: Another world named \"{0}\" already exists.", newName );
                        return;
                    case WorldOpExceptionCode.InvalidWorldName:
                        player.MessageNow( "WRename: Invalid world name: \"{0}\"", newName );
                        return;
                    case WorldOpExceptionCode.MapMoveError:
                        player.MessageNow( "WRename: World \"{0}\" was renamed to \"{1}\", but the map file could not be moved due to an error: {2}",
                                            oldName, newName, ex.InnerException );
                        return;
                    default:
                        player.MessageNow( "WRename: Unexpected error renaming world \"{0}\": {1}", oldName, ex.Message );
                        Logger.Log( "WorldCommands.Rename: Unexpected error while renaming world {0} to {1}: {2}",
                                    LogType.Error, oldWorld.Name, newName, ex );
                        return;
                }
            }

            WorldManager.SaveWorldList();
            Logger.Log( "{0} renamed the world \"{1}\" to \"{2}\".", LogType.UserActivity,
                        player.Name, oldName, newName );
            Server.Message( "{0}&S renamed the world \"{1}\" to \"{2}\"",
                              player.ClassyName, oldName, newName );
        }

        #endregion


        #region WorldSave

        static readonly CommandDescriptor CdWorldSave = new CommandDescriptor {
            Name = "wsave",
            Aliases = new[] { "save" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wsave FileName &Sor&H /save WorldName FileName",
            Help = "Saves a map copy to a file with the specified name. " +
                   "The \".fcm\" file extension can be omitted. " +
                   "If a file with the same name already exists, it will be overwritten.",
            Handler = WorldSaveHandler
        };

        static void WorldSaveHandler( Player player, Command cmd ) {
            string p1 = cmd.Next(), p2 = cmd.Next();
            if( p1 == null ) {
                CdWorldSave.PrintUsage( player );
                return;
            }

            World world = player.World;
            string fileName;
            if( p2 == null ) {
                fileName = p1;
                if( world == null ) {
                    player.Message( "When called from console, /save requires WorldName. See \"/help save\" for details." );
                    return;
                }
            } else {
                world = WorldManager.FindWorldOrPrintMatches( player, p1 );
                if( world == null ) return;
                fileName = p2;
            }

            // normalize the path
            fileName = fileName.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
            if( fileName.EndsWith( "/" ) && fileName.EndsWith( @"\" ) ) {
                fileName += world.Name + ".fcm";
            } else if( !fileName.ToLower().EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase ) ) {
                fileName += ".fcm";
            }
            string fullFileName = Path.Combine( Paths.MapPath, fileName );
            if( !Paths.IsValidPath( fullFileName ) ) {
                player.Message( "Invalid filename." );
                return;
            }
            if( !Paths.Contains( Paths.MapPath, fullFileName ) ) {
                player.MessageUnsafePath();
                return;
            }

            // Ask for confirmation if overwriting
            if( File.Exists( fullFileName ) ) {
                FileInfo targetFile = new FileInfo( fullFileName );
                FileInfo sourceFile = new FileInfo( world.MapFileName );
                if( !targetFile.FullName.Equals( sourceFile.FullName, StringComparison.OrdinalIgnoreCase ) ) {
                    if( !cmd.IsConfirmed ) {
                        player.Confirm( cmd, "Target file \"{0}\" already exists, and will be overwritten.", targetFile.Name );
                        return;
                    }
                }
            }

            // Create the target directory if it does not exist
            string dirName = fullFileName.Substring( 0, fullFileName.LastIndexOf( Path.DirectorySeparatorChar ) );
            if( !Directory.Exists( dirName ) ) {
                Directory.CreateDirectory( dirName );
            }

            player.MessageNow( "Saving map to {0}", fileName );

            const string mapSavingErrorMessage = "Map saving failed. See server logs for details.";
            Map map = world.Map;
            if( map == null ) {
                if( File.Exists( world.MapFileName ) ) {
                    try {
                        File.Copy( world.MapFileName, fullFileName, true );
                    } catch( Exception ex ) {
                        Logger.Log( "StandardCommands.Save: Error occured while trying to copy an unloaded map: {0}", LogType.Error, ex );
                        player.Message( mapSavingErrorMessage );
                    }
                } else {
                    Logger.Log( "StandardCommands.Save: Map for world \"{0}\" is unloaded, and file does not exist.", LogType.Error, world.Name );
                    player.Message( mapSavingErrorMessage );
                }
            } else if( map.Save( fullFileName ) ) {
                player.Message( "Map saved succesfully." );
            } else {
                Logger.Log( "StandardCommands.Save: Saving world \"{0}\" failed.", LogType.Error, world.Name );
                player.Message( mapSavingErrorMessage );
            }
        }

        #endregion


        #region WorldUnload

        static readonly CommandDescriptor CdWorldUnload = new CommandDescriptor {
            Name = "wunload",
            Aliases = new[] { "wremove", "wdelete" },
            Category = CommandCategory.World,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.ManageWorlds },
            Usage = "/wunload WorldName",
            Help = "Removes the specified world from the world list, and moves all players from it to the main world. " +
                   "The main world itself cannot be removed with this command. You will need to delete the map file manually.",
            Handler = WorldUnloadHandler
        };

        static void WorldUnloadHandler( Player player, Command cmd ) {
            string worldName = cmd.Next();
            if( worldName == null ) {
                CdWorldUnload.PrintUsage( player );
                return;
            }

            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );
            if( world == null ) return;

            try {
                WorldManager.RemoveWorld( world );
            } catch( WorldOpException ex ) {
                switch( ex.ErrorCode ) {
                    case WorldOpExceptionCode.CannotDoThatToMainWorld:
                        player.MessageNow( "&WWorld {0}&W is set as the main world. " +
                                           "Assign a new main world before deleting this one.",
                                           world.ClassyName );
                        return;
                    case WorldOpExceptionCode.WorldNotFound:
                        player.MessageNow( "&WWorld {0}&W is already unloaded.",
                                           world.ClassyName );
                        return;
                    default:
                        player.MessageNow( "&WUnexpected error occured while unloading world {0}&W: {1}",
                                           world.ClassyName, ex.GetType().Name );
                        Logger.Log( "WorldCommands.WorldUnload: Unexpected error while unloading world {0}: {1}",
                                    LogType.Error, world.Name, ex );
                        return;
                }
            }

            WorldManager.SaveWorldList();
            Server.Message( player,
                            "{0}&S removed {1}&S from the world list.",
                            player.ClassyName, world.ClassyName );
            player.Message( "Removed {0}&S from the world list. You can now delete the map file ({1}.fcm) manually.",
                            world.ClassyName, world.Name );
            Logger.Log( "{0} removed \"{1}\" from the world list.", LogType.UserActivity,
                        player.Name, worldName );

            Server.RequestGC();
        }

        #endregion
    }
}