﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
// With contributions by Sean "SystemX17" Dolan (/line, /sphere)
// With contributions by Conrad "Redshift" Morgan (/ellipsoidhollow)
using System;
using System.Collections.Generic;

namespace fCraft {
    /// <summary>
    /// Commands for drawing commands - cuboid, ellipsoid, etc. Also copy/paste commands.
    /// </summary>
    static class DrawCommands {

        #region State Objects and Enums

        public enum DrawMode {
            Cuboid,
            CuboidHollow,
            CuboidWireframe,
            Ellipsoid,
            EllipsoidHollow,
            Sphere,
            SphereHollow,
            Replace,
            ReplaceNot,
            Line
        }


        public sealed class CopyInformation {
            public byte[, ,] Buffer;
            public int WidthX, WidthY, Height;
        }


        struct ReplaceArgs {
            public bool DoExclude;
            public Block[] Types;
            public Block ReplacementBlock;
        }


        struct PasteArgs {
            public bool DoInclude, DoExclude;
            public Block[] BlockTypes;
        }


        struct CuboidHollowArgs {
            public Block InnerBlock;
            public Block OuterBlock;
        }

        #endregion

        public static int MaxUndoCount = 2000000;
        const int DrawStride = 16;


        const string GeneralDrawingHelp = " Use &H/cancel&S to exit draw mode. " +
                                          "Use &H/undo&S to undo the last draw operation. " +
                                          "Use &H/lock&S to cancel drawing after it started.";

        internal static void Init() {
            cdCuboid.Help += GeneralDrawingHelp;
            cdCuboidHollow.Help += GeneralDrawingHelp;
            cdCuboidWireframe.Help += GeneralDrawingHelp;
            cdEllipsoid.Help += GeneralDrawingHelp;
            cdEllipsoidHollow.Help += GeneralDrawingHelp;
            cdSphere.Help += GeneralDrawingHelp;
            cdSphereHollow.Help += GeneralDrawingHelp;
            cdLine.Help += GeneralDrawingHelp;
            cdReplace.Help += GeneralDrawingHelp;
            cdReplaceNot.Help += GeneralDrawingHelp;
            cdCut.Help += GeneralDrawingHelp;
            cdPasteNot.Help += GeneralDrawingHelp;
            cdPaste.Help += GeneralDrawingHelp;

            CommandList.RegisterCommand( cdCuboid );
            CommandList.RegisterCommand( cdCuboidHollow );
            CommandList.RegisterCommand( cdCuboidWireframe );
            CommandList.RegisterCommand( cdEllipsoid );
            CommandList.RegisterCommand( cdEllipsoidHollow );
            CommandList.RegisterCommand( cdSphere );
            CommandList.RegisterCommand( cdSphereHollow );
            CommandList.RegisterCommand( cdReplace );
            CommandList.RegisterCommand( cdReplaceNot );
            CommandList.RegisterCommand( cdLine );

            CommandList.RegisterCommand( cdMark );
            CommandList.RegisterCommand( cdCancel );
            CommandList.RegisterCommand( cdUndo );

            CommandList.RegisterCommand( cdCopy );
            CommandList.RegisterCommand( cdCut );
            CommandList.RegisterCommand( cdPasteNot );
            CommandList.RegisterCommand( cdPaste );
            CommandList.RegisterCommand( cdMirror );
            CommandList.RegisterCommand( cdRotate );

        }


        #region Command Descriptors

        static readonly CommandDescriptor cdCuboid = new CommandDescriptor {
            Name = "cuboid",
            Aliases = new[] { "blb", "c", "cub", "z" },
            Permissions = new[] { Permission.Draw },
            Usage = "/cuboid [BlockName]",
            Help = "Allows to fill a rectangular area (cuboid) with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = Cuboid
        };

