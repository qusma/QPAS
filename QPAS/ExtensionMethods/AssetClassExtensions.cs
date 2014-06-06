// -----------------------------------------------------------------------
// <copyright file="AssetClassExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;

namespace QPAS
{
    public static class AssetClassExtensions
    {
        public static decimal GetCapitalUsageMultiplier(this AssetClass ac)
        {
            if (ac == AssetClass.Option || ac == AssetClass.FutureOption)
            {
                return Properties.Settings.Default.optionsCapitalUsageMultiplier;
            }
            else
            {
                return 1;
            }
        }
    }
}