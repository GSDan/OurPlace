using OurPlace.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace OurPlace.Common.Models
{
    public class ActivityCollection : FeedItem
    {
        public DateTime CreatedAt { get; set; }
        public virtual List<Place> Places { get; set; }
        public bool IsTrail { get; set; }
        public string ActivityOrder { get; set; }
        public string QRCodeUrl { get; set; }
        public string InviteCode { get; set; }
        public bool SoftDeleted { get; set; }
        public virtual List<LearningActivity> Activities { get; set; }
        public virtual Application Application { get; set; }
        public int CollectionVersionNumber { get; set; }
    }
}