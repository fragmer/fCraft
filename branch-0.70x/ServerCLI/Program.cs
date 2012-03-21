﻿// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.ComponentModel;
using fCraft.Events;

namespace fCraft.ServerCLI {

    static class Program {
        static bool useColor = true;

        static void Main( string[] args ) {
            Logger.Logged += OnLogged;
            Heartbeat.UriChanged += OnHeartbeatUriChanged;

            Console.Title = "fCraft " + Updater.CurrentRelease.VersionString + " - starting...";

#if !DEBUG
            try {
#endif
                Server.InitLibrary( args );
                useColor = !Server.HasArg( ArgKey.NoConsoleColor );

                Server.InitServer();

                CheckForUpdates();
                Console.Title = "fCraft " + Updater.CurrentRelease.VersionString + " - " + ConfigKey.ServerName.GetString();

                if( !ConfigKey.ProcessPriority.IsBlank() ) {
                    try {
                        Process.GetCurrentProcess().PriorityClass = ConfigKey.ProcessPriority.GetEnum<ProcessPriorityClass>();
                    } catch( Exception ) {
                        Logger.Log( LogType.Warning, "Program.Main: Could not set process priority, using defaults." );
                    }
                }

                if( Server.StartServer() ) {
                    Console.WriteLine( "** Running fCraft version {0}. **", Updater.CurrentRelease.VersionString );
                    Console.WriteLine( "** Server is now ready. Type /Shutdown to exit safely. **" );

                    while( !Server.IsShuttingDown ) {
                        string cmd = Console.ReadLine();
                        if( cmd.Equals( "/Clear", StringComparison.OrdinalIgnoreCase ) ) {
                            Console.Clear();
                        } else {
                            try {
                                Player.Console.ParseMessage( cmd );
                            } catch( Exception ex ) {
                                Logger.LogAndReportCrash( "Error while executing a command from console", "ServerCLI", ex, false );
                            }
                        }
                    }

                } else {
                    ReportFailure( ShutdownReason.FailedToStart );
                }
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Unhandled exception in ServerCLI", "ServerCLI", ex, true );
                ReportFailure( ShutdownReason.Crashed );
            } finally {
                Console.ResetColor();
            }
#endif
        }


        static void ReportFailure( ShutdownReason reason ) {
            Console.Title = String.Format( "fCraft {0} {1}", Updater.CurrentRelease.VersionString, reason );
            if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine( "** {0} **", reason );
            if( useColor ) Console.ResetColor();
            Server.Shutdown( new ShutdownParams( reason, TimeSpan.Zero, false, false ), true );
            if( !Server.HasArg( ArgKey.ExitOnCrash ) ) {
                Console.ReadLine();
            }
        }


        [DebuggerStepThrough]
        static void OnLogged( object sender, LogEventArgs e ) {
            if( !e.WriteToConsole ) return;
            switch( e.MessageType ) {
                case LogType.Error:
                    if(useColor)Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.SeriousError:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.White;
                    if( useColor ) Console.BackgroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.Warning:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                case LogType.Debug:
                case LogType.Trace:
                    if( useColor ) Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine( e.Message );
                    if( useColor ) Console.ResetColor();
                    return;

                default:
                    Console.WriteLine( e.Message );
                    return;
            }
        }


        static void OnHeartbeatUriChanged( object sender, UriChangedEventArgs e ) {
            File.WriteAllText( "externalurl.txt", e.NewUri.ToString(), Encoding.ASCII );
            Console.WriteLine( "** URL: {0} **", e.NewUri );
            Console.WriteLine( "URL is also saved to file externalurl.txt" );
        }


        #region Updates


        static readonly AutoResetEvent UpdateDownloadWaiter = new AutoResetEvent( false );
        static bool updateFailed;

        static readonly object ProgressReportLock = new object();
        static void OnUpdateDownloadProgress( object sender, DownloadProgressChangedEventArgs e ) {
            lock( ProgressReportLock ) {
                Console.CursorLeft = 0;
                int maxProgress = Console.WindowWidth - 9;
                int progress = (int)Math.Round( ( e.ProgressPercentage / 100f ) * ( maxProgress - 1 ) );
                Console.Write( "{0,3}% |", e.ProgressPercentage );
                Console.Write( new String( '=', progress ) );
                Console.Write( '>' );
                Console.Write( new String( ' ', maxProgress - progress ) );
                Console.Write( '|' );
            }
        }


        static void OnUpdateDownloadCompleted( object sender, AsyncCompletedEventArgs e ) {
            Console.WriteLine();
            if( e.Error != null ) {
                updateFailed = true;
                if( useColor ) Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( "Downloading the updater failed: {0}", e.Error );
                if( useColor ) Console.ResetColor();
            } else {
                Console.WriteLine( "Update download finished." );
            }
            UpdateDownloadWaiter.Set();
        }


        static void CheckForUpdates() {
            UpdaterMode updaterMode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
            if( updaterMode == UpdaterMode.Disabled ) return;

            UpdaterResult update = Updater.CheckForUpdates();

            if( update.UpdateAvailable ) {
                Console.WriteLine( "** A new version of fCraft is available: {0}, released {1:0} day(s) ago. **",
                                   update.LatestRelease.VersionString,
                                   update.LatestRelease.Age.TotalDays );
                if( updaterMode != UpdaterMode.Notify ) {
                    WebClient client = new WebClient();
                    client.DownloadProgressChanged += OnUpdateDownloadProgress;
                    client.DownloadFileCompleted += OnUpdateDownloadCompleted;
                    client.DownloadFileAsync( update.DownloadUri, Paths.UpdaterFileName );
                    UpdateDownloadWaiter.WaitOne();
                    if( updateFailed ) return;

                    if( updaterMode == UpdaterMode.Prompt ) {
                        Console.WriteLine( "Restart the server and update now? y/n" );
                        var key = Console.ReadKey();
                        Console.ReadLine();
                        if( key.KeyChar == 'y' ) {
                            RestartForUpdate();
                            return;
                        } else {
                            Console.WriteLine( "You can update manually by shutting down the server and running " + Paths.UpdaterFileName );
                        }
                    } else {
                        RestartForUpdate();
                        return;
                    }
                }
            }
        }


        static void RestartForUpdate() {
            string restartArgs = String.Format( "{0} --restart=\"{1}\"",
                                                Server.GetArgString(),
                                                MonoCompat.PrependMono( "ServerGUI.exe" ) );
            MonoCompat.StartDotNetProcess( Paths.UpdaterFileName, restartArgs, true );
        }

        #endregion
    }
}