        internal static void Cuboid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Cuboid );
        }



        static readonly CommandDescriptor cdCuboidHollow = new CommandDescriptor {
            Name = "cubh",
            Aliases = new[] { "cuboidh", "ch", "h", "bhb" },
            Permissions = new[] { Permission.Draw },
            Usage = "/cuboidh [OuterBlockName [InnerBlockName]]",
            Help = "Allows to box a rectangular area (cuboid) with blocks. " +
                   "If OuterBlockName is omitted, uses the block that player is holding. " +
                   "Unless InnerBlockName is specified, the inside is left untouched.",
            Handler = CuboidHollow
        };

        internal static void CuboidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidHollow );
        }



        static readonly CommandDescriptor cdCuboidWireframe = new CommandDescriptor {
            Name = "cubw",
            Aliases = new[] { "cuboidw", "cw", "bfb" },
            Permissions = new[] { Permission.Draw },
            Usage = "/cuboidw [BlockName]",
            Help = "Draws a wireframe box around selected area. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = CuboidWireframe
        };

        internal static void CuboidWireframe( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.CuboidWireframe );
        }



        static readonly CommandDescriptor cdEllipsoid = new CommandDescriptor {
            Name = "ellipsoid",
            Aliases = new[] { "e" },
            Permissions = new[] { Permission.Draw },
            Usage = "/ellipsoid [BlockName]",
            Help = "Fills a sphere-like (ellipsoidal) area with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = Ellipsoid
        };

        internal static void Ellipsoid( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Ellipsoid );
        }


        static readonly CommandDescriptor cdEllipsoidHollow = new CommandDescriptor {
            Name = "ellipsoidh",
            Aliases = new[] { "eh" },
            Permissions = new[] { Permission.Draw },
            Usage = "/ellipsoidh [BlockName]",
            Help = "Allows to fill a sphere-like (ellipsoidal) area with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = EllipsoidHollow
        };

        internal static void EllipsoidHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.EllipsoidHollow );
        }


        static readonly CommandDescriptor cdSphere = new CommandDescriptor {
            Name = "sphere",
            Aliases = new[] { "sp", "spheroid" },
            Permissions = new[] { Permission.Draw },
            Usage = "/sphere [BlockName]",
            Help = "Fills a spherical area with blocks. " +
                   "First mark is the center of the sphere, second mark defines the radius." +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = Sphere
        };

        internal static void Sphere( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Sphere );
        }


        static readonly CommandDescriptor cdSphereHollow = new CommandDescriptor {
            Name = "sphereh",
            Aliases = new[] { "sph", "hsphere" },
            Permissions = new[] { Permission.Draw },
            Usage = "/sphereh [BlockName]",
            Help = "Surrounds a spherical area with a shell of blocks. " +
                   "First mark is the center of the sphere, second mark defines the radius." +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = SphereHollow
        };

        internal static void SphereHollow( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.SphereHollow );
        }


        static readonly CommandDescriptor cdReplace = new CommandDescriptor {
            Name = "replace",
            Aliases = new[] { "r" },
            Permissions = new[] { Permission.Draw },
            Usage = "/replace BlockToReplace [AnotherOne, ...] ReplacementBlock",
            Help = "Replaces all blocks of specified type(s) in an area.",
            Handler = Replace
        };

        internal static void Replace( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Replace );
        }



        static readonly CommandDescriptor cdReplaceNot = new CommandDescriptor {
            Name = "replacenot",
            Aliases = new[] { "rn" },
            Permissions = new[] { Permission.Draw },
            Usage = "/replacenot (ExcludedBlock [AnotherOne]) ReplacementBlock",
            Help = "Replaces all blocks EXCEPT specified type(s) in an area.",
            Handler = ReplaceNot
        };

        internal static void ReplaceNot( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.ReplaceNot );
        }



        static readonly CommandDescriptor cdLine = new CommandDescriptor {
            Name = "line",
            Aliases = new[] { "ln" },
            Permissions = new[] { Permission.Draw },
            Usage = "/line [BlockName]",
            Help = "Draws a line between two points with blocks. " +
                   "If BlockType is omitted, uses the block that player is holding.",
            Handler = Line
        };

        internal static void Line( Player player, Command cmd ) {
            Draw( player, cmd, DrawMode.Line );
        }

        #endregion


        internal static void Draw( Player player, Command cmd, DrawMode mode ) {
            string blockName = cmd.Next();
            Block block = Block.Undefined;

            Permission permission = Permission.Build;

            // if a type is specified in chat, try to parse it
            if( blockName != null ) {
                block = Map.GetBlockByName( blockName );

                switch( block ) {
                    case Block.Admincrete:
                        permission = Permission.PlaceAdmincrete; break;
                    case Block.Air:
                        permission = Permission.Delete; break;
                    case Block.Water:
                    case Block.StillWater:
                        permission = Permission.PlaceWater; break;
                    case Block.Lava:
                    case Block.StillLava:
                        permission = Permission.PlaceLava; break;
                    case Block.Undefined:
                        player.MessageNow( "{0}: Unrecognized block: {1}",
                                           mode, blockName );
                        return;
                }
            } // else { use the last-used-block }

            // ReplaceNot does not need permission (since the block is EXCLUDED)
            if( !player.Can( permission ) && mode != DrawMode.ReplaceNot ) {
                player.MessageNow( "{0}: You are not allowed to draw with this block ({1})",
                                   mode, blockName );
                return;
            }

            player.SelectionArgs = (byte)block;
            switch( mode ) {
                case DrawMode.Cuboid:
                    player.SelectionCallback = CuboidCallback;
                    break;

                case DrawMode.CuboidHollow:
                    player.SelectionCallback = CuboidHollowCallback;
                    string innerBlockName = cmd.Next();
                    Block innerBlock = Block.Undefined;
                    if( innerBlockName != null ) {
                        innerBlock = Map.GetBlockByName( innerBlockName );
                        if( innerBlock == Block.Undefined ) {
                            player.Message( "{0}: Unrecognized block: {1}",
                                            mode, innerBlockName );
                        }
                    }
                    player.SelectionArgs = new CuboidHollowArgs {
                        OuterBlock = block,
                        InnerBlock = innerBlock
                    };
                    break;

                case DrawMode.CuboidWireframe:
                    player.SelectionCallback = CuboidWireframeCallback;
                    break;

                case DrawMode.Ellipsoid:
                    player.SelectionCallback = EllipsoidCallback;
                    break;

                case DrawMode.EllipsoidHollow:
                    player.SelectionCallback = EllipsoidHollowCallback;
                    break;

                case DrawMode.Sphere:
                    player.SelectionCallback = SphereCallback;
                    break;

                case DrawMode.SphereHollow:
                    player.SelectionCallback = SphereHollowCallback;
                    break;

                case DrawMode.Replace:
                case DrawMode.ReplaceNot:
                    List<Block> affectedTypes = new List<Block> { block };

                    string affectedBlockName = cmd.Next();

                    if( affectedBlockName == null ) {
                        if( mode == DrawMode.ReplaceNot ) {
                            cdReplaceNot.PrintUsage( player );
                        } else {
                            cdReplace.PrintUsage( player );
                        }
                        return;
                    }

                    do {
                        Block affectedType = Map.GetBlockByName( affectedBlockName );
                        if( affectedType != Block.Undefined ) {
                            affectedTypes.Add( affectedType );
                        } else {
                            player.MessageNow( "{0}: Unrecognized block type: {1}", mode, affectedBlockName );
                            return;
                        }
                    } while( (affectedBlockName = cmd.Next()) != null );

                    if( affectedTypes.Count > 1 ) {
                        Block replacementType = affectedTypes[affectedTypes.Count - 1];
                        affectedTypes.RemoveAt( affectedTypes.Count - 1 );
                        player.SelectionArgs = new ReplaceArgs {
                            DoExclude = (mode == DrawMode.ReplaceNot),
                            Types = affectedTypes.ToArray(),
                            ReplacementBlock = replacementType
                        };
                        string affectedString = "";
                        foreach( Block affectedBlock in affectedTypes ) {
                            affectedString += ", " + affectedBlock;
                        }
                        if( mode == DrawMode.ReplaceNot ) {
                            player.MessageNow( "ReplaceNot: Ready to replace everything EXCEPT ({0}) with {1}", affectedString.Substring( 2 ), replacementType );
                        } else {
                            player.MessageNow( "Replace: Ready to replace ({0}) with {1}", affectedString.Substring( 2 ), replacementType );
                        }
                        player.SelectionCallback = ReplaceCallback;
                    }
                    break;

                case DrawMode.Line:
                    player.SelectionCallback = LineCallback;
                    break;
            }

            player.SelectionMarksExpected = 2;
            player.SelectionMarkCount = 0;
            player.SelectionMarks.Clear();
            if( block != Block.Undefined ) {
                player.MessageNow( "{0} ({1}): Click a block or use &H/mark",
                                   mode, block );
            } else {
                player.MessageNow( "{0}: Click a block or use &H/mark",
                   mode, block );
            }
        }


        #region Undo / Redo

        static readonly CommandDescriptor cdUndo = new CommandDescriptor {
            Name = "undo",
            Permissions = new[] { Permission.Draw },
            Aliases = new[] { "redo" },
            Help = "Selectively removes changes from your last drawing command. " +
                   "Note that commands involving over 2 million blocks cannot be undone due to memory restrictions.",
            Handler = Undo
        };

        internal static void Undo( Player player, Command command ) {
            if( player.UndoBuffer.Count > 0 ) {
                // no need to set player.drawingInProgress here because this is done on the user thread
                Logger.Log( "Player {0} initiated /undo affecting {1} blocks (on world {2})", LogType.UserActivity,
                            player.Name,
                            player.UndoBuffer.Count,
                            player.World.Name );
                player.MessageNow( "Restoring {0} blocks...", player.UndoBuffer.Count );
                Queue<BlockUpdate> redoBuffer = new Queue<BlockUpdate>();
                while( player.UndoBuffer.Count > 0 ) {
                    BlockUpdate newBlock = player.UndoBuffer.Dequeue();
                    BlockUpdate oldBlock = new BlockUpdate( null, newBlock.X, newBlock.Y, newBlock.H,
                                                            player.World.Map.GetBlock( newBlock.X, newBlock.Y, newBlock.H ) );
                    player.World.Map.QueueUpdate( newBlock );
                    redoBuffer.Enqueue( oldBlock );
                }
                player.UndoBuffer = redoBuffer;
                redoBuffer.TrimExcess();
                player.MessageNow( "Type /undo again to reverse this command." );
                Server.RequestGC();

            } else {
                player.MessageNow( "There is currently nothing to undo." );
            }
        }

        #endregion


        #region Draw Callbacks

        static void DrawOneBlock( Player player, byte drawBlock, int x, int y, int h, ref int blocks, ref int blocksDenied, ref bool cannotUndo ) {
            if( !player.World.Map.InBounds( x, y, h ) ) return;
            if( player.CanPlace( x, y, h, drawBlock ) != CanPlaceResult.Allowed ) {
                blocksDenied++;
                return;
            }
            byte block = player.World.Map.GetBlock( x, y, h );
            if( block == drawBlock ) return;

            // this would've been an easy way to do block tracking for draw commands BUT
            // if i set "origin" to player, he will not receive the block update. I tried.
            player.World.Map.QueueUpdate( new BlockUpdate( null, x, y, h, drawBlock ) );
            //player.SendDelayed( PacketWriter.MakeSetBlock( x, y, h, drawBlock ) );

            if( blocks < MaxUndoCount ) {
                player.UndoBuffer.Enqueue( new BlockUpdate( null, x, y, h, block ) );
            } else if( !cannotUndo ) {
                player.UndoBuffer.Clear();
                player.UndoBuffer.TrimExcess();
                player.MessageNow( "NOTE: This draw command is too massive to undo." );
                if( player.Can( Permission.ManageWorlds ) ) {
                    player.MessageNow( "Reminder: You can use &H/wflush&S to accelerate draw commands." );
                }
                cannotUndo = true;
            }
            blocks++;
        }


        static void DrawingFinished( Player player, string verb, int blocks, int blocksDenied ) {
            if( blocks == 0 ) {
                if( blocksDenied > 0 ) {
                    player.MessageNow( "No blocks could be {0} due to permission issues.", verb );
                } else {
                    player.MessageNow( "No blocks were {0}.", verb );
                }
            } else {
                if( blocksDenied > 0 ) {
                    player.MessageNow( "{0} {1} blocks ({2} blocks skipped due to permission issues)... " +
                                       "The map is now being updated.", verb, blocks, blocksDenied );
                } else {
                    player.MessageNow( "{0} {1} blocks... The map is now being updated.", verb, blocks );
                }
            }
            if( blocks > 0 ) {
                player.Info.ProcessDrawCommand( blocks );
                player.UndoBuffer.TrimExcess();
                Server.RequestGC();
            }
        }


        #region Cuboid, CuboidHollow, CuboidWireframe

        internal static void CuboidCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.LastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sh = Math.Min( marks[0].H, marks[1].H );
            int eh = Math.Max( marks[0].H, marks[1].H );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {
                                DrawOneBlock( player, drawBlock, x + x3, y + y3, h, ref blocks, ref blocksDenied, ref cannotUndo );
                            }
                        }
                    }
                }
            }
            DrawingFinished( player, "drawn", blocks, blocksDenied );
            Logger.Log( "{0} drew a cuboid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name );
        }


        internal static void CuboidHollowCallback( Player player, Position[] marks, object tag ) {
            CuboidHollowArgs args = (CuboidHollowArgs)tag;
            byte drawBlock = (byte)args.OuterBlock;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.LastUsedBlockType;
            }


            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sh = Math.Min( marks[0].H, marks[1].H );
            int eh = Math.Max( marks[0].H, marks[1].H );

            bool fillInner = (args.InnerBlock != Block.Undefined && (ex - sx) > 1 && (ey - sy) > 1 && (eh - sh) > 1);


            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !fillInner ) {
                volume -= (ex - sx - 1) * (ey - sy - 1) * (eh - sh - 1);
            }

            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    DrawOneBlock( player, drawBlock, x, y, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, y, eh, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }
            for( int x = sx; x <= ex; x++ ) {
                for( int h = sh; h <= eh; h++ ) {
                    DrawOneBlock( player, drawBlock, x, sy, h, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, x, ey, h, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }
            for( int y = sy; y <= ey; y++ ) {
                for( int h = sh; h <= eh; h++ ) {
                    DrawOneBlock( player, drawBlock, sx, y, h, ref blocks, ref blocksDenied, ref cannotUndo );
                    DrawOneBlock( player, drawBlock, ex, y, h, ref blocks, ref blocksDenied, ref cannotUndo );
                }
            }

            if( fillInner ) {
                for( int x = sx + 1; x < ex; x += DrawStride ) {
                    for( int y = sy + 1; y < ey; y += DrawStride ) {
                        for( int h = sh + 1; h < eh; h++ ) {
                            for( int y3 = 0; y3 < DrawStride && y + y3 < ey; y3++ ) {
                                for( int x3 = 0; x3 < DrawStride && x + x3 < ex; x3++ ) {
                                    DrawOneBlock( player, (byte)args.InnerBlock, x + x3, y + y3, h, ref blocks, ref blocksDenied, ref cannotUndo );
                                }
                            }
                        }
                    }
                }
            }

            Logger.Log( "{0} drew a hollow cuboid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void CuboidWireframeCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.LastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sh = Math.Min( marks[0].H, marks[1].H );
            int eh = Math.Max( marks[0].H, marks[1].H );

            int volume = (ex - sx + 1) * 4 + (ey - sy + 1) * 4 + (eh - sh + 1) * 4 - 16;

            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied=0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                DrawOneBlock( player, drawBlock, x, sy, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, x, sy, eh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, x, ey, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, x, ey, eh, ref blocks, ref blocksDenied, ref cannotUndo );
            }

            for( int y = sy; y <= ey; y++ ) {
                DrawOneBlock( player, drawBlock, sx, y, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, sx, y, eh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, ex, y, sh, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, ex, y, eh, ref blocks, ref blocksDenied, ref cannotUndo );
            }

            for( int h = sh; h <= eh; h++ ) {
                DrawOneBlock( player, drawBlock, sx, sy, h, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, ex, sy, h, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, sx, ey, h, ref blocks, ref blocksDenied, ref cannotUndo );
                DrawOneBlock( player, drawBlock, ex, ey, h, ref blocks, ref blocksDenied, ref cannotUndo );
            }

            Logger.Log( "{0} drew a wireframe cuboid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }

        #endregion


        unsafe internal static void ReplaceCallback( Player player, Position[] marks, object drawArgs ) {
            ReplaceArgs args = (ReplaceArgs)drawArgs;

            byte* specialTypes = stackalloc byte[args.Types.Length];
            int specialTypeCount = args.Types.Length;
            for( int i = 0; i < args.Types.Length; i++ ) {
                specialTypes[i] = (byte)args.Types[i];
            }
            byte replacementBlock = (byte)args.ReplacementBlock;
            bool doExclude = args.DoExclude;

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sh = Math.Min( marks[0].H, marks[1].H );
            int eh = Math.Max( marks[0].H, marks[1].H );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                    player.Info.Rank.DrawLimit,
                                    volume );
                return;
            }

            player.UndoBuffer.Clear();

            bool cannotUndo = false;
            int blocks = 0, blocksDenied = 0;
            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {

                                byte block = player.World.Map.GetBlock( x + x3, y + y3, h );

                                bool skip = !args.DoExclude;
                                for( int i = 0; i < specialTypeCount; i++ ) {
                                    if( block == specialTypes[i] ) {
                                        skip = args.DoExclude;
                                        break;
                                    }
                                }
                                if( skip ) continue;

                                if( player.CanPlace( x + x3, y + y3, h, replacementBlock ) != CanPlaceResult.Allowed ) {
                                    blocksDenied++;
                                    continue;
                                }
                                player.World.Map.QueueUpdate( new BlockUpdate( null, x + x3, y + y3, h, replacementBlock ) );
                                if( blocks < MaxUndoCount ) {
                                    player.UndoBuffer.Enqueue( new BlockUpdate( null, x + x3, y + y3, h, block ) );
                                } else if( !cannotUndo ) {
                                    player.UndoBuffer.Clear();
                                    player.UndoBuffer.TrimExcess();
                                    player.MessageNow( "NOTE: This draw command is too massive to undo." );
                                    cannotUndo = true;
                                    if( player.Can( Permission.ManageWorlds ) ) {
                                        player.MessageNow( "Reminder: You can use &H/wflush&S to accelerate draw commands." );
                                    }
                                }
                                blocks++;

                            }
                        }
                    }
                }
            }

            string affectedString = "";
            for( int i = 0; i < specialTypeCount; i++ ) {
                affectedString += ", " + ((Block)specialTypes[i]);
            }
            Logger.Log( "{0} replaced {1} blocks {2} ({3}) with {4} (on world {5})", LogType.UserActivity,
                        player.Name, blocks,
                        (doExclude ? "except" : "of"),
                        affectedString.Substring( 2 ), (Block)replacementBlock,
                        player.World.Name );

            DrawingFinished( player, "replaced", blocks, blocksDenied );
        }


        #region Ellipsoid, Hollow Ellipsoid, Sphere, HollowSphere

        internal static void EllipsoidCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.LastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sh = Math.Min( marks[0].H, marks[1].H );
            int eh = Math.Max( marks[0].H, marks[1].H );

            // find axis lengths
            double rx = (ex - sx + 1) / 2d;
            double ry = (ey - sy + 1) / 2d;
            double rh = (eh - sh + 1) / 2d;

            double rx2 = 1 / (rx * rx);
            double ry2 = 1 / (ry * ry);
            double rh2 = 1 / (rh * rh);

            // find center points
            double cx = (ex + sx) / 2d;
            double cy = (ey + sy) / 2d;
            double ch = (eh + sh) / 2d;


            int volume = (int)(4 / 3d * Math.PI * rx * ry * rh);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x += DrawStride ) {
                for( int y = sy; y <= ey; y += DrawStride ) {
                    for( int h = sh; h <= eh; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= ey; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= ex; x3++ ) {

                                // get relative coordinates
                                double dx = (x + x3 - cx);
                                double dy = (y + y3 - cy);
                                double dh = (h - ch);

                                // test if it's inside ellipse
                                if( (dx * dx) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 <= 1 ) {
                                    DrawOneBlock( player, drawBlock, x + x3, y + y3, h, ref blocks, ref blocksDenied, ref cannotUndo );
                                }
                            }
                        }
                    }
                }
            }
            Logger.Log( "{0} drew an ellipsoid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void EllipsoidHollowCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.LastUsedBlockType;
            }

            // find start/end coordinates
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sh = Math.Min( marks[0].H, marks[1].H );
            int eh = Math.Max( marks[0].H, marks[1].H );

            // find axis lengths
            double rx = (ex - sx + 1) / 2d;
            double ry = (ey - sy + 1) / 2d;
            double rh = (eh - sh + 1) / 2d;

            double rx2 = 1 / (rx * rx);
            double ry2 = 1 / (ry * ry);
            double rh2 = 1 / (rh * rh);

            // find center points
            double cx = (ex + sx) / 2d;
            double cy = (ey + sy) / 2d;
            double ch = (eh + sh) / 2d;

            // rougher estimation than the non-hollow form, a voxelized surface is a bit funky
            int volume = (int)(4 / 3d * Math.PI * ((rx + .5) * (ry + .5) * (rh + .5) - (rx - .5) * (ry - .5) * (rh - .5)) * 0.85);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( "You are only allowed to run draw commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                   player.Info.Rank.DrawLimit,
                                   volume );
                return;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {

                        double dx = (x - cx);
                        double dy = (y - cy);
                        double dh = (h - ch);

                        if( (dx * dx) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 <= 1 ) {
                            // we touched the surface
                            // keep drilling until we hit an internal block
                            do {
                                DrawOneBlock( player, drawBlock, x, y, h, ref blocks, ref blocksDenied, ref cannotUndo );
                                DrawOneBlock( player, drawBlock, x, y, (int)(ch - dh), ref blocks, ref blocksDenied, ref cannotUndo );
                                dh = (++h - ch);
                            } while( h <= (int)ch &&
                                    ((dx + 1) * (dx + 1) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 > 1 ||
                                     (dx - 1) * (dx - 1) * rx2 + (dy * dy) * ry2 + (dh * dh) * rh2 > 1 ||
                                     (dx * dx) * rx2 + (dy + 1) * (dy + 1) * ry2 + (dh * dh) * rh2 > 1 ||
                                     (dx * dx) * rx2 + (dy - 1) * (dy - 1) * ry2 + (dh * dh) * rh2 > 1 ||
                                     (dx * dx) * rx2 + (dy * dy) * ry2 + (dh + 1) * (dh + 1) * rh2 > 1 ||
                                     (dx * dx) * rx2 + (dy * dy) * ry2 + (dh - 1) * (dh - 1) * rh2 > 1) );
                            break;
                        }
                    }
                }
            }
            Logger.Log( "{0} drew a hollow ellipsoid containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }


        internal static void SphereCallback( Player player, Position[] marks, object tag ) {
            double radius = Math.Sqrt( (marks[0].X - marks[1].X) * (marks[0].X - marks[1].X) +
                                       (marks[0].Y - marks[1].Y) * (marks[0].Y - marks[1].Y) +
                                       (marks[0].H - marks[1].H) * (marks[0].H - marks[1].H) );

            marks[1].X = (short)Math.Round( marks[0].X - radius );
            marks[1].Y = (short)Math.Round( marks[0].Y - radius );
            marks[1].H = (short)Math.Round( marks[0].H - radius );

            marks[0].X = (short)Math.Round( marks[0].X + radius );
            marks[0].Y = (short)Math.Round( marks[0].Y + radius );
            marks[0].H = (short)Math.Round( marks[0].H + radius );

            EllipsoidCallback( player, marks, tag );
        }


        internal static void SphereHollowCallback( Player player, Position[] marks, object tag ) {
            double radius = Math.Sqrt( (marks[0].X - marks[1].X) * (marks[0].X - marks[1].X) +
                                       (marks[0].Y - marks[1].Y) * (marks[0].Y - marks[1].Y) +
                                       (marks[0].H - marks[1].H) * (marks[0].H - marks[1].H) );

            marks[1].X = (short)Math.Round( marks[0].X - radius );
            marks[1].Y = (short)Math.Round( marks[0].Y - radius );
            marks[1].H = (short)Math.Round( marks[0].H - radius );

            marks[0].X = (short)Math.Round( marks[0].X + radius );
            marks[0].Y = (short)Math.Round( marks[0].Y + radius );
            marks[0].H = (short)Math.Round( marks[0].H + radius );

            EllipsoidHollowCallback( player, marks, tag );
        }

        #endregion


        internal static void LineCallback( Player player, Position[] marks, object tag ) {
            byte drawBlock = (byte)tag;
            if( drawBlock == (byte)Block.Undefined ) {
                drawBlock = (byte)player.LastUsedBlockType;
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            // LINE CODE

            int x1 = marks[0].X,
                y1 = marks[0].Y,
                z1 = marks[0].H,
                x2 = marks[1].X,
                y2 = marks[1].Y,
                z2 = marks[1].H;
            int i, err1, err2;
            int[] pixel = new int[3];
            pixel[0] = x1;
            pixel[1] = y1;
            pixel[2] = z1;
            int dx = x2 - x1;
            int dy = y2 - y1;
            int dz = z2 - z1;
            int xInc = (dx < 0) ? -1 : 1;
            int l = Math.Abs( dx );
            int yInc = (dy < 0) ? -1 : 1;
            int m = Math.Abs( dy );
            int zInc = (dz < 0) ? -1 : 1;
            int n = Math.Abs( dz );
            int dx2 = l << 1;
            int dy2 = m << 1;
            int dz2 = n << 1;

            DrawOneBlock( player, drawBlock, x2, y2, z2, ref blocks, ref blocksDenied, ref cannotUndo );

            if( (l >= m) && (l >= n) ) {

                err1 = dy2 - l;
                err2 = dz2 - l;
                for( i = 0; i < l; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err1 > 0 ) {
                        pixel[1] += yInc;
                        err1 -= dx2;
                    }
                    if( err2 > 0 ) {
                        pixel[2] += zInc;
                        err2 -= dx2;
                    }
                    err1 += dy2;
                    err2 += dz2;
                    pixel[0] += xInc;
                }
            } else if( (m >= l) && (m >= n) ) {
                err1 = dx2 - m;
                err2 = dz2 - m;
                for( i = 0; i < m; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err1 > 0 ) {
                        pixel[0] += xInc;
                        err1 -= dy2;
                    }
                    if( err2 > 0 ) {
                        pixel[2] += zInc;
                        err2 -= dy2;
                    }
                    err1 += dx2;
                    err2 += dz2;
                    pixel[1] += yInc;
                }
            } else {
                err1 = dy2 - n;
                err2 = dx2 - n;
                for( i = 0; i < n; i++ ) {
                    DrawOneBlock( player, drawBlock, pixel[0], pixel[1], pixel[2], ref blocks, ref blocksDenied, ref cannotUndo );
                    if( err1 > 0 ) {
                        pixel[1] += yInc;
                        err1 -= dz2;
                    }
                    if( err2 > 0 ) {
                        pixel[0] += xInc;
                        err2 -= dz2;
                    }
                    err1 += dy2;
                    err2 += dx2;
                    pixel[2] += zInc;
                }
            }

            // END LINE CODE
            Logger.Log( "{0} drew a line containing {1} blocks of type {2} (on world {3})", LogType.UserActivity,
                        player.Name,
                        blocks,
                        (Block)drawBlock,
                        player.World.Name );
            DrawingFinished( player, "drawn", blocks, blocksDenied );
        }

        #endregion


        #region Copy and Paste

        static readonly CommandDescriptor cdCopy = new CommandDescriptor {
            Name = "copy",
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Copy blocks for pasting. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            Handler = Copy
        };

        internal static void Copy( Player player, Command cmd ) {
            player.SetCallback( 2, CopyCallback, null );
            player.MessageNow( "Copy: Place a block or type /mark to use your location." );
        }

        internal static void CopyCallback( Player player, Position[] marks, object tag ) {
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sh = Math.Min( marks[0].H, marks[1].H );
            int eh = Math.Max( marks[0].H, marks[1].H );

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.Info.Rank.DrawLimit, volume ) );
                return;
            }

            // remember dimensions and orientation
            CopyInformation copyInfo = new CopyInformation {
                WidthX = marks[1].X - marks[0].X,
                WidthY = marks[1].Y - marks[0].Y,
                Height = marks[1].H - marks[0].H,
                Buffer = new byte[ex - sx + 1,ey - sy + 1,eh - sh + 1]
            };

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {
                        copyInfo.Buffer[x - sx, y - sy, h - sh] = player.World.Map.GetBlock( x, y, h );
                    }
                }
            }

            player.CopyInformation = copyInfo;
            player.MessageNow( "{0} blocks were copied. You can now &H/paste", volume );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.Height > 0 ? "bottom" : "top"),
                               (copyInfo.WidthY > 0 ? "south" : "north"),
                               (copyInfo.WidthX > 0 ? "west" : "east") );

            Logger.Log( "{0} copied {1} blocks from {2}.", LogType.UserActivity,
                        player.Name, volume, player.World.Name );
        }



        static readonly CommandDescriptor cdCut = new CommandDescriptor {
            Name = "cut",
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Copies and removes blocks for pasting. Unless a different block type is specified, the area is filled with air. " +
                   "Used together with &H/paste&S and &H/pastenot&S commands. " +
                   "Note that pasting starts at the same corner that you started &H/cut&S from.",
            Usage = "/cut [FillBlock]",
            Handler = Cut
        };

        internal static void Cut( Player player, Command cmd ) {
            Block fillBlock = Block.Air;
            string fillBlockName = cmd.Next();
            if(fillBlockName!=null){
                fillBlock = Map.GetBlockByName( fillBlockName );
                if( fillBlock == Block.Undefined ) {
                    player.Message( "Cut: Unknown block type \"{0}\"", fillBlockName );
                    return;
                }
            }
            player.SetCallback( 2, CutCallback, fillBlock );
            player.MessageNow( "Cut: Place a block or type /mark to use your location." );
        }

        internal static void CutCallback( Player player, Position[] marks, object tag ) {
            int sx = Math.Min( marks[0].X, marks[1].X );
            int ex = Math.Max( marks[0].X, marks[1].X );
            int sy = Math.Min( marks[0].Y, marks[1].Y );
            int ey = Math.Max( marks[0].Y, marks[1].Y );
            int sh = Math.Min( marks[0].H, marks[1].H );
            int eh = Math.Max( marks[0].H, marks[1].H );

            byte fillType = (byte)tag;

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1);
            if( !player.CanDraw( volume ) ) {
                player.MessageNow( String.Format( "You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.Info.Rank.DrawLimit, volume ) );
                return;
            }

            // remember dimensions and orientation
            CopyInformation copyInfo = new CopyInformation {
                WidthX = marks[1].X - marks[0].X,
                WidthY = marks[1].Y - marks[0].Y,
                Height = marks[1].H - marks[0].H,
                Buffer = new byte[ex - sx + 1, ey - sy + 1, eh - sh + 1]
            };

            player.UndoBuffer.Clear();
            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = sx; x <= ex; x++ ) {
                for( int y = sy; y <= ey; y++ ) {
                    for( int h = sh; h <= eh; h++ ) {
                        copyInfo.Buffer[x - sx, y - sy, h - sh] = player.World.Map.GetBlock( x, y, h );
                        DrawOneBlock( player, fillType, x, y, h, ref blocks, ref blocksDenied, ref cannotUndo );
                    }
                }
            }

            player.CopyInformation = copyInfo;
            player.MessageNow( "{0} blocks were cut. You can now &H/paste", volume );
            player.MessageNow( "Origin at {0} {1}{2} corner.",
                               (copyInfo.Height > 0 ? "bottom" : "top"),
                               (copyInfo.WidthY > 0 ? "south" : "north"),
                               (copyInfo.WidthX > 0 ? "west" : "east") );
            ;
            Logger.Log( "{0} cut {1} blocks from {2}, replacing {3} blocks with {4}.", LogType.UserActivity,
                        player.Name, volume, player.World.Name, blocks, (Block)fillType );

            player.UndoBuffer.TrimExcess();
            Server.RequestGC();
        }



        static readonly CommandDescriptor cdPasteNot = new CommandDescriptor {
            Name = "pastenot",
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Paste previously copied blocks, excluding specified block type(s). " +
                   "Used together with &H/copy&S command. " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from. ",
            Usage = "/pastenot ExcludedBlock [AnotherOne [AndAnother]]",
            Handler = PasteNot
        };

        internal static void PasteNot( Player player, Command cmd ) {
            if( player.CopyInformation == null ) {
                player.MessageNow( "Nothing to paste! Copy something first." );
                return;
            }

            PasteArgs args;

            List<Block> excludedTypes = new List<Block>();
            string excludedBlockName = cmd.Next();
            if( excludedBlockName != null ) {
                do {
                    Block excludedType = Map.GetBlockByName( excludedBlockName );
                    if( excludedType != Block.Undefined ) {
                        excludedTypes.Add( excludedType );
                    } else {
                        player.MessageNow( "PasteNot: Unrecognized block type: {0}", excludedBlockName );
                        return;
                    }
                } while( (excludedBlockName = cmd.Next()) != null );
            }

            if( excludedTypes.Count > 0 ) {
                args = new PasteArgs {
                    DoExclude = true,
                    BlockTypes = excludedTypes.ToArray()
                };
                string includedString = "";
                foreach( Block block in excludedTypes ) {
                    includedString += ", " + block;
                }
                player.MessageNow( "Ready to paste all EXCEPT {0}", includedString.Substring( 2 ) );
            } else {
                player.MessageNow( "PasteNot: Please specify block(s) to exclude." );
                return;
            }

            player.SetCallback( 1, PasteCallback, args );

            player.MessageNow( "PasteNot: Place a block or type /mark to use your location. " );
        }


        static readonly CommandDescriptor cdPaste = new CommandDescriptor {
            Name = "paste",
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Pastes previously copied blocks. Used together with &H/copy&S command. " +
                   "If one or more optional IncludedBlock parameters are specified, ONLY pastes blocks of specified type(s). " +
                   "Note that pasting starts at the same corner that you started &H/copy&S from.",
            Usage = "/paste [IncludedBlock [AnotherOne [AndAnother]]]",
            Handler = Paste
        };

        internal static void Paste( Player player, Command cmd ) {
            if( player.CopyInformation == null ) {
                player.MessageNow( "Nothing to paste! Copy something first." );
                return;
            }

            List<Block> includedTypes = new List<Block>();
            string includedBlockName = cmd.Next();
            if( includedBlockName != null ) {
                do {
                    Block includedType = Map.GetBlockByName( includedBlockName );
                    if( includedType != Block.Undefined ) {
                        includedTypes.Add( includedType );
                    } else {
                        player.MessageNow( "PasteNot: Unrecognized block type: {0}", includedBlockName );
                        return;
                    }
                } while( (includedBlockName = cmd.Next()) != null );
            }

            PasteArgs args;
            if( includedTypes.Count > 0 ) {
                args = new PasteArgs {
                    DoInclude = true,
                    BlockTypes = includedTypes.ToArray()
                };
                string includedString = "";
                foreach( Block block in includedTypes ) {
                    includedString += ", " + block;
                }
                player.MessageNow( "Ready to paste ONLY {0}", includedString.Substring( 2 ) );
            } else {
                args = new PasteArgs {
                    BlockTypes = new Block[0]
                };
            }

            player.SetCallback( 1, PasteCallback, args );

            player.MessageNow( "Paste: Place a block or type /mark to use your location. " );
        }


        unsafe internal static void PasteCallback( Player player, Position[] marks, object tag ) {
            CopyInformation info = player.CopyInformation;

            PasteArgs args = (PasteArgs)tag;
            byte* specialTypes = stackalloc byte[args.BlockTypes.Length];
            int specialTypeCount = args.BlockTypes.Length;
            for( int i = 0; i < args.BlockTypes.Length; i++ ) {
                specialTypes[i] = (byte)args.BlockTypes[i];
            }
            Map map = player.World.Map;

            BoundingBox bounds = new BoundingBox( marks[0], info.WidthX, info.WidthY, info.Height );

            if( bounds.xMin < 0 || bounds.xMax > map.WidthX - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (X), paste cut off." );
            }
            if( bounds.yMin < 0 || bounds.yMax > map.WidthY - 1 ) {
                player.MessageNow( "Warning: Not enough room horizontally (Y), paste cut off." );
            }
            if( bounds.hMin < 0 || bounds.hMax > map.Height - 1 ) {
                player.MessageNow( "Warning: Not enough room vertically, paste cut off." );
            }

            player.UndoBuffer.Clear();

            int blocks = 0, blocksDenied = 0;
            bool cannotUndo = false;

            for( int x = bounds.xMin; x <= bounds.xMax; x += DrawStride ) {
                for( int y = bounds.yMin; y <= bounds.yMax; y += DrawStride ) {
                    for( int h = bounds.hMin; h <= bounds.hMax; h++ ) {
                        for( int y3 = 0; y3 < DrawStride && y + y3 <= bounds.yMax; y3++ ) {
                            for( int x3 = 0; x3 < DrawStride && x + x3 <= bounds.xMax; x3++ ) {
                                byte block = info.Buffer[x + x3 - bounds.xMin, y + y3 - bounds.yMin, h - bounds.hMin];

                                if( args.DoInclude ) {
                                    bool skip = true;
                                    for( int i = 0; i < specialTypeCount; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = false;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                } else if( args.DoExclude ) {
                                    bool skip = false;
                                    for( int i = 0; i < specialTypeCount; i++ ) {
                                        if( block == specialTypes[i] ) {
                                            skip = true;
                                            break;
                                        }
                                    }
                                    if( skip ) continue;
                                }
                                DrawOneBlock( player, block, x + x3, y + y3, h, ref blocks, ref blocksDenied, ref cannotUndo );
                            }
                        }
                    }
                }
            }

            Logger.Log( "{0} pasted {1} blocks to {2}.", LogType.UserActivity,
                        player.Name, blocks, player.World.Name );
            DrawingFinished( player, "pasted", blocks, blocksDenied );
        }


        static readonly CommandDescriptor cdMirror = new CommandDescriptor {
            Name = "mirror",
            Aliases = new[] { "flip" },
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Flips copied blocks along specified axis/axes. " +
                   "The axes are: X = horizontal (east-west), Y = horizontal (north-south), Z = vertical. " +
                   "You can mirror more than one axis at a time, e.g. &H/copymirror X Y&S.",
            Usage = "/mirror [X] [Y] [Z]",
            Handler = Mirror
        };

        internal static void Mirror( Player player, Command cmd ) {
            if( player.CopyInformation == null ) {
                player.MessageNow( "Nothing to flip! Copy something first." );
                return;
            }

            bool flipX = false, flipY = false, flipH = false;
            string axis;
            while( (axis = cmd.Next()) != null ) {
                foreach( char c in axis.ToLower() ) {
                    if( c == 'x' ) flipX = true;
                    if( c == 'y' ) flipY = true;
                    if( c == 'z' ) flipH = true;
                }
            }

            if( !flipX && !flipY && !flipH ) {
                cdMirror.PrintUsage( player );
                return;
            }

            byte block;
            byte[, ,] buffer = player.CopyInformation.Buffer;

            if( flipX ) {
                int left = 0;
                int right = buffer.GetLength( 0 ) - 1;
                while( left < right ) {
                    for( int y = player.CopyInformation.Buffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                        for( int h = player.CopyInformation.Buffer.GetLength( 2 ) - 1; h >= 0; h-- ) {
                            block = buffer[left, y, h];
                            buffer[left, y, h] = buffer[right, y, h];
                            buffer[right, y, h] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipY ) {
                int left = 0;
                int right = buffer.GetLength( 1 ) - 1;
                while( left < right ) {
                    for( int x = player.CopyInformation.Buffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                        for( int h = player.CopyInformation.Buffer.GetLength( 2 ) - 1; h >= 0; h-- ) {
                            block = buffer[x, left, h];
                            buffer[x, left, h] = buffer[x, right, h];
                            buffer[x, right, h] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipH ) {
                int left = 0;
                int right = buffer.GetLength( 2 ) - 1;
                while( left < right ) {
                    for( int x = player.CopyInformation.Buffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                        for( int y = player.CopyInformation.Buffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                            block = buffer[x, y, left];
                            buffer[x, y, left] = buffer[x, y, right];
                            buffer[x, y, right] = block;
                        }
                    }
                    left++;
                    right--;
                }
            }

            if( flipX ) {
                if( flipY ) {
                    if( flipH ) {
                        player.Message( "Flipped copy along all axes." );
                    } else {
                        player.Message( "Flipped copy along X (east/west) and Y (north/south) axes." );
                    }
                } else {
                    if( flipH ) {
                        player.Message( "Flipped copy along X (east/west) and Z (vertical) axes." );
                    } else {
                        player.Message( "Flipped copy along X (east/west) axis." );
                    }
                }
            } else {
                if( flipY ) {
                    if( flipH ) {
                        player.Message( "Flipped copy along Y (north/south) and Z (vertical) axes." );
                    } else {
                        player.Message( "Flipped copy along Y (north/south) axis." );
                    }
                } else {
                    player.Message( "Flipped copy along Z (vertical) axis." );
                }
            }
        }


        static readonly CommandDescriptor cdRotate = new CommandDescriptor {
            Name = "rotate",
            Permissions = new[] { Permission.CopyAndPaste },
            Help = "Rotates copied blocks around specifies axis/axes. If no axis is given, rotates around Z (vertical).",
            Usage = "/rotate (-90|90|180|270) (X|Y|Z)",
            Handler = Rotate
        };

        enum RotationAxis {
            X, Y, Z
        }
        internal static void Rotate( Player player, Command cmd ) {
            if( player.CopyInformation == null ) {
                player.MessageNow( "Nothing to rotate! Copy something first." );
                return;
            }

            int degrees;
            if( !cmd.NextInt( out degrees ) || (degrees != 90 && degrees != -90 && degrees != 180 && degrees != 270) ) {
                cdRotate.PrintUsage( player );
                return;
            }

            string axisName = cmd.Next();
            RotationAxis axis = RotationAxis.Z;
            if( axisName != null ) {
                switch( axisName.ToLower() ) {
                    case "x":
                        axis = RotationAxis.X;
                        break;
                    case "y":
                        axis = RotationAxis.Y;
                        break;
                    case "z":
                    case "h":
                        axis = RotationAxis.Z;
                        break;
                    default:
                        cdRotate.PrintUsage( player );
                        return;
                }
            }


            // allocate the new buffer
            byte[, ,] oldBuffer = player.CopyInformation.Buffer;
            byte[, ,] newBuffer;

            if( degrees == 180 ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 2 )];

            } else if( axis == RotationAxis.X ) {
                newBuffer = new byte[oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 )];
                int dimY = player.CopyInformation.WidthY;
                player.CopyInformation.WidthY = player.CopyInformation.Height;
                player.CopyInformation.Height = dimY;

            } else if( axis == RotationAxis.Y ) {
                newBuffer = new byte[oldBuffer.GetLength( 2 ), oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 )];
                int dimX = player.CopyInformation.WidthX;
                player.CopyInformation.WidthX = player.CopyInformation.Height;
                player.CopyInformation.Height = dimX;

            } else {
                newBuffer = new byte[oldBuffer.GetLength( 1 ), oldBuffer.GetLength( 0 ), oldBuffer.GetLength( 2 )];
                int dimY = player.CopyInformation.WidthY;
                player.CopyInformation.WidthY = player.CopyInformation.WidthX;
                player.CopyInformation.WidthX = dimY;
            }


            // construct the rotation matrix
            int[,] matrix = new[,]{
                {1,0,0},
                {0,1,0},
                {0,0,1}
            };

            int a, b;
            switch( axis ) {
                case RotationAxis.X:
                    a = 1;
                    b = 2;
                    break;
                case RotationAxis.Y:
                    a = 0;
                    b = 2;
                    break;
                default:
                    a = 0;
                    b = 1;
                    break;
            }

            switch( degrees ) {
                case 90:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = -1;
                    matrix[b, a] = 1;
                    break;
                case 180:
                    matrix[a, a] = -1;
                    matrix[b, b] = -1;
                    break;
                case -90:
                case 270:
                    matrix[a, a] = 0;
                    matrix[b, b] = 0;
                    matrix[a, b] = 1;
                    matrix[b, a] = -1;
                    break;
            }

            // apply the rotation matrix
            int nx, ny, nz;
            for( int x = oldBuffer.GetLength( 0 ) - 1; x >= 0; x-- ) {
                for( int y = oldBuffer.GetLength( 1 ) - 1; y >= 0; y-- ) {
                    for( int z = oldBuffer.GetLength( 2 ) - 1; z >= 0; z-- ) {
                        nx = (matrix[0, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[0, 0] > 0 ? x : 0)) +
                             (matrix[0, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[0, 1] > 0 ? y : 0)) +
                             (matrix[0, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[0, 2] > 0 ? z : 0));
                        ny = (matrix[1, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[1, 0] > 0 ? x : 0)) +
                             (matrix[1, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[1, 1] > 0 ? y : 0)) +
                             (matrix[1, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[1, 2] > 0 ? z : 0));
                        nz = (matrix[2, 0] < 0 ? oldBuffer.GetLength( 0 ) - 1 - x : (matrix[2, 0] > 0 ? x : 0)) +
                             (matrix[2, 1] < 0 ? oldBuffer.GetLength( 1 ) - 1 - y : (matrix[2, 1] > 0 ? y : 0)) +
                             (matrix[2, 2] < 0 ? oldBuffer.GetLength( 2 ) - 1 - z : (matrix[2, 2] > 0 ? z : 0));
                        newBuffer[nx, ny, nz] = oldBuffer[x, y, z];
                    }
                }
            }

            player.Message( "Rotated copy by {0} degrees around {1} axis.", degrees, axis );
            player.CopyInformation.Buffer = newBuffer;
        }

        #endregion


        static readonly CommandDescriptor cdMark = new CommandDescriptor {
            Name = "mark",
            Aliases = new[] { "m" },
            Help = "When making a selection (for drawing or zoning) use this to make a marker at your position in the world. " +
                   "You can mark in places where making blocks is difficult (e.g. mid-air).",
            Handler = Mark
        };

        internal static void Mark( Player player, Command command ) {
            Position pos = new Position( (short)((player.Position.X - 1) / 32), (short)((player.Position.Y - 1) / 32), (short)((player.Position.H - 1) / 32) );
            pos.X = (short)Math.Min( player.World.Map.WidthX - 1, Math.Max( 0, (int)pos.X ) );
            pos.Y = (short)Math.Min( player.World.Map.WidthY - 1, Math.Max( 0, (int)pos.Y ) );
            pos.H = (short)Math.Min( player.World.Map.Height - 1, Math.Max( 0, (int)pos.H ) );

            if( player.SelectionMarksExpected > 0 ) {
                player.SelectionMarks.Enqueue( pos );
                player.SelectionMarkCount++;
                if( player.SelectionMarkCount >= player.SelectionMarksExpected ) {
                    player.SelectionCallback( player, player.SelectionMarks.ToArray(), player.SelectionArgs );
                    player.SelectionMarksExpected = 0;
                } else {
                    player.MessageNow( "Block #{0} marked at ({1},{2},{3}). Place mark #{4}.",
                                       player.SelectionMarkCount,
                                       pos.X, pos.Y, pos.H,
                                       player.SelectionMarkCount + 1 );
                }
            } else {
                player.MessageNow( "Cannot mark - no draw or zone commands initiated." );
            }
        }



        static readonly CommandDescriptor cdCancel = new CommandDescriptor {
            Name = "cancel",
            Help = "Cancels current selection (for drawing or zoning) operation, for instance if you misclicked on the first block. " +
                   "If you wish to stop a drawing in-progress, use &H/lock&S instead.",
            Handler = Cancel
        };

        internal static void Cancel( Player player, Command command ) {
            if( player.SelectionMarksExpected > 0 ) {
                player.SelectionMarksExpected = 0;
                player.MessageNow( "Selection cancelled." );
            } else {
                player.MessageNow( "There is currently nothing to cancel." );
            }
        }
    }
}