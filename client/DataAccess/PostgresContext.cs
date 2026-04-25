using System;
using System.Collections.Generic;
using DominiShop.Model;
using Microsoft.EntityFrameworkCore;

namespace DominiShop.DataAccess;

public partial class PostgresContext : DbContext
{
    public PostgresContext(DbContextOptions<PostgresContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerTier> CustomerTiers { get; set; }

    public virtual DbSet<CustomerVoucher> CustomerVouchers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderVoucher> OrderVouchers { get; set; }

    public virtual DbSet<Owner> Owners { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Tax> Taxes { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("graphql", "pg_graphql")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categorie_pkey");

            entity.ToTable("category");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('categorie_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Owner).WithMany(p => p.Categories)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("category_owner_id_fkey");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => new { e.Phone, e.OwnerId }).HasName("customer_pkey");

            entity.ToTable("customer");

            entity.HasIndex(e => e.Email, "customer_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "customer_username_key").IsUnique();

            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.TierId).HasColumnName("tier_id");
            entity.Property(e => e.TotalPoints).HasColumnName("total_points");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Username).HasColumnName("username");

            entity.HasOne(d => d.Owner).WithMany(p => p.Customers)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("customer_owner_id_fkey");

            entity.HasOne(d => d.Tier).WithMany(p => p.Customers)
                .HasForeignKey(d => d.TierId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("customer_tier_id_fkey");
        });

        modelBuilder.Entity<CustomerTier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_tier_pkey");

            entity.ToTable("customer_tier");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.MinPoint).HasColumnName("min_point");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Percent).HasColumnName("percent");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Owner).WithMany(p => p.CustomerTiers)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("customer_tier_owner_id_fkey");
        });

        modelBuilder.Entity<CustomerVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_voucher_pkey");

            entity.ToTable("customer_voucher");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.Voucher).WithMany(p => p.CustomerVouchers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("customer_voucher_voucher_id_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_pkey");

            entity.ToTable("order");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.OrderAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("order_at");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.ShippingFee).HasColumnName("shipping_fee");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.Orders)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_owner_id_fkey");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Orders)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("order_voucher_id_fkey");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_detail_pkey");

            entity.ToTable("order_detail");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("order_detail_order_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("order_detail_food_id_fkey");
        });

        modelBuilder.Entity<OrderVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_voucher_pkey");

            entity.ToTable("order_voucher");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.VoucherId).HasColumnName("voucher_id");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderVouchers)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_voucher_order_id_fkey");

            entity.HasOne(d => d.Voucher).WithMany(p => p.OrderVouchers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("order_voucher_voucher_id_fkey");
        });

        modelBuilder.Entity<Owner>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("owner_pkey");

            entity.ToTable("owner");

            entity.HasIndex(e => e.Email, "owner_email_key").IsUnique();

            entity.HasIndex(e => e.Email, "owner_email_key1").IsUnique();

            entity.HasIndex(e => e.Username, "owner_username_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.Username).HasColumnName("username");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("food_pkey");

            entity.ToTable("product");

            entity.HasIndex(e => e.Name, "food_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('food_id_seq'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.BasePrice).HasColumnName("base_price");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Sold).HasColumnName("sold");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("food_category_id_fkey");

            entity.HasOne(d => d.Owner).WithMany(p => p.Products)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("food_owner_id_fkey");
        });

        modelBuilder.Entity<Tax>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tax_pkey");

            entity.ToTable("tax", tb => tb.HasComment("Tax"));

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Percent).HasColumnName("percent");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Owner).WithMany(p => p.Taxes)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("tax_owner_id_fkey");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("voucher_pkey");

            entity.ToTable("voucher");

            entity.HasIndex(e => e.Code, "voucher_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxPerPerson).HasColumnName("max_per_person");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Percent).HasColumnName("percent");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Owner).WithMany(p => p.Vouchers)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("voucher_owner_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
