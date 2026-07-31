using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Camefor.Tools.Linq
{
    public class ExpressionAndLambdaHelper
    {

        /// <summary>
        /// 根据属性名称 生成具体的 实际的 委托
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="TProperty"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static LambdaExpression CreatePropertyGetterExpression(Type entityType, Type TProperty, string propertyName)
        {
            PropertyInfo property = entityType.GetProperty(propertyName);
            var parameter = Expression.Parameter(TProperty, "e");
            var body = Expression.MakeMemberAccess(parameter, property);
            return Expression.Lambda(body, parameter);
        }

        /// <summary>
        /// 根据属性名称 生成具体的 实际的 委托
        /// </summary>
        /// <typeparam name="TEntity">泛型参数：entity model的类型</typeparam>
        /// <param name="propertyName">其中要生成的Func委托目标属性名称</param>
        /// <returns></returns>
        public static LambdaExpression CreatePropertyGetterExpression<TEntity>(string propertyName)
        {
            var entityType = typeof(TEntity);
            PropertyInfo property = entityType.GetProperty(propertyName);
            var parameter = Expression.Parameter(entityType, "e");
            var body = Expression.MakeMemberAccess(parameter, property);
            return Expression.Lambda(body, parameter);
            //usage:  Func<TEntity, TProp> func1 = (Func<TEntity, TProp>)lambda.Compile();
            //等效于：GetFuncT 方法
        }

        /// <summary>
        /// 根据类 类型和属性名称创建泛型Func委托
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="propertyName"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <returns></returns>
        public static Func<T, TProperty> GetFuncT<T, TProperty>(Type entityType, string propertyName)
        {
            Func<T, TProperty> getPropertyDelegate = GetExpression<T, TProperty>(entityType, propertyName).Compile();
            return getPropertyDelegate;
        }

        /// <summary>
        /// 根据类 类型和属性名称创建 Expression TDelegate> 类
        /// 将强类型化的 Lambda 表达式表示为表达式树形式的数据结构。
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="propertyName"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <returns></returns>
        public static Expression<Func<T, TProperty>> GetExpression<T, TProperty>(Type entityType, string propertyName)
        {
            // 创建实体类型参数
            ParameterExpression entityParameter = Expression.Parameter(entityType, "Property");
            // 创建属性访问表达式
            MemberExpression propertyExpression = Expression.Property(entityParameter, "Name");
            // 创建委托类型参数
            ParameterExpression delegateParameter = Expression.Parameter(entityType);
            // 创建委托表达式
            Expression<Func<T, TProperty>> lambdaExpression
                = Expression.Lambda<Func<T, TProperty>>(propertyExpression, delegateParameter);
            return lambdaExpression;
        }


    }
}
