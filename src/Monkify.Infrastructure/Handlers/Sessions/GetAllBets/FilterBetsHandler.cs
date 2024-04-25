using Microsoft.EntityFrameworkCore;
using Monkify.Common.Messaging;
using Monkify.Common.Models;
using Monkify.Domain.Sessions.Entities;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.ResponseTypes.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.GetAllBets
{
    public class FilterBetsHandler : BaseRequestHandler<FilterBetsRequest, PaginatedList<BetDto>>
    {
        public FilterBetsHandler(MonkifyDbContext context, IMessaging messaging) : base(context, messaging) { }

        public override async Task<PaginatedList<BetDto>> HandleRequest(FilterBetsRequest request, CancellationToken cancellationToken)
        {
            var query = Context.SessionBets.Where(x => Bet.PublicBetStatus.Contains(x.Status));

            var result = await PaginatedList<Bet>.CreateAsync(query, request.PageNumber.Value, request.PageSize.Value);
            return PaginatedList<BetDto>.CreateFromPaginatedList(result.Items.Select(x => new BetDto(x)), result);
        }
    }
}
