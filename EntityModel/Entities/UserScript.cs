// -----------------------------------------------------------------------
// <copyright file="UserScript.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EntityModel
{
    public class UserScript : INotifyPropertyChanged
    {
        private string _code;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [MaxLength(65535)]
        [Column(TypeName = "TEXT")]
        public string Code
        {
            get { return _code; }
            set { _code = value; OnPropertyChanged();  }
        }

        [MaxLength(255)]
        public string Name { get; set; }

        [NotMapped]
        public ICollection<string> ReferencedAssemblies { get; set; }

        /// <summary>
        /// Used to save in the db.
        /// </summary>
        [MaxLength(65535)]
        [Column(TypeName = "TEXT")]
        public string ReferencedAssembliesAsString
        {
            get
            {
                return string.Join("|", ReferencedAssemblies);
            }

            set
            {
                ReferencedAssemblies.Clear();
                foreach(string s in value.Split('|'))
                {
                    ReferencedAssemblies.Add(s);
                }
            }
        }

        public UserScript()
        {
            ReferencedAssemblies = new ObservableCollection<string>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
