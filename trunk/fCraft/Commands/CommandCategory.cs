﻿using System;

namespace fCraft {
    [Flags]
    public enum CommandCategory {
        None = 0,
        Building = 1,
        Chat = 2,
        Info = 4,
        Moderation = 8,
        Maintenance = 16,
        World = 32,
        Zone = 64
    }
}
