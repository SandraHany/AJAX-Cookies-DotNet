using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using static CookiesOpml.Pages.OpmlModel;

namespace CookiesOpml.Pages;

public class StarredModel : PageModel
{

    private readonly RssListService _rssListService;
    public StarredModel(RssListService rssListService)
    {
        _rssListService = rssListService;
    }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public int TotalItemCount { get; set; } = 0;
    public List<RssModelClass> RssListStarred { get; set; } = new();
    public List<RssModelClass> RssListGlobal { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int? page)
    {
        _rssListService.PrintListJson();
        var likedItemGuids = Request.Cookies["liked"]?.Split("_") ?? new string[0];
        var rssListStarred = new List<RssModelClass>();

        foreach (var rss in _rssListService.RssListGlobal)
        {
            var starredItems = new List<RssItem>();

            foreach (var item in rss.Items)
            {
                if (!string.IsNullOrEmpty(item.Guid) && likedItemGuids.Contains(item.Guid))
                {
                        item.IsStarred = true;
                        starredItems.Add(item);
                    
                }
                else
                {
                    item.IsStarred = false;
                }
            }
            if (starredItems.Any())
            {
                var starredRss = new RssModelClass
                {
                    Text = rss.Text,
                    XmlUrl = rss.XmlUrl,
                    HtmlUrl = rss.HtmlUrl,
                    Items = starredItems
                };

                rssListStarred.Add(starredRss);
            }
        }
         
        RssListStarred = rssListStarred;
        TotalItemCount = RssListStarred.Count();
        return Page();
    }
    [HttpPost]
    public IActionResult OnPostToggleStarred(string guid, bool isStarred)
    {
        var item = FindItemByGuid(guid);
        if (item != null)
        {
            item.IsStarred = isStarred;
            var existingRssModel = _rssListService.RssListGlobal.FirstOrDefault(r => r.Items.Any(i => i.Guid == item.Guid));
            if (existingRssModel != null)
            {
                existingRssModel.Items.First(i => i.Guid == item.Guid).IsStarred = item.IsStarred;
                int index = _rssListService.RssListGlobal.FindIndex(r => r.Items.Any(i => i.Guid == item.Guid));
                _rssListService.RssListGlobal[index] = existingRssModel;
            }
            return new JsonResult(new { isStarred });
        }
        return NotFound();
    }
    private RssItem FindItemByGuid(string guid)
    {
        foreach (var rss in _rssListService.RssListGlobal)
        {
            var item = rss.Items.FirstOrDefault(i => i.Guid == guid);
            if (item != null)
            {
                return item;
            }
        }
        return null;
    }
}

