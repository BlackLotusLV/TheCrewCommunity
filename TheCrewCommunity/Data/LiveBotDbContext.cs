using Microsoft.EntityFrameworkCore;
using TheCrewCommunity.Data.GameData;
using TheCrewCommunity.Data.WebData;
using TheCrewCommunity.Data.WebData.ProSettings;

namespace TheCrewCommunity.Data;

public class LiveBotDbContext : DbContext
{
    public DbSet<StreamNotifications> StreamNotifications { get; init; }
    public DbSet<User> Users { get; init; }
    public DbSet<GuildUser> GuildUsers { get; init; }
    public DbSet<Infraction> Infractions { get; init; }
    public DbSet<Guild> Guilds { get; init; }
    public DbSet<RankRoles> RankRoles { get; init; }
    public DbSet<ModMail> ModMail { get; init; }
    public DbSet<RoleTagSettings> RoleTagSettings { get; init; }
    public DbSet<SpamIgnoreChannels> SpamIgnoreChannels { get; init; }
    public DbSet<ButtonRoles> ButtonRoles { get; init; }
    public DbSet<UbiInfo> UbiInfo { get; init; }
    public DbSet<UserActivity> UserActivity { get; init; }
    public DbSet<WhiteListSettings> WhiteListSettings { get; init; }
    public DbSet<WhiteList> WhiteLists { get; init; }
    public DbSet<PhotoCompSettings> PhotoCompSettings { get; init; }
    public DbSet<PhotoCompEntries> PhotoCompEntries { get; init; }
    public DbSet<Tag> Tags { get; init; }
    public DbSet<VanityWhitelist> VanityWhitelist { get; init; }
    public DbSet<ApplicationUser> ApplicationUsers { get; init; }
    public DbSet<Brand> Brands { get; init; }
    public DbSet<Game> Games { get; init; }
    public DbSet<Vehicle> Vehicles { get; init; }
    public DbSet<VehicleCategory> VehicleCategories { get; init; }
    public DbSet<MtfstCarProSettings> MotorfestCarProSettings { get; init; }
    public DbSet<MtfstCarProSettingsLikes> MotorfestCarProSettingsLikes { get; set; }

