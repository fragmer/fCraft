﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
#if DEBUG
using System.Collections.Generic;
#endif
using JetBrains.Annotations;

// ReSharper disable VirtualMemberNeverOverriden.Global
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable MemberCanBeProtected.Global
namespace fCraft.Drawing {

    public abstract class DrawOperation {
        public readonly Player Player;
        public readonly Map Map;
        public Position[] Marks;
        public DateTime StartTime;

        public BoundingBox Bounds;

        public bool IsDone;
        public bool IsCancelled;

        public IBrushInstance Brush;

        public int BlocksProcessed,
                   BlocksUpdated,
                   BlocksDenied,
                   BlocksSkipped,
                   BlocksTotalEstimate;

        public int PercentDone {
            get {
                return (BlocksProcessed * 100) / BlocksTotalEstimate;
            }
        }

        public bool CannotUndo;

        public Vector3I Coords;

        public bool UseAlternateBlock;

        public abstract string Name { get; }

        public abstract string Description { get; }


        protected DrawOperation( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( player.World == null || player.World.Map == null ) {
                throw new ArgumentException( "Player must have a world.", "player" );
            }

            Player = player;
            Map = player.World.Map;
        }


        public virtual bool Begin( [NotNull] Position[] marks ) {
            if( marks == null ) throw new ArgumentNullException( "marks" );
            Marks = marks;
            if( Player == null ) throw new InvalidOperationException( "Player not set" );
            if( Map == null ) throw new InvalidOperationException( "Map not set" );
            Bounds = new BoundingBox( Marks[0], Marks[1] );
            if( Bounds == null ) throw new InvalidOperationException( "Bounds not set" );
            if( !Brush.Begin( Player, this ) ) return false;
            Player.LastDrawOp = this;
            Player.UndoBuffer.Clear();
            StartTime = DateTime.UtcNow;
            return true;
        }


        public abstract int DrawBatch( int maxBlocksToDraw );


        public virtual void Cancel() {
            IsCancelled = true;
        }

        public virtual void End() {
            Player.Info.ProcessDrawCommand( BlocksUpdated );
            Brush.End();
        }


        protected bool DrawOneBlock() {
            BlocksProcessed++;

            if( !Map.InBounds( Coords.X, Coords.Y, Coords.Z ) ) {
                BlocksSkipped++;
                return false;
            }

#if DEBUG
            TestForDuplicateModification();
#endif

            Block newBlock = Brush.NextBlock( this );
            if( newBlock == Block.Undefined ) return false;

            int blockIndex = Map.Index( Coords.X, Coords.Y, Coords.Z );

            Block oldBlock = (Block)Map.Blocks[blockIndex];
            if( oldBlock == newBlock ) {
                BlocksSkipped++;
                return false;
            }

            if( Player.CanPlace( Coords.X, Coords.Y, Coords.Z, newBlock, false ) != CanPlaceResult.Allowed ) {
                BlocksDenied++;
                return false;
            }

            Map.Blocks[blockIndex] = (byte)newBlock;

            World world = Map.World;
            if( world != null && !world.IsFlushing ) {
                world.Players.SendLowPriority( PacketWriter.MakeSetBlock( Coords.X, Coords.Y, Coords.Z, newBlock ) );
            }

            Player.RaisePlayerPlacedBlockEvent( Player, Map, (short)Coords.X, (short)Coords.Y, (short)Coords.Z,
                                                oldBlock, newBlock, false );

            if( BuildingCommands.MaxUndoCount < 1 || BlocksUpdated < BuildingCommands.MaxUndoCount ) {
                Player.UndoBuffer.Enqueue( new BlockUpdate( null, Coords.X, Coords.Y, Coords.Z, oldBlock ) );
            } else if( !CannotUndo ) {
                Player.LastDrawOp = null;
                Player.UndoBuffer.Clear();
                Player.UndoBuffer.TrimExcess();
                Player.Message( "{0}: Too many blocks to undo.", Description );
                CannotUndo = true;
            }
            BlocksUpdated++;
            return true;
        }

#if DEBUG

        // Single modification per block policy enforcement
        readonly HashSet<int> modifiedBlockIndices = new HashSet<int>();
        void TestForDuplicateModification() {
            int index = Map.Index( Coords );
            if( modifiedBlockIndices.Contains( index ) ) {
                throw new InvalidOperationException( "Duplicate block modification at " + Coords );
            }
            modifiedBlockIndices.Add( index );
        }


#endif
    }
}