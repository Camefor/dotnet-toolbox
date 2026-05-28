using System;
using System.Collections.Generic;
using System.Text;
using Camefor.Tools.Collections;

namespace Camefor.Tools.Extensions
{
    /// <summary>
    /// 描   述  ：  List扩展                        
    /// 版   本  ： V1.0.0                            
    /// 创 建 人 ： rhyswang                                  
    /// 日    期 ：                         
    /// 创 建 人 ：                                   
    /// 创建时间 ：                                  
    /// 修 改 人 ：                                   
    /// 修改时间 ：                                   
    /// 修改描述 ：                                   
    /// </summary> 

    public static partial class Extensions
    {

        /// <summary>
        /// 扩展去重
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// 获取list的分页数据
        /// </summary>
        /// <param name="obj">list对象</param>
        /// <param name="pagination">分页参数</param>
        /// <returns></returns>
        public static List<T> FindPage<T>(this List<T> obj, Pagination pagination) where T : class
        {
            try
            {
                pagination.records = obj.Count;
                int index = (pagination.page - 1) * pagination.rows;
                if (index >= obj.Count)
                {
                    return new List<T>();
                }
                int end = index + pagination.rows;
                int count = end > obj.Count ? obj.Count - index : pagination.rows;
                List<T> list = obj.GetRange(index, count);
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("获取list的分页数据出错：" + ex.Message);
            }
        }
        /// <summary>
        /// object集合转T集合
        /// </summary>
        /// <param name="list">object集合</param>
        /// <returns></returns>
        //public static List<T> ToDynamicList<T>(this List<object> list) where T : class
        //{
        //    try
        //    {
        //        List<T> result = new List<T>();
        //        list.ForEach(f =>
        //        {
        //            result.Add(f.ToDynamic<T>());
        //        });
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("获取list的分页数据出错：" + ex.Message);
        //    }
        //}
        /// <summary>
        /// object集合转double集合
        /// </summary>
        /// <param name="list">decimal集合</param>
        /// <returns></returns>
        public static List<double> ToDoubleList(this List<object> list)
        {
            try
            {
                List<double> result = new List<double>();
                list.ForEach(f =>
                {
                    result.Add(f.ToDouble());
                });
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("获取list的分页数据出错：" + ex.Message);
            }
        }
        /// <summary>
        /// decimal集合转double集合
        /// </summary>
        /// <param name="list">decimal集合</param>
        /// <returns></returns>
        public static List<double> ToDoubleList(this List<decimal> list)
        {
            try
            {
                List<double> result = new List<double>();
                list.ForEach(f =>
                {
                    result.Add(f.ToDouble());
                });
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("获取list的分页数据出错：" + ex.Message);
            }
        }
    }
}
