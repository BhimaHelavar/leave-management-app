using LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

// Schema "people". House rules: no cascade deletes, no hard deletes (use IsActive).

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> b)
    {
        b.ToTable("Location", "people");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.CountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        b.Property(x => x.TimeZoneId).HasMaxLength(50).IsUnicode(false).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employee", "people");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.ExternalId).HasMaxLength(64).IsUnicode(false);
        b.Property(x => x.JoinDate).HasColumnType("date");

        b.HasIndex(x => x.Email).IsUnique();

        // A plain unique index allows only one NULL in SQL Server, so filter out the NULLs.
        b.HasIndex(x => x.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");

        b.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EmployeeRoleConfiguration : IEntityTypeConfiguration<EmployeeRole>
{
    public void Configure(EntityTypeBuilder<EmployeeRole> b)
    {
        b.ToTable("EmployeeRole", "people", t =>
            t.HasCheckConstraint("CK_EmployeeRole_Role", EnumColumn.InList<Role>("Role")));

        b.HasKey(x => new { x.EmployeeId, x.Role });
        b.Property(x => x.Role).AsText(10);

        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
