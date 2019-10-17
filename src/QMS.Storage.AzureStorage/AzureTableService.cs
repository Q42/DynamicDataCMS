﻿using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using QMS.Core.Models;
using QMS.Storage.AzureStorage.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QMS.Storage.AzureStorage
{
    public class AzureTableService
    {
        private readonly AzureStorageConfig _config;

        public AzureTableService(IOptions<AzureStorageConfig> config)
        {
            _config = config.Value;
        }

        private async Task<CloudTable> GetCloudTableClient(string tableName)
        {
            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_config.StorageAccount);

            // Create a table client for interacting with the table service
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());

            // Create a table client for interacting with the table service 
            CloudTable table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            return table;

        }

        private string GetIdForItem(Guid id, string? lang)
        {
            if (lang != null)
                return id.ToString() + "_" + lang;
            else
                return id.ToString();
        }

        public async Task<CmsItemTableEntity?> InsertOrMergeEntityAsync<T>(T cmsItem, string? lang) where T : CmsItem
        {
            if (cmsItem == null)
            {
                throw new ArgumentNullException("entity");
            }
            try
            {
                var table = await GetCloudTableClient(cmsItem.CmsType);

                CmsItemTableEntity entity = new CmsItemTableEntity(GetIdForItem(cmsItem.Id, lang), cmsItem.CmsType)
                {
                    JsonData = JsonSerializer.Serialize(cmsItem)
                };

                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                // Execute the operation.
                TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
                CmsItemTableEntity? resultEntity = result.Result as CmsItemTableEntity;

                return resultEntity;
            }
            catch (StorageException e)
            {
                throw;
            }
        }

        public async Task DeleteEntityAsync(string cmsType, Guid id, string? lang)
        {
            try
            {
                CmsItemTableEntity? resultEntity = await GetCmsItemTableEntity(cmsType, id, lang);
                if (resultEntity != null)
                {
                    var table = await GetCloudTableClient(cmsType);
                    TableOperation deleteOperation = TableOperation.Delete(resultEntity);
                    TableResult result = await table.ExecuteAsync(deleteOperation);
                }
            }
            catch (StorageException e)
            {
                throw;
            }
        }

        public async Task<T?> GetEntityAsync<T>(string cmsType, Guid id, string? lang) where T : CmsItem
        {
            try
            {
                CmsItemTableEntity? resultEntity = await GetCmsItemTableEntity(cmsType, id, lang);

                if (resultEntity != null)
                {
                    return JsonSerializer.Deserialize<T>(resultEntity.JsonData);
                }

                return null;
            }
            catch (StorageException e)
            {
                throw;
            }
        }

        private async Task<CmsItemTableEntity?> GetCmsItemTableEntity(string cmsType, Guid id, string? lang)
        {
            var table = await GetCloudTableClient(cmsType);

            TableOperation retreiveOperation = TableOperation.Retrieve<CmsItemTableEntity>(cmsType, GetIdForItem(id, lang));
            TableResult result = await table.ExecuteAsync(retreiveOperation);

            CmsItemTableEntity? resultEntity = result.Result as CmsItemTableEntity;

            return resultEntity;
        }
    }
}
