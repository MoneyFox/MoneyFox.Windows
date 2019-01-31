﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyFox.DataLayer.Entities
{
    public class Category
    {
        //used by EF Core
        private Category() { }

        public Category(string name, string note = "")
        {
            UpdateData(name, note);
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        public string Name { get; private set; }

        public string Note { get; private set; }

        public List<Payment> Payments { get; private set; }

        public void UpdateData(string name, string note = "")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
            Note = note;
        }
    }
}