﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QMS.Core.Models;

namespace QMS.Core.Services
{
    public class CmsTreeService
    {
        private readonly DataProviderWrapperService dataProvider;

        public CmsTreeService(DataProviderWrapperService dataProvider)
        {
            this.dataProvider = dataProvider;
        }

        public Task<CmsTreeItem?> GetCmsTreeItem(string cmsTreeType, string? lang)
        {
            return dataProvider.Read<CmsTreeItem>(cmsTreeType, Guid.Empty, lang: null);
        }

        public async Task<CmsTreeItem> SetCmsTreeNodeType(string cmsTreeType, Guid nodeId, string cmsItemType, Guid cmsItemId, string? lang, string? currentUser)
        {
            var document = await GetCmsTreeItem(cmsTreeType, lang).ConfigureAwait(false);

            if (document == null)
                throw new Exception("Tree item not found");

            //Get node
            var existing = document.Nodes.Where(x => x.NodeId == nodeId).FirstOrDefault();

            existing.CmsItemType = cmsItemType;
            existing.CmsItemId = cmsItemId;

            await dataProvider.Write<CmsTreeItem>(document, cmsTreeType, Guid.Empty, lang: null, currentUser).ConfigureAwait(false);

            return document;
        }

        public async Task<CmsTreeItem> ClearCmsTreeNode(string cmsTreeType, Guid nodeId, string? lang, string? currentUser)
        {
            var document = await GetCmsTreeItem(cmsTreeType, lang).ConfigureAwait(false);

            if (document == null)
                throw new Exception("Tree item not found");

            //Get node
            var existing = document.Nodes.Where(x => x.NodeId == nodeId).FirstOrDefault();

            existing.CmsItemType = null;
            existing.CmsItemId = null;

            await dataProvider.Write<CmsTreeItem>(document, cmsTreeType, Guid.Empty, lang: null, currentUser).ConfigureAwait(false);

            while(await TreeShake(cmsTreeType, lang, currentUser) > 0)
            { }

            return document;
        }

        /// <summary>
        /// Clear all nodes that don't have children
        /// </summary>
        /// <param name="cmsTreeType"></param>
        /// <param name="lang"></param>
        /// <param name="currentUser"></param>
        /// <returns></returns>
        public async Task<int> TreeShake(string cmsTreeType, string? lang, string? currentUser)
        {
            var document = await GetCmsTreeItem(cmsTreeType, lang).ConfigureAwait(false);
            if (document == null)
                return 0;

            var potential = document.Nodes.Where(x => !x.CmsItemId.HasValue);
            var allParentIds = document.Nodes.Select(x => x.ParentId);

            var deleteNodes = potential.Where(x => !allParentIds.Contains(x.NodeId)).Select(x => x.NodeId).ToList();

            if(deleteNodes.Any())
            {
                int removed = document.Nodes.RemoveAll(x => deleteNodes.Contains(x.NodeId));
                await dataProvider.Write<CmsTreeItem>(document, cmsTreeType, Guid.Empty, lang: null, currentUser).ConfigureAwait(false);
                return removed;
            }

            return 0;
        }

        public async Task<CmsTreeNode> CreateOrUpdateCmsTreeNodeForSlug(string cmsTreeType, string slug, CmsTreeNode node, string? lang, string? currentUser)
        {
            var document = await GetCmsTreeItem(cmsTreeType, lang).ConfigureAwait(false);
            document ??= new CmsTreeItem();

            List<string> slugParts = GetSlugList(slug);

            var all = document.Nodes;

            // Add empty nodes to support full structure
            List<Guid?> nodeTreeIds = new List<Guid?>();
            foreach (var part in slugParts)
            {
                var lastNode = nodeTreeIds.LastOrDefault();

                var current = all.FirstOrDefault(x => x.Name == part && x.ParentId == lastNode);
                if (current == null)
                {
                    current = new CmsTreeNode
                    {
                        Name = part,
                        ParentId = lastNode
                    };
                    all.Add(current);
                }

                nodeTreeIds.Add(current.NodeId);
            }


            var existing = all.Where(x => x.GetSlug(all) == slug).FirstOrDefault();
            if (existing != null)
                document.Nodes.Remove(existing);

            if(nodeTreeIds.Count >1)
                node.ParentId = nodeTreeIds[^2];

            node.NodeId = existing!.NodeId;
            node.Name = slugParts.Last();
            document.Nodes.Add(node);

            await dataProvider.Write<CmsTreeItem>(document, cmsTreeType, Guid.Empty, lang: null, currentUser).ConfigureAwait(false);

            return node;
        }

        private static List<string> GetSlugList(string slug)
        {
            var slugParts = slug.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            slugParts.Insert(0, "/");
            return slugParts;
        }

        public async Task<List<CmsTreeNode>> GetCmsTreeNodesForCmsItemId(string cmsTreeType, Guid cmsItemId, string? lang)
        {
            var treeItem = await GetCmsTreeItem(cmsTreeType, lang).ConfigureAwait(false);
            if (treeItem == null)
                return new List<CmsTreeNode>();

            return treeItem.Nodes.Where(x => x.CmsItemId == cmsItemId).ToList();
        }

        public async Task<List<CmsTreeNode>> GetCmsTreeNodesForNodeId(string cmsTreeType, Guid nodeId, string? lang)
        {
            var treeItem = await GetCmsTreeItem(cmsTreeType, lang).ConfigureAwait(false);
            if (treeItem == null)
                return new List<CmsTreeNode>();

            return treeItem.Nodes.Where(x => x.NodeId == nodeId).ToList();
        }

        public async Task<CmsTreeNode?> GetCmsTreeNode(string cmsTreeType, string slug, string? lang)
        {
            var treeItem = await GetCmsTreeItem(cmsTreeType, lang).ConfigureAwait(false);
            if (treeItem == null)
                return null;

            var all = treeItem.Nodes;

            return treeItem.Nodes.Where(x => x.GetSlug(all) == slug).FirstOrDefault();
        }


    }
}