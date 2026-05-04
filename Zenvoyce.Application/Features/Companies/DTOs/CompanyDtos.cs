using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Features.Companies.DTOs;

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Masothue { get; set; } = string.Empty;
    public string Tendonvi { get; set; } = string.Empty;
    public string? Diachi { get; set; }
    public string? Dienthoai { get; set; }
    public string? Nguoidaidien { get; set; }
    public string? Emailcongty { get; set; }
    public int? BankId { get; set; }
    public string? BankAccount { get; set; }
    public short Trangthai { get; set; }

    public static CompanyDto FromDomain(Ttcty x) => new()
    {
        Id = x.Id,
        Masothue = x.Masothue,
        Tendonvi = x.Tendonvi,
        Diachi = x.Diachi,
        Dienthoai = x.Dienthoai,
        Nguoidaidien = x.Nguoidaidien,
        Emailcongty = x.Emailcongty,
        BankId = x.BankId,
        BankAccount = x.BankAccount,
        Trangthai = x.Trangthai
    };
}
