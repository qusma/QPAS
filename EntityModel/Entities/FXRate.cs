// -----------------------------------------------------------------------
// <copyright file="FXRate.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NXmlMapper;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityModel
{
    public class FXRate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [NotXmlMapped]
        public int FromCurrencyID { get; set; }

        [NotXmlMapped]
        public virtual Currency FromCurrency { get; set; }

        /// <summary>
        /// used for xml parsing
        /// </summary>
        [NotMapped]
        [AttributeName("fromCurrency")]
        public string FromCurrencyString { get; set; }

        [NotXmlMapped]
        public int ToCurrencyID { get; set; }

        [NotXmlMapped]
        public virtual Currency ToCurrency { get; set; }

        /// <summary>
        /// used for xml parsing
        /// </summary>
        [NotMapped]
        [AttributeName("toCurrency")]
        public string ToCurrencyString { get; set; }

        [AttributeName("reportDate", "yyyyMMdd")]
        public DateTime Date { get; set; }

        [AttributeName("rate")]
        public decimal Rate { get; set; }
    }
}