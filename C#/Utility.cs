﻿using System;
using System.IO;

namespace Utility
{
    [Flags]
    public enum Trivalent : byte
    {
        NoChanges = 0,
        ChangesPending = 1,
        Changed = 2
    }
}
