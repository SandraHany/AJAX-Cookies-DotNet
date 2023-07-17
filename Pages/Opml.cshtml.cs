using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using System.Web;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace CookiesOpml.Pages;

public class OpmlModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly List<RssModelClass> _rssList = new List<RssModelClass>();
    private readonly RssListService _rssListService;
    private readonly ILogger<OpmlModel> _logger;
    private static bool _isLoaded = false;
    public List<RssModelClass> RssList { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public static int TotalItemCount { get; set; } = 0;
    private static int Counter { get; set; } = 0;
    public List<RssModelClass> RssListGlobal { get; set; } = new();
    public static List<int> PreviouslyLoadedPages { get; set; } = new();
    public OpmlModel(IHttpClientFactory httpClientFactory, RssListService rssListService, ILogger<OpmlModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _rssListService = rssListService;
        _logger = logger;
    }
        public async Task<IActionResult> OnGetAsync(int? page)
    { 
        PageNumber = Convert.ToInt32(Request.Query["page"]);
        _logger.LogInformation($"PageNumber: {PageNumber}, PageSize: {PageSize}");
        if (!_isLoaded)
        {
            var client = _httpClientFactory.CreateClient();
            using (client)
            {
                var response = await client.GetStringAsync("https://blue.feedland.org/opml?screenname=dave");
                var doc = new XmlDocument();
                doc.LoadXml(response);
                var manager = new XmlNamespaceManager(doc.NameTable);
                manager.AddNamespace("opml", "http://www.opml.org/spec2");
                var outlineNodes = doc.SelectNodes("//outline[@xmlUrl]", manager).Cast<XmlNode>();
                TotalItemCount = outlineNodes.Count();
                foreach (var node in outlineNodes)
                {
                    var modelObject = new RssModelClass();
                    modelObject.Text = node.Attributes["text"].Value;
                    modelObject.XmlUrl = node.Attributes["xmlUrl"].Value;
                    modelObject.HtmlUrl = node.Attributes["htmlUrl"]?.Value ?? "";
                    modelObject.Items = new List<RssItem>();
                    _rssListService.RssListGlobal.Add(modelObject);
                }
            }

            _isLoaded = true;
        }
        var filteredRssList = _rssListService.RssListGlobal
        .Skip((PageNumber - 1) * PageSize)
        .Take(PageSize)
        .ToList();
        if (!PreviouslyLoadedPages.Contains(PageNumber)){
            foreach (var modelObject in filteredRssList)
            {
                if (!modelObject.Items.Any())
                {
                    using (var client = _httpClientFactory.CreateClient())
                    {
                        var feedResponse = await client.GetStringAsync(modelObject.XmlUrl);
                        var feedDoc = new XmlDocument();
                        feedDoc.LoadXml(feedResponse);
                        foreach (XmlNode itemNode in feedDoc.SelectNodes("/rss/channel/item"))
                        {
                            Counter++;
                            var item = new RssItem();
                            item.Link = itemNode["link"]?.InnerText;
                            item.Description = itemNode["description"]?.InnerText;
                            item.PublishDate = DateTime.TryParse(itemNode["pubDate"]?.InnerText, out var publishDate) ? publishDate : DateTime.MinValue;
                            item.Guid = Counter.ToString();
                            modelObject.Items.Add(item);
                        }
                    }
                }

            }
            PreviouslyLoadedPages.Add(PageNumber);
        }
        string likedCookie = Request.Cookies["liked"];
        string[] likedItems = null;
        if (likedCookie != null && likedCookie.Length > 0)
        {
           likedItems = likedCookie.Split("_");
        }
        if (likedItems != null)
        {
            foreach (var rss in filteredRssList)
            {
                foreach (var item in rss.Items)
                {
                    if (!string.IsNullOrEmpty(item.Guid) && likedItems.Contains(item.Guid))
                    {
                        item.IsStarred = true;
                    }
                    else if (!string.IsNullOrEmpty(item.Guid) && !likedItems.Contains(item.Guid) && item.IsStarred)
                    {
                        item.IsStarred = false;
                    }
                }
            }
        }
        foreach (var rss in filteredRssList)
        {
            foreach (var item in rss.Items)
            {
                var existingRssModel = _rssListService.RssListGlobal.FirstOrDefault(r => r.Items.Any(i => i.Guid == item.Guid));
                if (existingRssModel != null)
                {
                    existingRssModel.Items.First(i => i.Guid == item.Guid).IsStarred = item.IsStarred;
                    int index = _rssListService.RssListGlobal.FindIndex(r => r.Items.Any(i => i.Guid == item.Guid));
                    _rssListService.RssListGlobal[index] = existingRssModel;
                }
                else
                {
                    _rssListService.RssListGlobal.Add(new RssModelClass
                    {
                        Text = rss.Text,
                        XmlUrl = rss.XmlUrl,
                        HtmlUrl = rss.HtmlUrl,
                        Items = new List<RssItem> { item }
                    });
                }
            }
        }
        RssList = filteredRssList;
        string requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
        return Page();
    }

    public class RssModelClass
    {
        public string Text { get; set; }
        public string XmlUrl { get; set; }
        public string HtmlUrl { get; set; }
        public List<RssItem> Items { get; set; }
    }

    public class RssItem
    {
        public string Link { get; set; }
        public string Description { get; set; } 
        public DateTime PublishDate { get; set; }
        public string Guid { get; set; }
        public bool IsStarred { get; set; }
    }
}

