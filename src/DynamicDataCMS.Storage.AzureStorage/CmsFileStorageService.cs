﻿using DynamicDataCMS.Core.Models;
using DynamicDataCMS.Storage.AzureStorage.Models;
using DynamicDataCMS.Storage.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDataCMS.Storage.AzureStorage
{
    /// <summary>
    /// Implements read and write interface for files for Azure Blob Storage
    /// </summary>
    public class CmsFileStorageService : IReadFile, IWriteFile
    {
        private readonly AzureStorageService azureStorageService;
        private readonly AzureStorageConfig storageConfig;

        public CmsFileStorageService(AzureStorageService azureStorageService, IOptions<AzureStorageConfig> storageConfig)
        {
            this.azureStorageService = azureStorageService;
            this.storageConfig = storageConfig.Value;
        }

        public async Task<CmsFile?> ReadFile(string fileName)
        {
            // get original file
            var blob = await azureStorageService.GetFileReference(fileName).ConfigureAwait(false);

            if ((!await blob.ExistsAsync().ConfigureAwait(false)))
                return null;

            using (var stream = new MemoryStream())
            {
                // download file
                await blob.DownloadToAsync(stream).ConfigureAwait(false);
                var fileBytes = stream.ToArray();
                var props = await blob.GetPropertiesAsync().ConfigureAwait(false);

                return new CmsFile { Bytes = fileBytes, ContentType = props.Value.ContentType };
            }
        }

        public async Task<string> WriteFile(CmsFile file, CmsType cmsType, Guid id, string fieldName, string? lang, string? currentUser)
        {
            string fileName = GenerateFileName(cmsType, id, fieldName, lang);

            var blob = await azureStorageService.StoreFileAsync(file.Bytes, fileName, file.ContentType).ConfigureAwait(false);

            return fileName;
        }

        private string GenerateFileName(CmsType cmsType, Guid id, string fieldName, string? lang)
        {
            string fileName = $"{cmsType}/{id}/{lang}/{fieldName}";
            if (string.IsNullOrEmpty(lang))
                fileName = $"{cmsType}/{id}/{fieldName}";

            if (storageConfig.GenerateUniqueFileName)
                fileName = $"{fileName}/{Guid.NewGuid()}";

            return fileName;
        }
    }
}
