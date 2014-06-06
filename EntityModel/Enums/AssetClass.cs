// -----------------------------------------------------------------------
// <copyright file="AssetClass.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace EntityModel
{
    [Serializable]
    public enum AssetClass : int
    {
        [Description("BAG")]
        Bag = 6,

        [Description("BILL")]
        Bill = 10,

        [Description("BOND")]
        Bond = 7,

        [Description("CASH")]
        Cash = 5,

        [Description("CFD")]
        CFD = 11,

        [Description("CMDTY")]
        Commodity = 9,

        [Description("FUT")]
        Future = 2,

        [Description("FOP")]
        FutureOption = 4,

        [Description("IND")]
        Index = 3,

        [Description("OPT")]
        Option = 1,

        [Description("STK")]
        Stock = 0,

        [Description("")]
        Undefined = 12,

        [Description("WAR")]
        Warrant = 8
    }
}