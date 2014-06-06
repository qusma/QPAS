// -----------------------------------------------------------------------
// <copyright file="Instrument.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NXmlMapper;

namespace EntityModel
{
    [ElementName("SecurityInfo")]
    public class Instrument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [NotXmlMapped]
        public int? QDMSInstrumentID { get; set; }

        [AttributeName("symbol")]
        [MaxLength(50)]
        public string Symbol { get; set; }

        [AttributeName("description")]
        [MaxLength(255)]
        public string Description { get; set; }

        [NotXmlMapped]
        public AssetClass AssetCategory { get; set; }

        [AttributeName("underlyingSymbol")]
        [MaxLength(50)]
        public string UnderlyingSymbol { get; set; }

        [AttributeName("multiplier")]
        public int Multiplier { get; set; }

        [AttributeName("expiry", "yyyy-MM-dd")]
        public DateTime? Expiration { get; set; }

        [AttributeName("type")]
        [MaxLength(10)]
        public string OptionType { get; set; }

        [AttributeName("strike")]
        public decimal Strike { get; set; }

        [AttributeName("conid")]
        [Index]
        public long ConID { get; set; }

        [AttributeName("assetCategory")]
        [NotMapped]
        public string SetAssetClass
        {
            set
            {
                AssetCategory = Utils.GetValueFromDescription<AssetClass>(value);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} ({2})", ID, Symbol, AssetCategory);
        }
    }
}