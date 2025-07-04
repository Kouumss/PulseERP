// UpdateAddressModel.cs
namespace PulseERP.Abstractions.Common.DTOs.Common.Models;

public sealed record UpdateAddressModel(
    string? Street = null,
    string? City = null,
    string? ZipCode = null,
    string? Country = null
);
