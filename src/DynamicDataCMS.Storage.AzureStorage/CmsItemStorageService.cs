﻿using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;
using DynamicDataCMS.Core.Models;
using DynamicDataCMS.Storage.AzureStorage.Models;
using DynamicDataCMS.Storage.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicDataCMS.Storage.AzureStorage
{
    public class CmsItemStorageService : IReadCmsItem, IWriteCmsItem
    {
        private readonly AzureStorageService azureStorageService;
        private readonly AzureTableService tableService;
        private readonly CmsConfiguration cmsConfiguration;
        private readonly AzureStorageConfig azureStorageConfig;

        public bool CanSort(CmsType cmsType) => true;
        public bool HandlesType(CmsType cmsType) => !azureStorageConfig.ExcludedTypes.Contains(cmsType);


        public CmsItemStorageService(AzureStorageService azureStorageService, AzureTableService tableService, IOptions<CmsConfiguration> cmsConfiguration, IOptions<AzureStorageConfig> azureStorageConfig)
        {
            this.azureStorageService = azureStorageService;
            this.tableService = tableService;
            this.cmsConfiguration = cmsConfiguration.Value;
            this.azureStorageConfig = azureStorageConfig.Value;

        }

        public async Task<(IReadOnlyList<CmsItem> results, int total)> List(CmsType cmsType, string? sortField, string? sortOrder, int pageSize = 20, int pageIndex = 0, string? searchQuery = null)
        {
            //Get index file
            var indexFileName = GenerateFileName(cmsType, "_index", null);

            //Get current index file
            var indexFile = await azureStorageService.ReadFileAsJson<List<CmsItem>>(indexFileName).ConfigureAwait(false);
            indexFile = indexFile ?? new List<CmsItem>();

            var returnItems = indexFile.AsQueryable();
            if (!string.IsNullOrEmpty(searchQuery))
            {
                returnItems = returnItems.Where(x => string.Join(" ", x.AdditionalProperties.Values.Select(x => x.ToString().ToLowerInvariant())).Contains(searchQuery!.ToLowerInvariant()));
            }

            if (sortField != null)
            {
                sortOrder = sortOrder ?? "Asc";
                if (sortOrder == "Asc")
                    returnItems = returnItems.OrderBy(x => x.AdditionalProperties[sortField].ToString());
                else
                    returnItems = returnItems.OrderByDescending(x => x.AdditionalProperties[sortField].ToString());
            }

            return (returnItems.Skip(pageSize * pageIndex).Take(pageSize).ToList(), indexFile.Count);

            //var directoryInfo = await azureStorageService.GetFilesFromDirectory(cmsType).ConfigureAwait(false);

            //List<CmsItem> result = new List<CmsItem>();

            //foreach(var file in directoryInfo.Skip(pageSize * pageIndex).Take(pageSize))
            //{
            //    if (file is CloudBlockBlob cloudBlockBlob)
            //    {
            //        string fileName = cloudBlockBlob.Name
            //            .Replace($"{cmsType}/", "")
            //            .Replace(".json", "");
            //        var cmsItem = await Read(cmsType, fileName, null).ConfigureAwait(false);
            //        if(cmsItem != null)
            //            result.Add(cmsItem);
            //    }
            //}

            //var total = directoryInfo.Count();

            //return (result, total);
        }

        public Task<T?> Read<T>(CmsType cmsType, Guid id, string? lang) where T : CmsItem
        {
            if (azureStorageConfig.StorageLocation == AzureStorageLocation.Tables
                || azureStorageConfig.StorageLocation == AzureStorageLocation.Both)
            {
                return tableService.GetEntityAsync<T>(cmsType, id, lang);
            }
            else
            {
                var fileName = GenerateFileName(cmsType, id, lang);

                return azureStorageService.ReadFileAsJson<T>(fileName);
            }
        }

        public async Task Write<T>(T item, CmsType cmsType, Guid id, string? lang, string? currentUser) where T : CmsItem
        {
            if (azureStorageConfig.StorageLocation == AzureStorageLocation.Tables
               || azureStorageConfig.StorageLocation == AzureStorageLocation.Both)
            {
                await tableService.InsertOrMergeEntityAsync<T>(item, lang);
            }
            else
            {
                var fileName = GenerateFileName(cmsType, id, lang);
                await azureStorageService.WriteFileAsJson(item, fileName);
            }

            //Write index file for paging and sorting
            var indexFileName = GenerateFileName(cmsType, "_index", lang);
            var typeInfo = cmsConfiguration.MenuItems.Where(x => x.Key == cmsType.Value).FirstOrDefault();

            if (typeInfo == null)
                return;

            //Get current index file
            var indexFile = await azureStorageService.ReadFileAsJson<List<CmsItem>>(indexFileName).ConfigureAwait(false);
            indexFile = indexFile ?? new List<CmsItem>();

            //Remove existing item
            indexFile.Remove(indexFile.Where(x => x.Id == item.Id).FirstOrDefault());

            var indexItem = new CmsItem { 
                Id = id, 
                CmsType = cmsType, 
                LastModifiedBy = item.LastModifiedBy,
                LastModifiedDate = item.LastModifiedDate 
            };

            foreach (var prop in typeInfo.ListViewProperties)
            {
                var value = item.AdditionalProperties[prop.Key];
                indexItem.AdditionalProperties[prop.Key] = value;
            }

            indexFile.Add(indexItem);

            await azureStorageService.WriteFileAsJson(indexFile, indexFileName);
        }



        public async Task Delete(CmsType cmsType, Guid id, string? lang, string? currentUser)
        {
            if (azureStorageConfig.StorageLocation == AzureStorageLocation.Tables
              || azureStorageConfig.StorageLocation == AzureStorageLocation.Both)
            {
                await tableService.DeleteEntityAsync(cmsType, id, lang);

                //Delete all translations
                foreach(var cmsLang in cmsConfiguration.Languages)
                {
                    await tableService.DeleteEntityAsync(cmsType, id, cmsLang);
                }
            }
            else
            {
                var fileName = GenerateFileName(cmsType, id, null);

                await azureStorageService.DeleteFileAsync(fileName).ConfigureAwait(false);

                //Get translations
                var files = await azureStorageService.GetFilesFromDirectory($"{cmsType}/{id}").ConfigureAwait(false);
                foreach (var file in files)
                {
                    if (file is CloudBlockBlob cloudBlockBlob)
                    {
                        await cloudBlockBlob.DeleteAsync().ConfigureAwait(false);
                    }
                }

            }

            //Write index file for paging and sorting
            var indexFileName = GenerateFileName(cmsType, "_index", lang);
            //Get current index file
            var indexFile = await azureStorageService.ReadFileAsJson<List<CmsItem>>(indexFileName).ConfigureAwait(false);
            indexFile = indexFile ?? new List<CmsItem>();

            //Remove existing item
            indexFile.Remove(indexFile.Where(x => x.Id == id).FirstOrDefault());
            await azureStorageService.WriteFileAsJson(indexFile, indexFileName);
        }

        public static string GenerateFileName(CmsType cmsType, Guid id, string? lang)
        {
            return GenerateFileName(cmsType, id.ToString(), lang);
        }
        public static string GenerateFileName(CmsType cmsType, string id, string? lang)
        {
            string fileName = $"{cmsType}/{id}.json";
            if (!string.IsNullOrEmpty(lang))
                fileName = $"{cmsType}/{id}/{lang}.json";
            return fileName;
        }
    }
}