    public LiveBotDbContext()
    {
    }
    public LiveBotDbContext(DbContextOptions<LiveBotDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ButtonRoles>().HasKey(x => x.Id);
        modelBuilder.Entity<Guild>().HasKey(x => x.Id);
        modelBuilder.Entity<GuildUser>().HasKey(x => new { x.UserDiscordId, x.GuildId });
        modelBuilder.Entity<Infraction>().HasKey(x => x.Id);
        modelBuilder.Entity<ModMail>().HasKey(x => x.Id);
        modelBuilder.Entity<RankRoles>().HasKey(x => x.Id);
        modelBuilder.Entity<RoleTagSettings>().HasKey(x => x.Id);
        modelBuilder.Entity<SpamIgnoreChannels>().HasKey(x => x.Id);
        modelBuilder.Entity<StreamNotifications>().HasKey(x => x.Id);
        modelBuilder.Entity<UbiInfo>().HasKey(x => x.Id);
        modelBuilder.Entity<User>().HasKey(x => x.DiscordId);
        modelBuilder.Entity<UserActivity>().HasKey(x => x.Id);
        modelBuilder.Entity<WhiteList>().HasKey(x => x.Id);
        modelBuilder.Entity<WhiteListSettings>().HasKey(x => x.Id);
        modelBuilder.Entity<PhotoCompSettings>().HasKey(x => x.Id);
        modelBuilder.Entity<PhotoCompEntries>().HasKey(x => x.Id);
        modelBuilder.Entity<MediaOnlyChannels>().HasKey(x => x.ChannelId);
        modelBuilder.Entity<Tag>().HasKey(x=>x.Id);
        modelBuilder.Entity<VanityWhitelist>().HasKey(x=>x.Id);
        modelBuilder.Entity<Brand>().HasKey(x => x.Id);
        modelBuilder.Entity<Game>().HasKey(x => x.Id);
        modelBuilder.Entity<VehicleCategory>().HasKey(x => x.Id);
        modelBuilder.Entity<Vehicle>().HasKey(x => x.Id);
        modelBuilder.Entity<MtfstCarProSettings>().HasKey(x => x.Id);
        modelBuilder.Entity<MtfstCarProSettingsLikes>().HasKey(x => x.Id);
        
        modelBuilder.Entity<Vehicle>()
            .HasOne(v=>v.VCat)
            .WithMany(vc => vc.Vehicles)
            .HasForeignKey(v => v.VCatId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Game)
            .WithMany(g => g.Vehicles)
            .HasForeignKey(v => v.GameId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Brand)
            .WithMany(b => b.Vehicles)
            .HasForeignKey(v => v.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MtfstCarProSettings>()
            .HasOne(mcp => mcp.ApplicationUser)
            .WithMany(au => au.MotorfestCarProSettings)
            .HasForeignKey(mcp => mcp.DiscordId)
            .HasPrincipalKey(au=>au.DiscordId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MtfstCarProSettings>()
            .HasOne(mcp => mcp.Vehicle)
            .WithMany(v => v.MotorfestCarProSettings)
            .HasForeignKey(mcp => mcp.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
        

        modelBuilder.Entity<MtfstCarProSettingsLikes>()
            .HasOne(likes => likes.MtfstCarProSettings)
            .WithMany(mcp => mcp.MotorfestCarProSettingLikes)
            .HasForeignKey(likes => likes.ProSettingsId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MtfstCarProSettingsLikes>()
            .HasOne(likes => likes.ApplicationUser)
            .WithMany(au => au.MotorfestCarProSettingLikes)
            .HasForeignKey(likes => likes.DiscordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VehicleCategory>()
            .HasOne(vc => vc.Game)
            .WithMany(g => g.VehicleCategories)
            .HasForeignKey(vc => vc.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(a => a.User)
            .WithOne()
            .HasForeignKey<ApplicationUser>(a => a.DiscordId)
            .HasPrincipalKey<User>(u => u.DiscordId);
        
        modelBuilder.Entity<User>()
            .HasOne(u => u.Parent)
            .WithMany(p => p.ChildUsers)
            .HasForeignKey(p => p.ParentDiscordId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasMany(u => u.UbiInfo)
            .WithOne(ui => ui.User)
            .HasForeignKey(ui => ui.UserDiscordId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasMany(u => u.UserGuilds)
            .WithOne(gu => gu.User)
            .HasForeignKey(gu => gu.UserDiscordId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasMany(u => u.PhotoCompEntries)
            .WithOne(pce => pce.User)
            .HasForeignKey(pce => pce.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>()
            .HasMany(u => u.Tags)
            .WithOne(t => t.Owner)
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.GuildUsers)
            .WithOne(gu => gu.Guild)
            .HasForeignKey(gu => gu.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.RankRoles)
            .WithOne(rr => rr.Guild)
            .HasForeignKey(rr => rr.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.SpamIgnoreChannels)
            .WithOne(sic => sic.Guild)
            .HasForeignKey(sic => sic.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.RoleTagSettings)
            .WithOne(rts => rts.Guild)
            .HasForeignKey(rts => rts.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.ButtonRoles)
            .WithOne(br => br.Guild)
            .HasForeignKey(br => br.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.StreamNotifications)
            .WithOne(sn => sn.Guild)
            .HasForeignKey(sn => sn.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.WhiteListSettings)
            .WithOne(wls => wls.Guild)
            .HasForeignKey(wls => wls.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.PhotoCompSettings)
            .WithOne(pcs => pcs.Guild)
            .HasForeignKey(pcs => pcs.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g=>g.MediaOnlyChannels)
            .WithOne(moc=>moc.Guild)
            .HasForeignKey(moc=>moc.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.Tags)
            .WithOne(t => t.Guild)
            .HasForeignKey(t => t.GuildId);
        modelBuilder.Entity<Guild>()
            .HasMany(g => g.WhitelistedVanities)
            .WithOne(vw => vw.Guild)
            .HasForeignKey(vw => vw.GuildId);
        

        modelBuilder.Entity<GuildUser>()
            .HasMany(gu => gu.Infractions)
            .WithOne(i => i.GuildUser)
            .HasForeignKey(i => new { i.UserId, i.GuildId });
        modelBuilder.Entity<GuildUser>()
            .HasMany(gu => gu.ModMails)
            .WithOne(mm => mm.GuildUser)
            .HasForeignKey(mm => new { mm.UserDiscordId, mm.GuildId });
        modelBuilder.Entity<GuildUser>()
            .HasMany(gu => gu.UserActivity)
            .WithOne(ua => ua.GuildUser)
            .HasForeignKey(ua => new { ua.UserDiscordId, ua.GuildId });

        modelBuilder.Entity<WhiteListSettings>()
            .HasMany(wls => wls.WhitelistedUsers)
            .WithOne(wlu => wlu.Settings)
            .HasForeignKey(wlu => wlu.WhiteListSettingsId);

        modelBuilder.Entity<PhotoCompSettings>()
            .HasMany(pcs => pcs.Entries)
            .WithOne(e => e.Competition)
            .HasForeignKey(e => e.CompetitionId);
    }
}