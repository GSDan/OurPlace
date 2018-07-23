#region copyright
/*
    OurPlace is a mobile learning platform, designed to support communities
    in creating and sharing interactive learning activities about the places they care most about.
    https://github.com/GSDan/OurPlace
    Copyright (C) 2018 Dan Richardson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see https://www.gnu.org/licenses.
*/
#endregion
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OurPlace.API.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string ImageUrl { get; set; }
        public DateTime DateCreated { get; set; }
        public string FirstName { get; set; }
        public string AuthProvider { get; set; }
        public string Surname { get; set; }
        public bool Trusted { get; set; }
        public DateTime LastConsent { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType = DefaultAuthenticationTypes.ApplicationCookie)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<Application> Applications { get; set; }

        public DbSet<Place> Places { get; set; }

        public DbSet<PlaceLocality> PlaceLocalities { get; set; }

        public DbSet<LearningActivity> LearningActivities { get; set; }

        public DbSet<LearningTask> LearningActivityTasks { get; set; }

        public DbSet<TaskType> TaskTypes { get; set; }
        
        public DbSet<CompletedActivity> CompletedActivities { get; set; }

        public DbSet<CompletedTask> CompletedTasks { get; set; }

        public DbSet<UploadShare> UploadShares { get; set; }

        public DbSet<UserDevice> UserDevices { get; set; }

        public DbSet<UsageLog> UsageLogs { get; set; }

        public DbSet<UsageLogType> LogTypes { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LearningTask>()
                .HasOptional(lt => lt.ParentTask)
                .WithMany(lt => lt.ChildTasks)
                .HasForeignKey(lt => lt.ParentTaskId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<LearningTask>()
                .HasOptional(lt => lt.LearningActivity)
                .WithMany(la => la.LearningTasks)
                .HasForeignKey(lt => lt.LearningActivityId)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Place>()
                .Property(p => p.Latitude).HasPrecision(11, 7);
            modelBuilder.Entity<Place>()
                .Property(p => p.Longitude).HasPrecision(11, 7);

            modelBuilder.Entity<PlaceLocality>()
                .Property(p => p.Latitude).HasPrecision(11, 7);
            modelBuilder.Entity<PlaceLocality>()
                .Property(p => p.Longitude).HasPrecision(11, 7);

            modelBuilder.Entity<CompletedActivity>()
                .HasOptional(a => a.Share)
                .WithRequired(s => s.Upload);

            base.OnModelCreating(modelBuilder);
        }
    }
}