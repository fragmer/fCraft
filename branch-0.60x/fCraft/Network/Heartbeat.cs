﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using fCraft.Events;

namespace fCraft {
    /// <summary> Static class responsible for sending heartbeats. </summary>
    public static class Heartbeat {
        public static TimeSpan Delay { get; set; }
        public static TimeSpan Timeout { get; set; }
        public static readonly Uri DefaultUri;
        public static readonly Uri WoMDirectUri;


        static SchedulerTask task;
        static HeartbeatData data;

        /// <summary> Whether last attempt to send a heartbeat failed. </summary>
        public static bool LastHeartbeatFailed { get; private set; }


        static Heartbeat() {
            DefaultUri = new Uri( "http://www.minecraft.net/heartbeat.jsp" );
            WoMDirectUri = new Uri( "http://direct.worldofminecraft.com/hb.php" );
            Delay = TimeSpan.FromSeconds( 25 );
            Timeout = TimeSpan.FromSeconds( 10 );
        }


        /// <summary> Starts the heartbeats. </summary>
        internal static void Start() {
            task = Scheduler.NewBackgroundTask( Beat ).RunForever( Delay );
        }


        static void Beat( SchedulerTask scheduledTask ) {
            if( Server.IsShuttingDown ) return;

            data = new HeartbeatData {
                IsPublic = ConfigKey.IsPublic.Enabled(),
                MaxPlayers = ConfigKey.MaxPlayers.GetInt(),
                PlayerCount = Server.CountPlayers( false ),
                ServerIP = Server.InternalIP,
                Port = Server.Port,
                ProtocolVersion = Config.ProtocolVersion,
                Salt = Server.Salt,
                ServerName = ConfigKey.ServerName.GetString()
            };

            // This needs to be wrapped in try/catch because and exception in an event handler
            // would permanently stop heartbeat sending.
            try {
                if( !RaiseHeartbeatSendingEvent( data ) ) {
                    return;
                }
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Heartbeat.Sending event handler failed", "fCraft", ex, false );
            }

            if( ConfigKey.HeartbeatEnabled.Enabled() ) {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat( "?public={0}&max={1}&users={2}&port={3}&version={4}&salt={5}&name={6}",
                                 data.IsPublic,
                                 data.MaxPlayers,
                                 data.PlayerCount,
                                 data.Port,
                                 data.ProtocolVersion,
                                 Uri.EscapeDataString( data.Salt ),
                                 Uri.EscapeDataString( data.ServerName ) );

                foreach( var pair in data.CustomData ) {
                    sb.AppendFormat( "&{0}={1}",
                                     Uri.EscapeDataString( pair.Key ),
                                     Uri.EscapeDataString( pair.Value ) );
                }

                SendBeat( new Uri( DefaultUri + sb.ToString() ), true );
                if( ConfigKey.HeartbeatToWoMDirect.Enabled() ) {
                    sb.Append( "&noforward=1" );
                    SendBeat( new Uri( WoMDirectUri + sb.ToString() ), false );
                }

            } else {
                // If heartbeats are disabled, the data is written to a text file (heartbeatdata.txt)
                const string tempFile = Paths.HeartbeatDataFileName + ".tmp";

                File.WriteAllLines( tempFile,
                    new[]{
                        Server.Salt,
                        Server.InternalIP.ToString(),
                        Server.Port.ToString(),
                        Server.CountPlayers(false).ToString(),
                        ConfigKey.MaxPlayers.GetString(),
                        ConfigKey.ServerName.GetString(),
                        ConfigKey.IsPublic.GetString()
                    },
                    Encoding.ASCII );

                Paths.MoveOrReplace( tempFile, Paths.HeartbeatDataFileName );
            }
        }


