﻿using System.Xml.Linq;

namespace fCraft {
    public sealed class EmptyMapGen : MapGenerator {
        public static EmptyMapGen Instance { get; private set; }

        static EmptyMapGen() {
            Instance = new EmptyMapGen {
                Name = "Empty"
            };
        }

        public override MapGeneratorParameters GetDefaultParameters() {
            return new EmptyMapGenParams();
        }

        public override MapGeneratorParameters CreateParameters( XElement serializedParameters ) {
            return GetDefaultParameters();
        }

        public override MapGeneratorParameters CreateParameters( Player player, CommandReader cmd ) {
            if( cmd.HasNext ) {
                player.Message( "Empty map generator does not take any parameters." );
                return null;
            } else {
                return GetDefaultParameters();
            }
        }

        public override MapGeneratorParameters CreateParameters( string presetName ) {
            if( presetName == null ) {
                return GetDefaultParameters();
            } else {
                return null;
            }
        }
    }


    class EmptyMapGenParams : MapGeneratorParameters {
        public EmptyMapGenParams() {
            Generator = EmptyMapGen.Instance;
        }

        public override MapGeneratorState CreateGenerator() {
            return new EmptyMapGenState( this );
        }
    }


    class EmptyMapGenState : MapGeneratorState {
        public EmptyMapGenState( MapGeneratorParameters genParams ) {
            Parameters = genParams;
        }

        public override Map Generate() {
            return new Map( null, Parameters.MapWidth, Parameters.MapLength, Parameters.MapHeight, true );
        }
    }
}