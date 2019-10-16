using System;
using System.Collections.Generic;

namespace OurPlace.API.Models
{
    public class ActivityCollection : Model
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public virtual ICollection<Place> Places { get; set; }
        public bool IsTrail { get; set; }
        public bool IsPublic { get; set; }
        public string ActivityOrder { get; set; }

        public virtual ApplicationUser Author { get; set; }
        public string QRCodeUrl { get; set; }
        public bool Approved { get; set; }
        public string InviteCode { get; set; }
        public bool SoftDeleted { get; set; }

        public virtual ICollection<LearningActivity> Activities { get; set; }
        public virtual Application Application { get; set; }

        public int CollectionVersionNumber { get; set; }
    }
}