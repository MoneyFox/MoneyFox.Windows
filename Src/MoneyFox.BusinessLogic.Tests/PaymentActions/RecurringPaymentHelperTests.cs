﻿using System;
using MoneyFox.BusinessLogic.Extensions;
using MoneyFox.BusinessLogic.PaymentActions;
using MoneyFox.DataLayer.Entities;
using MoneyFox.Foundation;
using Should;
using Xunit;

namespace MoneyFox.BusinessLogic.Tests.PaymentActions
{
    public class RecurringPaymentHelperTests
    {
        [Fact]
        public void CheckIfRepeatable_NullRecurring_Exception()
        {

        }

        [Theory]
        [InlineData(PaymentRecurrence.Daily, 1, true)]
        [InlineData(PaymentRecurrence.Weekly, 8, true)]
        [InlineData(PaymentRecurrence.Biweekly, 14, true)]
        [InlineData(PaymentRecurrence.Monthly, 31, true)]
        [InlineData(PaymentRecurrence.Bimonthly, 62, true)]
        [InlineData(PaymentRecurrence.Quarterly, 94, true)]
        [InlineData(PaymentRecurrence.Biannually, 184, true)]
        [InlineData(PaymentRecurrence.Yearly, 365, true)]
        [InlineData(PaymentRecurrence.Daily, 0, false)]
        [InlineData(PaymentRecurrence.Weekly, 5, false)]
        [InlineData(PaymentRecurrence.Biweekly, 10, false)]
        [InlineData(PaymentRecurrence.Bimonthly, 20, false)]
        [InlineData(PaymentRecurrence.Quarterly, 59, false)]
        [InlineData(PaymentRecurrence.Biannually, 137, false)]
        [InlineData(PaymentRecurrence.Yearly, 300, false)]
        [InlineData(PaymentRecurrence.Biannually, 355, true)] // with year change
        [InlineData(PaymentRecurrence.Quarterly, 355, true)] // with year change
        public void CheckIfRepeatable_ValidatedRecurrence(PaymentRecurrence recurrence, int amountOfDaysPassed, bool expectedResult)
        {
            var account = new Account("foo");

            var payment = new Payment(DateTime.Today.AddDays(-amountOfDaysPassed), 105, PaymentType.Expense, account);
            payment.AddRecurringPayment(recurrence, DateTime.Today);

            RecurringPaymentHelper.CheckIfRepeatable(payment)
                                  .ShouldEqual(expectedResult);
        }

        [Theory]
        [InlineData(PaymentRecurrence.Daily, 0)]
        [InlineData(PaymentRecurrence.Weekly, 5)]
        [InlineData(PaymentRecurrence.Biweekly, 10)]
        [InlineData(PaymentRecurrence.Monthly, 28)]
        [InlineData(PaymentRecurrence.Bimonthly, 55)]
        [InlineData(PaymentRecurrence.Yearly, 340)]
        public void CheckIfRepeatable_UnclearedPayment_ReturnFalse(PaymentRecurrence recurrence, int amountOfDaysUntilRepeat)
        {
            var account = new Account("foo");

            var payment = new Payment(DateTime.Today.AddDays(amountOfDaysUntilRepeat), 105, PaymentType.Expense, account);
            payment.AddRecurringPayment(recurrence, DateTime.Today);

            RecurringPaymentHelper.CheckIfRepeatable(payment)
                .ShouldBeFalse();

        }

        [Fact]
        public void CheckIfRepeatable_ValidatedRecurrenceMonthly_False()
        {
            var account = new Account("foo");

            var payment = new Payment(DateTime.Today.GetFirstDayOfMonth(), 105, PaymentType.Expense, account);
            payment.AddRecurringPayment(PaymentRecurrence.Monthly, DateTime.Today);

            RecurringPaymentHelper.CheckIfRepeatable(payment)
                .ShouldBeFalse();
        }
    }
}
