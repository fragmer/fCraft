﻿// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.MapConversion {
    /// <summary> Exception caused by problems with the map file's incorrect format or structure. </summary>
    public sealed class MapFormatException : Exception {
        public MapFormatException() { }
        public MapFormatException( string message ) : base( message ) { }
    }
}
