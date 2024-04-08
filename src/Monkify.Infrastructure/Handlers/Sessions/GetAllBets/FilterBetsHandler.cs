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
    public class FilterBetsHandler : BaseRequestHandler<FilterBetsRequest, PaginatedList<BetHistoryDto>>
    {
        public FilterBetsHandler(MonkifyDbContext context, IMessaging messaging) : base(context, messaging) { }

        public override async Task<PaginatedList<BetHistoryDto>> HandleRequest(FilterBetsRequest request, CancellationToken cancellationToken)
        {
            var query = Context.SessionBets.Include(x => x.User);

            if (request.OrderByWon.HasValue)
            {
                if (request.OrderByWon == OrderBy.Asc)
                    query.OrderBy(x => x.Won);
                else
                    query.OrderByDescending(x => x.Won);
            }

            if (request.OrderByDate.HasValue)
            {
                if (request.OrderByDate == OrderBy.Asc)
                    query.OrderBy(x => x.CreatedDate);
                else
                    query.OrderByDescending(x => x.CreatedDate);
            }

            if (request.OrderByAmount.HasValue)
            {
                if (request.OrderByAmount == OrderBy.Asc)
                    query.OrderBy(x => x.BetAmount);
                else
                    query.OrderByDescending(x => x.BetAmount);
            }

            if (request.OrderByChoice.HasValue)
            {
                if (request.OrderByChoice == OrderBy.Asc)
                    query.OrderBy(x => x.BetChoice);
                else
                    query.OrderByDescending(x => x.BetChoice);
            }

            var result = await PaginatedList<Bet>.CreateAsync(query, request.PageNumber.Value, request.PageSize.Value);
            return PaginatedList<BetHistoryDto>.CreateFromPaginatedList(result.Items.Select(x => new BetHistoryDto(x)), result);
        }
    }
}
