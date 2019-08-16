﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QMS.Models;
using QMS.Services;
using QMS.Storage.CosmosDB;
using QMS.Core.Models;

namespace QMS.Core.Controllers
{
    [Area("cms")]
    [Route("[area]")]
    public class HomeController : Controller
    {
        private readonly CosmosService cosmosService;
        private readonly JsonSchemaService schemaService;

        public HomeController(CosmosService cosmosService, JsonSchemaService schemaService)
        {
            this.cosmosService = cosmosService;
            this.schemaService = schemaService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [Route("list/{cmsType}")]
        public async Task<IActionResult> List([FromRoute]string cmsType)
        {
            var result = await cosmosService.List(cmsType);
            var schema = schemaService.GetSchema(cmsType);

            if(schema.IsSingleton)
            {
                if (result.Any())
                    return RedirectToAction("Edit", new { cmsType = cmsType, id = result.First().Id });
                else
                    return RedirectToAction("Create", new { cmsType = cmsType });
            }

            var model = new ListViewModel
            {
                CmsType = cmsType,
                Schema = schema,
                Items = result
            };
            return View(model);
        }

        [Route("edit/{cmsType}/{id}/{lang?}")]
        public async Task<IActionResult> Edit([FromRoute]string cmsType, [FromRoute]string id, [FromRoute]string lang)
        {
            if (string.IsNullOrEmpty(id))
                return new NotFoundResult();

            var schema = schemaService.GetSchema(cmsType);
            var cmsItem = await cosmosService.Load(cmsType, id);

            CmsDataItem data = cmsItem;
            if (lang != null)
                data = cmsItem.Translations.FirstOrDefault(x => x.Key == lang).Value;

            var model = new EditViewModel
            {
                CmsType = cmsType,
                Id = id,
                SchemaLocation = schema,
                CmsConfiguration = schemaService.GetCmsConfiguration(),
                Language = lang,
                Data = data
            };
            return View(model);
        }

        [Route("create/{cmsType}")]
        public IActionResult Create([FromRoute]string cmsType)
        {
            var schema = schemaService.GetSchema(cmsType);

            var model = new EditViewModel
            {
                CmsType = cmsType,
                Id = Guid.NewGuid().ToString(),
                SchemaLocation = schema,
            };
            return View("Edit", model);
        }


        //[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //public IActionResult Error()
        //{
        //    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //}
    }
}