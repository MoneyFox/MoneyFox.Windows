﻿using Android.Runtime;
using MoneyManager.Core.ViewModels;

namespace MoneyFox.Droid.Fragments
{
	[Register("moneymanager.droid.fragments.SettingsSecurityFragment")]
    public class SettingsSecurityFragment : BaseFragment<SettingsSecurityViewModel>
    {
        protected override int FragmentId => Resource.Layout.fragment_settings_security;
    }
}

