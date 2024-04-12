using MediatR;
using Monkify.Common.Models;
using Monkify.Infrastructure.ResponseTypes.Sessions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.GetAllBets
{
    public class FilterBetsRequest : IRequest<PaginatedList<BetDto>>
    {
        [Required(ErrorMessage = "Page number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be bigger than 1")]
        public int? PageNumber { get; set; }
        [Required(ErrorMessage = "Page size is required")]
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int? PageSize { get; set; }
    }
}
