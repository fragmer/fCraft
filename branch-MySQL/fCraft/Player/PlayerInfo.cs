﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.ComponentModel;
using JetBrains.Annotations;
using System.IO;

namespace fCraft {
    /// <summary> Object representing persistent state ("record") of a player, online or offline.
    /// There is exactly one PlayerInfo object for each known Minecraft account. All data is stored in the PlayerDB. </summary>
    public sealed partial class PlayerInfo : IClassy, INotifyPropertyChanged {
        public const int MinFieldCount = 24;

        #region Properties

        bool changed;

        /// <summary> Player's unique numeric ID. Immutable. Issued on first join. </summary>
        public int ID { get; private set; }


        /// <summary> Player's Minecraft account name. </summary>
        [NotNull]
        public string Name {
            get { return name; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( name != null && value.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    throw new ArgumentException( "You may only change capitalization of the name.", "value" );
                }
                if( name != value ) {
                    name = value;
                    OnChanged( "Name" );
                }
            }
        }
        [NotNull]
        string name;


        /// <summary> If set, replaces Name when printing name in chat. </summary>
        [CanBeNull]
        public string DisplayedName {
            get { return displayedName; }
            set {
                if( value != displayedName ) {
                    displayedName = value;
                    OnChanged( "DisplayedName" );
                }
            }
        }
        [CanBeNull]
        string displayedName;


        /// <summary> First time the player ever logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime FirstLoginDate {
            get { return firstLoginDate; }
            set {
                if( value != firstLoginDate ) {
                    firstLoginDate = value;
                    OnChanged("FirstLoginDate");
                }
            }
        }
        DateTime firstLoginDate;


