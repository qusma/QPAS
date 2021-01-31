﻿// -----------------------------------------------------------------------
// <copyright file="Tag.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace EntityModel
{
    [Serializable]
    public class Tag : INotifyPropertyChanged
    {
        private int _id;
        private string _name;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID
        {
            get { return _id; }
            set { _id = value; OnPropertyChanged(); }
        }

        [MaxLength(50)]
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        public override string ToString()
        {
            return Name;
        }

        public ICollection<Trade> Trades { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            return obj is Tag tag &&
                   _id == tag._id &&
                   _name == tag._name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_id, _name);
        }
    }
}