﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    public sealed class CutDrawOperation : DrawOperation {
        public override string Name {
            get { return "Cut"; }
        }

        public override string DescriptionWithBrush {
            get {
                var normalBrush = Brush as NormalBrush;
                if( normalBrush != null && normalBrush.Block != Block.Air ) {
                    return String.Format( "{0}/{1}", Name, normalBrush.Block );
                } else {
                    return Name;
                }
            }
        }

        public CutDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( Player.World == null ) PlayerOpException.ThrowNoWorld( Player );
            if( !base.Prepare( marks ) ) return false;

            BlocksTotalEstimate = Bounds.Volume;

            Coords.X = Bounds.XMin;
            Coords.Y = Bounds.YMin;
            Coords.Z = Bounds.ZMin;

            // remember dimensions and orientation
            CopyState copyInfo = new CopyState( marks[0], marks[1] );

            for( int x = Bounds.XMin; x <= Bounds.XMax; x++ ) {
                for( int y = Bounds.YMin; y <= Bounds.YMax; y++ ) {
                    for( int z = Bounds.ZMin; z <= Bounds.ZMax; z++ ) {
                        copyInfo.Buffer[x - Bounds.XMin, y - Bounds.YMin, z - Bounds.ZMin] = Map.GetBlockByte( x, y, z );
                    }
                }
            }
            copyInfo.OriginWorld = Player.World.Name;
            copyInfo.CopyTime = DateTime.UtcNow;
            Player.SetCopyInformation( copyInfo );

            Player.Message( "{0} blocks cut into slot #{1}. You can now &H/paste",
                            Bounds.Volume, Player.CopySlot + 1 );
            Player.Message( "Origin at {0} {1}{2} corner.",
                            (copyInfo.Orientation.X == 1 ? "bottom" : "top"),
                            (copyInfo.Orientation.Y == 1 ? "south" : "north"),
                            (copyInfo.Orientation.Z == 1 ? "east" : "west") );

            Context |= BlockChangeContext.Cut;
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if( DrawOneBlock() ) {
                            blocksDone++;
                            if( blocksDone >= maxBlocksToDraw ) {
                                Coords.Z++;
                                return blocksDone;
                            }
                        }
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if( TimeToEndBatch ) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }
    }
}