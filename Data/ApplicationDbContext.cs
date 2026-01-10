using LifeNetAssist.MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace LifeNetAssist.MVC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<HelpRequest> HelpRequests { get; set; }
        public DbSet<VolunteerProfile> VolunteerProfiles { get; set; }
    }
}