        static void SendBeat( Uri uri, bool getServerUri ) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( uri );
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( Server.BindIPEndPointCallback );
            request.Method = "GET";
            request.Timeout = (int)Timeout.TotalMilliseconds;
            request.CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );
            request.UserAgent = Updater.UserAgent;
            request.BeginGetResponse( ResponseCallback, new HeartbeatRequestState {
                Request = request,
                GetServerUri = getServerUri
            } );
        }



        class HeartbeatRequestState {
            public HttpWebRequest Request;
            public bool GetServerUri;
        }

        static void ResponseCallback( IAsyncResult result ) {
            if( Server.IsShuttingDown ) return;
            HeartbeatRequestState state = (HeartbeatRequestState)result.AsyncState;
            try {
                string responseText;
                using( HttpWebResponse response = (HttpWebResponse)state.Request.EndGetResponse( result ) ) {
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        responseText = responseReader.ReadToEnd();
                    }
                    LastHeartbeatFailed = false;
                    RaiseHeartbeatSentEvent( data, response, responseText );
                }

                if( state.GetServerUri ) {
                    string replyString = responseText.Trim();
                    if( replyString.StartsWith( "bad heartbeat", StringComparison.OrdinalIgnoreCase ) ) {
                        LastHeartbeatFailed = true;
                        Logger.Log( "Heartbeat: {0}", LogType.Error, replyString );
                    } else {
                        try {
                            Uri newUri = new Uri( replyString );
                            Uri oldUri = Server.Uri;
                            if( newUri != oldUri ) {
                                Server.Uri = newUri;
                                RaiseUriChangedEvent( oldUri, newUri );
                            }
                        } catch( UriFormatException ) {
                            Logger.Log( "Heartbeat: Server replied with: {0}", LogType.Error,
                                        replyString );
                        }
                    }
                }
            } catch( Exception ex ) {
                LastHeartbeatFailed = true;
                if( ex is WebException || ex is IOException ) {
                    Logger.Log( "Heartbeat: {0} is probably down ({1})", LogType.Warning,
                                state.Request.RequestUri.Host,
                                ex.Message );
                } else {
                    Logger.Log( "Heartbeat: {0}", LogType.Error, ex );
                }
            }
        }


        /*
        const string WoMDirectSettingsString = "http://direct.worldofminecraft.com/server.php?ip={0}&port={1}&salt={2}&desc={3}&flags={4}&insecure=1";
        const string WoMDirectFlags = "[fCraft]";
        const int WoMDirectSettingsTimeout = 30000;
        
        /// <summary> Checks server's external IP, as reported by checkip.dyndns.org. </summary>
        internal static void SetWoMDirectSettings() {
            Uri finalUri = new Uri( String.Format( WoMDirectSettingsString,
                                                   Server.ExternalIP,
                                                   Server.Port,
                                                   Uri.EscapeDataString( Server.Salt ),
                                                   Uri.EscapeDataString( ConfigKey.WoMDirectDescription.GetString() ),
                                                   Uri.EscapeDataString( WoMDirectFlags ) ) );

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( finalUri );
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( Server.BindIPEndPointCallback );
            request.Timeout = WoMDirectSettingsTimeout;
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );
            request.UserAgent = Updater.UserAgent;

            try {
                using( WebResponse response = request.GetResponse() ) {
                    using( StreamReader reader = new StreamReader( response.GetResponseStream() ) ) {
                        Logger.Log( reader.ReadToEnd(), LogType.Debug );
                    }
                }
            } catch( WebException ex ) {
                Logger.Log( "Could not set WoM Direct settings: {0}", LogType.Warning, ex );
            }
        }
        */

        #region Events

        /// <summary> Occurs when a heartbeat is about to be sent (cancellable). </summary>
        public static event EventHandler<HeartbeatSendingEventArgs> Sending;

        /// <summary> Occurs when a heartbeat has been sent. </summary>
        public static event EventHandler<HeartbeatSentEventArgs> Sent;

        /// <summary> Occurs when the server Uri has been set or changed. </summary>
        public static event EventHandler<UriChangedEventArgs> UriChanged;


        static bool RaiseHeartbeatSendingEvent( HeartbeatData heartbeatData ) {
            var h = Sending;
            if( h == null ) return true;
            var e = new HeartbeatSendingEventArgs( heartbeatData );
            h( null, e );
            return !e.Cancel;
        }

        static void RaiseHeartbeatSentEvent( HeartbeatData heartbeatData,
                                             HttpWebResponse response,
                                             string text ) {
            var h = Sent;
            if( h != null ) {
                h( null, new HeartbeatSentEventArgs( heartbeatData,
                                                     response.Headers,
                                                     response.StatusCode,
                                                     text ) );
            }
        }

        static void RaiseUriChangedEvent( Uri oldUri, Uri newUri ) {
            var h = UriChanged;
            if( h != null ) h( null, new UriChangedEventArgs( oldUri, newUri ) );
        }

        #endregion
    }


    public sealed class HeartbeatData {
        public HeartbeatData() {
            CustomData = new Dictionary<string, string>();
        }
        public string Salt { get; set; }
        public IPAddress ServerIP { get; set; }
        public int Port { get; set; }
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public string ServerName { get; set; }
        public bool IsPublic { get; set; }
        public int ProtocolVersion { get; set; }
        public Dictionary<string, string> CustomData { get; private set; }
    }
}


namespace fCraft.Events {

    public sealed class HeartbeatSentEventArgs : EventArgs {
        internal HeartbeatSentEventArgs( HeartbeatData heartbeatData,
                                         WebHeaderCollection headers,
                                         HttpStatusCode status,
                                         string text ) {
            HeartbeatData = heartbeatData;
            ResponseHeaders = headers;
            ResponseStatusCode = status;
            ResponseText = text;
        }
        public HeartbeatData HeartbeatData { get; private set; }
        public WebHeaderCollection ResponseHeaders { get; private set; }
        public HttpStatusCode ResponseStatusCode { get; private set; }
        public string ResponseText { get; private set; }
    }


    public sealed class HeartbeatSendingEventArgs : EventArgs, ICancellableEvent {
        internal HeartbeatSendingEventArgs( HeartbeatData data ) {
            HeartbeatData = data;
        }
        public bool Cancel { get; set; }
        public HeartbeatData HeartbeatData { get; private set; }
    }


    public sealed class UriChangedEventArgs : EventArgs {
        internal UriChangedEventArgs( Uri oldUri, Uri newUri ) {
            OldUri = oldUri;
            NewUri = newUri;
        }
        public Uri OldUri { get; private set; }
        public Uri NewUri { get; private set; }
    }
}