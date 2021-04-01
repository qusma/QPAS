// -----------------------------------------------------------------------
// <copyright file="Account.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace EntityModel
{
    public class Account
    {
        public int ID { get; set; }

        [MaxLength(20)]
        public string AccountId { get; set; }
    }
}