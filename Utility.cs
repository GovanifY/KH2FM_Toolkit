using System;

namespace KH2FM_Toolkit
{
    [Flags]
    public enum Trivalent : byte
    {
        NoChanges = 0,
        ChangesPending = 1,
        Changed = 2
    }
}