﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;

namespace fCraft.Drawing {
    public sealed class MarbledBrushFactory : IBrushFactory {
        public static readonly MarbledBrushFactory Instance = new MarbledBrushFactory();

        MarbledBrushFactory() { }

        public string Name {
            get { return "Marbled"; }
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
                return new MarbledBrush();
            } else if( blocks.Count == 1 ) {
                return new MarbledBrush( blocks[0], blockRatios[0] );
            } else {
                return new MarbledBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }
    }


    public sealed class MarbledBrush : AbstractPerlinNoiseBrush, IBrush {

        public MarbledBrush() {
            Frequency = 0.1f;
        }

        public MarbledBrush( Block oneBlock, int ratio )
            : base( oneBlock, ratio ) {
            Frequency = 0.1f;
        }

        public MarbledBrush( Block[] blocks, int[] ratios )
            : base( blocks, ratios ) {
            Frequency = 0.1f;
        }

        public MarbledBrush( AbstractPerlinNoiseBrush other )
            : base( other ) {
            Frequency = 0.1f;
        }


        #region IBrush members

        public IBrushFactory Factory {
            get { return MarbledBrushFactory.Instance; }
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
                    return new MarbledBrush( this );
                }
            } else if( blocks.Count == 1 ) {
                return new MarbledBrush( blocks[0], blockRatios[0] );
            } else {
                return new MarbledBrush( blocks.ToArray(), blockRatios.ToArray() );
            }
        }

        #endregion


        #region AbstractPerlinNoiseBrush members

        public override IBrush Brush {
            get { return this; }
        }


        public override string InstanceDescription {
            get {
                return Description;
            }
        }


        protected override float MapValue( float rawValue ) {
            return Math.Abs( rawValue * 2 - 1 );
        }

        protected unsafe override bool MapAllValues( float[, ,] rawValues ) {
            fixed( float* ptr = rawValues ) {
                for( int i = 0; i < rawValues.Length; i++ ) {
                    ptr[i] = Math.Abs( ptr[i] * 2 - 1 );
                }
            }
            return false;
        }

        #endregion
    }
}