using OurPlace.Common.Models;
using System;
using System.Collections.Generic;

namespace OurPlace.Common.Models
{
    public class FeedItem : Model
    {
        public DateTime CreatedAt { get; set; }
        public int AppVersionNumber { get; set; }
        public LimitedApplicationUser Author { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public bool IsPublic { get; set; }
        public bool Approved { get; set; }
        public string InviteCode { get; set; }
        public IEnumerable<Place> Places { get; set; }
    }
}
