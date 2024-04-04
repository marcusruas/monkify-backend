using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Common.Models
{
    public class PaginatedList<T>
    {
        public PaginatedList() { }

        public PaginatedList(IEnumerable<T> items, int currentPage, int requestedAmountOfRecords, int totalNumberOfRecords)
        {
            Items = items;
            CurrentPage = currentPage;
            TotalNumberOfRecords = totalNumberOfRecords;
            TotalNumberOfPages = (int)Math.Ceiling(totalNumberOfRecords / (double)requestedAmountOfRecords);
        }

        public IEnumerable<T> Items { get; private set; }    
        public int CurrentPage { get; private set; }
        public int TotalNumberOfRecords { get; private set; }
        public int TotalNumberOfPages { get; private set; }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> query, int page, int recordsPerPage)
        {
            var itens = await query.Skip((page - 1) * recordsPerPage).Take(recordsPerPage).ToListAsync();
            var totalRecordCount = await query.CountAsync();

            return new PaginatedList<T>(itens, page, recordsPerPage, totalRecordCount);
        }

        public static PaginatedList<TDestination> CreateFromPaginatedList<TDestination, TSource>(IEnumerable<TDestination> items, PaginatedList<TSource> paginatedList)
        {
            var result = new PaginatedList<TDestination>();
            result.Items = items;
            result.CurrentPage = paginatedList.CurrentPage;
            result.TotalNumberOfRecords = paginatedList.TotalNumberOfRecords;
            result.TotalNumberOfPages = paginatedList.TotalNumberOfPages;

            return result;
        }
    }
}
