﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public sealed class ReplaceNotBrushFactory : IBrushFactory {
        public static readonly ReplaceNotBrushFactory Instance = new ReplaceNotBrushFactory();

        ReplaceNotBrushFactory() { }

        public string Name {
            get { return "ReplaceNot"; }
        }


        static readonly string[] aliases = new[] { "rn" };
        public string[] Aliases {
            get { return aliases; }
        }

        const string HelpString = "ReplaceNot brush: Replaces all blocks except the given type(s) with another type. " +
                                  "Usage similar to &H/replacenot&S command.";
        public string Help {
            get { return HelpString; }
        }


        public IBrush MakeBrush( [NotNull] Player player, [NotNull] Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Push( block );
            }
            if( blocks.Count == 0 ) {
                return new ReplaceNotBrush();
            } else if( blocks.Count == 1 ) {
                return new ReplaceNotBrush( blocks.ToArray(), Block.Undefined );
            } else {
                Block replacement = blocks.Pop();
                return new ReplaceNotBrush( blocks.ToArray(), replacement );
            }
        }
    }


    public sealed class ReplaceNotBrush : IBrushInstance, IBrush {
        public Block[] Blocks { get; private set; }
        public Block Replacement { get; private set; }

        public ReplaceNotBrush() { }

        public ReplaceNotBrush( Block[] blocks, Block replacement ) {
            Blocks = blocks;
            Replacement = replacement;
        }


        public ReplaceNotBrush( [NotNull] ReplaceNotBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
            Replacement = other.Replacement;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return ReplaceBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks == null ) {
                    return Factory.Name;
                } else if( Replacement == Block.Undefined ) {
                    return String.Format( "{0}({1})",
                                          Factory.Name,
                                          Replacement );
                } else {
                    return String.Format( "{0}({1} -> {2})",
                                          Factory.Name,
                                          Blocks.JoinToString(),
                                          Replacement );
                }
            }
        }


        public IBrushInstance MakeInstance( [NotNull] Player player, [NotNull] Command cmd, [NotNull] DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            Stack<Block> blocks = new Stack<Block>();
            while( cmd.HasNext ) {
                Block block = cmd.NextBlock( player );
                if( block == Block.Undefined ) return null;
                blocks.Push( block );
            }

            if( blocks.Count == 0 && Blocks == null ) {
                player.Message( "ReplaceNot brush requires at least 1 block." );
                return null;
            }

            if( blocks.Count > 0 ) {
                if( blocks.Count > 1 ) Replacement = blocks.Pop();
                Blocks = blocks.ToArray();
            }

            return new ReplaceNotBrush( this );
        }

        #endregion


        #region IBrushInstance members

        public IBrush Brush {
            get { return this; }
        }


        public bool HasAlternateBlock {
            get { return false; }
        }


        public string InstanceDescription {
            get { return Description; }
        }


        public bool Begin( [NotNull] Player player, [NotNull] DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( Blocks == null || Blocks.Length == 0 ) {
                throw new InvalidOperationException( "No blocks given." );
            }
            if( Replacement == Block.Undefined ) {
                if( player.LastUsedBlockType == Block.Undefined ) {
                    player.Message( "Cannot deduce desired replacement block. Click a block or type out the blocktype name." );
                    return false;
                } else {
                    Replacement = player.GetBind( player.LastUsedBlockType );
                }
            }
            return true;
        }


        public Block NextBlock( [NotNull] DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            Block block = state.Map.GetBlock( state.Coords.X, state.Coords.Y, state.Coords.Z );
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < Blocks.Length; i++ ) {
                // ReSharper restore LoopCanBeConvertedToQuery
                if( block == Blocks[i] ) {
                    return Block.Undefined;
                }
            }
            return Replacement;
        }


        public void End() { }

        #endregion
    }
}