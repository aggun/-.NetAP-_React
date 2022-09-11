using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using AutoMapper;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Products
{
    public class List
    {
         public class ProductsEnvelope
        {
            public List<ProductDto> Products { get; set; }
            public int ProductCount { get; set; }
        }
        public class Query : IRequest<ProductsEnvelope>
        {
            public Query(int? limit, int? offset, bool isLiked, bool isHost, DateTime? startDate)
            {
                Limit = limit;
                Offset = offset;
                IsLiked = isLiked;
                IsHost = isHost;
                StartDate = startDate ?? DateTime.Now;
            }
            public int? Limit { get; set; }
            public int? Offset { get; set; }
            public bool IsLiked { get; set; }
            public bool IsHost { get; set; }
            public DateTime? StartDate { get; set; }
        }

        public class Handler : IRequestHandler<Query, ProductsEnvelope>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;
            private readonly IUserAccessor _userAccessor;
            public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
            {
                _userAccessor = userAccessor;
                _mapper = mapper;
                _context = context;
            }

            public async Task<ProductsEnvelope> Handle(Query request, CancellationToken cancellationToken)
            {
                var queryable = _context.Products
                    .Where(x => x.Date >= request.StartDate)
                    .OrderBy(x => x.Date)
                    .AsQueryable();

                if (request.IsLiked && !request.IsHost)
                {
                    queryable = queryable.Where(x => x.UserProducts.Any(a => a.AppUser.UserName == _userAccessor.GetCurrentUsername()));
                }

                if (request.IsHost && !request.IsLiked)
                {
                    queryable = queryable.Where(x => x.UserProducts.Any(a => a.AppUser.UserName == _userAccessor.GetCurrentUsername() && a.IsHost));
                }

                var products = await queryable
                    .Skip(request.Offset ?? 0)
                    .Take(request.Limit ?? 3).ToListAsync();

                return new ProductsEnvelope
                {
                    Products = _mapper.Map<List<Product>, List<ProductDto>>(products),
                    ProductCount = queryable.Count()
                };
            }
        }
    }
}