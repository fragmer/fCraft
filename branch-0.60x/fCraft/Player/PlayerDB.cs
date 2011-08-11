﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.Events;

namespace fCraft {
    public static class PlayerDB {
        static readonly Trie<PlayerInfo> Trie = new Trie<PlayerInfo>();
        static List<PlayerInfo> list = new List<PlayerInfo>();

        /// <summary> Cached list of all players in the database.
        /// May be quite long. Make sure to copy a reference to
        /// the list before accessing it in a loop, since this 
        /// array be frequently be replaced by an updated one. </summary>
        public static PlayerInfo[] PlayerInfoList { get; private set; }

        static int maxID = 255;

        /* 
         * Version 0 - before 0.530 - all dates/times are local
         * Version 1 - 0.530-0.536 - all dates and times are stored as UTC unix timestamps (milliseconds)
         * Version 2 - 0.600 dev - all dates and times are stored as UTC unix timestamps (seconds)
         * Version 3 - 0.600+ - same as v2, but sorting by ID is enforced
         */
        public const int FormatVersion = 3;

        const string Header = "fCraft PlayerDB | Row format: " +
                              "Name,IPAddress,Rank,RankChangeDate,RankChangedBy,Banned,BanDate,BannedBy," +
                              "UnbanDate,UnbannedBy,BanReason,UnbanReason,LastFailedLoginDate," +
                              "LastFailedLoginIP,FailedLoginCount,FirstLoginDate,LastLoginDate,TotalTime," +
                              "BlocksBuilt,BlocksDeleted,TimesVisited,LinesWritten,UNUSED,UNUSED," +
                              "PreviousRank,RankChangeReason,TimesKicked,TimesKickedOthers," +
                              "TimesBannedOthers,ID,RankChangeType,LastKickDate,LastSeen,BlocksDrawn," +
                              "LastKickBy,LastKickReason,IsFrozen,FrozenBy,FrozenOn, MutedUntil,MutedBy," +
                              "Password,Online,BandwidthUseMode";


        // used to ensure PlayerDB consistency when adding/removing PlayerDB entries
        static readonly object AddLocker = new object();

        // used to prevent concurrent access to the PlayerDB file
        static readonly object SaveLoadLocker = new object();


        public static bool IsLoaded { get; private set; }


        public static PlayerInfo AddFakeEntry( string name, RankChangeType rankChangeType ) {
            if( name == null ) throw new ArgumentNullException( "name" );

            PlayerInfo info;
            lock( AddLocker ) {
                info = Trie.Get( name );
                if( info != null ) {
                    throw new ArgumentException( "A PlayerDB entry already exists for this name.", "name" );
                }

                var e = new PlayerInfoCreatingEventArgs( name, IPAddress.None, RankManager.DefaultRank, true );
                Server.RaisePlayerInfoCreatingEvent( e );
                if( e.Cancel ) {
                    throw new OperationCanceledException( "Cancelled by a plugin." );
                }

                info = new PlayerInfo( name, e.StartingRank, false, rankChangeType );

                list.Add( info );
                Trie.Add( info.Name, info );
                UpdateCache();
            }
            Server.RaisePlayerInfoCreatedEvent( info, false );
            return info;
        }


        #region Saving/Loading

