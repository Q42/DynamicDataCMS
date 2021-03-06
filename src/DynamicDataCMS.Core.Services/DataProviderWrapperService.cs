﻿using DynamicDataCMS.Core.Models;
using DynamicDataCMS.Storage.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDataCMS.Core.Services
{
    /// <summary>
    /// Wraps dataprovider calls because there can be multiple interface implementations
    /// Always read from one source, write to all sources
    /// </summary>
    public class DataProviderWrapperService :
        IReadFile,
        IWriteFile,
        IReadCmsItem,
        IWriteCmsItem
    {
        private readonly IReadFile readFileProvider;
        private readonly IEnumerable<IWriteFile> writeFileProviders;
        private readonly IEnumerable<IReadCmsItem> readCmsItemProviders;
        private readonly IEnumerable<IWriteCmsItem> writeCmsItemProviders;
        private readonly IEnumerable<IWriteCmsItemInterceptor> writeCmsItemInterceptors;

        public DataProviderWrapperService(IReadFile readFileProvider,
           IEnumerable<IWriteFile> writeFileProviders,
           IEnumerable<IReadCmsItem> readCmsItemProviders,
           IEnumerable<IWriteCmsItem> writeCmsItemProviders,
           IEnumerable<IWriteCmsItemInterceptor> writeCmsItemInterceptors
           )
        {
            this.readFileProvider = readFileProvider;
            this.writeFileProviders = writeFileProviders;
            this.readCmsItemProviders = readCmsItemProviders;
            this.writeCmsItemProviders = writeCmsItemProviders;

            this.writeCmsItemInterceptors = writeCmsItemInterceptors;
        }


        public bool HandlesType(CmsType cmsType) => true;

        public bool CanSort(CmsType cmsType)
        {
            IReadCmsItem firstReadProdiver = GetReadProvider(cmsType);
            return firstReadProdiver.CanSort(cmsType);
        }

        private IReadCmsItem GetReadProvider(CmsType cmsType)
        {
            var readProvider = readCmsItemProviders.Where(x => x.HandlesType(cmsType)).FirstOrDefault();
            if (readProvider == null)
                throw new Exception($"No read provider found that can handle type {cmsType}");
            return readProvider;
        }

        public Task<(IReadOnlyList<CmsItem> results, int total)> List(CmsType cmsType, string? sortField, string? sortOrder, int pageSize = 20, int pageIndex = 0, string? searchQuery = null)
        {
            IReadCmsItem firstReadProdiver = GetReadProvider(cmsType);
            return firstReadProdiver.List(cmsType, sortField, sortOrder, pageSize, pageIndex, searchQuery);
        }

        public Task<T?> Read<T>(CmsType cmsType, Guid id, string? lang) where T : CmsItem
        {
            IReadCmsItem firstReadProdiver = GetReadProvider(cmsType);
            return firstReadProdiver.Read<T>(cmsType, id, lang);
        }

        public Task<CmsFile?> ReadFile(string fileName)
        {
            return readFileProvider.ReadFile(fileName);
        }

        public async Task Write<T>(T item, CmsType cmsType, Guid id, string? lang, string? currentUser) where T : CmsItem
        {
            //Run interceptors before saving
            foreach(var interceptor in writeCmsItemInterceptors.Where(x => x.HandlesType(cmsType)))
            {
               item = await interceptor.InterceptAsync(item, cmsType, id, lang, currentUser).ConfigureAwait(false);
            };

            //Set id and type property, they might get lost during intercept
            item.Id = id;
            item.CmsType = cmsType;
            item.LastModifiedBy = currentUser;

            await Task.WhenAll(writeCmsItemProviders.Where(x => x.HandlesType(cmsType)).Select(x => x.Write(item, cmsType, id, lang, currentUser))).ConfigureAwait(false);
        }

        public Task Delete(CmsType cmsType, Guid id, string? lang, string? currentUser)
        {
            return Task.WhenAll(writeCmsItemProviders.Where(x => x.HandlesType(cmsType)).Select(x => x.Delete(cmsType, id, lang, currentUser)));
        }

        public async Task<string> WriteFile(CmsFile file, CmsType cmsType, Guid id, string fieldName, string? lang, string? currentUser)
        {
            var task = await Task.WhenAll(writeFileProviders.Select(x => x.WriteFile(file, cmsType, id, fieldName, lang, currentUser))).ConfigureAwait(false);

            return task.First();
        }

       
    }
}
