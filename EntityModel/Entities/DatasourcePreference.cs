// -----------------------------------------------------------------------
// <copyright file="DatasourcePreference.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityModel
{
    public class DatasourcePreference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public AssetClass AssetClass { get; set; }

        [MaxLength(255)]
        public string Datasource { get; set; }
    }
}