        internal static void Load() {
            lock( SaveLoadLocker ) {
                if( File.Exists( Paths.PlayerDBFileName ) ) {
                    Stopwatch sw = Stopwatch.StartNew();
                    using( StreamReader reader = File.OpenText( Paths.PlayerDBFileName ) ) {

                        string header = reader.ReadLine();

                        if( header == null ) return; // if PlayerDB is an empty file

                        lock( AddLocker ) {
                            int version = IdentifyFormatVersion( header );
                            if( version > FormatVersion ) {
                                Logger.Log( "PlayerDB.Load: Attempting to load unsupported PlayerDB format ({0}). Errors may occur.",
                                            LogType.Warning,
                                            version );
                            } else if( version < FormatVersion ) {
                                Logger.Log( "PlayerDB.Load: Converting PlayerDB to a newer format (version {0} to {1}).",
                                            LogType.Warning,
                                            version, FormatVersion );
                            }

                            while( true ) {
                                string line = reader.ReadLine();
                                if( line == null ) break;
                                string[] fields = line.Split( ',' );
                                if( fields.Length >= PlayerInfo.MinFieldCount ) {
#if !DEBUG
                                    try {
#endif
                                        PlayerInfo info;
                                        switch( version ) {
                                            case 0:
                                                info = PlayerInfo.LoadFormat0( fields, true );
                                                break;
                                            case 1:
                                                info = PlayerInfo.LoadFormat1( fields );
                                                break;
                                            case 2:
                                            case 3:
                                                info = PlayerInfo.LoadFormat2( fields );
                                                break;
                                            default:
                                                return;
                                        }

                                        if( info.ID > maxID ) {
                                            maxID = info.ID;
                                            Logger.Log( "PlayerDB.Load: Adjusting wrongly saved MaxID ({0} to {1}).", LogType.Warning );
                                        }

                                        if( Trie.ContainsKey( info.Name ) ) {
                                            Logger.Log( "PlayerDB.Load: Duplicate record for player \"{0}\" skipped.",
                                                        LogType.Error, info.Name );
                                        } else {
                                            Trie.Add( info.Name, info );
                                            list.Add( info );
                                        }
#if !DEBUG
                                    } catch( Exception ex ) {
                                        Logger.LogAndReportCrash( "Error while parsing PlayerInfo record", "fCraft", ex,
                                                                  false );
                                    }
#endif
                                } else {
                                    Logger.Log( "PlayerDB.Load: Unexpected field count ({0}), expecting at least {1} fields for a PlayerDB entry.",
                                                LogType.Error,
                                                fields.Length, PlayerInfo.MinFieldCount );
                                }
                            }
                            if( version < 3 ) {
                                Logger.Log( "Sorting PlayerDB by ID...", LogType.SystemActivity );
                                list.Sort( PlayerIDComparer.Instance );
                            }
                        }
                    }
                    list.TrimExcess();
                    sw.Stop();
                    Logger.Log( "PlayerDB.Load: Done loading player DB ({0} records) in {1}ms. MaxID={2}", LogType.Debug,
                                Trie.Count, sw.ElapsedMilliseconds, maxID );
                } else {
                    Logger.Log( "PlayerDB.Load: No player DB file found.", LogType.Warning );
                }
                UpdateCache();
                IsLoaded = true;
            }
        }


        static int IdentifyFormatVersion( string header ) {
            if( header == null ) throw new ArgumentNullException( "header" );
            string[] headerParts = header.Split( ' ' );
            if( headerParts.Length < 2 ) {
                throw new FormatException( "Invalid PlayerDB header format: " + header );
            }
            int maxIDField;
            if( Int32.TryParse( headerParts[0], out maxIDField ) ) {
                if( maxIDField >= 255 ) {// IDs start at 256
                    maxID = maxIDField;
                }
            }
            int version;
            if( Int32.TryParse( headerParts[1], out version ) ) {
                return version;
            } else {
                return 0;
            }
        }


        public static void Save() {

            const string tempFileName = Paths.PlayerDBFileName + ".temp";

            lock( SaveLoadLocker ) {
                PlayerInfo[] listCopy = GetPlayerListCopy();
                Stopwatch sw = Stopwatch.StartNew();
                using( FileStream fs = new FileStream( tempFileName, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024 ) ) {
                    using( StreamWriter writer = new StreamWriter( fs, Encoding.ASCII, 64 * 1024 ) ) {
                        writer.WriteLine( "{0} {1} {2}", maxID, FormatVersion, Header );

                        StringBuilder sb = new StringBuilder();
                        for( int i = 0; i < listCopy.Length; i++ ) {
                            listCopy[i].Serialize( sb );
                            writer.WriteLine( sb.ToString() );
                            sb.Length = 0;
                        }
                    }
                }
                sw.Stop();
                Logger.Log( "PlayerDB.Save: Saved player database ({0} records) in {1}ms", LogType.Debug,
                            Trie.Count, sw.ElapsedMilliseconds );

                try {
                    Paths.MoveOrReplace( tempFileName, Paths.PlayerDBFileName );
                } catch( Exception ex ) {
                    Logger.Log( "PlayerDB.Save: An error occured while trying to save PlayerDB: " + ex, LogType.Error );
                }
            }
        }

        #endregion


        #region Scheduled Saving

        static SchedulerTask saveTask;
        static TimeSpan saveInterval = TimeSpan.FromSeconds( 90 );
        public static TimeSpan SaveInterval {
            get { return saveInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException();
                saveInterval = value;
                if( saveTask != null ) saveTask.Interval = value;
            }
        }

        internal static void StartSaveTask() {
            saveTask = Scheduler.NewBackgroundTask( delegate { Save(); } )
                                .RunForever( SaveInterval, SaveInterval + TimeSpan.FromSeconds( 15 ) );
        }

