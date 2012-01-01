﻿// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// Plugin subsystem contributed by Jared Klopper (LgZ-optical).
using System;

namespace fCraft {
    public sealed class PluginLoadFailedEventArgs : EventArgs {
        public PluginLoadFailedEventArgs( string pluginFile, Exception exception ) {
            Exception = exception;
            PluginFile = pluginFile;
        }
        public Exception Exception { get; set; }
        public string PluginFile { get; set; }
    }
}