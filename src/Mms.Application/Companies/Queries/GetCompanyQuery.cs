using MediatR;
using Mms.Application.Companies.Dtos;

namespace Mms.Application.Companies.Queries;

public record GetCompanyQuery : IRequest<CompanyDto?>;
