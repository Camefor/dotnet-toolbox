using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Camefor.Tools.Util
{
    /// <summary>
    /// 描   述  ： 扩展.可空类型                         
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
        /// 拆分一个较的大数据集合为几个小数据集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">数据源</param>
        /// <param name="takeCount">拆分的一个小数据集合所含元素数量</param>
        /// <returns></returns>
        public static List<IEnumerable<T>> SplitList<T>(this IEnumerable<T> source, int takeCount = 1000)
        {
            List<IEnumerable<T>> listOfLists = new List<IEnumerable<T>>();
            for (int i = 0; i < source.Count(); i += takeCount)
            {
                listOfLists.Add(source.Skip(i).Take(takeCount));
            }


            //var splitedTotal = listOfLists.Select(c => c.Count()).Sum();
            //if (source.Count() == splitedTotal)
            //{
            //}
            //else
            //{
            //    //throw new InvalidOperationException("拆分一个较的大数据集合为几个小数据集合处理异常");
            //}

            return listOfLists;

        }


        /// <summary>
        /// DataRow 数据获取特定列的数据 处理行不包含列异常情况
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public static object GetValue(this DataRow row, string column)
        {
            return row.Table.Columns.Contains(column) ? row[column] : null;
        }

        /// <summary>
        /// 安全返回值
        /// </summary>
        /// <param name="value">可空值</param>
        public static T SafeValue<T>(this T? value) where T : struct
        {
            return value ?? default(T);
        }
        /// <summary>
        /// 是否包含
        /// </summary>
        /// <param name="obj">字串</param>
        /// <param name="value">包含字串</param>
        /// <returns></returns>
        public static bool ContainsEx(this string obj, string value)
        {
            if (string.IsNullOrEmpty(obj))
            {
                return false;
            }
            else
            {
                return obj.Contains(value);
            }
        }
        /// <summary>
        /// 字串是否在指定字串中存在
        /// </summary>
        /// <param name="obj">字串</param>
        /// <param name="value">被包含字串</param>
        /// <returns></returns>
        public static bool Like(this string obj, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            else if (string.IsNullOrEmpty(obj))
            {
                return false;
            }
            else
            {
                if (value.IndexOf(obj) != -1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
