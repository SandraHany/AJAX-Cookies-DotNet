using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using static CookiesOpml.Pages.OpmlModel;

namespace CookiesOpml.Pages
{
    public class StarredModel : PageModel
    {

        private readonly RssListService _rssListService;

        public StarredModel(RssListService rssListService)
        {
            _rssListService = rssListService;
        }

        //public List<OpmlModel.RssModelClass> RssList => _rssListService.RssListGlobal;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalItemCount { get; set; }
        public List<RssModelClass> RssListStarred { get; set; } = new();
        public List<RssModelClass> RssListGlobal { get; set; } = new();
        public async Task<IActionResult> OnGetAsync(int? page)
        {
        
            var likedItemGuids = Request.Cookies["liked"]?.Split("_") ?? new string[0];

            string newItemGuid=null;
            string[] splitResult = null;
            var rssListStarred = new List<RssModelClass>();

            foreach (var rss in _rssListService.RssListGlobal)
            {
                var starredItems = new List<RssItem>();

                foreach (var item in rss.Items)
                {
                    if (!string.IsNullOrEmpty(item.Guid) && likedItemGuids.Contains(item.Guid))
                    {
                            starredItems.Add(item);
                        
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
    }
}

