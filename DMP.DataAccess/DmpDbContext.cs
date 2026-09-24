using System.Text.Json;
using System.Text.Json.Serialization;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Order;
using Microsoft.EntityFrameworkCore;

namespace DMP.DataAccess;

public class DmpDbContext(DbContextOptions<DmpDbContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions UserFeatureWriteOptions =
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private static readonly JsonSerializerOptions UserFeatureReadOptions = new() { PropertyNameCaseInsensitive = true };

    public DbSet<MenuDAL> Menus { get; set; } = null!;
    public DbSet<MenuCategoryDAL> MenuCategories { get; set; } = null!;
    public DbSet<TranslationDAL> Translations { get; set; } = null!;
    public DbSet<ProductDAL> Products { get; set; } = null!;
    public DbSet<FeatureDAL> Features { get; set; } = null!;
    public DbSet<ProductFeatureDAL> ProductFeatures { get; set; } = null!;
    public DbSet<MailDAL> Mails { get; set; } = null!;
    public DbSet<UserDAL> Users { get; set; } = null!;
    public DbSet<EmailConfirmationDAL> EmailConfirmations { get; set; } = null!;
    public DbSet<ApplicationSettingsDAL> ApplicationSettings { get; set; } = null!;
    public DbSet<OrderHeaderDAL> OrderHeaders { get; set; } = null!;
    public DbSet<OrderLineDAL> OrderLines { get; set; } = null!;
    public DbSet<CartItemDAL> CartItems { get; set; } = null!;
    public DbSet<InvoiceWorkerTaskDAL> InvoiceWorkerTasks { get; set; } = null!;
    public DbSet<PaymentDAL> Payments { get; set; } = null!;
    public DbSet<TransactionWorkerTaskDAL> TransactionWorkerTasks { get; set; } = null!;
    public DbSet<EmailTemplateDAL> EmailTemplates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuDAL>(entity =>
        {
            entity.ToTable("Menu");
            entity.HasKey(p => p.MenuId);

            entity.Property(p => p.Description)
                .HasColumnName("Description")
                .HasMaxLength(100);
        });

        modelBuilder.Entity<MenuCategoryDAL>(entity =>
        {
            entity.ToTable("MenuCategory");
            entity.HasKey(p => p.MenuCategoryId);

            entity.Property(e => e.MenuId)
                .HasColumnName("MenuId");

            entity.Property(p => p.Name)
                .IsRequired()
                .HasColumnName("Name")
                .HasMaxLength(100);

            entity.Property(p => p.ParentId)
                .HasColumnName("ParentId");

            entity.Property(p => p.Order)
                .HasColumnName("Order");

            entity.Property(p => p.Slug)
                .IsRequired()
                .HasColumnName("Slug")
                .HasMaxLength(5000);

            entity.HasOne(p => p.Menu)
                .WithMany(m => m.Categories)
                .HasForeignKey(k => k.MenuId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Menu_MenuId_MenuId");

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MenuCategory_ParentId");
        });

        modelBuilder.Entity<TranslationDAL>(entity =>
        {
            entity.ToTable("Translation");
            entity.HasKey(p => p.Key);

            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(t => t.Translations)
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<ProductDAL>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(p => p.ProductId);

            entity.Property(e => e.MenuCategoryId)
                .IsRequired();

            entity.Property(t => t.UserFeature)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, UserFeatureWriteOptions),
                    v => JsonSerializer.Deserialize<UserFeatureDAL>(v, UserFeatureReadOptions)
                )
                .HasColumnType("jsonb");

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(e => e.SellerId).IsRequired();
            entity.Property(e => e.Price).IsRequired();
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Unlimited).IsRequired();
            entity.Property(e => e.ImgLinks);

            entity.HasOne(p => p.MenuCategory)
                .WithMany(m => m.Products)
                .HasForeignKey(k => k.MenuCategoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MenuCategory_MenuCategoryId");
        });

        modelBuilder.Entity<FeatureDAL>(entity =>
        {
            entity.ToTable("Feature");
            entity.HasKey(p => p.FeatureId);

            entity.Property(e => e.Name)
                .IsRequired();


        });

        modelBuilder.Entity<ProductFeatureDAL>(entity =>
        {
            entity.ToTable("ProductFeature");
            entity.HasKey(p => new { p.ProductId, p.FeatureId });

            entity.Property(e => e.Value);

            entity.HasOne(p => p.Product)
                .WithMany(i => i.ProductFeatures)
                .HasForeignKey(p => p.ProductId);

            entity.HasOne(p => p.Feature)
                .WithMany(f => f.ProductFeatures)
                .HasForeignKey(p => p.FeatureId);
        });

        modelBuilder.Entity<MailDAL>(entity =>
        {
            entity.ToTable("Mail");
            entity.HasKey(p => p.MailId);

            entity.Property(e => e.MailId).HasColumnName("MailId").IsRequired();
            entity.Property(e => e.To).HasColumnName("To").IsRequired().HasMaxLength(320);
            entity.Property(e => e.From).HasColumnName("From").IsRequired().HasMaxLength(320);
            entity.Property(e => e.Subject).HasColumnName("Subject").HasMaxLength(988);
            entity.Property(e => e.Copy).HasColumnName("Copy").HasMaxLength(1000);
            entity.Property(e => e.Body).HasColumnName("Body");
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql();
            entity.Property(e => e.Attempts).HasColumnName("Attempts").IsRequired();
            entity.Property(e => e.EmailTemplateId);
            entity.Property(t => t.Model);

            entity.HasOne(p => p.EmailTemplate)
                .WithMany(f => f.Mails)
                .HasForeignKey(p => p.EmailTemplateId);
        });

        modelBuilder.Entity<UserDAL>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(p => p.UserId);

            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Email).HasColumnName("Email").IsRequired().HasMaxLength(320);
            entity.Property(e => e.Password).HasColumnName("Password").IsRequired();
            entity.Property(e => e.Salt).HasColumnName("Salt").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.Status).HasColumnName("Status").HasDefaultValueSql();
            entity.Property(e => e.Flags).HasColumnName("Flags").IsRequired();
            entity.Property(e => e.Language).HasColumnName("Language").IsRequired();
        });

        modelBuilder.Entity<EmailConfirmationDAL>(entity =>
        {
            entity.ToTable("EmailConfirmation");
            entity.HasKey(p => p.EmailConfirmationId);

            entity.Property(e => e.EmailConfirmationId).HasColumnName("EmailConfirmationId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Token).HasColumnName("Token").IsRequired();
            entity.Property(e => e.Used).HasColumnName("Used").HasDefaultValueSql();
        });

        modelBuilder.Entity<ApplicationSettingsDAL>(entity =>
        {
            entity.ToTable("ApplicationSettings");
            entity.HasKey(p => p.Key);

            entity.Property(e => e.Key).HasColumnName("Key").IsRequired();
            entity.Property(e => e.Value).HasColumnName("Value").HasColumnType("jsonb");
        });

        modelBuilder.Entity<OrderHeaderDAL>(entity =>
        {
            entity.ToTable("OrderHeader");
            entity.HasKey(p => p.OrderId);
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.ReferenceNumber).HasColumnName("ReferenceNumber").HasMaxLength(200);
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();
        });

        modelBuilder.Entity<OrderLineDAL>(entity =>
        {
            entity.ToTable("OrderLine");
            entity.HasKey(p => p.OrderLineId);
            entity.Property(p => p.OrderLineId).IsRequired();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("ProductId").IsRequired();
            entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
            entity.Property(e => e.Price).HasColumnName("Price").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("Quantity").IsRequired();
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();

            entity.HasOne(p => p.OrderHeader)
                .WithMany(m => m.OrderLines)
                .HasForeignKey(k => k.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_OrderLine_OrderId_OrderHeader_OrderId");
        });

        modelBuilder.Entity<CartItemDAL>(entity =>
        {
            entity.ToTable("CartItem");
            entity.HasKey(p => new { p.UserId, p.ProductId });
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("ProductId").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("Quantity").IsRequired();
            entity.Property(e => e.Selected).HasColumnName("Selected").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
        });

        modelBuilder.Entity<InvoiceWorkerTaskDAL>(entity =>
        {
            entity.ToTable("InvoiceWorkerTask");
            entity.HasKey(p => p.InvoiceId);
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
        });

        modelBuilder.Entity<PaymentDAL>(entity =>
        {
            entity.ToTable("Payment");
            entity.HasKey(p => p.PaymentId);
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("Amount").IsRequired();
            entity.Property(e => e.CurrencyCode).HasColumnName("CurrencyCode");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
        });

        modelBuilder.Entity<TransactionWorkerTaskDAL>(entity =>
        {
            entity.ToTable("TransactionWorkerTask");
            entity.HasKey(p => p.InvoiceId);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql();
            entity.Property(e => e.Attempts).HasColumnName("Attempts").HasDefaultValueSql();
            entity.Property(e => e.SourceTransactionId).HasColumnName("SourceTransactionId");
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Comment);
        });

        modelBuilder.Entity<EmailTemplateDAL>(entity =>
        {
            entity.ToTable("EmailTemplate");
            entity.HasKey(p => p.EmailTemplateId);
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.Subject).IsRequired();
            entity.Property(p => p.Body).IsRequired();
            entity.Property(p => p.Language).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
