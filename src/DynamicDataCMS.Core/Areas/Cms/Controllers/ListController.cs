﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DynamicDataCMS.Core.Models;
using DynamicDataCMS.Core.Services;
using DynamicDataCMS.Storage.Interfaces;
using System.Net.Http;
using NJsonSchema;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DynamicDataCMS.Core.Areas.Cms.Controllers
{
    [Area("cms")]
    [Route("[area]")]
    public class ListController : Controller
    {
        private readonly IReadCmsItem readCmsItemService;
        private readonly JsonSchemaService schemaService;
        private readonly CmsTreeService cmsTreeService;

        public ListController(DataProviderWrapperService dataProviderService, JsonSchemaService schemaService, CmsTreeService cmsTreeService)
        {
            this.readCmsItemService = dataProviderService;
            this.schemaService = schemaService;
            this.cmsTreeService = cmsTreeService;
        }

        [Route("list/{cmsType}")]
        [HttpGet]
        public async Task<IActionResult> List([FromRoute]string cmsType, [FromQuery]string? sortField, [FromQuery]string? sortOrder, [FromQuery]int pageIndex, [FromQuery]string? q)
        {
            var cmsMenuItem = schemaService.GetCmsType(cmsType);
            if (cmsMenuItem == null)
                return new NotFoundResult();

            if (cmsMenuItem.IsTree)
                return await ListTree(cmsType, null);

            if (cmsMenuItem.SchemaKey == null)
                return new NotFoundResult();

            var schema = schemaService.GetSchema(cmsMenuItem.SchemaKey);
            int pageSize = cmsMenuItem.PageSize;
            if (pageSize <= 0)
                pageSize = 20;

            var (results, total) = await readCmsItemService.List(cmsType, sortField, sortOrder, pageSize: pageSize, pageIndex, q).ConfigureAwait(false);

            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;

            if (cmsMenuItem.IsSingleton)
            {
                if (results.Any())
                    return RedirectToAction("Edit", "Edit", new { cmsType = cmsType, id = results.First().Id });
                else
                    return RedirectToAction("Create", "Edit", new { cmsType = cmsType });
            }

            var model = new ListViewModel
            {
                CmsType = cmsType,
                Schema = schema,
                MenuCmsItem = cmsMenuItem,
                Items = results,
                TotalPages = total,
                CurrentPage = pageIndex + 1,
                PageSize = pageSize
            };
            return View(model);
        }

        public async Task<IActionResult> ListTree(string cmsType, string? lang)
        {
            var cmsMenuItem = schemaService.GetCmsType(cmsType);
            if (cmsMenuItem == null || !cmsMenuItem.IsTree)
                return new NotFoundResult();

            var treeItem = await cmsTreeService.GetCmsTreeItem(cmsType, lang);

            if (treeItem == null)
                return new NotFoundResult();

            var model = new ListTreeViewModel
            {
                CmsType = cmsType,
                MenuCmsItem = cmsMenuItem,
                CmsTreeItem = treeItem,
            };

            return View("ListTree", model);
        }
      
    }
}
