using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Camefor.Tools.Xml;

namespace Camefor.Tools.Extensions
{

    /// <summary>
    /// 特殊扩展
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// 获取两个实体字段参数不一致的字段集合
        /// 增加字段的注释显示
        /// </summary>
        /// <param name="original"></param>
        /// <param name="updated"></param>
        /// <returns></returns>
        public static List<Tuple<string, string, object, object>> GetChangedFieldsWithComment<T>(object original, object updated)
        {
            var result = new List<Tuple<string, string, object, object>>();

            var comment = XmlDocumentHelper.GetPropCommentByClassType<T>();


            Type type = original.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead)
                {
                    continue;
                }

                object originalValue = property.GetValue(original);
                object updatedValue = property.GetValue(updated);

                if (!Equals(originalValue, updatedValue))
                {
                    var propertyName = property.Name;
                    var propertyComment = propertyName;
                    if (comment.TryGetValue(propertyName, out string propertyDocumentation))
                    {
                        propertyComment = propertyDocumentation ?? string.Empty;
                    }
                    else
                    {
                        propertyComment = propertyName;
                    }

                    if (propertyName != null && propertyComment != null)
                    {
                        result.Add(Tuple.Create(propertyName, propertyComment, originalValue, updatedValue));
                    }
                }
            }

            return result;
        }
    }
}
