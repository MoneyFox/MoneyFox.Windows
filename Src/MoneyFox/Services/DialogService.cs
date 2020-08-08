﻿using MoneyFox.Application.Common.Interfaces;
using MoneyFox.Views.Dialogs;
using System.Threading.Tasks;

namespace MoneyFox.Services
{
    public class DialogService : IDialogService
    {
        private LoadingDialog? loadingDialog;

        /// <inheritdoc/>
        public async Task ShowLoadingDialogAsync(string? message = null)
        {
            if(loadingDialog != null)
            {
                await HideLoadingDialogAsync();
            }

            loadingDialog = new LoadingDialog();
            await loadingDialog.ShowAsync();
        }

        /// <inheritdoc/>
        public async Task HideLoadingDialogAsync()
        {
            if(loadingDialog == null)
                return;

            await loadingDialog.DismissAsync();
        }
    }
}