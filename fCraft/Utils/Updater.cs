﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using fCraft.Events;
using System.Text;

namespace fCraft {

    public class ReleaseInfo {

        internal ReleaseInfo( int version, int revision, DateTime releaseDate,
                              string summary, string changeLog, ReleaseFlags releaseType ) {
            Version = version;
            Revision = revision;
            Date = releaseDate;
            Summary = summary;
            ChangeLog = changeLog.Split( new[] { '\n' } );
            Flags = releaseType;
        }

        public ReleaseFlags Flags { get; private set; }

        public string FlagsString { get { return ReleaseFlagsToString( Flags ); } }

        public string[] FlagsList { get { return ReleaseFlagsToStringArray( Flags ); } }

        public int Version { get; private set; }

        public int Revision { get; private set; }

        public DateTime Date { get; private set; }

        public TimeSpan Age {
            get {
                return DateTime.UtcNow.Subtract( Date );
            }
        }

        public string VersionString {
            get {
                return String.Format( "{0:0.000}_r{1}", Decimal.Divide( Version, 1000 ), Revision );
            }
        }

        public string Summary { get; private set; }

        public string[] ChangeLog { get; private set; }

        public static ReleaseFlags StringToReleaseFlags( string str ) {
            ReleaseFlags flags = ReleaseFlags.None;
            for( int i = 0; i < str.Length; i++ ) {
                switch( Char.ToUpper( str[i] ) ) {
                    case 'A':
                        flags |= ReleaseFlags.APIChange;
                        break;
                    case 'B':
                        flags |= ReleaseFlags.Bugfix;
                        break;
                    case 'C':
                        flags |= ReleaseFlags.ConfigFormatChange;
                        break;
                    case 'D':
                        flags |= ReleaseFlags.Dev;
                        break;
                    case 'F':
                        flags |= ReleaseFlags.Feature;
                        break;
                    case 'M':
                        flags |= ReleaseFlags.MapFormatChange;
                        break;
                    case 'P':
                        flags |= ReleaseFlags.PlayerDBFormatChange;
                        break;
                    case 'S':
                        flags |= ReleaseFlags.Security;
                        break;
                    case 'U':
                        flags |= ReleaseFlags.Unstable;
                        break;
                }
            }
            return flags;
        }

