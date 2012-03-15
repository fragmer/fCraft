﻿// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {

    /*
     * Config format-version changelog:
     * 100 - r1-r133
     *
     * 101 - r134 - Per-class draw limits and antigrief detection
     *              Removed AntigriefBlockCount and AntigriefBlockInterval keys
     *
     * 102 - r171 - Added RequireBanReason, RequireClassChangeReason, AnnounceKickAndBanReasons, AnnounceClassChanges keys
     *              Removed AnnounceUnverifiedNames key
     *
     * 103 - r190 - Added UseSpeedHack permission
     *              Added PrivateMessageColor and IRCColor keys
     *
     * 104 - r198 - Added IRCBotAnnounceServerJoins and IRCBotAnnounceIRCJoins keys
     *              Removed IRCBotMsg key
     *
     * 105 - r205 - Added SubmitCrashReports key
     *              Removed PolicyColorCodesInChat, PolicyIllegalCharacters, and RunOnStartup
     *
     * 106 - r212 - Added IRCDelay key
     * 
     * 107 - r214 - Added ShowJoinedWorldMessages and ClassColorsInWorldNames keys
     *              Removed ChangeName permission
     *
     * 108 - r224 - Added IP config key
     *              Capped MaxPlayers at 128
     *
     * 109 - r226 - Added PatrolledClass config key
     *              Added Patrol permission
     *
     * 110 - r227 - Added ShutdownServer and Mute permissions.
     *              NOTE: This build does not support loading config.xml of this or earlier versions.
     * 
     * 111 - r231 - Renamed config keys:
     *                  DefaultClass             -> DefaultRank
     *                  ClassColorsInChat        -> RankColorsInChat
     *                  ClassColorsInWorldNames  -> RankColorsInWorldNames
     *                  ClassPrefixesInChat      -> RankPrefixesInChat
     *                  ClassPrefixesInList      -> RankPrefixesInList
     *                  PatrolledClass           -> PatrolledRank
     *                  RequireClassChangeReason -> RequireRankChangeReason
     *                  AnnounceClassChanges     -> AnnounceRankChanges
     *              Renamed XML elements:
     *                  Classes     -> Ranks
     *                  PlayerClass -> Rank
     *              Removed "rank" from PlayerClass/Rank object
     *              Made the order of Rank elements determine the relative index
     *              Config.xml files of earlier versions than 111 can NOT be loaded by this copy of fCraft.
     *
     * 112 - r235 - Removed PingInterval key
     *              Removed inactive ControlPhysics and AddLandmarks permissions
     *
     * 113 - r243 - Removed IRCBotQuitMsg key
     * 
     * 114 - r244 - Added IRCRegisteredNick, IRCNickServ, and IRCNickServMessage keys
     * 
     * 115 - r265 - Added IRCThreads key
     * 
     * 116 - r272 - Added AutoRankEnabled key
     * 
     * 117 - r280 - Added MaxUndo key
     * 
     * 118 - r318 - Added MeColor and WarningColor keys
     * 
     * 119 - r331 - Added LogPath and MapPath keys
     * 
     * 120 - r332 - Added DataPath key
     * 
     * 121 - r335 - Renamed SendRedundantBlockUpdates key to RelayAllBlockUpdates
     * 
     * 122 - r341 - Added IRCUseColor key
     * 
     * 123 - r346 - Added IRCBotAnnounceServerEvents
     * 
     * 124 - r354 - Added HeartbeatEnabled
     * 
     * 125 - r356 - Removed LogPath and DataPath keys
     * 
     * 126 - r366 - Added PreventSecurityCircumvention key
     * 
     * 127 - r368 - Removed PreventSecurityCircumvention key
     *              Added per-rank AllowSecurityCircumvention setting instead
     *              
     * 128 - r379 - Added ConsoleName
     * 
     * 129 - r405 - Added ShowConnectedMessages
     * 
     * 130 - r413 - Added ShowBannedConnectionMessages
     * 
     * 131 - r460 - Changed default for IRCNick from "fBot" to "MinecraftBot"
     *              Relaxed range limits on many integer keys.
     *              Renamed ProcessPriority value "Low" to "Idle", to match WinAPI 
     *              
     * 132 - r477 - Added BackupBeforeUpdate, RunBeforeUpdate, and RunAfterUpdate config keys
     *              Renamed UpdateMode to UpdaterMode
     *              
     * 133 - r517 - Added UseColorCodes permission
     * 
     * 134 - r520 - Removed LimitOneConnectionPerIP key
     *              Added MaxConnectionsPerIP key
     *              
     * 135 - r526 - Added RequireKickReason and AnnounceRankChangeReasons keys
     *              Added ViewPlayerIPs permission
     *              
     * 136 - r528 - Added BringAll permission.
     * 
     * 137 - r556 - Added BandwidthUseMode key.
     * 
     * 138 - r578 - Removed SaveOnShutdown key.
     *              Tweaked range checks on some keys.
     *              Grouped key tags into section tags.
     *              When saving, keys with default values are now commented out.
     *              CONFIGS SAVED WITH THIS VERSION ARE NOT LOADABLE. It is obsolete.
     *              
     * 139 - r579 - Fixed XML structure messed up by 138. Sections are now saved into <Section> elements.
     * 
     * 140 - r616 - Added Spectate permission.
     * 
     * 141 - r622 - Added RestartInterval key.
     * 
     * 142 - r638 - Added BackupDataOnStartup key.
     * 
     * 143 - r676 - Added LoadPlugins key (currently unused).
     * 
     * 144 - r787 - Added DrawAdvanced permission.
     * 
     * 145 - r794 - Added UndoOthersActions permission.
     * 
     * 146 - r910 - Renamed BackupInterval to DefaultBackupInterval
     * 
     * 147 - r926 - Renamed EnableBlockDB to BlockDBEnabled
     * 
     * 148 - r1014 - Added BlockDBAutoEnable and BlockDBAutoEnableRank keys
     *               Moved BlockDBEnabled to Security ConfigSection
     *               
     * 149 - r1061 - Added HeartbeatToWoMDirect, WoMDirectDescription, and WoMEnableEnvExtensions keys
     * 
     * 150 - r1066 - Removed WoMDirectDescription key
     * 
     * 151 - r1169 - Added MaxUndoStates key
     *               Added fillLimit rank attribute.
     *               Changed defaults for some keys:
     *                  BlockDBEnabled to "true"
     *                  WomEnableEnvExtensions to "false"
     *                  IRCBotAnnounceServerEvents to "true"
     *                  
     * 152 - r1243 - Changed the way fCraft stores config keys.
     *               Before: <fCraftConfig><Section name="blah"><KeyName>Value</KeyName></Section></fCraftConfig>
     *               After: <fCraftConfig><Settings><ConfigKey key="KeyName" value="Value" default="DefaultValue" /></Settings></fCraftConfig>
     *               
     * 153 - r1246 - Added PlayerDBProvider data
     *               Removed BackupDataOnStartup key.
     *               
     * 154 - r1312 - Added SeparateWorldAndGlobalChat key (default: false)
     * 
     * 155 - r1464 - Added WoMDirectDescription and WoMDirectFlags keys
     * 
     * 156 - r1473 - Removed AutoRankEnabled key
     * 
     */

    /// <summary> Static class that handles loading/saving configuration, contains config defaults,
    /// and various configuration-related utilities. </summary>
    public static class Config {
        /// <summary>  Supported version of the Minecraft classic protocol. </summary>
        public const int ProtocolVersion = 7;

        /// <summary> Latest version of config.xml available at the time of building this copy of fCraft.
        /// Config.xml files saved with this build will have this version number embedded. </summary>
        public const int CurrentVersion = 156;

        const int LowestSupportedVersion = 111,
                  FirstVersionWithMaxPlayersKey = 134, // LEGACY
                  FirstVersionWithSectionTags = 139, // LEGACY
                  FirstVersionWithSettingsTag = 152; // LEGACY

        const string XmlRootName = "fCraftConfig";

        // Mapping of keys to their values
        static readonly string[] Settings;
        static readonly bool[] SettingsEnabledCache; // cached .Enabled() calls
        static readonly bool[] SettingsUseEnabledCache; // cached .Enabled() calls

        // Mapping of keys to their metadata containers.
        static readonly ConfigKeyAttribute[] KeyMetadata;

        // Keys organized by sections
        static readonly Dictionary<ConfigSection, ConfigKey[]> KeySections = new Dictionary<ConfigSection, ConfigKey[]>();

        // List of renamed/remapped keys.
        static readonly Dictionary<string, ConfigKey> LegacyConfigKeys = new Dictionary<string, ConfigKey>(); // LEGACY

        // List of renamed/remapped key values.
        static readonly Dictionary<ConfigKey, KeyValuePair<string, string>> LegacyConfigValues =
                    new Dictionary<ConfigKey, KeyValuePair<string, string>>(); // LEGACY


        static Config() {
            int keyCount = Enum.GetValues( typeof( ConfigKey ) ).Length;
            Settings = new string[keyCount];
            SettingsEnabledCache = new bool[keyCount];
            SettingsUseEnabledCache = new bool[keyCount];
            KeyMetadata = new ConfigKeyAttribute[keyCount];

            // gather metadata for ConfigKeys
            foreach( var keyField in typeof( ConfigKey ).GetFields() ) {
                foreach( var attribute in (ConfigKeyAttribute[])keyField.GetCustomAttributes( typeof( ConfigKeyAttribute ), false ) ) {
                    ConfigKey key = (ConfigKey)keyField.GetValue( null );
                    attribute.Key = key;
                    KeyMetadata[(int)key] = attribute;
                }
            }

            // organize ConfigKeys into categories, based on metadata
            foreach( ConfigSection section in Enum.GetValues( typeof( ConfigSection ) ) ) {
                ConfigSection sec = section;
                KeySections.Add( section, KeyMetadata.Where( meta => (meta.Section == sec) )
                                                     .Select( meta => meta.Key )
                                                     .ToArray() );
            }

            LoadDefaults();

            // These keys were renamed at some point. LEGACY
            LegacyConfigKeys.Add( "SendRedundantBlockUpdates".ToLower(), ConfigKey.RelayAllBlockUpdates );
            LegacyConfigKeys.Add( "AutomaticUpdates".ToLower(), ConfigKey.UpdaterMode );
            LegacyConfigKeys.Add( "IRCBot".ToLower(), ConfigKey.IRCBotEnabled );
            LegacyConfigKeys.Add( "UpdateMode".ToLower(), ConfigKey.UpdaterMode );
            LegacyConfigKeys.Add( "BackupInterval".ToLower(), ConfigKey.DefaultBackupInterval );
            LegacyConfigKeys.Add( "EnableBlockDB".ToLower(), ConfigKey.BlockDBEnabled );

            // These values have been renamed at some point. LEGACY
            LegacyConfigValues.Add( ConfigKey.ProcessPriority,
                                    new KeyValuePair<string, string>( "Low", ProcessPriorityClass.Idle.ToString() ) );
        }


#if DEBUG
        // Makes sure that defaults and metadata containers are set.
        // This is invoked by Server.InitServer() if built with DEBUG flag.
        internal static void RunSelfTest() {
            foreach( ConfigKey key in Enum.GetValues( typeof( ConfigKey ) ) ) {
                if( Settings[(int)key] == null ) {
                    throw new Exception( "One of the ConfigKey keys is null: " + key );
                }

                if( KeyMetadata[(int)key] == null ) {
                    throw new Exception( "One of the ConfigKey keys does not have metadata set: " + key );
                }
            }
        }
#endif


        #region Defaults

        /// <summary> Overwrites current settings with defaults. </summary>
        public static void LoadDefaults() {
            for( int i = 0; i < KeyMetadata.Length; i++ ) {
                SetValue( (ConfigKey)i, KeyMetadata[i].DefaultValue );
            }
        }


        /// <summary> Loads defaults for keys in a given ConfigSection. </summary>
        public static void LoadDefaults( ConfigSection section ) {
            foreach( var key in KeySections[section] ) {
                SetValue( key, KeyMetadata[(int)key].DefaultValue );
            }
        }


        /// <summary> Checks whether given ConfigKey still has its default value. </summary>
        public static bool IsDefault( this ConfigKey key ) {
            return KeyMetadata[(int)key].IsDefault( Settings[(int)key] );
        }

        /// <summary> Checks whether given ConfigKey still has its default value. </summary>
        public static bool IsDefault( this ConfigKey key, string value ) {
            return KeyMetadata[(int)key].IsDefault( value );
        }


        /// <summary> Provides the default value for a given ConfigKey. </summary>
        public static string GetDefault( this ConfigKey key ) {
            return KeyMetadata[(int)key].DefaultValue;
        }


        public static void ResetLogOptions() {
            for( int i = 0; i < Logger.ConsoleOptions.Length; i++ ) {
                Logger.ConsoleOptions[i] = true;
                Logger.LogFileOptions[i] = true;
            }
            Logger.ConsoleOptions[(int)LogType.ConsoleInput] = false;
            Logger.ConsoleOptions[(int)LogType.Debug] = false;
        }

        #endregion


        #region Loading

        /// <summary> Whether Config has been loaded. If true, calling Config.Load() again will fail. </summary>
        public static bool IsLoaded { get; private set; }


        /// <summary> Loads configuration from file. </summary>
        public static void Load() {
            if( IsLoaded ) {
                throw new InvalidOperationException( "Config is already loaded. Use Config.Reload instead." );
            }
            Load( false, true );
            IsLoaded = true;
        }


        /// <summary> Reloads configuration from file. Raises ConfigReloaded event. </summary>
        public static void Reload( bool loadRankList ) {
            Load( true, loadRankList );
        }


        static void Load( bool reloading, bool loadRankList ) {
            bool fromFile = false;

            // try to load config file (XML)
            XElement config;
            if( File.Exists( Paths.ConfigFileName ) ) {
                try {
                    XDocument file = XDocument.Load( Paths.ConfigFileName );
                    config = file.Root;
                    if( config == null || config.Name != XmlRootName ) {
                        Logger.Log( LogType.Warning,
                                    "Config.Load: Malformed or incompatible config file {0}. Loading defaults.",
                                    Paths.ConfigFileName );
                        config = new XElement( XmlRootName );
                    } else {
                        fromFile = true;
                    }
                } catch( XmlException ex ) {
                    string errorMsg = "Config.Load: config.xml is not properly formatted: " + ex.Message;
                    throw new MisconfigurationException( errorMsg, ex );
                }
            } else {
                // create a new one (with defaults) if no file exists
                config = new XElement( XmlRootName );
            }

            int version = 0;
            if( fromFile ) {
                XAttribute attr = config.Attribute( "version" );
                if( attr != null && Int32.TryParse( attr.Value, out version ) ) {
                    if( version < LowestSupportedVersion ) {
                        Logger.Log( LogType.Warning,
                                    "Config.Load: Your copy of config.xml is too old to be loaded properly. " +
                                    "Some settings will be lost or replaced with defaults. " +
                                    "Please run ConfigGUI to make sure that everything is in order." );
                    } else if( version != CurrentVersion ) {
                        Logger.Log( LogType.Warning,
                                    "Config.Load: Your config.xml was made for a different version of fCraft. " +
                                    "Some obsolete settings might be ignored, and some recently-added settings will be set to defaults. " +
                                    "It is recommended that you run ConfigGUI to make sure that everything is in order." );
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Config.Load: Unknown version of config.xml found. It might be corrupted. " +
                                "Please run ConfigGUI to make sure that everything is in order." );
                }
            }

            // read rank definitions
            if( loadRankList ) {
                RankManager.Reset();
                LoadRankList( config, fromFile );
            }
            
            ResetLogOptions();

            // read log options for console
            XElement consoleOptions = config.Element( "ConsoleOptions" );
            if( consoleOptions != null ){
                LoadLogOptions( consoleOptions, Logger.ConsoleOptions );
            }else if(fromFile){
                Logger.Log( LogType.Warning, "Config.Load: using default console options." );
            }

            // read log options for logfiles
            XElement logFileOptions = config.Element( "LogFileOptions" );
            if( logFileOptions != null ){
                LoadLogOptions( logFileOptions, Logger.LogFileOptions );
            }else if(fromFile){
                Logger.Log( LogType.Warning, "Config.Load: using default log file options." );
            }


            // read the rest of the keys
            if( version < FirstVersionWithSectionTags ) {
                foreach( XElement element in config.Elements() ) {
                    ParseKeyElementLegacy( element );
                }
            } else if( version < FirstVersionWithSettingsTag ) {
                foreach( XElement section in config.Elements( "Section" ) ) {
                    foreach( XElement keyElement in section.Elements() ) {
                        ParseKeyElementLegacy( keyElement );
                    }
                }
            } else {
                XElement settings = config.Element( "Settings" );
                if( settings != null ) {
                    foreach( XElement pair in settings.Elements( "ConfigKey" ) ) {
                        ParseKeyElement( pair );
                    }
                } else {
                    Logger.Log( LogType.Warning,
                                "Config.Load: No <Settings> tag present. Using default for everything." );
                }
            }

            if( !reloading ) {
                RankManager.DefaultRank = Rank.Parse( ConfigKey.DefaultRank.GetString() );
                RankManager.DefaultBuildRank = Rank.Parse( ConfigKey.DefaultBuildRank.GetString() );
                RankManager.PatrolledRank = Rank.Parse( ConfigKey.PatrolledRank.GetString() );
                RankManager.BlockDBAutoEnableRank = Rank.Parse( ConfigKey.BlockDBAutoEnableRank.GetString() );
            }

            // key relation validation
            if( version < FirstVersionWithMaxPlayersKey ) {
                ConfigKey.MaxPlayersPerWorld.TrySetValue( ConfigKey.MaxPlayers.GetInt() );
            }
            if( ConfigKey.MaxPlayersPerWorld.GetInt() > ConfigKey.MaxPlayers.GetInt() ) {
                Logger.Log( LogType.Warning,
                            "Value of MaxPlayersPerWorld ({0}) was lowered to match MaxPlayers ({1}).",
                            ConfigKey.MaxPlayersPerWorld.GetInt(),
                            ConfigKey.MaxPlayers.GetInt() );
                ConfigKey.MaxPlayersPerWorld.TrySetValue( ConfigKey.MaxPlayers.GetInt() );
            }

            // parse PlayerDBProvider
            XElement playerDBProviderEl = config.Element( "PlayerDBProvider" );
            PlayerDB.ProviderType = PlayerDBProviderType.Flatfile;
            if( playerDBProviderEl == null ) {
                Logger.Log( LogType.Warning,
                            "Config.Load: No PlayerDBProvider information specified in config. Assuming default (flatfile)." );
                PlayerDBProviderConfig = null;
            } else {
                PlayerDBProviderType providerType;
                XAttribute typeAttr = playerDBProviderEl.Attribute( "type" );
                if( typeAttr == null ) {
                    Logger.Log( LogType.Warning,
                                "Config.Load: No PlayerDBProvider specified in config. Assuming default (flatfile)." );
                } else if( EnumUtil.TryParse( typeAttr.Value, out providerType, true ) ) {
                    PlayerDB.ProviderType = providerType;
                    PlayerDBProviderConfig = playerDBProviderEl.Elements().FirstOrDefault();
                } else {
                    Logger.Log( LogType.Warning,
                                "Config.Load: Unknown PlayerDBProvider type: {0}. Assuming default (flatfile).",
                                typeAttr.Value );
                }
            }

            if( reloading ) RaiseReloadedEvent();
        }


        public static XElement PlayerDBProviderConfig { get; set; }


        // LEGACY loader (for compatibility with config.xml versions prior to 152)
        static void ParseKeyElementLegacy( [NotNull] XElement element ) {
            if( element == null ) throw new ArgumentNullException( "element" );

            string keyName = element.Name.ToString().ToLower();
            ConfigKey key;
            if( EnumUtil.TryParse( keyName, out key, true ) ) {
                // known key
                TrySetValue( key, element.Value );

            } else if( LegacyConfigKeys.ContainsKey( keyName ) ) {
                // LEGACY
                // renamed/legacy key
                TrySetValue( LegacyConfigKeys[keyName], element.Value );

            } else if( keyName == "limitoneconnectionperip" ) {
                // LEGACY
                Logger.Log( LogType.Warning,
                            "Config: LimitOneConnectionPerIP (bool) was replaced by MaxConnectionsPerIP (int). " +
                            "Adjust your configuration accordingly." );
                ConfigKey.MaxConnectionsPerIP.TrySetValue( 1 );

            } else if( keyName != "consoleoptions" &&
                       keyName != "logfileoptions" &&
                       keyName != "ranks" &&
                       keyName != "legacyrankmapping" ) {
                // unknown key
                Logger.Log( LogType.Warning,
                            "Config: Unrecognized entry ignored: {0} = {1}",
                            element.Name, element.Value );
            }
        }


        static void ParseKeyElement( [NotNull] XElement element ) {
            if( element == null ) throw new ArgumentNullException( "element" );

            XAttribute keyAttr = element.Attribute( "key" );
            XAttribute valueAttr = element.Attribute( "value" );
            if( keyAttr == null || valueAttr == null ) {
                Logger.Log( LogType.Error,
                            "Malformed ConfigKey element: {0}",
                            element );
                return;
            }
            XAttribute defaultAttr = element.Attribute( "default" );
            string keyName = keyAttr.Value;
            string value = valueAttr.Value;

            ConfigKey key;
            if( !EnumUtil.TryParse( keyName, out key, true )) {
                if( LegacyConfigKeys.ContainsKey( keyName ) ) {
                    key = LegacyConfigKeys[keyName];
                } else {
                    // unknown key
                    Logger.Log( LogType.Warning,
                                "Config: Unrecognized key ignored: {0} = {1}",
                                element.Name, element.Value );
                    return;
                }
            }

            // see if setting is on its default value, and whether defaults have changed
            if( defaultAttr != null ) {
                string oldDefault = defaultAttr.Value;
                if( key.GetString() == key.GetString( value ) && !key.IsDefault( oldDefault ) ) {
                    Logger.Log( LogType.Warning,
                                "Config: Default value for {0} has been changed from {1} (\"{2}\") to {3} (\"{4}\"). " +
                                "You may want to adjust your settings accordingly.",
                                key,
                                key.GetPresentationString( oldDefault ),
                                oldDefault,
                                key.GetPresentationString( key.GetDefault() ),
                                key.GetDefault() );
                }
            }

            // known key
            TrySetValue( key, value );
        }


        static void LoadLogOptions( [NotNull] XContainer el, [NotNull] IList<bool> list ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            if( list == null ) throw new ArgumentNullException( "list" );

            for( int i = 0; i < list.Count; i++ ) {
                if( el.Element( ((LogType)i).ToString() ) != null ) {
                    list[i] = true;
                } else {
                    list[i] = false;
                }
            }
        }


        static void ApplyKeyChange( ConfigKey key ) {
            switch( key ) {
                case ConfigKey.AnnouncementColor:
                    Color.Announcement = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.AntispamInterval:
                    Player.AntispamInterval = key.GetInt();
                    break;

                case ConfigKey.AntispamMessageCount:
                    Player.AntispamMessageCount = key.GetInt();
                    break;

                case ConfigKey.DefaultBuildRank:
                    RankManager.DefaultBuildRank = Rank.Parse( key.GetString() );
                    break;

                case ConfigKey.DefaultRank:
                    RankManager.DefaultRank = Rank.Parse( key.GetString() );
                    break;

                case ConfigKey.BandwidthUseMode:
                    Player[] playerListCache = Server.Players;
                    if( playerListCache != null ) {
                        foreach( Player p in playerListCache ) {
                            if( p.BandwidthUseMode == BandwidthUseMode.Default ) {
                                // resets the use tweaks
                                p.BandwidthUseMode = BandwidthUseMode.Default;
                            }
                        }
                    }
                    break;

                case ConfigKey.BlockDBAutoEnableRank:
                    RankManager.BlockDBAutoEnableRank = Rank.Parse( key.GetString() );
                    if( BlockDB.IsEnabledGlobally ) {
                        World[] worldListCache = WorldManager.Worlds;
                        foreach( World world in worldListCache ) {
                            if( world.BlockDB.AutoToggleIfNeeded() ) {
                                if( world.BlockDB.IsEnabled ) {
                                    Logger.Log( LogType.SystemActivity,
                                                "BlockDB is now auto-enabled on world {0}", world.Name );
                                } else {
                                    Logger.Log( LogType.SystemActivity,
                                                "BlockDB is now auto-disabled on world {0}", world.Name );
                                }
                            }
                        }
                    }
                    break;

                case ConfigKey.BlockUpdateThrottling:
                    Server.BlockUpdateThrottling = key.GetInt();
                    break;

                case ConfigKey.ConsoleName:
                    if( Player.Console != null ) {
                        Player.Console.Info.Name = key.GetString();
                    }
                    break;

                case ConfigKey.HelpColor:
                    Color.Help = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.IRCDelay:
                    IRC.SendDelay = key.GetInt();
                    break;

                case ConfigKey.IRCMessageColor:
                    Color.IRC = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.LogMode:
                    Logger.SplittingType = key.GetEnum<LogSplittingType>();
                    break;

                case ConfigKey.MapPath:
                    if( !Paths.IgnoreMapPathConfigKey && GetString( ConfigKey.MapPath ).Length > 0 ) {
                        if( Paths.TestDirectory( "MapPath", GetString( ConfigKey.MapPath ), true ) ) {
                            Paths.MapPath = Path.GetFullPath( GetString( ConfigKey.MapPath ) );
                        }
                    }
                    break;

                case ConfigKey.MaxUndo:
                    BuildingCommands.MaxUndoCount = key.GetInt();
                    break;

                case ConfigKey.MeColor:
                    Color.Me = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.NoPartialPositionUpdates:
                    if( key.Enabled() ) {
                        Player.FullPositionUpdateInterval = 0;
                    } else {
                        Player.FullPositionUpdateInterval = Player.FullPositionUpdateIntervalDefault;
                    }
                    break;

                case ConfigKey.PatrolledRank:
                    RankManager.PatrolledRank = Rank.Parse( key.GetString() );
                    break;

                case ConfigKey.PrivateMessageColor:
                    Color.PM = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.RelayAllBlockUpdates:
                    Player.RelayAllUpdates = key.Enabled();
                    break;

                case ConfigKey.SayColor:
                    Color.Say = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.SystemMessageColor:
                    Color.Sys = Color.Parse( key.GetString() );
                    break;

                case ConfigKey.TickInterval:
                    Server.TicksPerSecond = 1000 / (float)key.GetInt();
                    break;

                case ConfigKey.UploadBandwidth:
                    Server.MaxUploadSpeed = key.GetInt();
                    break;

                case ConfigKey.WarningColor:
                    Color.Warning = Color.Parse( key.GetString() );
                    break;
            }
        }

        #endregion


        #region Saving

        /// <summary> Saves current configuration to default location (Paths.ConfigFileName). </summary>
        /// <returns> True is saving succeeded; otherwise false. </returns>
        public static bool Save() {
            return Save( Paths.ConfigFileName );
        }

        /// <summary> Saves current configuration to a custom location. </summary>
        /// <returns> True is saving succeeded; otherwise false. </returns>
        public static bool Save( string path ) {
            XDocument file = new XDocument();

            XElement config = new XElement( XmlRootName );
            config.Add( new XAttribute( "version", CurrentVersion ) );

            XElement settings = new XElement( "Settings" );

            // save general settings
            foreach( ConfigSection section in Enum.GetValues( typeof( ConfigSection ) ) ) {
                settings.Add( new XComment( section.ToString() ) );
                foreach( ConfigKey key in KeySections[section] ) {
                    XElement keyPairEl = new XElement( "ConfigKey" );
                    keyPairEl.Add( new XAttribute( "key", key ) );
                    keyPairEl.Add( new XAttribute( "value", Settings[(int)key] ) );
                    keyPairEl.Add( new XAttribute( "default", key.GetDefault() ) );
                    settings.Add( keyPairEl );
                }
            }
            config.Add( settings );

            // save console options
            XElement consoleOptions = new XElement( "ConsoleOptions" );
            for( int i = 0; i < Logger.ConsoleOptions.Length; i++ ) {
                if( Logger.ConsoleOptions[i] ) {
                    consoleOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( consoleOptions );

            // save logfile options
            XElement logFileOptions = new XElement( "LogFileOptions" );
            for( int i = 0; i < Logger.LogFileOptions.Length; i++ ) {
                if( Logger.LogFileOptions[i] ) {
                    logFileOptions.Add( new XElement( ((LogType)i).ToString() ) );
                }
            }
            config.Add( logFileOptions );

            // save ranks
            XElement ranksTag = new XElement( "Ranks" );
            foreach( Rank rank in RankManager.Ranks ) {
                ranksTag.Add( rank.Serialize() );
            }
            config.Add( ranksTag );

            if( RankManager.LegacyRankMapping.Count > 0 ) {
                // save legacy rank mapping
                XElement legacyRankMappingTag = new XElement( "LegacyRankMapping" );
                legacyRankMappingTag.Add( new XComment( "Legacy rank mapping is used for compatibility if cases when ranks are renamed or deleted." ) );
                foreach( KeyValuePair<string, string> pair in RankManager.LegacyRankMapping ) {
                    XElement rankPair = new XElement( "LegacyRankPair" );
                    rankPair.Add( new XAttribute( "from", pair.Key ), new XAttribute( "to", pair.Value ) );
                    legacyRankMappingTag.Add( rankPair );
                }
                config.Add( legacyRankMappingTag );
            }

            XElement playerDBProviderEl = new XElement( "PlayerDBProvider" );
            playerDBProviderEl.Add( new XAttribute( "type", PlayerDB.ProviderType ) );
            if( PlayerDBProviderConfig != null ) {
                playerDBProviderEl.Add( PlayerDBProviderConfig );
            }
            config.Add( playerDBProviderEl );

            file.Add( config );
            try {
                // write out the changes
                string tempFileName = path + ".temp";
                file.Save( tempFileName );
                Paths.MoveOrReplaceFile( tempFileName, path );
                return true;
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Config failed to save", "fCraft", ex, true );
                return false;
            }
        }

        #endregion


        #region Getters

        /// <summary> Checks whether any value has been set for a given key. </summary>
        public static bool IsBlank( this ConfigKey key ) {
            return (Settings[(int)key].Length == 0);
        }


        /// <summary> Returns normalized string value for the given key. </summary>
        public static string GetString( this ConfigKey key ) {
            return KeyMetadata[(int)key].GetUsableString( Settings[(int)key] );
        }

        /// <summary> Returns normalized string value for the given key. </summary>
        public static string GetString( this ConfigKey key, string value ) {
            return KeyMetadata[(int)key].GetUsableString( value );
        }


        /// <summary> Returns nicely formatted string (but not necessarily parsable) value for the given key. </summary>
        public static string GetPresentationString( this ConfigKey key ) {
            return KeyMetadata[(int)key].GetPresentationString( Settings[(int)key] );
        }

        /// <summary> Returns nicely formatted string (but not necessarily parsable) value for the given key. </summary>
        public static string GetPresentationString( this ConfigKey key, string value ) {
            return KeyMetadata[(int)key].GetPresentationString( value );
        }


        /// <summary> Returns raw string value for the given key (the value straight from config.xml) </summary>
        public static string GetRawString( this ConfigKey key ) {
            return Settings[(int)key];
        }


        /// <summary> Attempts to parse given key's value as an integer. </summary>
        /// <exception cref="T:System.FormatException" />
        public static int GetInt( this ConfigKey key ) {
            return Int32.Parse( GetString( key ) );
        }


        /// <summary> Attempts to parse a given key's value as an integer. </summary>
        /// <param name="key"> ConfigKey to get value from. </param>
        /// <param name="result"> Will be set to the value on success, or to 0 on failure. </param>
        /// <returns> Whether parsing succeeded. </returns>
        public static bool TryGetInt( this ConfigKey key, out int result ) {
            return Int32.TryParse( GetString( key ), out result );
        }


        /// <summary> Attempts to parse a given key's value as an enumeration.
        /// An ArgumentException is thrown if value could not be parsed.
        /// Note the parsing is done in a case-insensitive way. </summary>
        /// <typeparam name="TEnum"> Enum to use for parsing.
        /// An ArgumentException will be thrown if this is not an enum. </typeparam>
        public static TEnum GetEnum<TEnum>( this ConfigKey key ) where TEnum : struct {
            if( !typeof( TEnum ).IsEnum ) throw new ArgumentException( "Enum type required" );
            return (TEnum)Enum.Parse( typeof( TEnum ), GetString( key ), true );
        }


        /// <summary> Attempts to parse given key's value as a boolean. </summary>
        /// <exception cref="T:System.FormatException" />
        public static bool Enabled( this ConfigKey key ) {
            if( SettingsUseEnabledCache[(int)key] ) {
                return SettingsEnabledCache[(int)key];
            } else {
                return Boolean.Parse( GetString( key ) );
            }
        }


        /// <summary> Attempts to parse a given key's value as a boolean. </summary>
        /// <param name="key"> ConfigKey to get value from. </param>
        /// <param name="result"> Will be set to the value on success, or to false on failure. </param>
        /// <returns> Whether parsing succeeded. </returns>
        public static bool TryGetBool( this ConfigKey key, out bool result ) {
            if( SettingsUseEnabledCache[(int)key] ) {
                result = SettingsEnabledCache[(int)key];
                return true;
            } else {
                result = false;
                return false;
            }
        }


        /// <summary> Returns the expected Type of the key's value, as specified in key metadata. </summary>
        public static Type GetValueType( this ConfigKey key ) {
            return KeyMetadata[(int)key].ValueType;
        }


        /// <summary> Returns the ConfigSection that a given key is associated with. </summary>
        public static ConfigSection GetSection( this ConfigKey key ) {
            return KeyMetadata[(int)key].Section;
        }


        /// <summary> Returns the description text for a given config key. </summary>
        public static string GetDescription( this ConfigKey key ) {
            return KeyMetadata[(int)key].Description;
        }

        /// <summary> Returns whether given ConfigKey contains a Minecraft color. </summary>
        public static bool IsColor( this ConfigKey key ) {
            return KeyMetadata[(int)key].IsColor;
        }

        #endregion


        #region Setters

        /// <summary> Resets key value to its default setting. </summary>
        /// <param name="key"> Config key to reset. </param>
        /// <returns> True if value was reset. False if resetting was canceled by an event handler/plugin. </returns>
        public static bool ResetValue( this ConfigKey key ) {
            return key.TrySetValue( key.GetDefault() );
        }


        /// <summary> Sets value of a given config key.
        /// Note that this method may throw exceptions if the given value is not acceptable.
        /// Use Config.TrySetValue() if you'd like to suppress exceptions in favor of a boolean return value. </summary>
        /// <param name="key"> Config key to set. </param>
        /// <param name="rawValue"> Value to assign to the key. If passed object is not a string, rawValue.ToString() is used. </param>
        /// <returns> True if value is valid and has been assigned.
        /// False if value is valid, but assignment was canceled by an event handler/plugin. </returns>
        /// <exception cref="T:System.ArgumentNullException" />
        /// <exception cref="T:System.FormatException" />
        public static bool SetValue( this ConfigKey key, object rawValue ) {
            if( rawValue == null ) {
                throw new ArgumentNullException( "rawValue", key + ": ConfigKey values cannot be null. Use an empty string to indicate unset value." );
            }

            string value = (rawValue as string ?? rawValue.ToString());

            if( LegacyConfigValues.ContainsKey( key ) ) {
                foreach( var pair in LegacyConfigValues.Values ) {
                    if( pair.Key.Equals( value, StringComparison.OrdinalIgnoreCase ) ) {
                        value = pair.Value;
                        break;
                    }
                }
            }

            // throws various exceptions (most commonly FormatException) if invalid
            KeyMetadata[(int)key].Validate( value );

            return DoSetValue( key, value );
        }


        /// <summary> Attempts to set the value of a given config key.
        /// Check the return value to make sure that the given value was acceptable. </summary>
        /// <param name="key"> Config key to set. </param>
        /// <param name="rawValue"> Value to assign to the key. If passed object is not a string, rawValue.ToString() is used. </param>
        /// <exception cref="T:System.ArgumentNullException" />
        /// <returns> True if value is valid and has been assigned.
        /// False if value was invalid, or if assignment was canceled by an event callback. </returns>
        public static bool TrySetValue( this ConfigKey key, object rawValue ) {
            try {
                return SetValue( key, rawValue );
            } catch( FormatException ex ) {
                Logger.Log( LogType.Error,
                            "{0}.TrySetValue: {1}",
                            key, ex.Message );
                return false;
            }
        }


        static bool DoSetValue( ConfigKey key, string newValue ) {
            string oldValue = Settings[(int)key];
            if( oldValue != newValue ) {
                if( !RaiseKeyChangingEvent( key, oldValue, ref newValue ) ) return false;
                Settings[(int)key] = newValue;

                bool enabledCache;
                if( Boolean.TryParse( newValue, out enabledCache ) ) {
                    SettingsUseEnabledCache[(int)key] = true;
                    SettingsEnabledCache[(int)key] = enabledCache;
                } else {
                    SettingsUseEnabledCache[(int)key] = false;
                    SettingsEnabledCache[(int)key] = false;
                }

                ApplyKeyChange( key );
                RaiseKeyChangedEvent( key, oldValue, newValue );
            }
            return true;
        }

        #endregion


        #region Ranks

        static void LoadRankList( [NotNull] XContainer el, bool fromFile ) {
            if( el == null ) throw new ArgumentNullException( "el" );

            XElement legacyRankMappingTag = el.Element( "LegacyRankMapping" );
            if( legacyRankMappingTag != null ) {
                foreach( XElement rankPair in legacyRankMappingTag.Elements( "LegacyRankPair" ) ) {
                    XAttribute fromRankID = rankPair.Attribute( "from" );
                    XAttribute toRankID = rankPair.Attribute( "to" );
                    if( fromRankID == null || String.IsNullOrEmpty( fromRankID.Value ) ||
                        toRankID == null || String.IsNullOrEmpty( toRankID.Value ) ) {
                        Logger.Log( LogType.Error,
                                    "Config.Load: Could not parse a LegacyRankMapping entry: {0}", rankPair );
                    } else {
                        RankManager.LegacyRankMapping.Add( fromRankID.Value, toRankID.Value );
                    }
                }
            }

            XElement rankList = el.Element( "Ranks" );

            if( rankList != null ) {
                XElement[] rankDefinitionList = rankList.Elements( "Rank" ).ToArray();

                foreach( XElement rankDefinition in rankDefinitionList ) {
                    try {
                        RankManager.AddRank( new Rank( rankDefinition ) );
                    } catch( RankDefinitionException ex ) {
                        Logger.Log( LogType.Error, ex.Message );
                    }
                }

                if( RankManager.RanksByName.Count == 0 ) {
                    Logger.Log( LogType.Warning,
                                "Config.Load: No ranks were defined, or none were defined correctly. "+
                                "Using default ranks (guest, builder, op, and owner)." );
                    rankList.Remove();
                    el.Add( RankManager.DefineDefaultRanks() );
                }

            } else {
                if( fromFile ) Logger.Log( LogType.Warning, "Config.Load: using default player ranks." );
                el.Add( RankManager.DefineDefaultRanks() );
            }

            // parse rank-limit permissions
            RankManager.ParsePermissionLimits();
        }

        #endregion


        #region Events

        /// <summary> Occurs after the entire configuration has been reloaded from file. </summary>
        public static event EventHandler Reloaded;


        /// <summary> Occurs when a config key is about to be changed (cancellable).
        /// The new value may be replaced by the callback. </summary>
        public static event EventHandler<ConfigKeyChangingEventArgs> KeyChanging;


        /// <summary> Occurs after a config key has been changed. </summary>
        public static event EventHandler<ConfigKeyChangedEventArgs> KeyChanged;


        static void RaiseReloadedEvent() {
            var handler = Reloaded;
            if( handler != null ) handler( null, EventArgs.Empty );
        }


        static bool RaiseKeyChangingEvent( ConfigKey key, string oldValue, ref string newValue ) {
            var handler = KeyChanging;
            if( handler == null ) return true;
            var e = new ConfigKeyChangingEventArgs( key, oldValue, newValue );
            handler( null, e );
            newValue = e.NewValue;
            return !e.Cancel;
        }


        static void RaiseKeyChangedEvent( ConfigKey key, string oldValue, string newValue ) {
            var handler = KeyChanged;
            var args = new ConfigKeyChangedEventArgs( key, oldValue, newValue );
            if( handler != null ) handler( null, args );
        }

        #endregion


        /// <summary> Returns a list of all keys in a section. </summary>
        public static ConfigKey[] GetKeys( this ConfigSection section ) {
            return KeySections[section];
        }
    }
}


namespace fCraft.Events {

    public sealed class ConfigKeyChangingEventArgs : EventArgs, ICancelableEvent {
        public ConfigKey Key { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; set; }
        public bool Cancel { get; set; }

        public ConfigKeyChangingEventArgs( ConfigKey key, string oldValue, string newValue ) {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
            Cancel = false;
        }
    }


    public sealed class ConfigKeyChangedEventArgs : EventArgs {
        public ConfigKey Key { get; private set; }
        public string OldValue { get; private set; }
        public string NewValue { get; private set; }

        public ConfigKeyChangedEventArgs( ConfigKey key, string oldValue, string newValue ) {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

}