using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

namespace Myrtille.Enterprise
{
    internal sealed class MigrationConfiguration : DbMigrationsConfiguration<Myrtille.Enterprise.MyrtilleEnterpriseDBContext>
    {
        public MigrationConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }

        protected override void Seed(Myrtille.Enterprise.MyrtilleEnterpriseDBContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
        }
    }
}
