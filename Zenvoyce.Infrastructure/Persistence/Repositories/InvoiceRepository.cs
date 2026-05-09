using Microsoft.EntityFrameworkCore;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Invoices.DTOs;
using Zenvoyce.Domain.Entities;
using ZenvoyceDbContext = Zenvoyce.Infrastructure.Entities.ZenvoyceDbContext;

namespace Zenvoyce.Infrastructure.Persistence.Repositories;

public class InvoiceRepository(ZenvoyceDbContext dbContext) : IInvoiceRepository
{
    public async Task CreateDraftInvoiceAsync(Hoadon invoice, IReadOnlyCollection<HoadonHanghoa> items, HoadonLichsu history, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.Tthoadons.Add(new Entities.Tthoadon
            {
                Id = invoice.Id,
                Donviid = invoice.Donviid,
                Khachhangid = invoice.Khachhangid,
                Mauctyid = invoice.Mauctyid,
                Kyhieu = invoice.Kyhieu,
                Sohoadon = invoice.Sohoadon,
                Ngaylap = invoice.Ngaylap,
                Tongtien = invoice.Tongtien,
                Tienthue = invoice.Tienthue,
                Tongthanhtoan = invoice.Tongthanhtoan,
                Trangthai = invoice.Trangthai,
                Xmldaky = invoice.Xmldaky,
                XmlMetadata = invoice.XmlMetadata,
                CreatedAt = invoice.CreatedAt,
                UpdatedAt = invoice.UpdatedAt,
                CreatedBy = invoice.CreatedBy,
                UpdatedBy = invoice.UpdatedBy,
                IsDeleted = invoice.IsDeleted,
                Thamchieuhoadonid = invoice.Thamchieuhoadonid
            });

            var detailEntities = items.Select(item => new Entities.Hoadonchitiet
            {
                Id = item.Id,
                Hoadonid = item.Hoadonid,
                Hanghoaid = item.Hanghoaid,
                Soluong = item.Soluong,
                Dongia = item.Dongia,
                Thuesuat = item.Thuesuat,
                Thanhtien = item.Thanhtien
            });
            dbContext.Hoadonchitiets.AddRange(detailEntities);

            dbContext.Lichsuhoadons.Add(new Entities.Lichsuhoadon
            {
                Id = history.Id,
                Hoadonid = history.Hoadonid,
                Hanhdong = history.Hanhdong,
                Trangthaicu = history.Trangthaicu,
                Trangthaimoi = history.Trangthaimoi,
                Thoigian = history.Thoigian,
                Nguoidungid = history.Nguoidungid
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Hoadon?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Tthoadons
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted != true, cancellationToken);

        return entity is null ? null : MapInvoice(entity);
    }

    public async Task UpdateStatusAsync(
        Guid invoiceId,
        string newStatus,
        HoadonLichsu history,
        DateTime updatedAt,
        Guid? updatedBy,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var invoice = await dbContext.Tthoadons.FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsDeleted != true, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

            invoice.Trangthai = newStatus;
            invoice.UpdatedAt = updatedAt;
            invoice.UpdatedBy = updatedBy;

            dbContext.Lichsuhoadons.Add(new Entities.Lichsuhoadon
            {
                Id = history.Id,
                Hoadonid = history.Hoadonid,
                Hanhdong = history.Hanhdong,
                Trangthaicu = history.Trangthaicu,
                Trangthaimoi = history.Trangthaimoi,
                Thoigian = history.Thoigian,
                Nguoidungid = history.Nguoidungid
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<InvoiceListItemDto>> GetInvoicesAsync(
        Guid? khachhangId,
        string? trangthai,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Tthoadons
            .AsNoTracking()
            .Where(x => x.IsDeleted != true);

        if (khachhangId.HasValue)
        {
            query = query.Where(x => x.Khachhangid == khachhangId.Value);
        }

        if (!string.IsNullOrWhiteSpace(trangthai))
        {
            var normalized = trangthai.Trim().ToLower();
            query = query.Where(x => x.Trangthai != null && x.Trangthai.ToLower() == normalized);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.Ngaylap.HasValue && x.Ngaylap.Value >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.Ngaylap.HasValue && x.Ngaylap.Value <= toDate.Value);
        }

        return await query
            .OrderByDescending(x => x.Ngaylap)
            .Select(x => new InvoiceListItemDto
            {
                Id = x.Id,
                DonviId = x.Donviid ?? Guid.Empty,
                KhachhangId = x.Khachhangid ?? Guid.Empty,
                MauctyId = x.Mauctyid ?? Guid.Empty,
                Kyhieu = x.Kyhieu,
                Sohoadon = x.Sohoadon,
                Ngaylap = x.Ngaylap ?? DateTime.MinValue,
                Tongtien = x.Tongtien ?? 0m,
                Tienthue = x.Tienthue ?? 0m,
                Tongthanhtoan = x.Tongthanhtoan ?? 0m,
                Trangthai = x.Trangthai ?? string.Empty,
                TenKhachhang = x.Khachhang != null ? x.Khachhang.Tenkhachhang : string.Empty,
                MaSoThueKhachhang = x.Khachhang != null ? x.Khachhang.Masothue : null,
                EmailKhachhang = x.Khachhang != null ? x.Khachhang.Email : null,
                TenDonvi = x.Donvi != null ? x.Donvi.Tendonvi : string.Empty,
                TenMau = x.Maucty != null && x.Maucty.Maugoc != null ? x.Maucty.Maugoc.Tenmau : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<HoadonLichsu>> GetInvoiceHistoryAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var history = await dbContext.Lichsuhoadons
            .AsNoTracking()
            .Where(x => x.Hoadonid == invoiceId)
            .OrderByDescending(x => x.Thoigian)
            .ToListAsync(cancellationToken);

        return history.Select(x => new HoadonLichsu
        {
            Id = x.Id,
            Hoadonid = x.Hoadonid ?? Guid.Empty,
            Hanhdong = x.Hanhdong ?? string.Empty,
            Trangthaicu = x.Trangthaicu,
            Trangthaimoi = x.Trangthaimoi,
            Thoigian = x.Thoigian ?? DateTime.MinValue,
            Nguoidungid = x.Nguoidungid
        }).ToArray();
    }

    public async Task<IReadOnlyCollection<SalesReportRow>> GetSalesByCustomerAsync(
        Guid? donviId,
        Guid? khachhangId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var q = dbContext.Tthoadons
            .AsNoTracking()
            .Where(x => x.IsDeleted != true && x.Trangthai != null && x.Trangthai.ToLower() == "issued");

        if (donviId.HasValue)
        {
            q = q.Where(x => x.Donviid == donviId.Value);
        }

        if (khachhangId.HasValue)
        {
            q = q.Where(x => x.Khachhangid == khachhangId.Value);
        }

        if (fromDate.HasValue)
        {
            q = q.Where(x => x.Ngaylap.HasValue && x.Ngaylap.Value >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            q = q.Where(x => x.Ngaylap.HasValue && x.Ngaylap.Value <= toDate.Value);
        }

        var grouped = await q
            .GroupBy(x => x.Khachhangid)
            .Select(g => new
            {
                KhachHangId = g.Key,
                SoHoaDon = g.Count(),
                TongTienHang = g.Sum(x => x.Tongtien ?? 0),
                TienThue = g.Sum(x => x.Tienthue ?? 0),
                TongThanhToan = g.Sum(x => x.Tongthanhtoan ?? 0)
            })
            .ToListAsync(cancellationToken);

        var khIds = grouped.Select(x => x.KhachHangId).Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        var names = await dbContext.Ttkhachhangs
            .AsNoTracking()
            .Where(x => khIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Tenkhachhang ?? string.Empty, cancellationToken);

        return grouped
            .Select(x => new SalesReportRow
            {
                KhachhangId = x.KhachHangId ?? Guid.Empty,
                TenKhachHang = x.KhachHangId.HasValue && names.TryGetValue(x.KhachHangId.Value, out var n)
                    ? n
                    : "(Không xác định)",
                SoHoaDon = x.SoHoaDon,
                TongTienHang = x.TongTienHang,
                TienThue = x.TienThue,
                TongThanhToan = x.TongThanhToan
            })
            .OrderByDescending(x => x.TongThanhToan)
            .ToArray();
    }

    public async Task UpdateSignedAsync(
        Guid invoiceId,
        string xmlDaky,
        HoadonLichsu history,
        DateTime updatedAt,
        Guid? updatedBy,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var invoice = await dbContext.Tthoadons.FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsDeleted != true, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

            invoice.Trangthai = "Signed";
            invoice.Xmldaky = xmlDaky;
            invoice.UpdatedAt = updatedAt;
            invoice.UpdatedBy = updatedBy;

            dbContext.Lichsuhoadons.Add(new Entities.Lichsuhoadon
            {
                Id = history.Id,
                Hoadonid = history.Hoadonid,
                Hanhdong = history.Hanhdong,
                Trangthaicu = history.Trangthaicu,
                Trangthaimoi = history.Trangthaimoi,
                Thoigian = history.Thoigian,
                Nguoidungid = history.Nguoidungid
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdatePublishedAsync(
        Guid invoiceId,
        string soHoadon,
        HoadonLichsu history,
        DateTime updatedAt,
        Guid? updatedBy,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var invoice = await dbContext.Tthoadons.FirstOrDefaultAsync(x => x.Id == invoiceId && x.IsDeleted != true, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

            invoice.Trangthai = "Issued";
            invoice.Sohoadon = soHoadon;
            invoice.UpdatedAt = updatedAt;
            invoice.UpdatedBy = updatedBy;

            dbContext.Lichsuhoadons.Add(new Entities.Lichsuhoadon
            {
                Id = history.Id,
                Hoadonid = history.Hoadonid,
                Hanhdong = history.Hanhdong,
                Trangthaicu = history.Trangthaicu,
                Trangthaimoi = history.Trangthaimoi,
                Thoigian = history.Thoigian,
                Nguoidungid = history.Nguoidungid
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<HoadonHanghoa>> GetInvoiceLinesAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var entities = await dbContext.Hoadonchitiets
            .AsNoTracking()
            .Where(x => x.Hoadonid == invoiceId)
            .ToListAsync(cancellationToken);

        return entities.Select(x => new HoadonHanghoa
        {
            Id = x.Id,
            Hoadonid = x.Hoadonid ?? Guid.Empty,
            Hanghoaid = x.Hanghoaid ?? Guid.Empty,
            Soluong = x.Soluong ?? 0m,
            Dongia = x.Dongia ?? 0m,
            Thuesuat = x.Thuesuat ?? 0m,
            Thanhtien = x.Thanhtien ?? 0m
        }).ToArray();
    }

    private static Hoadon MapInvoice(Entities.Tthoadon entity)
    {
        return new Hoadon
        {
            Id = entity.Id,
            Donviid = entity.Donviid ?? Guid.Empty,
            Khachhangid = entity.Khachhangid ?? Guid.Empty,
            Mauctyid = entity.Mauctyid ?? Guid.Empty,
            Kyhieu = entity.Kyhieu,
            Sohoadon = entity.Sohoadon,
            Ngaylap = entity.Ngaylap ?? DateTime.MinValue,
            Tongtien = entity.Tongtien ?? 0,
            Tienthue = entity.Tienthue ?? 0,
            Tongthanhtoan = entity.Tongthanhtoan ?? 0,
            Trangthai = entity.Trangthai ?? string.Empty,
            Xmldaky = entity.Xmldaky,
            XmlMetadata = entity.XmlMetadata,
            CreatedAt = entity.CreatedAt ?? DateTime.MinValue,
            UpdatedAt = entity.UpdatedAt ?? DateTime.MinValue,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            IsDeleted = entity.IsDeleted ?? false,
            Thamchieuhoadonid = entity.Thamchieuhoadonid
        };
    }

    public async Task<int> GetInvoiceCountByDateAsync(Guid donviId, DateTime date, CancellationToken cancellationToken)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);
        
        return await dbContext.Tthoadons
            .CountAsync(x => x.Donviid == donviId && 
                             x.Ngaylap >= startOfDay && 
                             x.Ngaylap < endOfDay, 
                        cancellationToken);
    }
}
