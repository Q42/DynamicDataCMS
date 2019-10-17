﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QMS.Core.Models
{
    /// <summary>
    /// Holds all CMS Configuration (schemas)
    /// </summary>
    public class CmsConfiguration
    {
        /// <summary>
        /// CMS Title to show in layout
        /// </summary>
        public string Title { get; set; }

        public List<string> Languages { get; set; } = new List<string>();
        public List<string> EditScripts { get; set; } = new List<string>();
        public List<string> Scripts { get; set; } = new List<string>();
        public List<string> Styles { get; set; } = new List<string>();

        public List<EntityGroupConfiguration> EntityGroups { get; set; } = new List<EntityGroupConfiguration>();
        public IEnumerable<EntityGroupConfiguration> EntityGroupsInitialized => EntityGroups.Where(x => x.Entities.Any(e => !string.IsNullOrEmpty(e.Schema)));


        public IEnumerable<SchemaLocation> Entities => EntityGroups.SelectMany(x => x.Entities);
        public IEnumerable<SchemaLocation> EntitiesInitialized => EntityGroupsInitialized.SelectMany(x => x.Entities);

    }

    public class EntityGroupConfiguration
    {
        public string Name { get; set; }
        public int Order { get; set; }

        public List<SchemaLocation> Entities { get; set; } = new List<SchemaLocation>();
        public IEnumerable<SchemaLocation> EntitiesInitialized => Entities.Where(x => !string.IsNullOrEmpty(x.Schema));
    }

    public class SchemaLocation
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public Uri? Uri { get; set; }
        public string? FileLocation { get; set; }

        /// <summary>
        /// Indicates there can be only one instance
        /// TODO: Replace with Min / Max option like JsonSchema?
        /// </summary>
        public bool IsSingleton { get; set; }

        /// <summary>
        /// Page size for overview page, default 20
        /// </summary>
        public int PageSize { get; set; } = 20;

        public string? Schema { get; set; }

        public List<ListViewProperty> ListViewProperties { get; set; } = new List<ListViewProperty>();

    }

    public class ListViewProperty
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
    }
}