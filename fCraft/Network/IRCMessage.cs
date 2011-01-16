﻿/* Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
 * 
 * Based, in part, on SmartIrc4net code. Original license is reproduced below.
 * 
 *
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2005 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
 *
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

namespace fCraft {
    public sealed class IRCMessage {
        public string From;
        public string Nick;
        public string Ident;
        public string Host;
        public string Channel;
        public string Message;
        public string[] MessageArray;
        public string RawMessage;
        public string[] RawMessageArray;
        public IRCMessageType Type;
        public IRCReplyCode ReplyCode;

        public IRCMessage( string _from, string _nick, string _ident, string _host, string _channel, string _message, string _rawmessage, IRCMessageType _type, IRCReplyCode _replycode ) {
            RawMessage = _rawmessage;
            RawMessageArray = _rawmessage.Split( new char[] { ' ' } );
            Type = _type;
            ReplyCode = _replycode;
            From = _from;
            Nick = _nick;
            Ident = _ident;
            Host = _host;
            Channel = _channel;
            if( _message != null ) {
                // message is optional
                Message = _message;
                MessageArray = _message.Split( new char[] { ' ' } );
            }
        }
    }
}
