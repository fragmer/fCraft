﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    public sealed class RandomBrushFactory : IBrushFactory {
        public static readonly RandomBrushFactory Instance = new RandomBrushFactory();

        RandomBrushFactory() { }

        public string Name {
            get { return "Random"; }
        }

        static readonly string[] aliases = new[] { "rand" };
        public string[] Aliases {
            get { return aliases; }
        }

        const string help = "Random brush: Chaotic pattern of two or more random block types. " +
                            "If only one block name is given, leaves every other block untouched.";
        public string Help {
            get { return help; }
        }


        public IBrush MakeBrush( Player player, Command cmd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            while( cmd.HasNext ) {
                int ratio = 1;
                Block block = cmd.NextBlockWithParam( player, ref ratio );
                if( block == Block.Undefined ) return null;
                if( ratio < 0 || ratio > 1000 ) {
                    player.Message( "{0} brush: Invalid block ratio ({1}). Must be between 1 and 1000.",
                                    Name, ratio );
                    return null;
                }
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            if( blocks.Count == 0 ) {
                return new RandomBrush();
            } else if( blocks.Count == 1 ) {
                return new RandomBrush( blocks[0], blockRatios[0] );
            } else {
                return new RandomBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }
    }


    public sealed class RandomBrush : IBrushInstance, IBrush {
        public Block[] Blocks { get; private set; }
        public int[] BlockRatios { get; private set; }
        readonly Block[] actualBlocks;
        readonly Random rand = new Random();

        public RandomBrush() {
            Blocks = new Block[0];
            BlockRatios = new int[0];
        }

        public RandomBrush( Block oneBlock, int ratio ) {
            Blocks = new[] { oneBlock, Block.Undefined };
            BlockRatios = new[] { ratio, 1 };
            actualBlocks = new[] { oneBlock, Block.Undefined };
        }

        public RandomBrush( Block[] blocks, int[] ratios ) {
            Blocks = blocks;
            BlockRatios = ratios;
            actualBlocks = new Block[BlockRatios.Sum()];
            int c = 0;
            for( int i = 0; i < Blocks.Length; i++ ) {
                for( int j = 0; j < BlockRatios[i]; j++ ) {
                    actualBlocks[c] = Blocks[i];
                    c++;
                }
            }
        }


        public RandomBrush( RandomBrush other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            Blocks = other.Blocks;
            BlockRatios = other.BlockRatios;
            actualBlocks = other.actualBlocks;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return RandomBrushFactory.Instance; }
        }


        public string Description {
            get {
                if( Blocks.Length == 0 ) {
                    return Factory.Name;
                } else if( Blocks.Length == 1 || (Blocks.Length == 2 && Blocks[1] == Block.Undefined) ) {
                    return String.Format( "{0}({1})", Factory.Name, Blocks[0] );
                } else {
                    StringBuilder sb = new StringBuilder();
                    sb.Append( Factory.Name );
                    sb.Append( '(' );
                    for( int i = 0; i < Blocks.Length; i++ ) {
                        if( i != 0 ) sb.Append( ',' ).Append( ' ' );
                        sb.Append( Blocks[i] );
                        if( BlockRatios[i] > 1 ) {
                            sb.Append( '/' );
                            sb.Digits( BlockRatios[i] );
                        }
                    }
                    sb.Append( ')' );
                    return sb.ToString();
                }
            }
        }


        public IBrushInstance MakeInstance( Player player, Command cmd, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( state == null ) throw new ArgumentNullException( "state" );

            List<Block> blocks = new List<Block>();
            List<int> blockRatios = new List<int>();
            while( cmd.HasNext ) {
                int ratio = 1;
                Block block = cmd.NextBlockWithParam( player, ref ratio );
                if( ratio < 0 || ratio > 1000 ) {
                    player.Message( "Invalid block ratio ({0}). Must be between 1 and 1000.", ratio );
                    return null;
                }
                if( block == Block.Undefined ) return null;
                blocks.Add( block );
                blockRatios.Add( ratio );
            }

            if( blocks.Count == 0 ) {
                if( Blocks.Length == 0 ) {
                    player.Message( "{0} brush: Please specify at least one block.", Factory.Name );
                    return null;
                } else {
                    return new RandomBrush( this );
                }
            } else if( blocks.Count == 1 ) {
                return new RandomBrush( blocks[0], blockRatios[0] );
            } else {
                return new RandomBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
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


        public bool Begin( Player player, DrawOperation state ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( state == null ) throw new ArgumentNullException( "state" );
            if( Blocks == null || Blocks.Length == 0 ) {
                throw new InvalidOperationException( "No blocks given." );
            }
            return true;
        }


        public Block NextBlock( DrawOperation state ) {
            if( state == null ) throw new ArgumentNullException( "state" );
            return actualBlocks[rand.Next( actualBlocks.Length )];
        }


        public void End() { }

        #endregion
    }
}