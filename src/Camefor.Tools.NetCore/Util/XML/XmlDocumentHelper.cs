using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Camefor.Tools.Util
{

    /// <summary>
    /// 解析xml
    /// </summary>
    public class XmlDocumentHelper
    {

        /// <summary>
        /// read comments from xml document
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Dictionary<string, string> GetPropCommentByClassType<T>()
        {
            var dicComments = new Dictionary<string, string>();//save comments data, the dictionary, key is class name or prop name. and value is comment
            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties();

            var path = type.Assembly.Location;
            var file = new FileInfo(path);
            var xmlPath = type.Assembly.Location.Replace(file.Extension, ".xml");

            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException($"{file.Name} xml document not found,please configure project propperty,Generate documentation");
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            // get class comment
            XmlNode classNode = xmlDoc.SelectSingleNode($"/doc/members/member[@name='T:{type.FullName}']");
            string classComment = classNode?.SelectSingleNode("summary")?.InnerText.Trim();
            dicComments.Add(type.FullName ?? "classFullName", classComment);

            //get property or field comment
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                //var memberName = property.MemberType switch
                //{
                //    MemberTypes.Property => $"P:{property?.DeclaringType?.FullName}.{property?.Name}",
                //    MemberTypes.Field => $"F:{property?.DeclaringType?.FullName}.{property?.Name}",
                //    _ => throw new NotSupportedException($"Unsupported member type: {property.MemberType}")
                //};
                var memberName = string.Empty;
                switch (property.MemberType)
                {
                    case MemberTypes.Field:
                        memberName = $"F:{property?.DeclaringType?.FullName}.{property?.Name}";
                        break;
                    case MemberTypes.Property:
                        memberName = $"P:{property?.DeclaringType?.FullName}.{property?.Name}";
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported member type: {property.MemberType}");
                }
                var summaryNode = xmlDoc.SelectSingleNode($"//member[starts-with(@name, '{memberName}')]/summary");
                var propertyDocumentation = summaryNode?.InnerText.Trim();
                dicComments.Add(propertyName, propertyDocumentation);
            }

            return dicComments;
        }
    }
}