        /// <summary> Most recent time the player logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastLoginDate {
            get { return lastLoginDate; }
            set {
                if( value != lastLoginDate ) {
                    lastLoginDate = value;
                    OnChanged("LastLoginDate");
                }
            }
        }
        DateTime lastLoginDate;


        /// <summary> Last time the player has been seen online (last logout), UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastSeen {
            get { return lastSeen; }
            set {
                if( value != lastSeen ) {
                    lastSeen = value;
                    OnChanged("LastSeen");
                }
            }
        }
        DateTime lastSeen;


        /// <summary> Reason for leaving the server last time. </summary>
        public LeaveReason LeaveReason {
            get { return leaveReason; }
            set {
                if( value != leaveReason ) {
                    leaveReason = value;
                    OnChanged("LeaveReason");
                }
            }
        }
        LeaveReason leaveReason;
            

        #region Rank

        /// <summary> Player's current rank.
        /// Should be set by using PlayerInfo.ChangeRank method. </summary>
        [NotNull]
        public Rank Rank {
            get { return rank; }
            internal set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value != rank ) {
                    rank = value;
                    OnChanged( "Rank" );
                }
            }
        }
        [NotNull]
        Rank rank;


        /// <summary> Player's previous rank.
        /// May be null if player has never been promoted/demoted before. </summary>
        [CanBeNull]
        public Rank PreviousRank {
            get { return previousRank; }
            set {
                if( value != previousRank ) {
                    previousRank = value;
                    OnChanged( "PreviousRank" );
                }
            }
        }
        [CanBeNull]
        Rank previousRank;


        /// <summary> Date of the most recent promotion/demotion, UTC.
        /// May be DateTime.MinValue if player has never been promoted/demoted before. </summary>
        public DateTime RankChangeDate {
            get { return rankChangeDate; }
            set {
                if( value != rankChangeDate ) {
                    rankChangeDate = value;
                    OnChanged( "RankChangeDate" );
                }
            }
        }
        DateTime rankChangeDate;


        /// <summary> Name of the entity that most recently promoted/demoted this player. May be null. </summary>
        [CanBeNull]
        public string RankChangedBy {
            get { return rankChangedBy; }
            set {
                if( value != rankChangedBy ) {
                    rankChangedBy = value;
                    OnChanged( "RankChangedBy" );
                }
            }
        }
        [CanBeNull]
        string rankChangedBy;


        /// <summary> Returns decorated name of RankChangedBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string RankChangedByClassy {
            get {
                return PlayerDB.FindExactClassyName( rankChangedBy );
            }
        }


        /// <summary> Reason given for the most recent promotion/demotion. May be null. </summary>
        [CanBeNull]
        public string RankChangeReason {
            get { return rankChangeReason; }
            set {
                if( value != rankChangeReason ) {
                    rankChangeReason = value;
                    OnChanged( "RankChangeReason" );
                }
            }
        }
        [CanBeNull]
        string rankChangeReason;


        /// <summary> Type of the most recent promotion/demotion. </summary>
        public RankChangeType RankChangeType {
            get { return rankChangeType; }
            set {
                if( value != rankChangeType ) {
                    rankChangeType = value;
                    OnChanged( "RankChangeType" );
                }
            }
        }
        RankChangeType rankChangeType;

        #endregion


        #region Bans

        /// <summary> Player's current BanStatus: Banned, NotBanned, or Exempt. </summary>
        public BanStatus BanStatus {
            get { return banStatus; }
            set {
                if( value != banStatus ) {
                    banStatus = value;
                    OnChanged( "BanStatus" );
                }
            }
        }
        BanStatus banStatus;


        /// <summary> Returns whether player is name-banned or not. Read-only, not serialized. </summary>
        public bool IsBanned {
            get { return banStatus == BanStatus.Banned; }
        }


        /// <summary> Date of most recent ban, UTC. May be DateTime.MinValue if player was never banned. </summary>
        public DateTime BanDate {
            get { return banDate; }
            set {
                if( value != banDate ) {
                    banDate = value;
                    OnChanged( "BanDate" );
                }
            }
        }
        DateTime banDate;


        /// <summary> Name of the entity responsible for most recent ban. May be null. </summary>
        [CanBeNull]
        public string BannedBy {
            get { return bannedBy; }
            set {
                if( value != bannedBy ) {
                    bannedBy = value;
                    OnChanged( "BannedBy" );
                }
            }
        }
        [CanBeNull]
        string bannedBy;


        /// <summary> Returns decorated name of BannedBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string BannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( bannedBy );
            }
        }


        /// <summary> Reason given for the most recent ban. May be null. </summary>
        [CanBeNull]
        public string BanReason {
            get { return banReason; }
            set {
                if( value != banReason ) {
                    banReason = value;
                    OnChanged( "BanReason" );
                }
            }
        }
        [CanBeNull]
        string banReason;


        /// <summary> Date of most recent unban, UTC. May be DateTime.MinValue if player was never unbanned. </summary>
        public DateTime UnbanDate {
            get { return unbanDate; }
            set {
                if( value != unbanDate ) {
                    unbanDate = value;
                    OnChanged( "UnbanDate" );
                }
            }
        }
        DateTime unbanDate;


        /// <summary> Name of the entity responsible for most recent unban. May be null. </summary>
        [CanBeNull]
        public string UnbannedBy {
            get { return unbannedBy; }
            set {
                if( value != unbannedBy ) {
                    unbannedBy = value;
                    OnChanged( "UnbannedBy" );
                }
            }
        }
        [CanBeNull]
        string unbannedBy;


        /// <summary> Returns decorated name of UnbannedBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string UnbannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( unbannedBy );
            }
        }


        /// <summary> Reason given for the most recent unban. May be null. </summary>
        [CanBeNull]
        public string UnbanReason {
            get { return unbanReason; }
            set {
                if( value != unbanReason ) {
                    unbanReason = value;
                    OnChanged( "UnbanReason" );
                }
            }
        }
        [CanBeNull]
        string unbanReason;


        /// <summary> Date of most recent failed attempt to log in, UTC. </summary>
        public DateTime LastFailedLoginDate {
            get { return lastFailedLoginDate; }
            set {
                if( value != lastFailedLoginDate ) {
                    lastFailedLoginDate = value;
                    OnChanged( "LastFailedLoginDate" );
                }
            }
        }
        DateTime lastFailedLoginDate;


        /// <summary> IP from which player most recently tried (and failed) to log in, UTC. </summary>
        [NotNull]
        public IPAddress LastFailedLoginIP {
            get { return lastFailedLoginIP; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value != lastFailedLoginIP ) {
                    lastFailedLoginIP = value;
                    OnChanged( "LastFailedLoginIP" );
                }
            }
        }
        [NotNull]
        IPAddress lastFailedLoginIP = IPAddress.None;

        #endregion


        #region Stats

        /// <summary> Total amount of time the player spent on this server. </summary>
        public TimeSpan TotalTime {
            get { return totalTime; }
            set {
                if( value != totalTime ) {
                    totalTime = value;
                    OnChanged( "TotalTime" );
                }
            }
        }
        TimeSpan totalTime;


        /// <summary> Total number of blocks manually built or painted by the player. </summary>
        public int BlocksBuilt {
            get { return blocksBuilt; }
            set {
                if( value != blocksBuilt ) {
                    blocksBuilt = value;
                    OnChanged( "BlocksBuilt" );
                }
            }
        }
        int blocksBuilt;


        /// <summary> Total number of blocks manually deleted by the player. </summary>
        public int BlocksDeleted {
            get { return blocksDeleted; }
            set {
                if( value != blocksDeleted ) {
                    blocksDeleted = value;
                    OnChanged( "BlocksDeleted" );
                }
            }
        }
        int blocksDeleted;


        /// <summary> Total number of blocks modified using draw and copy/paste commands. </summary>
        public long BlocksDrawn {
            get { return blocksDrawn; }
            set {
                if( value != blocksDrawn ) {
                    blocksDrawn = value;
                    OnChanged( "BlocksDrawn" );
                }
            }
        }
        long blocksDrawn;


        /// <summary> Number of sessions/logins. </summary>
        public int TimesVisited {
            get { return timesVisited; }
            set {
                if( value != timesVisited ) {
                    timesVisited = value;
                    OnChanged( "TimesVisited" );
                }
            }
        }
        int timesVisited;


        /// <summary> Total number of messages written. </summary>
        public int MessagesWritten {
            get { return messagesWritten; }
            set {
                if( value != messagesWritten ) {
                    messagesWritten = value;
                    OnChanged( "MessagesWritten" );
                }
            }
        }
        int messagesWritten;


        /// <summary> Number of kicks issues by this player. </summary>
        public int TimesKickedOthers {
            get { return timesKickedOthers; }
            set {
                if( value != timesKickedOthers ) {
                    timesKickedOthers = value;
                    OnChanged( "TimesKickedOthers" );
                }
            }
        }
        int timesKickedOthers;


        /// <summary> Number of bans issued by this player. </summary>
        public int TimesBannedOthers {
            get { return timesBannedOthers; }
            set {
                if( value != timesBannedOthers ) {
                    timesBannedOthers = value;
                    OnChanged( "TimesBannedOthers" );
                }
            }
        }
        int timesBannedOthers;

        #endregion


        #region Kicks

        /// <summary> Number of times that this player has been manually kicked. </summary>
        public int TimesKicked {
            get { return timesKicked; }
            set {
                if( value != timesKicked ) {
                    timesKicked = value;
                    OnChanged( "TimesKicked" );
                }
            }
        }
        int timesKicked;


        /// <summary> Date of the most recent kick.
        /// May be DateTime.MinValue if the player has never been kicked. </summary>
        public DateTime LastKickDate {
            get { return lastKickDate; }
            set {
                if( value != lastKickDate ) {
                    lastKickDate = value;
                    OnChanged( "LastKickDate" );
                }
            }
        }
        DateTime lastKickDate;


        /// <summary> Name of the entity that most recently kicked this player. May be null. </summary>
        [CanBeNull]
        public string LastKickBy {
            get { return lastKickBy; }
            set {
                if( value != lastKickBy ) {
                    lastKickBy = value;
                    OnChanged( "LastKickBy" );
                }
            }
        }
        [CanBeNull]
        string lastKickBy;


        /// <summary> Returns decorated name of LastKickByClassy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string LastKickByClassy {
            get {
                return PlayerDB.FindExactClassyName( lastKickBy );
            }
        }


        /// <summary> Reason given for the most recent kick. May be null. </summary>
        [CanBeNull]
        public string LastKickReason {
            get { return lastKickReason; }
            set {
                if( value != lastKickReason ) {
                    lastKickReason = value;
                    OnChanged( "LastKickReason" );
                }
            }
        }
        [CanBeNull]
        string lastKickReason;

        #endregion


        #region Freeze And Mute

        /// <summary> Whether this player is currently frozen. </summary>
        public bool IsFrozen {
            get { return isFrozen; }
            set {
                if( value != isFrozen ) {
                    isFrozen = value;
                    OnChanged( "IsFrozen" );
                }
            }
        }
        bool isFrozen;


        /// <summary> Date of the most recent freezing.
        /// May be DateTime.MinValue of the player has never been frozen. </summary>
        public DateTime FrozenOn {
            get { return frozenOn; }
            set {
                if( value != frozenOn ) {
                    frozenOn = value;
                    OnChanged( "FrozenOn" );
                }
            }
        }
        DateTime frozenOn;


        /// <summary> Name of the entity that most recently froze this player. May be null. </summary>
        [CanBeNull]
        public string FrozenBy {
            get { return frozenBy; }
            set {
                if( value != frozenBy ) {
                    frozenBy = value;
                    OnChanged( "FrozenBy" );
                }
            }
        }
        [CanBeNull]
        string frozenBy;


        /// <summary> Returns decorated name of FrozenBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string FrozenByClassy {
            get {
                return PlayerDB.FindExactClassyName( frozenBy );
            }
        }


        /// <summary> Whether this player is currently muted. Read-only, not serialized. </summary>
        public bool IsMuted {
            get {
                return DateTime.UtcNow < MutedUntil;
            }
        }


        /// <summary> Date until which the player is muted. If the date is in the past, player is NOT muted. </summary>
        public DateTime MutedUntil {
            get { return mutedUntil; }
            set {
                if( value != mutedUntil ) {
                    mutedUntil = value;
                    OnChanged( "MutedUntil" );
                }
            }
        }
        [CanBeNull]
        DateTime mutedUntil;


        /// <summary> Name of the entity that most recently muted this player. May be null. </summary>
        [CanBeNull]
        public string MutedBy {
            get { return mutedBy; }
            set {
                if( value != mutedBy ) {
                    mutedBy = value;
                    OnChanged( "MutedBy" );
                }
            }
        }
        [CanBeNull]
        string mutedBy;


        /// <summary> Returns decorated name of MutedBy player, or "?" if it was null or unknown player.
        /// Read-only, not serialized. </summary>
        [NotNull]
        public string MutedByClassy {
            get {
                return PlayerDB.FindExactClassyName( mutedBy );
            }
        }

        #endregion


        #region Session

        /// <summary> Whether the player is currently online.
        /// Another way to check online status is to check if PlayerObject is null. </summary>
        public bool IsOnline {
            get { return isOnline; }
            private set {
                if( value != isOnline ) {
                    isOnline = value;
                    OnChanged( "IsOnline" );
                }
            }
        }
        bool isOnline;


        /// <summary> If player is online, Player object associated with the session.
        /// If player is offline, null. </summary>
        [CanBeNull]
        public Player PlayerObject {
            get { return playerObject; }
            private set {
                if( value != playerObject ) {
                    playerObject = value;
                    OnChanged( "PlayerObject" );
                }
            }
        }
        Player playerObject;


        /// <summary> Whether the player is currently hidden.
        /// Use Player.CanSee() method to check visibility to specific observers. </summary>
        public bool IsHidden {
            get { return isHidden; }
            set {
                if( value != isHidden ) {
                    isHidden = value;
                    OnChanged( "IsHidden" );
                }
            }
        }
        bool isHidden;


        /// <summary> For offline players, last IP used to succesfully log in.
        /// For online players, current IP. </summary>
        [NotNull]
        public IPAddress LastIP {
            get { return lastIP; }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( value != lastIP ) {
                    lastIP = value;
                    OnChanged( "LastIP" );
                }
            }
        }
        IPAddress lastIP = IPAddress.None;

