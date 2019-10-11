using OurPlace.Common.Models;

namespace OurPlace.Common.Models
{
    public class FeedItem : Model
    {
        public LimitedApplicationUser Author { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public bool IsPublic { get; set; }
        public bool Approved { get; set; }
    }
}
