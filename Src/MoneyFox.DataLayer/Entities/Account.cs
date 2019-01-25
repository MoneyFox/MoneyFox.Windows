﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MoneyFox.DataLayer.Entities
{
    public class Account
    {
        //used by EF Core
        private Account(){}

        public Account(string name, double currentBalance = 0, string note = "", bool isexcluded = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
            CurrentBalance = currentBalance;
            Note = note;
            IsOverdrawn = currentBalance < 0;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        public string Name { get; private set; }
        public double CurrentBalance { get; private set; }
        public string Note { get; private set; }
        public bool IsOverdrawn { get; private set; }
        public bool IsExcluded { get; private set; }

        //Use uninitialised backing fields - this means we can detect if the collection was loaded
        private HashSet<Payment> chargedPayments;
        private HashSet<Payment> targetedPayments;
        private HashSet<RecurringPayment> chargedRecurringPayments;
        private HashSet<RecurringPayment> targetedRecurringPayments;

        public IEnumerable<Payment> ChargedPayments => chargedPayments?.ToList();
        public IEnumerable<Payment> TargetedPayments => targetedPayments?.ToList();

        public IEnumerable<RecurringPayment> ChargedRecurringPayments => chargedRecurringPayments?.ToList();
        public IEnumerable<RecurringPayment> TargetedRecurringPayments => targetedRecurringPayments?.ToList();
    }
}