        #endregion


        #region Lookup

        public static PlayerInfo FindOrCreateInfoForPlayer( string name, IPAddress lastIP ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            PlayerInfo info;

            // this flag is used to avoid executing PlayerInfoCreated event in the lock
            bool raiseCreatedEvent = false;

            lock( AddLocker ) {
                info = Trie.Get( name );
                if( info == null ) {
                    var e = new PlayerInfoCreatingEventArgs( name, lastIP, RankManager.DefaultRank, false );
                    Server.RaisePlayerInfoCreatingEvent( e );
                    if( e.Cancel ) throw new OperationCanceledException( "Cancelled by a plugin." );

                    info = new PlayerInfo( name, lastIP, e.StartingRank );
                    Trie.Add( name, info );
                    list.Add( info );
                    UpdateCache();

                    raiseCreatedEvent = true;
                }
            }

            if( raiseCreatedEvent ) {
                Server.RaisePlayerInfoCreatedEvent( info, false );
            }
            return info;
        }


        public static PlayerInfo[] FindPlayers( IPAddress address ) {
            return FindPlayers( address, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( IPAddress address, int limit ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            PlayerInfo[] cache = PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( cache[i].LastIP.Equals( address ) ) {
                    result.Add( cache[i] );
                    count++;
                    if( count >= limit ) return result.ToArray();
                }
            }
            return result.ToArray();
        }


        public static PlayerInfo[] FindPlayers( Regex regex ) {
            return FindPlayers( regex, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( Regex regex, int limit ) {
            if( regex == null ) throw new ArgumentNullException( "regex" );
            List<PlayerInfo> result = new List<PlayerInfo>();
            int count = 0;
            PlayerInfo[] cache = PlayerInfoList;
            for( int i = 0; i < cache.Length; i++ ) {
                if( regex.IsMatch( cache[i].Name ) ) {
                    result.Add( cache[i] );
                    count++;
                    if( count >= limit ) break;
                }
            }
            return result.ToArray();
        }


        public static PlayerInfo[] FindPlayers( string namePart ) {
            return FindPlayers( namePart, Int32.MaxValue );
        }


        public static PlayerInfo[] FindPlayers( string namePart, int limit ) {
            if( namePart == null ) throw new ArgumentNullException( "namePart" );
            lock( AddLocker ) {
                //return Trie.ValuesStartingWith( namePart ).Take( limit ).ToArray(); // <- works, but is slightly slower
                return Trie.GetList( namePart, limit ).ToArray();
            }
        }

        /// <summary>Searches for player names starting with namePart, returning just one or none of the matches.</summary>
        /// <param name="name">Partial or full player name</param>
        /// <param name="info">PlayerInfo to output (will be set to null if no single match was found)</param>
        /// <returns>true if one or zero matches were found, false if multiple matches were found</returns>
        public static bool FindPlayerInfo( string name, out PlayerInfo info ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( AddLocker ) {
                return Trie.GetOneMatch( name, out info );
            }
        }


        public static PlayerInfo FindPlayerInfoExact( string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            lock( AddLocker ) {
                return Trie.Get( name );
            }
        }

        #endregion


        #region Stats

        public static int CountBannedPlayers() {
            return PlayerInfoList.Count( t => t.Banned );
        }


        public static int CountTotalPlayers() {
            return Trie.Count;
        }


        public static int CountPlayersByRank( Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            return PlayerInfoList.Count( t => t.Rank == rank );
        }

        #endregion


        public static int GetNextID() {
            return Interlocked.Increment( ref maxID );
        }


        public static PlayerInfo FindPlayerInfoByID( int id ) {
            PlayerInfo dummy = new PlayerInfo( id );
            lock( AddLocker ) {
                int index = list.BinarySearch( dummy, PlayerIDComparer.Instance );
                if( index >= 0 ) {
                    return list[index];
                } else {
                    return null;
                }
            }
        }


        public static int MassRankChange( Player player, Rank from, Rank to, bool silent ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( from == null ) throw new ArgumentNullException( "from" );
            if( to == null ) throw new ArgumentNullException( "to" );
            int affected = 0;
            lock( AddLocker ) {
                for( int i = 0; i < PlayerInfoList.Length; i++ ) {
                    if( PlayerInfoList[i].Rank == from ) {
                        ModerationCommands.DoChangeRank( player,
                                                         PlayerInfoList[i],
                                                         to,
                                                         "~MassRank",
                                                         silent,
                                                         false );
                        affected++;
                    }
                }
                return affected;
            }
        }


        public static PlayerInfo[] GetPlayerListCopy() {
            return PlayerInfoList;
        }


        public static PlayerInfo[] GetPlayerListCopy( Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            PlayerInfo[] cache = PlayerInfoList;
            return cache.Where( info => info.Rank == rank ).ToArray();
        }


        static void UpdateCache() {
            lock( AddLocker ) {
                PlayerInfoList = list.ToArray();
            }
        }


        #region Experimental & Debug things

        // TODO: figure out a good way of making this persistent. ReaderWriterLockSlim maybe? (If Mono is fixed)
        static Dictionary<IPAddress, List<PlayerInfo>> playersByIP;


        internal static int CountInactivePlayers() {
            lock( AddLocker ) {
                playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] playerInfoListCache = PlayerInfoList;
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( playerInfoListCache[i].LastIP ) ) {
                        playersByIP[playerInfoListCache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[playerInfoListCache[i].LastIP].Add( PlayerInfoList[i] );
                }

                int count = 0;
                // ReSharper disable LoopCanBeConvertedToQuery
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    // ReSharper restore LoopCanBeConvertedToQuery
                    if( PlayerIsInactive( playerInfoListCache[i], true ) ) count++;
                }
                playersByIP = null;
                return count;
            }
        }


        internal static int RemoveInactivePlayers( Player player ) {
            int estimate = CountInactivePlayers();
            int count = 0;
            lock( AddLocker ) {
                playersByIP = new Dictionary<IPAddress, List<PlayerInfo>>();
                PlayerInfo[] playerInfoListCache = PlayerInfoList;
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    if( !playersByIP.ContainsKey( playerInfoListCache[i].LastIP ) ) {
                        playersByIP[playerInfoListCache[i].LastIP] = new List<PlayerInfo>();
                    }
                    playersByIP[playerInfoListCache[i].LastIP].Add( PlayerInfoList[i] );
                }
                List<PlayerInfo> newList = new List<PlayerInfo>();
                for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                    PlayerInfo p = playerInfoListCache[i];
                    if( PlayerIsInactive( p, true ) ) {
                        count++;
                        if( (count % (estimate / 4) == 0) ) {
                            player.Message( "PruneDB: {0}% complete.", (count * 100) / estimate );
                        }
                    } else {
                        newList.Add( p );
                    }
                }

                list = newList;
                Trie.Clear();
                foreach( PlayerInfo p in list ) {
                    Trie.Add( p.Name, p );
                }

                list.TrimExcess();
                UpdateCache();
                playersByIP = null;
            }
            player.Message( "PruneDB: Removed {0} inactive players!", count );
            return count;
        }


