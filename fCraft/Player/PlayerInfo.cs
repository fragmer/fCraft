﻿// Copyright 2009, 2010 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using System.Net;
using System.Threading;


namespace fCraft {
    public sealed class PlayerInfo {

        public const int MinFieldCount = 24,
                         MaxFieldCount = 36;


        public string name;
        public IPAddress lastIP = IPAddress.None;
        public Rank rank;
        public DateTime rankChangeDate = DateTime.MinValue;
        public string rankChangedBy = "";

        public bool banned = false;
        public DateTime banDate = DateTime.MinValue;
        public string bannedBy = "";
        public DateTime unbanDate = DateTime.MinValue;
        public string unbannedBy = "";
        public string banReason = "";
        public string unbanReason = "";

        public DateTime lastFailedLoginDate = DateTime.MinValue;
        public IPAddress lastFailedLoginIP = IPAddress.None;
        public int failedLoginCount;
        public DateTime firstLoginDate = DateTime.MinValue;
        public DateTime lastLoginDate = DateTime.MinValue;

        public TimeSpan totalTime = TimeSpan.Zero;
        public int blocksBuilt;
        public int blocksDeleted;
        public int timesVisited;
        public int linesWritten;

        public Rank previousRank = null;
        public string rankChangeReason = "";
        public int timesKicked;
        public int timesKickedOthers;
        public int timesBannedOthers;

        public int ID;
        public RankChangeType rankChangeType = RankChangeType.Default;
        public DateTime lastKickDate = DateTime.MinValue;
        public DateTime lastSeen = DateTime.MinValue;
        public long blocksDrawn;

        public string lastKickBy = "";
        public string lastKickReason = "";


        // === Serialization & Defaults =======================================
        // fabricate info for a player
        public PlayerInfo( string _name, Rank _rank, bool setLoginDate, RankChangeType _rankChangeType ) {
            name = _name;
            rank = _rank;
            if( setLoginDate ) {
                firstLoginDate = DateTime.Now;
                lastLoginDate = firstLoginDate;
                lastSeen = firstLoginDate;
                timesVisited = 1;
            }
            rankChangeType = _rankChangeType;
        }


        // generate info for a new player
        public PlayerInfo( Player player ) {
            name = player.name;
            lastIP = player.session.GetIP();
            rank = RankList.DefaultRank;
            firstLoginDate = DateTime.Now;
            lastLoginDate = firstLoginDate;
            lastSeen = firstLoginDate;
            timesVisited = 1;
        }


        // load info from file
        public PlayerInfo( string[] fields ) {
            name = fields[0];
            if( fields[1] == "" || !IPAddress.TryParse( fields[1], out lastIP ) ) { // LEGACY
                lastIP = IPAddress.None;
            }

            rank = RankList.ParseRank( fields[2] );
            if( rank == null ) {
                rank = RankList.DefaultRank;
                Logger.Log( "PlayerInfo: Could not parse class for player {0}. Setting to default ({1}).", LogType.Error, name, rank.Name );
            }
            if( fields[3] != "-" && fields[3] != "" ) rankChangeDate = DateTime.Parse( fields[3] ); // LEGACY
            rankChangedBy = fields[4];
            if( rankChangedBy == "-" ) rankChangedBy = "";

            banned = (fields[5] == "b");

            // ban information
            if( fields[6] != "-" && fields[6] != "" && DateTime.TryParse( fields[6], out banDate ) ) {
                banDate = DateTime.Parse( fields[6] ); // LEGACY
                bannedBy = fields[7];
                banReason = Unescape( fields[10] );
                if( banReason == "-" ) banReason = "";
            }

            // unban information
            if( fields[8] != "-" && fields[8] != "" && DateTime.TryParse( fields[8], out unbanDate ) ) {
                unbanDate = DateTime.Parse( fields[8] ); // LEGACY
                unbannedBy = fields[9];
                unbanReason = Unescape( fields[11] );
                if( unbanReason == "-" ) unbanReason = "";
            }

            // failed logins
            if( fields[12] != "-" && fields[12] != "" ) lastFailedLoginDate = DateTime.Parse( fields[12] ); // LEGACY
            if( fields[13] == "-" || fields[13] == "" || !IPAddress.TryParse( fields[13], out lastFailedLoginIP ) ) { // LEGACY
                lastFailedLoginIP = IPAddress.None;
            }
            failedLoginCount = Int32.Parse( fields[14] );
            firstLoginDate = DateTime.Parse( fields[15] );

            // login/logout times
            lastLoginDate = DateTime.Parse( fields[16] );
            totalTime = TimeSpan.Parse( fields[17] );

            // stats
            Int32.TryParse( fields[18], out blocksBuilt );
            Int32.TryParse( fields[19], out blocksDeleted );
            Int32.TryParse( fields[20], out timesVisited );
            Int32.TryParse( fields[21], out linesWritten );
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) previousRank = RankList.ParseRank( fields[24] );
                if( fields[25].Length > 0 ) rankChangeReason = Unescape( fields[25] );
                Int32.TryParse( fields[26], out timesKicked );
                Int32.TryParse( fields[27], out timesKickedOthers );
                Int32.TryParse( fields[28], out timesBannedOthers );
                if( fields.Length > 29 ) {
                    ID = Int32.Parse( fields[29] );
                    if( ID < 256 ) ID = PlayerDB.GetNextID();
                    int rankChangeTypeCode;
                    if( Int32.TryParse( fields[30], out rankChangeTypeCode ) ) {
                        rankChangeType = (RankChangeType)rankChangeTypeCode;
                        if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                            GuessRankChangeType();
                        }
                    } else {
                        GuessRankChangeType();
                    }
                    DateTime.TryParse( fields[31], out lastKickDate );
                    if( !DateTime.TryParse( fields[32], out lastSeen ) || lastSeen < lastLoginDate ) {
                        lastSeen = lastLoginDate;
                    }
                    Int64.TryParse( fields[33], out blocksDrawn );

