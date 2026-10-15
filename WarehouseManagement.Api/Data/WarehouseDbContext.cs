using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Entities;

namespace WarehouseManagement.Api.Data;

public partial class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
        : base(options)
    {
    }

    // Đăng ký đầy đủ các bảng nghiệp vụ kho cũ
    public virtual DbSet<Material> Materials { get; set; }
    public virtual DbSet<Warehouse> Warehouses { get; set; }
    public virtual DbSet<StockTransaction> StockTransactions { get; set; }
    public virtual DbSet<StockTransactionDetail> StockTransactionDetails { get; set; }

    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();

    // --- BỔ SUNG: Đăng ký 3 bảng phục vụ xác thực người dùng Step 6.1 ---
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Cấu hình bảng Materials (Giữ nguyên theo schema cũ)
        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId);
            entity.Property(e => e.MaterialCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.MaterialName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BaseUnit).HasMaxLength(20).IsRequired();
        });

        // 2. Cấu hình bảng Warehouses (Giữ nguyên theo schema cũ)
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId);
            entity.Property(e => e.WarehouseCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.WarehouseName).HasMaxLength(200).IsRequired();
        });

        // 3. Cấu hình bảng StockTransactions (Giữ nguyên theo schema cũ)
        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(e => e.StockTransactionId);
            entity.HasIndex(e => new { e.WarehouseId, e.Status, e.PostedAt }, "IX_StockTransactions_Warehouse_Status_PostedAt");
            entity.HasIndex(e => e.TransactionNumber, "UQ_StockTransactions_TransactionNumber").IsUnique();
            entity.HasIndex(e => e.ReversesTransactionId, "UX_StockTransactions_ReversesTransaction")
                .IsUnique()
                .HasFilter("([ReversesTransactionId] IS NOT NULL)");

            entity.Property(e => e.CreatedAt).HasPrecision(0).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PostedAt).HasPrecision(0);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Draft");
            entity.Property(e => e.TransactionNumber).HasMaxLength(50);
            entity.Property(e => e.TransactionType).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasPrecision(0).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ReversesTransaction).WithOne(p => p.InverseReversesTransaction)
                .HasForeignKey<StockTransaction>(d => d.ReversesTransactionId)
                .HasConstraintName("FK_StockTransactions_Reverses");
        });

        // 4. Cấu hình bảng StockTransactionDetails (Giữ nguyên theo schema cũ)
        modelBuilder.Entity<StockTransactionDetail>(entity =>
        {
            entity.HasKey(e => e.StockTransactionDetailId);
            entity.Property(e => e.StockTransactionDetailId).ValueGeneratedOnAdd();
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 3)");

            entity.HasOne(d => d.StockTransaction)
                .WithMany(p => p.StockTransactionDetails)
                .HasForeignKey(d => d.StockTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Material)
                .WithMany(p => p.StockTransactionDetails)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 5. Cấu hình bảng InventoryBalances (Giữ nguyên theo schema cũ)
        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("InventoryBalances");
            entity.HasKey(e => new { e.MaterialId, e.WarehouseId });
            entity.Property(e => e.QuantityOnHand).HasColumnType("decimal(18,3)").HasDefaultValue(0m);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.Material).WithMany().HasForeignKey(e => e.MaterialId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Warehouse).WithMany().HasForeignKey(e => e.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        // --- BỔ SUNG 6. Cấu hình bảng Users (Khớp chính xác theo ảnh SSMS thực tế) ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId); // Khóa chính là UserId (không phải Id)
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).HasPrecision(0);
        });

        // --- BỔ SUNG 7. Cấu hình bảng Roles ---
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
        });

        // --- BỔ SUNG 8. Cấu hình bảng liên kết UserRoles (Khóa ngoại phức hợp) ---
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => new { e.UserId, e.RoleId }); // Khóa chính kết hợp

            entity.HasOne(d => d.User)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Role)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
