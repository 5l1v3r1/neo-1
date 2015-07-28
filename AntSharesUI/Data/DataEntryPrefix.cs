﻿namespace AntShares.Data
{
    internal enum DataEntryPrefix : byte
    {
        Block = 0x00,
        Transaction = 0x01,
        Unspent = 0x02,

        IX_Asset = 0x81,
        IX_Enrollment = 0x84,
        IX_AntShare = 0x90,
        IX_Vote = 0x94,

        ST_QuantityIssued = 0xc1,

        Configuration = 0xf0
    }
}