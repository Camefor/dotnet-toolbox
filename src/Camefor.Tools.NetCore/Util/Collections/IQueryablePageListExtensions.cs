//using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Camefor.Tools.Util
{
    /// <summary>
    /// 
    /// </summary>
    public static class IQueryablePageListExtensions
    {
        /// <summary>
        /// 在数据中取得固定页的数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="pageIndex">当前页</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="indexFrom">起始页</param>
        /// <param name="cancellationToken">异步观察参数</param>
        /// <returns></returns>
        public static IPagedList<T> ToPagedList<T>(this IQueryable<T> source, int pageIndex, int pageSize, int indexFrom = 1, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (indexFrom > pageIndex)
            {
                throw new ArgumentException($"indexFrom: {indexFrom} > pageIndex: {pageIndex}, must indexFrom <= pageIndex");
            }

            var count = source.Count();
            var items = source.Skip((pageIndex - indexFrom) * pageSize)
                                    .Take(pageSize).ToList();

            var pagedList = new PagedList<T>()
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                IndexFrom = indexFrom,
                TotalCount = count,
                Items = items,
                TotalPages = (int)Math.Ceiling(count / (double)pageSize)
            };

            return pagedList;
        }
    }
}
//针对IQueryable、IEnumerable类型的数据做了分页扩展方法封装，主要用于向数据库获取数据时进行分页筛选