        public static string ReleaseFlagsToString( ReleaseFlags flags ) {
            StringBuilder sb = new StringBuilder();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) sb.Append( 'A' );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) sb.Append( 'B' );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) sb.Append( 'C' );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) sb.Append( 'D' );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) sb.Append( 'F' );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) sb.Append( 'M' );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) sb.Append( 'P' );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) sb.Append( 'S' );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) sb.Append( 'U' );
            return sb.ToString();
        }

        public static string[] ReleaseFlagsToStringArray( ReleaseFlags flags ) {
            List<string> list = new List<string>();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) list.Add( "API Changes" );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) list.Add( "Fixes" );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) list.Add( "Config Changes" );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) list.Add( "Developer" );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) list.Add( "New Features" );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) list.Add( "Map Format Changes" );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) list.Add( "PlayerDB Changes" );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) list.Add( "Security Patch" );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) list.Add( "Unstable" );
            return list.ToArray();
        }
    }


    public class UpdaterResult {
        public static UpdaterResult NoUpdate {
            get {
                return new UpdaterResult( false, null, new ReleaseInfo[0] );
            }
        }
        internal UpdaterResult( bool updateAvailable, string downloadUrl, ReleaseInfo[] releases ) {
            UpdateAvailable = updateAvailable;
            DownloadUrl = downloadUrl;
            History = releases.OrderByDescending( r => r.Revision ).ToArray();
            LatestRelease = releases.FirstOrDefault();
        }
        public bool UpdateAvailable { get; private set; }
        public string DownloadUrl { get; private set; }
        public ReleaseInfo[] History { get; private set; }
        public ReleaseInfo LatestRelease { get; private set; }
    }

    [Flags]
    public enum ReleaseFlags {
        None = 0,

        APIChange = 1,
        Bugfix = 2,
        ConfigFormatChange = 4,
        Dev = 8,
        Feature = 16,
        MapFormatChange = 32,
        PlayerDBFormatChange = 64,
        Security = 128,
        Unstable = 256
    }

    /// <summary>
    /// Checks for updates, and keeps track of current version/revision.
    /// </summary>
    public static class Updater {

        public static readonly ReleaseInfo CurrentRelease = new ReleaseInfo(
            Version,
            Revision,
            new DateTime( 2011, 3, 22, 10, 55, 0, DateTimeKind.Utc ),
            "WIP",
            "WIP",
            ReleaseFlags.Dev | ReleaseFlags.Unstable
        );

        public const int Version = 510,
                         Revision = 477;
        public const bool IsDev = true,
                          IsBroken = true;

        public const string LatestStable = "0.506_r427";

        public static string UpdateUrl { get; set; }

        static Updater() {
            UpdateUrl = "http://www.fcraft.net/UpdateCheck.php?r={0}";
        }


        public static UpdaterResult CheckForUpdates() {
            // TODO: fix the rest
            UpdaterMode mode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
            if( mode == UpdaterMode.Disabled ) return UpdaterResult.NoUpdate;

            string url = String.Format( UpdateUrl, Revision );
            if( FireCheckingForUpdatesEvent( ref url ) ) return UpdaterResult.NoUpdate;

            Logger.Log( "Checking for fCraft updates...", LogType.SystemActivity );
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( url );

                request.Method = "GET";
                request.UserAgent = "fCraft";
                request.Timeout = 2000;
                request.ReadWriteTimeout = 2000;
                request.CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );

                using( WebResponse response = request.GetResponse() ) {
                    using( XmlTextReader reader = new XmlTextReader( response.GetResponseStream() ) ) {
                        XDocument doc = XDocument.Load( reader );
                        XElement root = doc.Root;
                        if( root.Attribute( "result" ).Value == "update" ) {
                            string downloadUrl = root.Attribute( "url" ).Value;
                            var releases = new List<ReleaseInfo>();
                            foreach( XElement el in root.Elements( "Release" ) ) {
                                releases.Add( new ReleaseInfo(
                                    Int32.Parse( el.Attribute( "v" ).Value ),
                                    Int32.Parse( el.Attribute( "r" ).Value ),
                                    Server.TimestampToDateTime( Int64.Parse( el.Attribute( "date" ).Value ) ),
                                    el.Element( "Summary" ).Value,
                                    el.Element( "ChangeLog" ).Value,
                                    ReleaseInfo.StringToReleaseFlags( el.Attribute( "flags" ).Value )
                                ) );
                            }
                            UpdaterResult result = new UpdaterResult( (releases.Count > 0), downloadUrl, releases.ToArray() );
                            FireCheckedForUpdatesEvent( UpdateUrl, result );
                            return result;
                        } else {
                            return UpdaterResult.NoUpdate;
                        }
                    }
                }
            } catch( Exception ex ) {
                Logger.Log( "An error occured while trying to check for updates: {0}: {1}", LogType.Error,
                            ex.GetType().ToString(), ex.Message );
                return UpdaterResult.NoUpdate;
            }
        }


        #region Events

        public static event EventHandler<CheckingForUpdatesEventArgs> CheckingForUpdates;


        public static event EventHandler<CheckedForUpdatesEventArgs> CheckedForUpdates;


        public static event EventHandler<BeforeUpdateRestartEventArgs> BeforeUpdateRestart;


        public static event EventHandler AfterUpdateRestart;


        static bool FireCheckingForUpdatesEvent( ref string updateUrl ) {
            var h = CheckingForUpdates;
            if( h == null ) return false;
            var e = new CheckingForUpdatesEventArgs( updateUrl );
            h( null, e );
            updateUrl = e.Url;
            return e.Cancel;
        }


        static void FireCheckedForUpdatesEvent( string url, UpdaterResult result ) {
            var h = CheckedForUpdates;
            if( h != null ) h( null, new CheckedForUpdatesEventArgs( url, result ) );
        }


        static bool FireBeforeUpdateRestartEvent() {
            var h = BeforeUpdateRestart;
            if( h == null ) return false;
            var e = new BeforeUpdateRestartEventArgs();
            h( null, e );
            return e.Cancel;
        }


        static void FireAfterUpdateRestartEvent() {
            var h = AfterUpdateRestart;
            if( h != null ) h( null, EventArgs.Empty );
        }

        #endregion
    }


    public enum UpdaterMode {
        Disabled,
        Notify,
        Prompt,
        Auto,
    }

}

#region EventArgs
namespace fCraft.Events {

    public sealed class CheckingForUpdatesEventArgs : EventArgs {
        internal CheckingForUpdatesEventArgs( string url ) {
            Url = url;
        }
        public string Url { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class CheckedForUpdatesEventArgs : EventArgs {
        internal CheckedForUpdatesEventArgs( string url, UpdaterResult result ) {
            Url = url;
            Result = result;
        }
        public string Url { get; private set; }
        public UpdaterResult Result { get; private set; }
    }


    public sealed class BeforeUpdateRestartEventArgs : EventArgs {
        internal BeforeUpdateRestartEventArgs() {
            Cancel = false;
        }
        public bool Cancel { get; set; }
    }

}
#endregion