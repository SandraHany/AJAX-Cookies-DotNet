using CookiesOpml.Pages;
using static CookiesOpml.Pages.OpmlModel;
namespace CookiesOpml
{
    public class RssListService
    {
        public List<RssModelClass> RssListGlobal { get; set; } = new List<RssModelClass>();
        public void Update(List<OpmlModel.RssModelClass> newList)
        {
            RssListGlobal = newList;
        }
        public void LoadPage(int page)
        {

        }

    }
}
