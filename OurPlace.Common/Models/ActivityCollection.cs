using System;
using System.Collections.Generic;
using System.Text;

namespace OurPlace.Common.Models
{
    public class ActivityCollection : LimitedActivityCollection
    {
        public virtual LimitedApplicationUser Author { get; set; }
    }

    public class LimitedActivityCollection : Model
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public virtual Place Location { get; set; }
        public bool IsTrail { get; set; }
        public bool IsPublic { get; set; }
        public string ActivityOrder { get; set; }

        public string QRCodeUrl { get; set; }
        public bool Approved { get; set; }
        public string InviteCode { get; set; }
        public bool SoftDeleted { get; set; }

        public virtual IEnumerable<LimitedLearningActivity> Activities { get; set; }
        public virtual Application Application { get; set; }

        public int CollectionVersionNumber { get; set; }
    }
}