        static bool PlayerIsInactive( PlayerInfo player, bool checkIP ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( player.Banned || !String.IsNullOrEmpty( player.UnbannedBy ) ||
                player.IsFrozen || player.IsMuted || player.TimesKicked != 0 ||
                player.Rank != RankManager.DefaultRank || player.PreviousRank != null ) {
                return false;
            }
            if( player.TotalTime.TotalMinutes > 30 || player.TimeSinceLastSeen.TotalDays < 30 ) {
                return false;
            }
            if( IPBanList.Get( player.LastIP ) != null ) {
                return false;
            }
            if( checkIP ) {
                return playersByIP[player.LastIP].All( other => (other == player) || PlayerIsInactive( other, false ) );
            }
            return true;
        }


        internal static void RecoverIPBans() {
            PlayerInfo[] playerInfoListCache = PlayerInfoList;
            for( int i = 0; i < playerInfoListCache.Length; i++ ) {
                PlayerInfo p = playerInfoListCache[i];
                if( p.Banned && p.BanReason.EndsWith( "~BanAll", StringComparison.OrdinalIgnoreCase ) && IPBanList.Get( p.LastIP ) == null ) {
                    IPBanList.Add( new IPBanInfo( p.LastIP, p.Name, p.BannedBy, p.BanReason ) );
                    Logger.Log( "PlayerDB.RecoverIPBans: Banned {0} by association with {1}. Banned by {2}. Reason: {3}", LogType.SystemActivity,
                                p.LastIP, p.Name, p.BannedBy, p.BanReason );
                }
            }
        }

        #endregion


        sealed class PlayerIDComparer : IComparer<PlayerInfo> {
            public static readonly PlayerIDComparer Instance = new PlayerIDComparer();
            private PlayerIDComparer() { }

            public int Compare( PlayerInfo x, PlayerInfo y ) {
                return x.ID - y.ID;
            }
        }
    }
}