﻿// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.Net;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Protocol encoder for outgoing packets. </summary>
    public sealed class PacketWriter : BinaryWriter {

        /// <summary> Underlying stream used to write packets to. </summary>
        /// <param name="stream"> Base stream to attach to. </param>
        public PacketWriter( Stream stream ) : base( stream ) { }


        #region Direct Writing

        /// <summary> Writes the OpCode as a byte to the current stream. </summary>
        /// <param name="opcode"> OpCode to write to stream. </param>
        public void Write( OpCode opcode ) {
            Write( (byte)opcode );
        }

        /// <summary>  Writes a 16-bit short integer in Big-Endian order. </summary>
        public override void Write( short data ) {
            base.Write( IPAddress.HostToNetworkOrder( data ) );
        }

        /// <summary>  Writes a 32-bit integer in Big-Endian order. </summary>
        public override void Write( int data ) {
            base.Write( IPAddress.HostToNetworkOrder( data ) );
        }

        /// <summary> Writes a string in Minecraft protocol format.
        /// Maximum length: 64 characters. </summary>
        public override void Write( string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length > 64 ) throw new ArgumentException( "String is too long (>64).", "str" );
            Write( Encoding.ASCII.GetBytes( str.PadRight( 64 ) ) );
        }

        #endregion


        #region Direct Writing Whole Packets

        /// <summary> Writes a ping packet to the stream. </summary>
        public void WritePing() {
            Write( OpCode.Ping );
        }

        /// <summary> Writes a MapBegin packet to the stream. </summary>
        public void WriteMapBegin() {
            Write( OpCode.MapBegin );
        }

        /// <summary> Writes a MapChunk to the stream. </summary>
        /// <param name="chunk"> GZipped map chunk. </param>
        /// <param name="chunkSize"> Size of map chunk. </param>
        /// <param name="progress"> Percent completion of download of map. </param>
        public void WriteMapChunk( [NotNull] byte[] chunk, int chunkSize, byte progress ) {
            if( chunk == null ) throw new ArgumentNullException( "chunk" );
            Write( OpCode.MapChunk );
            Write( (short)chunkSize );
            Write( chunk, 0, 1024 );
            Write( progress );
        }

        internal void WriteMapEnd( [NotNull] Map map ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            Write( OpCode.MapEnd );
            Write( (short)map.Width );
            Write( (short)map.Height );
            Write( (short)map.Length );
        }

        /// <summary> Writes an AddEntity packet to the stream. </summary>
        /// <param name="id"> Entity ID. </param>
        /// <param name="player"> Name of player. </param>
        /// <param name="pos"> Position of entity. </param>
        public void WriteAddEntity( byte id, [NotNull] Player player, Position pos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Write( OpCode.AddEntity );
            Write( id );
            Write( player.ListName );
            Write( pos.X );
            Write( pos.Z );
            Write( pos.Y );
            Write( pos.R );
            Write( pos.L );
        }

        /// <summary> Write a Teleport packet to the stream. </summary>
        /// <param name="id"> ID of the entity to teleport. </param>
        /// <param name="pos"> Position to teleport the entity to. </param>
        public void WriteTeleport( byte id, Position pos ) {
            Write( OpCode.Teleport );
            Write( id );
            Write( pos.X );
            Write( pos.Z );
            Write( pos.Y );
            Write( pos.R );
            Write( pos.L );
        }

        #endregion


        #region Packet Making

        internal static Packet MakeHandshake( [NotNull] Player player, [NotNull] string serverName, [NotNull] string motd ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( serverName == null ) throw new ArgumentNullException( "serverName" );
            if( motd == null ) throw new ArgumentNullException( "motd" );

            Packet packet = new Packet( OpCode.Handshake );
            packet.Data[1] = Config.ProtocolVersion;
            Encoding.ASCII.GetBytes( serverName.PadRight( 64 ), 0, 64, packet.Data, 2 );
            Encoding.ASCII.GetBytes( motd.PadRight( 64 ), 0, 64, packet.Data, 66 );
            packet.Data[130] = (byte)(player.Can( Permission.DeleteAdmincrete ) ? 100 : 0);
            return packet;
        }


        internal static Packet MakeMessage( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );

            Packet packet = new Packet( OpCode.Message );
            packet.Data[1] = 0; // unused
            Encoding.ASCII.GetBytes( message.PadRight( 64 ), 0, 64, packet.Data, 2 );
            return packet;
        }


        internal static Packet MakeAddEntity( int id, [NotNull] string name, Position pos ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            Packet packet = new Packet( OpCode.AddEntity );
            packet.Data[1] = (byte)id;
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Data, 2 );
            ToNetOrder( pos.X, packet.Data, 66 );
            ToNetOrder( pos.Z, packet.Data, 68 );
            ToNetOrder( pos.Y, packet.Data, 70 );
            packet.Data[72] = pos.R;
            packet.Data[73] = pos.L;
            return packet;
        }


        internal static Packet MakeDisconnect( [NotNull] string reason ) {
            if( reason == null ) throw new ArgumentNullException( "reason" );

            Packet packet = new Packet( OpCode.Kick );
            Encoding.ASCII.GetBytes( reason.PadRight( 64 ), 0, 64, packet.Data, 1 );
            return packet;
        }


        internal static Packet MakeRemoveEntity( int id ) {
            Packet packet = new Packet( OpCode.RemoveEntity );
            packet.Data[1] = (byte)id;
            return packet;
        }


        internal static Packet MakeTeleport( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Teleport );
            packet.Data[1] = (byte)id;
            ToNetOrder( pos.X, packet.Data, 2 );
            ToNetOrder( pos.Z, packet.Data, 4 );
            ToNetOrder( pos.Y, packet.Data, 6 );
            packet.Data[8] = pos.R;
            packet.Data[9] = pos.L;
            return packet;
        }


        internal static Packet MakeSelfTeleport( Position pos ) {
            return MakeTeleport( 255, pos.GetFixed() );
        }


        internal static Packet MakeMoveRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.MoveRotate );
            packet.Data[1] = (byte)id;
            packet.Data[2] = (byte)(pos.X & 0xFF);
            packet.Data[3] = (byte)(pos.Z & 0xFF);
            packet.Data[4] = (byte)(pos.Y & 0xFF);
            packet.Data[5] = pos.R;
            packet.Data[6] = pos.L;
            return packet;
        }


        internal static Packet MakeMove( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Move );
            packet.Data[1] = (byte)id;
            packet.Data[2] = (byte)pos.X;
            packet.Data[3] = (byte)pos.Z;
            packet.Data[4] = (byte)pos.Y;
            return packet;
        }


        internal static Packet MakeRotate( int id, Position pos ) {
            Packet packet = new Packet( OpCode.Rotate );
            packet.Data[1] = (byte)id;
            packet.Data[2] = pos.R;
            packet.Data[3] = pos.L;
            return packet;
        }


        internal static Packet MakeSetBlock( int x, int y, int z, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( x, packet.Data, 1 );
            ToNetOrder( z, packet.Data, 3 );
            ToNetOrder( y, packet.Data, 5 );
            packet.Data[7] = (byte)type;
            return packet;
        }


        internal static Packet MakeSetBlock( Vector3I coords, Block type ) {
            Packet packet = new Packet( OpCode.SetBlockServer );
            ToNetOrder( coords.X, packet.Data, 1 );
            ToNetOrder( coords.Z, packet.Data, 3 );
            ToNetOrder( coords.Y, packet.Data, 5 );
            packet.Data[7] = (byte)type;
            return packet;
        }


        internal static Packet MakeSetPermission( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            Packet packet = new Packet( OpCode.SetPermission );
            packet.Data[1] = (byte)(player.Can( Permission.DeleteAdmincrete ) ? 100 : 0);
            return packet;
        }

        #endregion


        internal static void ToNetOrder( int number, byte[] arr, int offset ) {
            arr[offset] = (byte)((number & 0xff00) >> 8);
            arr[offset + 1] = (byte)(number & 0x00ff);
        }
    }
}