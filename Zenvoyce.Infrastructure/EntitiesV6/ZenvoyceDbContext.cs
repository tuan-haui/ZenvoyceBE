using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Zenvoyce.Infrastructure.Entities;

public partial class ZenvoyceDbContext : DbContext
{
    public ZenvoyceDbContext()
    {
    }

    public ZenvoyceDbContext(DbContextOptions<ZenvoyceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Danhmuchanghoa> Danhmuchanghoas { get; set; }

    public virtual DbSet<Lichsuguithue> Lichsuguithues { get; set; }

    public virtual DbSet<Lichsuhoadon> Lichsuhoadons { get; set; }

    public virtual DbSet<Mauchocty> Mauchocties { get; set; }

    public virtual DbSet<Mauhoadongoc> Mauhoadongocs { get; set; }

    public virtual DbSet<Nguoidung> Nguoidungs { get; set; }

    public virtual DbSet<Nhomquyen> Nhomquyens { get; set; }

    public virtual DbSet<Phanquyenchucnang> Phanquyenchucnangs { get; set; }

    public virtual DbSet<Qlkyso> Qlkysos { get; set; }

    public virtual DbSet<Sysmenu> Sysmenus { get; set; }

    public virtual DbSet<Thongtinhdmau> Thongtinhdmaus { get; set; }

    public virtual DbSet<Ttcty> Ttcties { get; set; }

    public virtual DbSet<Tthanghoa> Tthanghoas { get; set; }

    public virtual DbSet<Tthoadon> Tthoadons { get; set; }

    public virtual DbSet<Ttkhachhang> Ttkhachhangs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=ep-lively-shape-aoidkfnj-pooler.c-2.ap-southeast-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=npg_H6yWkpeKF5nZ;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Danhmuchanghoa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("danhmuchanghoa_pkey");

            entity.ToTable("danhmuchanghoa");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Dongia)
                .HasPrecision(18, 2)
                .HasColumnName("dongia");
            entity.Property(e => e.Donviid).HasColumnName("donviid");
            entity.Property(e => e.Donvitinh)
                .HasMaxLength(50)
                .HasColumnName("donvitinh");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Tenhanghoa)
                .HasMaxLength(255)
                .HasColumnName("tenhanghoa");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Donvi).WithMany(p => p.Danhmuchanghoas)
                .HasForeignKey(d => d.Donviid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("danhmuchanghoa_donviid_fkey");
        });

        modelBuilder.Entity<Lichsuguithue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lichsuguithue_pkey");

            entity.ToTable("lichsuguithue");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Hoadonid).HasColumnName("hoadonid");
            entity.Property(e => e.Maphanhoi)
                .HasMaxLength(50)
                .HasColumnName("maphanhoi");
            entity.Property(e => e.Ngaygui)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngaygui");
            entity.Property(e => e.Noidungphanhoi).HasColumnName("noidungphanhoi");

            entity.HasOne(d => d.Hoadon).WithMany(p => p.Lichsuguithues)
                .HasForeignKey(d => d.Hoadonid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("lichsuguithue_hoadonid_fkey");
        });

        modelBuilder.Entity<Lichsuhoadon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lichsuhoadon_pkey");

            entity.ToTable("lichsuhoadon");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Hanhdong)
                .HasMaxLength(255)
                .HasColumnName("hanhdong");
            entity.Property(e => e.Hoadonid).HasColumnName("hoadonid");
            entity.Property(e => e.Nguoidungid).HasColumnName("nguoidungid");
            entity.Property(e => e.Thoigian)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("thoigian");
            entity.Property(e => e.Trangthaicu)
                .HasMaxLength(100)
                .HasColumnName("trangthaicu");
            entity.Property(e => e.Trangthaimoi)
                .HasMaxLength(100)
                .HasColumnName("trangthaimoi");

            entity.HasOne(d => d.Hoadon).WithMany(p => p.Lichsuhoadons)
                .HasForeignKey(d => d.Hoadonid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("lichsuhoadon_hoadonid_fkey");

            entity.HasOne(d => d.Nguoidung).WithMany(p => p.Lichsuhoadons)
                .HasForeignKey(d => d.Nguoidungid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("lichsuhoadon_nguoidungid_fkey");
        });

        modelBuilder.Entity<Mauchocty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mauchocty_pkey");

            entity.ToTable("mauchocty");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Css).HasColumnName("css");
            entity.Property(e => e.Donviid).HasColumnName("donviid");
            entity.Property(e => e.Header).HasColumnName("header");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Lamaumacdinh)
                .HasDefaultValue(false)
                .HasColumnName("lamaumacdinh");
            entity.Property(e => e.Maugocid).HasColumnName("maugocid");
            entity.Property(e => e.Ngaykichhoat).HasColumnName("ngaykichhoat");
            entity.Property(e => e.Trangthaiphathanh)
                .HasDefaultValue((short)0)
                .HasColumnName("trangthaiphathanh");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Donvi).WithMany(p => p.Mauchocties)
                .HasForeignKey(d => d.Donviid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("mauchocty_donviid_fkey");

            entity.HasOne(d => d.Maugoc).WithMany(p => p.Mauchocties)
                .HasForeignKey(d => d.Maugocid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("mauchocty_maugocid_fkey");
        });

        modelBuilder.Entity<Mauhoadongoc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mauhoadongoc_pkey");

            entity.ToTable("mauhoadongoc");

            entity.HasIndex(e => e.Kyhieu, "mauhoadongoc_unique").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Cautrucxml).HasColumnName("cautrucxml");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Kyhieu)
                .HasMaxLength(50)
                .HasColumnName("kyhieu");
            entity.Property(e => e.Loaihoadon)
                .HasMaxLength(100)
                .HasColumnName("loaihoadon");
            entity.Property(e => e.Tenmau)
                .HasMaxLength(255)
                .HasColumnName("tenmau");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        });

        modelBuilder.Entity<Nguoidung>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("nguoidung_pkey");

            entity.ToTable("nguoidung");

            entity.HasIndex(e => e.Tendangnhap, "nguoidung_tendangnhap_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Dienthoai)
                .HasMaxLength(20)
                .HasColumnName("dienthoai");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Hoten)
                .HasMaxLength(200)
                .HasColumnName("hoten");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Madonvi).HasColumnName("madonvi");
            entity.Property(e => e.Matkhau)
                .HasMaxLength(255)
                .HasColumnName("matkhau");
            entity.Property(e => e.Tendangnhap)
                .HasMaxLength(50)
                .HasColumnName("tendangnhap");
            entity.Property(e => e.Trangthai)
                .HasDefaultValue((short)1)
                .HasColumnName("trangthai");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.MadonviNavigation).WithMany(p => p.Nguoidungs)
                .HasForeignKey(d => d.Madonvi)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("nguoidung_madonvi_fkey");
        });

        modelBuilder.Entity<Nhomquyen>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("nhomquyen_pkey");

            entity.ToTable("nhomquyen");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Mota)
                .HasMaxLength(255)
                .HasColumnName("mota");
            entity.Property(e => e.Tenquyen)
                .HasMaxLength(100)
                .HasColumnName("tenquyen");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        });

        modelBuilder.Entity<Phanquyenchucnang>(entity =>
        {
            entity.HasKey(e => new { e.Nguoidungid, e.Quyenid, e.Menuid }).HasName("phanquyenchucnang_pkey");

            entity.ToTable("phanquyenchucnang");

            entity.Property(e => e.Nguoidungid).HasColumnName("nguoidungid");
            entity.Property(e => e.Quyenid).HasColumnName("quyenid");
            entity.Property(e => e.Menuid).HasColumnName("menuid");

            entity.HasOne(d => d.Menu).WithMany(p => p.Phanquyenchucnangs)
                .HasForeignKey(d => d.Menuid)
                .HasConstraintName("phanquyenchucnang_menuid_fkey");

            entity.HasOne(d => d.Nguoidung).WithMany(p => p.Phanquyenchucnangs)
                .HasForeignKey(d => d.Nguoidungid)
                .HasConstraintName("phanquyenchucnang_nguoidungid_fkey");

            entity.HasOne(d => d.Quyen).WithMany(p => p.Phanquyenchucnangs)
                .HasForeignKey(d => d.Quyenid)
                .HasConstraintName("phanquyenchucnang_quyenid_fkey");
        });

        modelBuilder.Entity<Qlkyso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("qlkyso_pkey");

            entity.ToTable("qlkyso");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Hoadonid).HasColumnName("hoadonid");
            entity.Property(e => e.Ngayky)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngayky");
            entity.Property(e => e.Nguoiky)
                .HasMaxLength(255)
                .HasColumnName("nguoiky");
            entity.Property(e => e.Thongtinchungchi).HasColumnName("thongtinchungchi");

            entity.HasOne(d => d.Hoadon).WithMany(p => p.Qlkysos)
                .HasForeignKey(d => d.Hoadonid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("qlkyso_hoadonid_fkey");
        });

        modelBuilder.Entity<Sysmenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sysmenu_pkey");

            entity.ToTable("sysmenu");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Duongdan)
                .HasMaxLength(255)
                .HasColumnName("duongdan");
            entity.Property(e => e.Menuchaid).HasColumnName("menuchaid");
            entity.Property(e => e.Quyenid).HasColumnName("quyenid");
            entity.Property(e => e.Tenmenu)
                .HasMaxLength(100)
                .HasColumnName("tenmenu");

            entity.HasOne(d => d.Menucha).WithMany(p => p.InverseMenucha)
                .HasForeignKey(d => d.Menuchaid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sysmenu_menuchaid_fkey");

            entity.HasOne(d => d.Quyen).WithMany(p => p.Sysmenus)
                .HasForeignKey(d => d.Quyenid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("sysmenu_quyenid_fkey");
        });

        modelBuilder.Entity<Thongtinhdmau>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("thongtinhdmau_pkey");

            entity.ToTable("thongtinhdmau");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Canle)
                .HasMaxLength(20)
                .HasColumnName("canle");
            entity.Property(e => e.Font)
                .HasMaxLength(50)
                .HasColumnName("font");
            entity.Property(e => e.Mauctyid).HasColumnName("mauctyid");
            entity.Property(e => e.Tentruong)
                .HasMaxLength(100)
                .HasColumnName("tentruong");
            entity.Property(e => e.Vitrinam)
                .HasMaxLength(50)
                .HasColumnName("vitrinam");

            entity.HasOne(d => d.Maucty).WithMany(p => p.Thongtinhdmaus)
                .HasForeignKey(d => d.Mauctyid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("thongtinhdmau_mauctyid_fkey");
        });

        modelBuilder.Entity<Ttcty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ttcty_pkey");

            entity.ToTable("ttcty");

            entity.HasIndex(e => e.Masothue, "ttcty_masothue_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.BankAccount)
                .HasMaxLength(50)
                .HasColumnName("bank_account");
            entity.Property(e => e.BankId).HasColumnName("bank_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Diachi)
                .HasMaxLength(500)
                .HasColumnName("diachi");
            entity.Property(e => e.Dienthoai)
                .HasMaxLength(20)
                .HasColumnName("dienthoai");
            entity.Property(e => e.Emailcongty)
                .HasMaxLength(100)
                .HasColumnName("emailcongty");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Masothue)
                .HasMaxLength(20)
                .HasColumnName("masothue");
            entity.Property(e => e.Nguoidaidien)
                .HasMaxLength(100)
                .HasColumnName("nguoidaidien");
            entity.Property(e => e.Tendonvi)
                .HasMaxLength(255)
                .HasColumnName("tendonvi");
            entity.Property(e => e.Trangthai)
                .HasDefaultValue((short)0)
                .HasColumnName("trangthai");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        });

        modelBuilder.Entity<Tthanghoa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tthanghoa_pkey");

            entity.ToTable("tthanghoa");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Dongia)
                .HasPrecision(18, 2)
                .HasColumnName("dongia");
            entity.Property(e => e.Hanghoaid).HasColumnName("hanghoaid");
            entity.Property(e => e.Hoadonid).HasColumnName("hoadonid");
            entity.Property(e => e.Soluong)
                .HasPrecision(10, 2)
                .HasColumnName("soluong");
            entity.Property(e => e.Thanhtien)
                .HasPrecision(18, 2)
                .HasColumnName("thanhtien");
            entity.Property(e => e.Thuesuat)
                .HasPrecision(18, 2)
                .HasColumnName("thuesuat");

            entity.HasOne(d => d.Hanghoa).WithMany(p => p.Tthanghoas)
                .HasForeignKey(d => d.Hanghoaid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tthanghoa_hanghoaid_fkey");

            entity.HasOne(d => d.Hoadon).WithMany(p => p.Tthanghoas)
                .HasForeignKey(d => d.Hoadonid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("tthanghoa_hoadonid_fkey");
        });

        modelBuilder.Entity<Tthoadon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tthoadon_pkey");

            entity.ToTable("tthoadon");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Donviid).HasColumnName("donviid");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Khachhangid).HasColumnName("khachhangid");
            entity.Property(e => e.Kyhieu)
                .HasMaxLength(50)
                .HasColumnName("kyhieu");
            entity.Property(e => e.Mauctyid).HasColumnName("mauctyid");
            entity.Property(e => e.Ngaylap)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaylap");
            entity.Property(e => e.Sohoadon)
                .HasMaxLength(50)
                .HasColumnName("sohoadon");
            entity.Property(e => e.ThamchieuHoadonId).HasColumnName("thamchieu_hoadon_id");
            entity.Property(e => e.Thamchieuhoadonid).HasColumnName("thamchieuhoadonid");
            entity.Property(e => e.Tienthue)
                .HasPrecision(18, 2)
                .HasColumnName("tienthue");
            entity.Property(e => e.Tongthanhtoan)
                .HasPrecision(18, 2)
                .HasColumnName("tongthanhtoan");
            entity.Property(e => e.Tongtien)
                .HasPrecision(18, 2)
                .HasColumnName("tongtien");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(50)
                .HasColumnName("trangthai");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.Xmldaky).HasColumnName("xmldaky");

            entity.HasOne(d => d.Donvi).WithMany(p => p.Tthoadons)
                .HasForeignKey(d => d.Donviid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("tthoadon_donviid_fkey");

            entity.HasOne(d => d.Khachhang).WithMany(p => p.Tthoadons)
                .HasForeignKey(d => d.Khachhangid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tthoadon_khachhangid_fkey");

            entity.HasOne(d => d.Maucty).WithMany(p => p.Tthoadons)
                .HasForeignKey(d => d.Mauctyid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tthoadon_mauchocty_fk");
        });

        modelBuilder.Entity<Ttkhachhang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ttkhachhang_pkey");

            entity.ToTable("ttkhachhang");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Dienthoai)
                .HasMaxLength(20)
                .HasColumnName("dienthoai");
            entity.Property(e => e.Donviid).HasColumnName("donviid");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Masothue)
                .HasMaxLength(20)
                .HasColumnName("masothue");
            entity.Property(e => e.Tenkhachhang)
                .HasMaxLength(255)
                .HasColumnName("tenkhachhang");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Donvi).WithMany(p => p.Ttkhachhangs)
                .HasForeignKey(d => d.Donviid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("ttkhachhang_donviid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
