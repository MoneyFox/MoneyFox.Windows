﻿using FluentAssertions;
using MoneyFox.Ui.Shared.ViewModels.Accounts;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace MoneyFox.Ui.Shared.Tests.ViewModels.Accounts
{
    [ExcludeFromCodeCoverage]
    public class AccountViewModelTests
    {
        [Theory]
        [MemberData(nameof(Data))]
        public void EqualsReturnsFalseOnNull(AccountViewModel vm1, AccountViewModel vm2, bool result)
        {
            // Act / Assert
            vm1.Equals(vm2).Should().Be(result);
        }

        public static IEnumerable<object[]> Data =>
            new List<object[]>
            {
                    new object[] { new AccountViewModel(), null, false },
                    new object[] { new AccountViewModel(), new AccountViewModel(), true },
                    new object[] { new AccountViewModel { Id = 2 }, new AccountViewModel { Id = 3 }, false }
            };
    }
}