#endregion


        #region Unfinished / Not Implemented

        /// <summary> Not implemented (IRC/server password hash). </summary>
        [CanBeNull]
        public string Password { // TODO
            get { return password; }
            set {
                if( value != password ) {
                    password = value;
                    OnChanged( "Password" );
                }
            }
        }
        string password;


        /// <summary> Date/time of last modification to this PlayerInfo.
        /// Unlike other properties, setting LastModified does NOT raise PropertyChanged event. </summary>
        public DateTime LastModified { get; private set; }


        public BandwidthUseMode BandwidthUseMode { // TODO
            get { return bandwidthUseMode; }
            set {
                if( value != bandwidthUseMode ) {
                    bandwidthUseMode = value;
                    OnChanged( "BandwidthUseMode" );
                }
            }
        }
        BandwidthUseMode bandwidthUseMode;


        /// <summary> Not implemented (for temp bans). </summary>
        public DateTime BannedUntil { // TODO
            get { return bannedUntil; }
            set {
                if( value != bannedUntil ) {
                    bannedUntil = value;
                    OnChanged( "BannedUntil" );
                }
            }
        }
        DateTime bannedUntil;

        #endregion

        #endregion


        #region Constructors and Serialization

        internal PlayerInfo( int id ) {
            ID = id;
        }


        // fabricate info for an unrecognized player
        public PlayerInfo( [NotNull] string name, [NotNull] Rank rank,
                           bool setLoginDate, RankChangeType rankChangeType ){
            if( name == null ) throw new ArgumentNullException( "name" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            this.name = name;
            this.rank = rank;
            if( setLoginDate ) {
                firstLoginDate = DateTime.UtcNow;
                lastLoginDate = firstLoginDate;
                lastSeen = firstLoginDate;
                TimesVisited = 1;
            }
            this.rankChangeType = rankChangeType;
            LastModified = DateTime.UtcNow;
        }


        // generate blank info for a new player
        public PlayerInfo( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank ){
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            FirstLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            LastLoginDate = DateTime.UtcNow;
            Rank = startingRank;
            Name = name;
            ID = PlayerDB.GetNextID();
            LastIP = lastIP;
            LastModified = DateTime.UtcNow;
        }

        #endregion


        #region Loading

        internal static PlayerInfo LoadFormat2( string[] fields ) {
            int id = Int32.Parse( fields[29] );
            if( id < 256 ) id = PlayerDB.GetNextID();

            PlayerInfo info = new PlayerInfo( id ) { name = fields[0] };

            if( fields[1].Length > 0 ) {
                IPAddress.TryParse( fields[1], out info.lastIP );
            }

            info.rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTime( out info.rankChangeDate );
            if( fields[4].Length > 0 ) info.rankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.banStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.banStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.banStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTime( out info.banDate ) ) {
                if( fields[7].Length > 0 ) info.bannedBy = Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.banReason = Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTime( out info.unbanDate ) ) {
                if( fields[9].Length > 0 ) info.unbannedBy = Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.unbanReason = Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTime( out info.lastFailedLoginDate );

            if( fields[13].Length > 1 ){
                IPAddress.TryParse( fields[13], out info.lastFailedLoginIP );
            }
            // skip 14

            // login/logout dates
            fields[15].ToDateTime( out info.firstLoginDate );
            fields[16].ToDateTime( out info.lastLoginDate );
            fields[17].ToTimeSpan( out info.totalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.blocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.blocksDeleted );
            Int32.TryParse( fields[20], out info.timesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.messagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.previousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.rankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.timesKicked );
            Int32.TryParse( fields[27], out info.timesKickedOthers );
            Int32.TryParse( fields[28], out info.timesBannedOthers );
            // fields[29] is ID, read above

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.rankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTime( out info.lastKickDate );
            if( !fields[32].ToDateTime( out info.lastSeen ) || info.lastSeen < info.lastLoginDate ) {
                info.lastSeen = info.lastLoginDate;
            }
            Int64.TryParse( fields[33], out info.blocksDrawn );

            if( fields[34].Length > 0 ) info.lastKickBy = Unescape( fields[34] );
            if( fields[35].Length > 0 ) info.lastKickReason = Unescape( fields[35] );

            fields[36].ToDateTime( out info.bannedUntil );
            info.isFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.frozenBy = Unescape( fields[38] );
            fields[39].ToDateTime( out info.frozenOn );
            fields[40].ToDateTime( out info.mutedUntil );
            if( fields[41].Length > 0 ) info.mutedBy = Unescape( fields[41] );
            info.password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            byte bandwidthUseModeCode;
            if( Byte.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.bandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.bandwidthUseMode = BandwidthUseMode.Default;
                }
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.isHidden = false;
                } else {
                    info.isHidden = info.Rank.Can( Permission.Hide );
                }
            }
            if( fields.Length > 46 ) {
                DateTime tempLastModified;
                fields[46].ToDateTime( out tempLastModified );
                info.LastModified = tempLastModified;
            }
            if( fields.Length > 47 && fields[47].Length > 0 ) {
                info.displayedName = Unescape( fields[47] );
            }

            if( info.lastSeen < info.firstLoginDate ) {
                info.lastSeen = info.firstLoginDate;
            }
            if( info.lastLoginDate < info.firstLoginDate ) {
                info.lastLoginDate = info.firstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat1( string[] fields ) {
            int id = Int32.Parse( fields[29] );
            if( id < 256 ) id = PlayerDB.GetNextID();

            PlayerInfo info = new PlayerInfo(id) { name = fields[0] };

            if( fields[1].Length > 0){
                IPAddress.TryParse( fields[1], out info.lastIP );
            }

            info.rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTimeLegacy( out info.rankChangeDate );
            if( fields[4].Length > 0 ) info.rankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.banStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.banStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.banStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTimeLegacy( out info.banDate ) ) {
                if( fields[7].Length > 0 ) info.bannedBy = Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.banReason = Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTimeLegacy( out info.unbanDate ) ) {
                if( fields[9].Length > 0 ) info.unbannedBy = Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.unbanReason = Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTimeLegacy( out info.lastFailedLoginDate );

            if( fields[13].Length > 1 ){
                IPAddress.TryParse( fields[13], out info.lastFailedLoginIP );
            }
            // skip 14

            // login/logout times
            fields[15].ToDateTimeLegacy( out info.firstLoginDate );
            fields[16].ToDateTimeLegacy( out info.lastLoginDate );
            fields[17].ToTimeSpanLegacy( ref info.totalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.blocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.blocksDeleted );
            Int32.TryParse( fields[20], out info.timesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.messagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.timesKicked );
            Int32.TryParse( fields[27], out info.timesKickedOthers );
            Int32.TryParse( fields[28], out info.timesBannedOthers );
            // fields[29] is ID, read above

            int rankChangeTypeCode;
            if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.rankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTimeLegacy( out info.lastKickDate );
            if( !fields[32].ToDateTimeLegacy( out info.lastSeen ) || info.lastSeen < info.lastLoginDate ) {
                info.lastSeen = info.lastLoginDate;
            }
            Int64.TryParse( fields[33], out info.blocksDrawn );

            if( fields[34].Length > 0 ) info.lastKickBy = Unescape( fields[34] );
            if( fields[34].Length > 0 ) info.lastKickReason = Unescape( fields[35] );

            fields[36].ToDateTimeLegacy( out info.bannedUntil );
            info.isFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.frozenBy = Unescape( fields[38] );
            fields[39].ToDateTimeLegacy( out info.frozenOn );
            fields[40].ToDateTimeLegacy( out info.mutedUntil );
            if( fields[41].Length > 0 ) info.mutedBy = Unescape( fields[41] );
            info.password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            int bandwidthUseModeCode;
            if( Int32.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.bandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.bandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.bandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.isHidden = false;
                } else {
                    info.isHidden = info.Rank.Can( Permission.Hide );
                }
            }

            if( info.lastSeen < info.firstLoginDate ) {
                info.lastSeen = info.firstLoginDate;
            }
            if( info.lastLoginDate < info.firstLoginDate ) {
                info.lastLoginDate = info.firstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat0( string[] fields ) {
            DateTime tempDateTime;

            // get ID
            int id;
            if( fields.Length > 29 ) {
                if( !Int32.TryParse( fields[29], out id ) || id < 256 ) {
                    id = PlayerDB.GetNextID();
                }
            } else {
                id = PlayerDB.GetNextID();
            }

            PlayerInfo info = new PlayerInfo( id ) { name = fields[0] };

            if( fields[1].Length > 1 ) {
                IPAddress.TryParse( fields[1], out info.lastIP );
            }

            info.rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            DateTimeUtil.TryParseLocalDate( fields[3], out info.rankChangeDate );
            if( fields[4].Length > 0 ) {
                info.rankChangedBy = fields[4];
                if( info.rankChangedBy == "-" ) info.rankChangedBy = null;
            }

            switch( fields[5] ) {
                case "b":
                    info.banStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.banStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.banStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( DateTimeUtil.TryParseLocalDate( fields[6], out info.banDate ) ) {
                if( fields[7].Length > 0 ) info.bannedBy = fields[7];
                if( fields[10].Length > 0 ) {
                    info.banReason = UnescapeOldFormat( fields[10] );
                    if( info.banReason == "-" ) info.banReason = null;
                }
            }

            // unban information
            if( DateTimeUtil.TryParseLocalDate( fields[8], out info.unbanDate ) ) {
                if( fields[9].Length > 0 ) info.unbannedBy = fields[9];
                if( fields[11].Length > 0 ) {
                    info.unbanReason = UnescapeOldFormat( fields[11] );
                    if( info.unbanReason == "-" ) info.unbanReason = null;
                }
            }

            // failed logins
            if( fields[12].Length > 1 ) {
                DateTimeUtil.TryParseLocalDate( fields[12], out info.lastFailedLoginDate );
            }
            if( fields[13].Length > 1 ) {
                IPAddress.TryParse( fields[13], out info.lastFailedLoginIP );
            }
            // skip 14

            // login/logout times
            DateTimeUtil.TryParseLocalDate( fields[15], out tempDateTime );
            info.firstLoginDate = tempDateTime;
            DateTimeUtil.TryParseLocalDate( fields[16], out tempDateTime );
            info.lastLoginDate = tempDateTime;
            TimeSpan.TryParse( fields[17], out info.totalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.blocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.blocksDeleted );
            Int32.TryParse( fields[20], out info.timesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.messagesWritten );
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) info.previousRank = Rank.Parse( fields[24] );
                if( fields[25].Length > 0 ) info.rankChangeReason = UnescapeOldFormat( fields[25] );
                Int32.TryParse( fields[26], out info.timesKicked );
                Int32.TryParse( fields[27], out info.timesKickedOthers );
                Int32.TryParse( fields[28], out info.timesBannedOthers );
                // fields[29] (id) already read/assigned by this point
                if( fields.Length > 29 ) {
                    int rankChangeTypeCode;
                    if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                        info.rankChangeType = (RankChangeType)rankChangeTypeCode;
                        if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                            info.GuessRankChangeType();
                        }
                    } else {
                        info.GuessRankChangeType();
                    }
                    DateTimeUtil.TryParseLocalDate( fields[31], out info.lastKickDate );
                    if( !DateTimeUtil.TryParseLocalDate( fields[32], out info.lastSeen ) || info.lastSeen < info.lastLoginDate ) {
                        info.lastSeen = info.lastLoginDate;
                    }
                    Int64.TryParse( fields[33], out info.blocksDrawn );

                    if( fields[34].Length > 0 ) info.lastKickBy = UnescapeOldFormat( fields[34] );
                    if( fields[35].Length > 0 ) info.lastKickReason = UnescapeOldFormat( fields[35] );

                } else {
                    info.GuessRankChangeType();
                    info.lastSeen = info.lastLoginDate;
                }

                if( fields.Length > 36 ) {
                    DateTimeUtil.TryParseLocalDate( fields[36], out info.bannedUntil );
                    info.isFrozen = (fields[37] == "f");
                    if( fields[38].Length > 0 ) info.frozenBy = UnescapeOldFormat( fields[38] );
                    DateTimeUtil.TryParseLocalDate( fields[39], out info.frozenOn );
                    DateTimeUtil.TryParseLocalDate( fields[40], out info.mutedUntil );
                    if( fields[41].Length > 0 ) info.mutedBy = UnescapeOldFormat( fields[41] );
                    info.password = UnescapeOldFormat( fields[42] );
                    // fields[43] is "online", and is ignored
                }

                if( fields.Length > 44 ) {
                    if( fields[44].Length != 0 ) {
                        info.bandwidthUseMode = (BandwidthUseMode)Int32.Parse( fields[44] );
                    }
                }
            }

            if( info.lastSeen < info.firstLoginDate ) {
                info.lastSeen = info.firstLoginDate;
            }
            if( info.lastLoginDate < info.firstLoginDate ) {
                info.lastLoginDate = info.firstLoginDate;
            }

            if( info.rankChangeDate != DateTime.MinValue ) info.rankChangeDate = info.rankChangeDate.ToUniversalTime();
            if( info.banDate != DateTime.MinValue ) info.banDate = info.banDate.ToUniversalTime();
            if( info.unbanDate != DateTime.MinValue ) info.unbanDate = info.unbanDate.ToUniversalTime();
            if( info.lastFailedLoginDate != DateTime.MinValue ) info.lastFailedLoginDate = info.lastFailedLoginDate.ToUniversalTime();
            if( info.firstLoginDate != DateTime.MinValue ) info.firstLoginDate = info.firstLoginDate.ToUniversalTime();
            if( info.lastLoginDate != DateTime.MinValue ) info.lastLoginDate = info.lastLoginDate.ToUniversalTime();
            if( info.lastKickDate != DateTime.MinValue ) info.lastKickDate = info.lastKickDate.ToUniversalTime();
            if( info.lastSeen != DateTime.MinValue ) info.lastSeen = info.lastSeen.ToUniversalTime();
            if( info.bannedUntil != DateTime.MinValue ) info.bannedUntil = info.bannedUntil.ToUniversalTime();
            if( info.frozenOn != DateTime.MinValue ) info.frozenOn = info.frozenOn.ToUniversalTime();
            if( info.mutedUntil != DateTime.MinValue ) info.mutedUntil = info.mutedUntil.ToUniversalTime();

            return info;
        }


        void GuessRankChangeType() {
            if( PreviousRank != null ) {
                if( RankChangeReason == "~AutoRank" || RankChangeReason == "~AutoRankAll" || RankChangeReason == "~MassRank" ) {
                    if( PreviousRank > Rank ) {
                        rankChangeType = RankChangeType.AutoDemoted;
                    } else if( PreviousRank < Rank ) {
                        rankChangeType = RankChangeType.AutoPromoted;
                    }
                } else {
                    if( PreviousRank > Rank ) {
                        rankChangeType = RankChangeType.Demoted;
                    } else if( PreviousRank < Rank ) {
                        rankChangeType = RankChangeType.Promoted;
                    }
                }
            } else {
                rankChangeType = RankChangeType.Default;
            }
        }


        internal static PlayerInfo LoadBinaryFormat0( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            int id = Read7BitEncodedInt( reader );
            // ReSharper disable UseObjectOrCollectionInitializer
            PlayerInfo info = new PlayerInfo( id );
            // ReSharper restore UseObjectOrCollectionInitializer

            // General
            info.name = reader.ReadString();
            info.displayedName = ReadString( reader );
            info.lastSeen = DateTimeUtil.ToDateTime( reader.ReadUInt32() );

            // Rank
            int rankIndex = Read7BitEncodedInt( reader );
            info.rank = PlayerDB.GetRankByIndex(rankIndex);
            {
                bool hasPrevRank = reader.ReadBoolean();
                if( hasPrevRank ) {
                    int prevRankIndex = Read7BitEncodedInt( reader );
                    info.rank = PlayerDB.GetRankByIndex( prevRankIndex );
                }
            }
            info.rankChangeType = (RankChangeType)reader.ReadByte();
            if( info.rankChangeType != RankChangeType.Default ) {
                info.rankChangeDate = ReadDate( reader );
                info.rankChangedBy = ReadString( reader );
                info.rankChangeReason = ReadString( reader );
            }

            // Bans
            info.banStatus = (BanStatus)reader.ReadByte();
            info.banDate = ReadDate( reader );
            info.bannedBy = ReadString( reader );
            info.banReason = ReadString( reader );
            if( info.banStatus == BanStatus.Banned ) {
                info.bannedUntil = ReadDate( reader );
                info.lastFailedLoginDate = ReadDate( reader );
                info.lastFailedLoginIP = new IPAddress( reader.ReadBytes( 4 ) );
            } else {
                info.unbanDate = ReadDate( reader );
                info.unbannedBy = ReadString( reader );
                info.unbanReason = ReadString( reader );
            }

            // Stats
            info.firstLoginDate = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.lastLoginDate = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.totalTime = new TimeSpan( reader.ReadUInt32() * TimeSpan.TicksPerSecond );
            info.blocksBuilt = Read7BitEncodedInt( reader );
            info.blocksDeleted = Read7BitEncodedInt( reader );
            if( reader.ReadBoolean() ) {
                info.blocksDrawn = reader.ReadInt64();
            }
            info.timesVisited = Read7BitEncodedInt( reader );
            info.messagesWritten = Read7BitEncodedInt( reader );
            info.timesKickedOthers = Read7BitEncodedInt( reader );
            info.timesBannedOthers = Read7BitEncodedInt( reader );

            // Kicks
            info.timesKicked = Read7BitEncodedInt( reader );
            if( info.timesKicked > 0 ) {
                info.lastKickDate = ReadDate( reader );
                info.lastKickBy = ReadString( reader );
                info.lastKickReason = ReadString( reader );
            }

            // Freeze/Mute
            info.isFrozen = reader.ReadBoolean();
            if( info.isFrozen ) {
                info.frozenOn = ReadDate( reader );
                info.frozenBy = ReadString( reader );
            }
            info.mutedUntil = ReadDate( reader );
            if( info.mutedUntil != DateTime.MinValue ) {
                info.mutedBy = ReadString( reader );
            }

            // Misc
            info.password = ReadString( reader );
            info.LastModified = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.isOnline = reader.ReadBoolean();
            info.isHidden = reader.ReadBoolean();
            info.lastIP = new IPAddress( reader.ReadBytes( 4 ) );
            info.leaveReason = (LeaveReason)reader.ReadByte();
            info.bandwidthUseMode = (BandwidthUseMode)reader.ReadByte();

            return info;
        }


        static DateTime ReadDate( [NotNull] BinaryReader reader ) {
            if( reader.ReadBoolean() ) {
                return DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            } else {
                return DateTime.MinValue;
            }
        }


        static string ReadString( [NotNull] BinaryReader reader ) {
            if( reader.ReadBoolean() ) {
                return reader.ReadString();
            } else {
                return null;
            }
        }


        static int Read7BitEncodedInt( [NotNull] BinaryReader reader ) {
            byte num3;
            int num = 0;
            int num2 = 0;
            do {
                if( num2 == 0x23 ) {
                    throw new FormatException( "Invalid 7bit encoded integer." );
                }
                num3 = reader.ReadByte();
                num |= (num3 & 0x7f) << num2;
                num2 += 7;
            }
            while( (num3 & 0x80) != 0 );
            return num;
        }

        #endregion


        #region Saving

        internal void Serialize( StringBuilder sb ) {
            sb.Append( name ).Append( ',' ); // 0
            if( !lastIP.Equals( IPAddress.None ) ) sb.Append( lastIP ); // 1
            sb.Append( ',' );

            sb.Append( rank.FullName ).Append( ',' ); // 2
            RankChangeDate.ToUnixTimeString( sb ).Append( ',' ); // 3

            sb.AppendEscaped( rankChangedBy ).Append( ',' ); // 4

            switch( banStatus ) {
                case BanStatus.Banned:
                    sb.Append( 'b' );
                    break;
                case BanStatus.IPBanExempt:
                    sb.Append( 'x' );
                    break;
            }
            sb.Append( ',' ); // 5

            banDate.ToUnixTimeString( sb ).Append( ',' ); // 6
            sb.AppendEscaped( bannedBy ).Append( ',' ); // 7
            unbanDate.ToUnixTimeString( sb ).Append( ',' ); // 8
            sb.AppendEscaped( unbannedBy ).Append( ',' ); // 9
            sb.AppendEscaped( banReason ).Append( ',' ); // 10
            sb.AppendEscaped( unbanReason ).Append( ',' ); // 11

            lastFailedLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 12

            if( !lastFailedLoginIP.Equals( IPAddress.None ) ) sb.Append( lastFailedLoginIP.ToString() ); // 13
            sb.Append( ',', 2 ); // skip 14

            firstLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 15
            lastLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 16

            if( isOnline ) {
                (TotalTime.Add( TimeSinceLastLogin )).ToTickString( sb ).Append( ',' ); // 17
            } else {
                TotalTime.ToTickString( sb ).Append( ',' ); // 17
            }

            if( blocksBuilt > 0 ) sb.Digits( blocksBuilt ); // 18
            sb.Append( ',' );

            if( blocksDeleted > 0 ) sb.Digits( blocksDeleted ); // 19
            sb.Append( ',' );

            sb.Append( timesVisited ).Append( ',' ); // 20


            if( messagesWritten > 0 ) sb.Digits( messagesWritten ); // 21
            sb.Append( ',', 3 ); // 22-23 no longer in use

            if( previousRank != null ) sb.Append( previousRank.FullName ); // 24
            sb.Append( ',' );

            sb.AppendEscaped( rankChangeReason ).Append( ',' ); // 25


            if( timesKicked > 0 ) sb.Digits( timesKicked ); // 26
            sb.Append( ',' );

            if( timesKickedOthers > 0 ) sb.Digits( timesKickedOthers ); // 27
            sb.Append( ',' );

            if( timesBannedOthers > 0 ) sb.Digits( timesBannedOthers ); // 28
            sb.Append( ',' );


            sb.Digits( ID ).Append( ',' ); // 29

            sb.Digits( (int)rankChangeType ).Append( ',' ); // 30


            lastKickDate.ToUnixTimeString( sb ).Append( ',' ); // 31

            if( isOnline ) DateTime.UtcNow.ToUnixTimeString( sb ); // 32
            else lastSeen.ToUnixTimeString( sb );
            sb.Append( ',' );

            if( blocksDrawn > 0 ) sb.Append( blocksDrawn ); // 33
            sb.Append( ',' );

            sb.AppendEscaped( lastKickBy ).Append( ',' ); // 34
            sb.AppendEscaped( lastKickReason ).Append( ',' ); // 35

            bannedUntil.ToUnixTimeString( sb ); // 36

            if( isFrozen ) {
                sb.Append( ',' ).Append( 'f' ).Append( ',' ); // 37
                sb.AppendEscaped( frozenBy ).Append( ',' ); // 38
                frozenOn.ToUnixTimeString( sb ).Append( ',' ); // 39
            } else {
                sb.Append( ',', 4 ); // 37-39
            }

            if( mutedUntil > DateTime.UtcNow ) {
                mutedUntil.ToUnixTimeString( sb ).Append( ',' ); // 40
                sb.AppendEscaped( mutedBy ).Append( ',' ); // 41
            } else {
                sb.Append( ',', 2 ); // 40-41
            }

            sb.AppendEscaped( password ).Append( ',' ); // 42

            if( isOnline ) sb.Append( 'o' ); // 43
            sb.Append( ',' );

            if( bandwidthUseMode != BandwidthUseMode.Default ) sb.Append( (int)bandwidthUseMode ); // 44
            sb.Append( ',' );

            if( isHidden ) sb.Append( 'h' ); // 45

            sb.Append( ',' );
            LastModified.ToUnixTimeString( sb ); // 46

            sb.Append( ',' );
            sb.AppendEscaped( displayedName ); // 47
        }


        internal void SaveBinaryFormat0( [NotNull] BinaryWriter writer ) {
            if( writer == null ) throw new ArgumentNullException( "writer" );
            // General
            writer.Write( name ); // 0
            WriteString( writer, displayedName ); // 1
            Write7BitEncodedInt( writer, ID ); // 2
            if( isOnline ) {
                writer.Write( (uint)DateTime.UtcNow.ToUnixTime() ); // 5
            } else {
                writer.Write( (uint)lastSeen.ToUnixTime() ); // 5
            }

            // Rank
            Write7BitEncodedInt( writer, rank.Index ); // 7
            {
                bool hasPrevRank = (previousRank != null); // 8 prefix
                writer.Write( hasPrevRank );
                if( hasPrevRank ) {
                    Write7BitEncodedInt( writer, previousRank.Index ); // 8
                }
            }
            writer.Write( (byte)rankChangeType ); // 12
            if( rankChangeType != RankChangeType.Default ) {
                WriteDate( writer, rankChangeDate ); // 9
                WriteString( writer, rankChangedBy ); // 10
                WriteString( writer, rankChangeReason ); // 11
            }

            // Bans
            writer.Write( (byte)banStatus ); // 13
            WriteDate( writer, banDate ); // 14
            WriteString( writer, bannedBy ); // 15
            WriteString( writer, banReason ); // 16
            if( banStatus == BanStatus.Banned ) {
                WriteDate( writer, bannedUntil ); // 14
                WriteDate( writer, lastFailedLoginDate ); // 20
                writer.Write( lastFailedLoginIP.GetAddressBytes() ); // 21
            } else {
                WriteDate( writer, unbanDate ); // 17
                WriteString( writer, unbannedBy ); // 18
                WriteString( writer, unbanReason ); // 18
            }

            // Stats
            writer.Write( (uint)firstLoginDate.ToUnixTime() ); // 3
            writer.Write( (uint)lastLoginDate.ToUnixTime() ); // 4
            if( isOnline ) {
                writer.Write( (uint)totalTime.Add( TimeSinceLastLogin ).ToSeconds() ); // 22
            } else {
                writer.Write( (uint)totalTime.ToSeconds() ); // 22
            }
            Write7BitEncodedInt( writer, blocksBuilt ); // 23
            Write7BitEncodedInt( writer, blocksDeleted ); // 24
            {
                bool hasBlocksDrawn = (blocksDrawn > 0); // 25 prefix
                writer.Write( hasBlocksDrawn );
                if( hasBlocksDrawn ) {
                    writer.Write( blocksDrawn ); // 25
                }
            }
            Write7BitEncodedInt( writer, timesVisited ); // 26
            Write7BitEncodedInt( writer, messagesWritten ); // 27
            Write7BitEncodedInt( writer, timesKickedOthers ); // 28
            Write7BitEncodedInt( writer, timesBannedOthers ); // 29

            // Kicks
            Write7BitEncodedInt( writer, timesKicked ); // 30
            if( TimesKicked > 0 ) {
                WriteDate( writer, lastKickDate ); // 31
                WriteString( writer, lastKickBy ); // 32
                WriteString( writer, lastKickReason ); // 33
            }

            // Freeze/Mute
            writer.Write( isFrozen ); // 34
            if( isFrozen ) {
                WriteDate( writer, frozenOn ); // 35
                WriteString( writer, frozenBy ); // 36
            }
            WriteDate( writer, mutedUntil ); // 37
            if( mutedUntil != DateTime.MinValue ) {
                WriteString( writer, mutedBy ); // 38
            }

            // Misc
            WriteString( writer, password ); // 39
            writer.Write( (uint)LastModified.ToUnixTime() ); // 40
            writer.Write( isOnline ); // 41
            writer.Write( isHidden ); // 42
            writer.Write( lastIP.GetAddressBytes() ); // 43
            writer.Write( (byte)leaveReason ); // 44
            writer.Write( (byte)bandwidthUseMode ); // 45
        }


        static void WriteDate( [NotNull] BinaryWriter writer, DateTime dateTime ) {
            bool hasDate = (dateTime != DateTime.MinValue);
            writer.Write( hasDate );
            if( hasDate ) {
                writer.Write( (uint)dateTime.ToUnixTime() );
            }
        }


        static void WriteString( [NotNull] BinaryWriter writer, [CanBeNull] string str ) {
            bool hasString = (str != null);
            writer.Write( hasString );
            if( hasString ) {
                writer.Write( str );
            }
        }


        static void Write7BitEncodedInt( [NotNull] BinaryWriter writer, int value ) {
            uint num = (uint)value;
            while( num >= 0x80 ) {
                writer.Write( (byte)(num | 0x80) );
                num = num >> 7;
            }
            writer.Write( (byte)num );
        }

        #endregion


        #region Update Handlers

        public void ProcessMessageWritten() {
            lock( actionLock ) {
                MessagesWritten++;
            }
        }


        public void ProcessLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( actionLock ) {
                LastIP = player.IP;
                LastLoginDate = DateTime.UtcNow;
                LastSeen = DateTime.UtcNow;
                TimesVisited++;
                IsOnline = true;
                PlayerObject = player;
            }
        }


        public void ProcessFailedLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            LastFailedLoginDate = DateTime.UtcNow;
            LastFailedLoginIP = player.IP;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessLogout( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            TotalTime += player.LastActiveTime.Subtract( player.LoginTime );
            LastSeen = DateTime.UtcNow;
            IsOnline = false;
            PlayerObject = null;
            LeaveReason = player.LeaveReason;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessRankChange( [NotNull] Rank newRank, [NotNull] string changer, [CanBeNull] string reason, RankChangeType type ) {
            if( newRank == null ) throw new ArgumentNullException( "newRank" );
            if( changer == null ) throw new ArgumentNullException( "changer" );
            PreviousRank = Rank;
            Rank = newRank;
            RankChangeDate = DateTime.UtcNow;

            RankChangedBy = changer;
            RankChangeReason = reason;
            RankChangeType = type;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessBlockPlaced( Block type ) {
            lock( actionLock ) {
                if( type == Block.Air ) {
                    BlocksDeleted++;
                } else {
                    blocksBuilt++;
                }
            }
        }


        public void ProcessDrawCommand( int blocksToAdd ) {
            lock( actionLock ) {
                blocksDrawn += blocksToAdd;
            }
        }


        internal void ProcessKick( [NotNull] Player kickedBy, [CanBeNull] string reason ) {
            if( kickedBy == null ) throw new ArgumentNullException( "kickedBy" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            lock( actionLock ) {
                TimesKicked++;
                lock( kickedBy.Info.actionLock ) {
                    kickedBy.Info.TimesKickedOthers++;
                }
                LastKickDate = DateTime.UtcNow;
                LastKickBy = kickedBy.Name;
                LastKickReason = reason;
                Unfreeze();
            }
        }

        #endregion


        #region Utilities

        public static string Escape( [CanBeNull] string str ) {
            if( String.IsNullOrEmpty( str ) ) {
                return "";
            } else if( str.IndexOf( ',' ) > -1 ) {
                return str.Replace( ',', '\xFF' );
            } else {
                return str;
            }
        }


        public static string UnescapeOldFormat( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            return str.Replace( '\xFF', ',' ).Replace( "\'", "'" ).Replace( @"\\", @"\" );
        }


        public static string Unescape( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.IndexOf( '\xFF' ) > -1 ) {
                return str.Replace( '\xFF', ',' );
            } else {
                return str;
            }
        }


        // implements IClassy interface
        public string ClassyName {
            get {
                StringBuilder sb = new StringBuilder();
                if( ConfigKey.RankColorsInChat.Enabled() ) {
                    sb.Append( Rank.Color );
                }
                if( DisplayedName != null ) {
                    sb.Append( DisplayedName );
                } else {
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        sb.Append( Rank.Prefix );
                    }
                    sb.Append( Name );
                }
                if( IsBanned ) {
                    sb.Append( Color.Red ).Append( '*' );
                } else if( IsFrozen ) {
                    sb.Append( Color.Blue ).Append( '*' );
                }
                return sb.ToString();
            }
        }

        #endregion


        #region TimeSince_____ shortcuts

        public TimeSpan TimeSinceRankChange {
            get { return DateTime.UtcNow.Subtract( RankChangeDate ); }
        }

        public TimeSpan TimeSinceBan {
            get { return DateTime.UtcNow.Subtract( BanDate ); }
        }

        public TimeSpan TimeSinceUnban {
            get { return DateTime.UtcNow.Subtract( UnbanDate ); }
        }

        public TimeSpan TimeSinceFirstLogin {
            get { return DateTime.UtcNow.Subtract( FirstLoginDate ); }
        }

        public TimeSpan TimeSinceLastLogin {
            get { return DateTime.UtcNow.Subtract( LastLoginDate ); }
        }

        public TimeSpan TimeSinceLastKick {
            get { return DateTime.UtcNow.Subtract( LastKickDate ); }
        }

        public TimeSpan TimeSinceLastSeen {
            get { return DateTime.UtcNow.Subtract( LastSeen ); }
        }

        public TimeSpan TimeSinceFrozen {
            get { return DateTime.UtcNow.Subtract( FrozenOn ); }
        }

        public TimeSpan TimeMutedLeft {
            get { return MutedUntil.Subtract( DateTime.UtcNow ); }
        }

        public TimeSpan TimeSinceLastModified {
            get { return DateTime.UtcNow.Subtract( LastModified ); }
        }

        #endregion


        public bool Can( Permission permission ) {
            return Rank.Can( permission );
        }

        public bool Can( Permission permission, Rank targetRank ) {
            return Rank.Can( permission, targetRank );
        }


        void OnChanged( string propertyName ) {
            changed = true;
            LastModified = DateTime.UtcNow;
            var h = PropertyChanged;
            if( h != null ) h( this, new PropertyChangedEventArgs( propertyName ) );
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public override string ToString() {
            return String.Format( "PlayerInfo({0},{1})", Name, Rank.Name );
        }
    }


    public sealed class PlayerInfoComparer : IComparer<PlayerInfo> {
        readonly Player observer;

        public PlayerInfoComparer( Player observer ) {
            this.observer = observer;
        }

        public int Compare( PlayerInfo x, PlayerInfo y ) {
            Player xPlayer = x.PlayerObject;
            Player yPlayer = y.PlayerObject;
            bool xIsOnline = xPlayer != null && observer.CanSee( xPlayer );
            bool yIsOnline = yPlayer != null && observer.CanSee( yPlayer );

            if( !xIsOnline && yIsOnline ) {
                return 1;
            } else if( xIsOnline && !yIsOnline ) {
                return -1;
            }

            if( x.Rank == y.Rank ) {
                return Math.Sign( y.LastSeen.Ticks - x.LastSeen.Ticks );
            } else {
                return x.Rank.Index - y.Rank.Index;
            }
        }
    }
}