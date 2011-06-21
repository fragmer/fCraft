﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.IO;

namespace fCraft.MapConversion {
    class MapXMap : IMapConverter {
        public const int FormatID = 88776580;                     // 88 77 45 80 - XMAP in ascii
        public const int FormatRevision = 20110319;               // This is based on the date the revision was finalized


        /// <summary> Returns name(s) of the server(s) that uses this format. </summary>
        public string ServerName { get { return "(Universal)"; } }


        /// <summary> Returns the format type (file-based or directory-based). </summary>
        public MapFormatType FormatType { get { return MapFormatType.SingleFile; } }


        /// <summary> Returns the format name. </summary>
        public MapFormat Format { get { return MapFormat.XMap; } }


        /// <summary> Returns true if the filename (or directory name) matches this format's expectations. </summary>
        public bool ClaimsName( string fileName ) {
            return fileName.EndsWith( ".xmap", StringComparison.OrdinalIgnoreCase );
        }


        /// <summary> Allows validating the map format while using minimal resources. </summary>
        /// <returns> Returns true if specified file/directory is valid for this format. </returns>
        public bool Claims( string path ) {
            using( FileStream fs = File.OpenRead( path ) ) {
                BinaryReader reader = new BinaryReader( fs );
                return (reader.ReadInt32() == FormatID) && (reader.ReadInt32() == FormatRevision);
            }
        }


        /// <summary> Attempts to load map dimensions from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        public Map LoadHeader( string path ) {
            using( FileStream fs = File.OpenRead( path ) ) {
                return LoadInternal( fs, false );
            }
        }


        /// <summary> Fully loads map from specified location. </summary>
        /// <returns> Map object on success, or null on failure. </returns>
        public Map Load( string path ) {
            using( FileStream fs = File.OpenRead( path ) ) {
                return LoadInternal( fs, true );
            }
        }


        /// <summary> Saves given map at the given location. </summary>
        /// <returns> true if saving succeeded. </returns>
        public bool Save( Map mapToSave, string path ) {
            throw new NotImplementedException();
        }


        static Map LoadInternal( Stream stream, bool readLayers ) {
            BinaryReader bs = new BinaryReader( stream );

            // headers
            if( bs.ReadInt32() != FormatID ) {
                throw new MapFormatException( "Invalid XMap format ID." );
            }

            if( bs.ReadInt32() != FormatRevision ) {
                throw new MapFormatException( "Invalid XMap format revision." );
            }

            // map dimensions
            int widthX = bs.ReadInt32();
            int height = bs.ReadInt32();
            int widthY = bs.ReadInt32();

            Map map = new Map( null, widthX, widthY, height, false );

            // spawn
            map.Spawn = new Position {
                X = (short)bs.ReadInt32(),
                H = (short)bs.ReadInt32(),
                Y = (short)bs.ReadInt32(),
                R = bs.ReadByte(),
                L = bs.ReadByte()
            };

            // creation/modification dates
            map.DateCreated = bs.ReadInt64().ToDateTime();
            map.DateModified = bs.ReadInt64().ToDateTime();


            int metaGroupCount = bs.ReadInt32();
            if( metaGroupCount < 0 ) throw new MapFormatException( "Negative meta group count." );

            int layerCount = bs.ReadInt32();
            if( layerCount < 0 ) throw new MapFormatException( "Negative layer count." );

            // metadata
            for( int i = 0; i < metaGroupCount; i++ ) {
                string groupName = ReadString( bs );
                int keyCount = bs.ReadInt32();
                if( keyCount < 0 ) throw new MapFormatException( "Negative meta group size." );
                for( int k = 0; k < keyCount; k++ ) {
                    string keyName = ReadString( bs );
                    string value = ReadString( bs );

                    // check for duplicate keys
                    string oldValue;
                    if( map.Metadata.TryGetValue( groupName, keyName, out oldValue ) ) {
                        Logger.Log( "MapXMap: Duplicate metadata entry \"{0}.{1}\". " +
                                    "Old value: \"{2}\", new value \"{3}\"", LogType.Warning,
                                    groupName, keyName, oldValue, value );
                    }

                    // parse or store metadata
                    switch( groupName ) {
                        case "fCraft.Zones":
                            try {
                                map.AddZone( new Zone( value, null ) );
                            } catch( Exception ex ) {
                                Logger.Log( "MapXMap: Error importing zone definition: {0}", LogType.Error,
                                            ex );
                            }
                            break;

                        default:
                            map.Metadata[groupName, keyName] = value;
                            break;
                    }
                }
            }

            // layers
            if( readLayers ) {
                for( int l = 0; l < layerCount; l++ ) {
                    string layerName = ReadString( bs );
                    int layerSize = bs.ReadInt32();



                    // TODO: map.SetLayer( layerType, layerSize, layerFlags, stream )
                }
            }

            return map;
        }


        internal static string ReadString( BinaryReader reader ) {
            int stringLength = reader.ReadInt32();
            if( stringLength < 0 ) throw new MapFormatException( "Negative string length." );
            return Encoding.ASCII.GetString( reader.ReadBytes( stringLength ) );
        }


        internal static void WriteString( BinaryWriter writer, string str ) {
            byte[] stringData = Encoding.ASCII.GetBytes( str );
            writer.Write( stringData.Length );
            writer.Write( stringData, 0, stringData.Length );
        }
    }


    public enum XMapLayerType {
        Unknown,

        /// <summary> Array of blocks that make up the world (1 byte per block). </summary>
        BlockArray,

        PlayerTable,        // Table of players that have been on the map
        BlockPhysicsCode,   // Definition of all the physics. Blocks should reference these
        BlockUndo,          // Last change (per-block) ***Not Used by MCSharp***
        BlockProperties,    // Parallel array to block array, defining what physics code to run on specific blocks
        BlockAccessLevel,   // Parallel array of block access levels
        BlockOwner          // Parallel array of PlayerIDs
    }


    public class XDataLayer {
        public string Name { get; set; }
        public XMapLayerType LayerType { get; set; }

        bool writeRaw = true;
        byte[] RawData;


        public XDataLayer( string name, int length, Stream stream ) {
            Name = name;

            try {
                LayerType = (XMapLayerType)Enum.Parse( typeof( XMapLayerType ), name, true );
            } catch( ArgumentException ) {
                LayerType = XMapLayerType.Unknown;
            }

            if( LayerType == XMapLayerType.Unknown || !Enum.IsDefined( typeof( XMapLayerType ), LayerType ) ) {
                LayerType = XMapLayerType.Unknown;
                stream.Read( RawData, 0, length );
            }

            // check if layer type is known, and set writeRaw
        }


        public static XDataLayer LoadFromStream( Stream stream ) {
            BinaryReader br = new BinaryReader( stream );
            string layerName = MapXMap.ReadString( br );
            int layerSize = br.ReadInt32();
            return new XDataLayer( layerName, layerSize, stream );
        }


        public void SaveToStream( Stream stream ) {
            BinaryWriter writer = new BinaryWriter( stream );
            MapXMap.WriteString( writer, Name );
            if( writeRaw ) {
            } else {
            }
        }
    }
}