                    lastKickBy = fields[34];
                    lastKickReason = fields[35];

                } else {
                    ID = PlayerDB.GetNextID();
                    GuessRankChangeType();
                    lastSeen = lastLoginDate;
                }
            }
        }


        void GuessRankChangeType() {
            if( previousRank != null ) {
                if( rankChangeReason == "~AutoRank" || rankChangeReason == "~AutoRankAll" ) {
                    if( previousRank > rank ) {
                        rankChangeType = RankChangeType.AutoDemoted;
                    } else if( previousRank < rank ) {
                        rankChangeType = RankChangeType.AutoPromoted;
                    }
                } else {
                    if( previousRank > rank ) {
                        rankChangeType = RankChangeType.Demoted;
                    } else if( previousRank < rank ) {
                        rankChangeType = RankChangeType.Promoted;
                    }
                }
            } else {
                rankChangeType = RankChangeType.Default;
            }
        }

        // save to file
        public string Serialize() {
            string[] fields = new string[MaxFieldCount];

            fields[0] = name;
            if( lastIP.ToString() != IPAddress.None.ToString() ) {
                fields[1] = lastIP.ToString();
            } else {
                fields[1] = "";
            }

            fields[2] = rank.ToString();
            if( rankChangeDate == DateTime.MinValue ) fields[3] = "";
            else fields[3] = rankChangeDate.ToCompactString();
            fields[4] = rankChangedBy;

            if( banned ) fields[5] = "b";
            else fields[5] = "";
            if( banDate == DateTime.MinValue ) fields[6] = "";
            else fields[6] = banDate.ToCompactString();
            fields[7] = bannedBy;
            if( unbanDate == DateTime.MinValue ) fields[8] = "";
            else fields[8] = unbanDate.ToCompactString();
            fields[9] = unbannedBy;
            fields[10] = Escape( banReason );
            fields[11] = Escape( unbanReason );

            if( lastFailedLoginDate == DateTime.MinValue ) fields[12] = "";
            else fields[12] = lastFailedLoginDate.ToCompactString();
            if( lastFailedLoginIP == IPAddress.None ) fields[13] = "";
            else fields[13] = lastFailedLoginIP.ToString();
            fields[14] = failedLoginCount.ToString();

            fields[15] = firstLoginDate.ToCompactString();
            fields[16] = lastLoginDate.ToCompactString();
            fields[17] = totalTime.ToString();

            fields[18] = blocksBuilt.ToString();
            fields[19] = blocksDeleted.ToString();
            fields[20] = timesVisited.ToString();
            fields[21] = linesWritten.ToString();

            // fields 22-23 are no longer in use
            fields[22] = "";
            fields[23] = "";

            if( previousRank != null ) fields[24] = previousRank.ToString();
            else fields[24] = "";
            fields[25] = Escape( rankChangeReason );
            fields[26] = timesKicked.ToString();
            fields[27] = timesKickedOthers.ToString();
            fields[28] = timesBannedOthers.ToString();
            fields[29] = ID.ToString();
            fields[30] = ((int)rankChangeType).ToString();
            fields[31] = lastKickDate.ToCompactString();
            fields[32] = lastSeen.ToCompactString();
            fields[33] = blocksDrawn.ToString();

            fields[34] = lastKickBy;
            fields[35] = lastKickReason;
            return String.Join( ",", fields );
        }


        // === Updating =======================================================

        // update information
        public void ProcessLogin( Player player ) {
            name = player.name;
            lastIP = player.session.GetIP();
            lastLoginDate = DateTime.Now;
            lastSeen = DateTime.Now;
            Interlocked.Increment( ref timesVisited );
        }


        public void ProcessFailedLogin( Player player ) {
            lastFailedLoginDate = DateTime.Now;
            lastFailedLoginIP = player.session.GetIP();
            Interlocked.Increment( ref failedLoginCount );
        }


        public void ProcessLogout( Player player ) {
            totalTime += DateTime.Now.Subtract( player.session.loginTime );
            lastSeen = DateTime.Now;
        }


        public bool ProcessBan( Player _bannedBy, string _banReason ) {
            if( !banned ) {
                banned = true;
                bannedBy = _bannedBy.name;
                banDate = DateTime.Now;
                banReason = _banReason;
                Interlocked.Increment( ref _bannedBy.info.timesBannedOthers );
                return true;
            } else {
                return false;
            }
        }


        public bool ProcessUnban( string _unbannedBy, string _unbanReason ) {
            if( banned ) {
                banned = false;
                unbannedBy = _unbannedBy;
                unbanDate = DateTime.Now;
                unbanReason = _unbanReason;
                return true;
            } else {
                return false;
            }
        }


        public void ProcessRankChange( Rank newClass, Player changer, string reason ) {
            previousRank = rank;
            rank = newClass;
            rankChangeDate = DateTime.Now;
            rankChangedBy = changer.name;
            rankChangeReason = reason;
        }


        public void ProcessBlockPlaced( byte type ) {
            if( type == 0 ) { // air
                Interlocked.Increment( ref blocksDeleted );
            } else {
                Interlocked.Increment( ref blocksBuilt );
            }
        }


        public void ProcessDrawCommand( int _blocksDrawn ) {
            Interlocked.Add( ref blocksDrawn, _blocksDrawn );
        }


        public void ProcessKick( Player kickedBy, string reason ) {
            Interlocked.Increment( ref timesKicked );
            Interlocked.Increment( ref kickedBy.info.timesKickedOthers );
            lastKickDate = DateTime.Now;
            lastKickBy = kickedBy.name;
            if( reason != null ) lastKickReason = reason;
            else lastKickReason = "";
        }


        // === Utils ==========================================================

        public static string Escape( string str ) {
            return str.Replace( "\\", "\\\\" ).Replace( "'", "\\'" ).Replace( ',', '\xFF' );
        }

        public static string Unescape( string str ) {
            return str.Replace( '\xFF', ',' ).Replace( "\\'", "'" ).Replace( "\\\\", "\\" );
        }

        public string GetClassyName() {
            string displayedName = name;
            if( Config.GetBool( ConfigKey.RankPrefixesInChat ) ) {
                displayedName = rank.Prefix + displayedName;
            }
            if( Config.GetBool( ConfigKey.RankColorsInChat ) ) {
                displayedName = rank.Color + displayedName;
                if( name == "fragmer" ) return "&4f&cr&ea&ag&bm&9e&5r";
                if( name == "Kirshi" ) return "&bKir&dshi";
            }
            return displayedName;
        }


        public static string PlayerArrayToString( PlayerInfo[] list ) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            for( int i = 0; i < list.Length; i++ ) {
                if( !first ) sb.Append( "&S, " );
                sb.Append( list[i].GetClassyName() );
                first = false;
            }
            return sb.ToString();
        }
    }
}