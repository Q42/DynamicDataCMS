﻿using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Options;
using QMS.Models;
using QMS.Storage.CosmosDB.Extensions;
using QMS.Storage.CosmosDB.Models;
using QMS.Storage.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace QMS.Storage.CosmosDB
{
    /// <summary>
    /// CosmosDB does not serialize with System.Text.Json, this wrapper converts the System.Text.json objects to newtonsoft compatible objects
    /// </summary>
    public class CosmosWrapperService : IReadCmsItem, IWriteCmsItem
    {
        internal readonly CosmosService _cosmosService;

        public CosmosWrapperService(CosmosService cosmosService)
        {
            _cosmosService = cosmosService;
        }
        public async Task<IReadOnlyList<CmsItem>> List(string cmsType)
        {
            var result = await _cosmosService.List(cmsType).ConfigureAwait(false);
            return result.Select(x => x.ToCmsItem()).ToList();
        }

        public Task Write(CmsItem item, string cmsType, string id, string? lang)
        {
            return _cosmosService.Write(item.ToCosmosCmsItem(), cmsType, id, lang);
        }

        public Task Delete(string cmsType, string id)
        {
            return _cosmosService.Delete(cmsType, id);

        }

        public async Task<CmsItem?> Read(string partitionKey, string documentId)
        {
            var result = await _cosmosService.Read(partitionKey, documentId).ConfigureAwait(false);
            return result?.ToCmsItem();
        }
    }
}
