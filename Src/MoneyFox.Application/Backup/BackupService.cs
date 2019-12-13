﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using MoneyFox.Application.Adapters;
using MoneyFox.Application.Constants;
using MoneyFox.Application.Extensions;
using MoneyFox.Application.Facades;
using MoneyFox.Application.FileStore;
using MoneyFox.Application.Messages;
#pragma warning disable S1128 // Unused "using" should be removed
using MoneyFox.Domain.Exceptions;
using NLog;
#pragma warning restore S1128 // Unused "using" should be removed

namespace MoneyFox.Application.Backup
{
    public interface IBackupService
    {
        /// <summary>
        ///     Login user.
        /// </summary>
        /// <exception cref="BackupAuthenticationFailedException">Thrown when the user couldn't be logged in.</exception>
        Task LoginAsync();

        /// <summary>
        ///     Logout user.
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        ///     Checks if there are backups to restore.
        /// </summary>
        /// <returns>Backups available or not.</returns>
        /// <exception cref="BackupAuthenticationFailedException">Thrown when the user couldn't be logged in.</exception>
        Task<bool> IsBackupExistingAsync();

        /// <summary>
        ///     Returns the date when the last backup was created.
        /// </summary>
        /// <returns>Creation date of the last backup.</returns>
        /// <exception cref="BackupAuthenticationFailedException">Thrown when the user couldn't be logged in.</exception>
        Task<DateTime> GetBackupDateAsync();

        /// <summary>
        ///     Restores an existing backup.
        /// </summary>
        /// <exception cref="BackupAuthenticationFailedException">Thrown when the user couldn't be logged in.</exception>
        /// <exception cref="NoBackupFoundException">Thrown when no backup with the right name is found.</exception>
        Task RestoreBackupAsync();

        /// <summary>
        ///     Enqueues a new backup task.
        /// </summary>
        /// <exception cref="NetworkConnectionException">Thrown if there is no internet connection.</exception>
        Task UploadBackupAsync(BackupMode backupMode = BackupMode.Automatic);
    }

    public class BackupService : IBackupService, IDisposable
    {
        private readonly ICloudBackupService cloudBackupService;
        private readonly IFileStore fileStore;
        private readonly ISettingsFacade settingsFacade;
        private readonly IConnectivityAdapter connectivity;
        private readonly IMessenger messenger;

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly NLog.Logger logManager = LogManager.GetCurrentClassLogger();

        public BackupService(ICloudBackupService cloudBackupService,
                             IFileStore fileStore,
                             ISettingsFacade settingsFacade,
                             IConnectivityAdapter connectivity,
                             IMessenger messenger)
        {
            this.cloudBackupService = cloudBackupService;
            this.fileStore = fileStore;
            this.settingsFacade = settingsFacade;
            this.connectivity = connectivity;
            this.messenger = messenger;
        }

        public async Task LoginAsync()
        {
            if (!connectivity.IsConnected)
                throw new NetworkConnectionException();

            try
            {
                await cloudBackupService.LoginAsync();

                settingsFacade.IsLoggedInToBackupService = true;
                settingsFacade.IsBackupAutouploadEnabled = true;
            }
            catch (BackupAuthenticationFailedException ex)
            {
                logManager.Error(ex, "Login Failed.");
                throw;
            }
            catch (MsalClientException ex)
            {
                logManager.Error(ex, "Login Failed.");
                throw;
            }
        }

        public async Task LogoutAsync()
        {
            if(!connectivity.IsConnected)
            {
                throw new NetworkConnectionException();
            }

            try
            {
                await cloudBackupService.LogoutAsync();

                settingsFacade.IsLoggedInToBackupService = false;
                settingsFacade.IsBackupAutouploadEnabled = false;
            }
            catch (BackupAuthenticationFailedException ex)
            {
                logManager.Error(ex, "Logout Failed.");

                throw;
            }
        }

        public async Task<bool> IsBackupExistingAsync()
        {
            if (!connectivity.IsConnected) return false;

            List<string> files = await cloudBackupService.GetFileNamesAsync();
            return files != null && files.Any();
        }

        public async Task<DateTime> GetBackupDateAsync()
        {
            if(!connectivity.IsConnected)
            {
                return DateTime.MinValue;
            }

            DateTime date = await cloudBackupService.GetBackupDateAsync();
            return date.ToLocalTime();
        }

        public async Task RestoreBackupAsync()
        {
            if(!connectivity.IsConnected)
            {
                throw new NetworkConnectionException();
            }

            try
            {
                await DownloadBackupAsync();
                settingsFacade.LastDatabaseUpdate = DateTime.Now;
                messenger.Send(new BackupRestoredMessage());
            }
            catch (BackupAuthenticationFailedException ex)
            {
                await LogoutAsync();
                logManager.Error(ex, "Download Backup failed.");
                throw;
            }
            catch (ServiceException ex)
            {
                await LogoutAsync();
                logManager.Error(ex, "Download Backup failed.");
                throw;
            }
        }

        private async Task DownloadBackupAsync()
        {
            List<string> backups = await cloudBackupService.GetFileNamesAsync();

            if (backups.Contains(DatabaseConstants.BACKUP_NAME))
            {
                using (Stream backupStream = await cloudBackupService.RestoreAsync(DatabaseConstants.BACKUP_NAME,
                                                                                  DatabaseConstants.BACKUP_NAME))
                {
                    fileStore.WriteFile(DatabaseConstants.BACKUP_NAME, backupStream.ReadToEnd());
                }

                bool moveSucceed = fileStore.TryMove(DatabaseConstants.BACKUP_NAME,
                                                     DatabasePathHelper.GetDbPath(),
                                                     true);

                if(!moveSucceed)
                {
                    throw new BackupException("Error Moving downloaded backup file");
                }
            }
        }

        public async Task UploadBackupAsync(BackupMode backupMode = BackupMode.Automatic)
        {
            if (backupMode == BackupMode.Automatic && !settingsFacade.IsBackupAutouploadEnabled)
            {
                return;
            }

            if (!settingsFacade.IsLoggedInToBackupService) await LoginAsync();

            await EnqueueBackupTaskAsync();
            settingsFacade.LastDatabaseUpdate = DateTime.Now;
        }

        private async Task EnqueueBackupTaskAsync(int attempts = 0)
        {
            if(!connectivity.IsConnected)
            {
                throw new NetworkConnectionException();
            }

            await semaphoreSlim.WaitAsync(ServiceConstants.BACKUP_OPERATION_TIMEOUT,
                                          cancellationTokenSource.Token);
            try
            {
                if (await cloudBackupService.UploadAsync(fileStore.OpenRead(DatabaseConstants.DB_NAME)))
                {
                    semaphoreSlim.Release();
                }
                else
                {
                    cancellationTokenSource.Cancel();
                }
            }
            catch (OperationCanceledException ex)
            {
                logManager.Error(ex, "Enqueue Backup failed.");
                await Task.Delay(ServiceConstants.BACKUP_REPEAT_DELAY);
                await EnqueueBackupTaskAsync(attempts + 1);
            }
            catch (BackupAuthenticationFailedException ex)
            {
                logManager.Error(ex, "Enqueue Backup failed.");
                await LogoutAsync();
                throw;
            }
            catch (ServiceException ex)
            {
                logManager.Error(ex, "Enqueue Backup failed.");
                await LogoutAsync();
                throw;
            }
            catch (Exception ex)
            {
                logManager.Error(ex, "Enqueue Backup failed.");
                throw;
            }

            logManager.Warn("Enqueue Backup failed.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            cancellationTokenSource.Dispose();
            semaphoreSlim.Dispose();
        }
    }
}