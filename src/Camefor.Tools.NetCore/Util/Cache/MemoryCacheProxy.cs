using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Camefor.Tools.Cache
{
    /// <summary>
    /// 创建能够识别 <see cref="MemoryCacheAttribute"/> 的接口代理。
    /// </summary>
    public static class MemoryCacheProxy
    {
        /// <summary>
        /// 为接口实现创建内存缓存代理。
        /// </summary>
        /// <typeparam name="TService">服务接口类型。</typeparam>
        /// <param name="target">真实的接口实现对象。</param>
        /// <param name="cacheStore">可选的缓存容器；不传时使用全局共享容器。</param>
        /// <returns>应用了特性缓存行为的接口代理。</returns>
        public static TService Create<TService>(TService target, MemoryCacheStore cacheStore = null)
            where TService : class
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!typeof(TService).GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("内存缓存代理仅支持接口类型。", nameof(TService));
            }

            var proxy = DispatchProxy.Create<TService, MemoryCacheDispatchProxy<TService>>();
            ((MemoryCacheDispatchProxy<TService>)(object)proxy).Initialize(target, cacheStore ?? MemoryCacheStore.Shared);
            return proxy;
        }
    }

    internal class MemoryCacheDispatchProxy<TService> : DispatchProxy
        where TService : class
    {
        private static readonly MethodInfo InvokeTaskMethod = GetGenericHelperMethod(nameof(InvokeTaskAsync));
        private static readonly MethodInfo InvokeValueTaskMethod = GetGenericHelperMethod(nameof(InvokeValueTaskAsync));
        private readonly ConcurrentDictionary<MethodInfo, MethodCacheMetadata> _methodMetadata =
            new ConcurrentDictionary<MethodInfo, MethodCacheMetadata>();
        private TService _target;
        private MemoryCacheStore _cacheStore;

        internal void Initialize(TService target, MemoryCacheStore cacheStore)
        {
            _target = target;
            _cacheStore = cacheStore;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod == null)
            {
                throw new ArgumentNullException(nameof(targetMethod));
            }

            var metadata = _methodMetadata.GetOrAdd(targetMethod, CreateMethodMetadata);
            if (metadata.CacheAttribute == null)
            {
                return InvokeTarget(metadata.TargetMethod, args);
            }

            EnsureCacheable(metadata.TargetMethod);
            var cacheKey = BuildCacheKey(metadata, args);
            var returnType = metadata.TargetMethod.ReturnType;

            if (returnType.GetTypeInfo().IsGenericType)
            {
                var genericType = returnType.GetGenericTypeDefinition();
                var resultType = returnType.GenericTypeArguments[0];
                if (genericType == typeof(Task<>))
                {
                    return InvokeGenericHelper(InvokeTaskMethod, resultType, metadata, args, cacheKey);
                }

                if (genericType == typeof(ValueTask<>))
                {
                    return InvokeGenericHelper(InvokeValueTaskMethod, resultType, metadata, args, cacheKey);
                }
            }

            return InvokeSynchronously(metadata, args, cacheKey);
        }

        private object InvokeSynchronously(MethodCacheMetadata metadata, object[] args, string cacheKey)
        {
            return _cacheStore.GetOrCreateAsync(
                    cacheKey,
                    TimeSpan.FromSeconds(metadata.CacheAttribute.ExpirationSeconds),
                    () => Task.FromResult(InvokeTarget(metadata.TargetMethod, args)),
                    value => metadata.CacheAttribute.CacheDefaultValues || !IsDefaultValue(value, metadata.TargetMethod.ReturnType))
                .GetAwaiter()
                .GetResult();
        }

        private async Task<TResult> InvokeTaskAsync<TResult>(
            MethodCacheMetadata metadata,
            object[] args,
            string cacheKey)
        {
            var value = await _cacheStore.GetOrCreateAsync(
                    cacheKey,
                    TimeSpan.FromSeconds(metadata.CacheAttribute.ExpirationSeconds),
                    async () => (object)await ((Task<TResult>)InvokeTarget(metadata.TargetMethod, args)).ConfigureAwait(false),
                    result => metadata.CacheAttribute.CacheDefaultValues || !IsDefaultValue(result, typeof(TResult)))
                .ConfigureAwait(false);

            return (TResult)value;
        }

        private async ValueTask<TResult> InvokeValueTaskAsync<TResult>(
            MethodCacheMetadata metadata,
            object[] args,
            string cacheKey)
        {
            var value = await _cacheStore.GetOrCreateAsync(
                    cacheKey,
                    TimeSpan.FromSeconds(metadata.CacheAttribute.ExpirationSeconds),
                    async () => (object)await ((ValueTask<TResult>)InvokeTarget(metadata.TargetMethod, args)).ConfigureAwait(false),
                    result => metadata.CacheAttribute.CacheDefaultValues || !IsDefaultValue(result, typeof(TResult)))
                .ConfigureAwait(false);

            return (TResult)value;
        }

        private MethodCacheMetadata CreateMethodMetadata(MethodInfo interfaceMethod)
        {
            var implementationMethod = ResolveImplementationMethod(interfaceMethod);
            var cacheAttribute = interfaceMethod.GetCustomAttribute<MemoryCacheAttribute>(true)
                ?? implementationMethod.GetCustomAttribute<MemoryCacheAttribute>(true);

            return new MethodCacheMetadata(implementationMethod, cacheAttribute, interfaceMethod.GetParameters());
        }

        private MethodInfo ResolveImplementationMethod(MethodInfo interfaceMethod)
        {
            var interfaceType = interfaceMethod.DeclaringType;
            var interfaceMap = _target.GetType().GetTypeInfo().GetRuntimeInterfaceMap(interfaceType);
            var methodToFind = interfaceMethod.IsGenericMethod
                ? interfaceMethod.GetGenericMethodDefinition()
                : interfaceMethod;

            for (var index = 0; index < interfaceMap.InterfaceMethods.Length; index++)
            {
                var mappedInterfaceMethod = interfaceMap.InterfaceMethods[index];
                var mappedMethodToCompare = mappedInterfaceMethod.IsGenericMethod
                    ? mappedInterfaceMethod.GetGenericMethodDefinition()
                    : mappedInterfaceMethod;
                if (mappedMethodToCompare != methodToFind)
                {
                    continue;
                }

                var implementationMethod = interfaceMap.TargetMethods[index];
                if (interfaceMethod.IsGenericMethod)
                {
                    implementationMethod = implementationMethod.MakeGenericMethod(interfaceMethod.GetGenericArguments());
                }

                return implementationMethod;
            }

            throw new MissingMethodException(_target.GetType().FullName, interfaceMethod.Name);
        }

        private string BuildCacheKey(MethodCacheMetadata metadata, object[] args)
        {
            var keySource = new StringBuilder();
            keySource.Append(typeof(TService).AssemblyQualifiedName);
            keySource.Append('|').Append(metadata.TargetMethod.DeclaringType.FullName);
            keySource.Append('|').Append(metadata.TargetMethod.Name);
            keySource.Append('|').Append(metadata.TargetMethod.MetadataToken.ToString(CultureInfo.InvariantCulture));

            if (metadata.TargetMethod.IsGenericMethod)
            {
                foreach (var genericArgument in metadata.TargetMethod.GetGenericArguments())
                {
                    keySource.Append('|').Append(genericArgument.AssemblyQualifiedName);
                }
            }

            for (var index = 0; index < args.Length; index++)
            {
                if (metadata.Parameters[index].GetCustomAttribute<MemoryCacheKeyIgnoreAttribute>(true) != null)
                {
                    continue;
                }

                var argument = args[index];
                keySource.Append('|');
                if (argument == null)
                {
                    keySource.Append("<null>");
                    continue;
                }

                keySource.Append(argument.GetType().AssemblyQualifiedName);
                keySource.Append(':').Append(JsonConvert.SerializeObject(argument, Formatting.None));
            }

            // 缓存字典只保存摘要，避免参数中的隐私或业务字段以明文长期驻留内存。
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource.ToString()));
                return "camefor-memory:" + ToHexString(hash);
            }
        }

        private object InvokeGenericHelper(
            MethodInfo helperMethod,
            Type resultType,
            MethodCacheMetadata metadata,
            object[] args,
            string cacheKey)
        {
            return helperMethod.MakeGenericMethod(resultType).Invoke(this, new object[] { metadata, args, cacheKey });
        }

        private object InvokeTarget(MethodInfo method, object[] args)
        {
            try
            {
                return method.Invoke(_target, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static void EnsureCacheable(MethodInfo method)
        {
            var returnType = method.ReturnType;
            if (returnType == typeof(void) || returnType == typeof(Task))
            {
                throw new InvalidOperationException("MemoryCacheAttribute 只能用于有返回值的方法、Task<T> 或 ValueTask<T>。");
            }

            foreach (var parameter in method.GetParameters())
            {
                if (parameter.ParameterType.GetTypeInfo().IsByRef)
                {
                    throw new InvalidOperationException("MemoryCacheAttribute 不支持 ref 或 out 参数。");
                }
            }
        }

        private static bool IsDefaultValue(object value, Type valueType)
        {
            if (value == null)
            {
                return true;
            }

            if (!valueType.GetTypeInfo().IsValueType)
            {
                return false;
            }

            return value.Equals(Activator.CreateInstance(valueType));
        }

        private static MethodInfo GetGenericHelperMethod(string methodName)
        {
            return typeof(MemoryCacheDispatchProxy<TService>).GetTypeInfo().GetDeclaredMethod(methodName);
        }

        private static string ToHexString(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
            {
                result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

        internal sealed class MethodCacheMetadata
        {
            internal MethodCacheMetadata(
                MethodInfo targetMethod,
                MemoryCacheAttribute cacheAttribute,
                ParameterInfo[] parameters)
            {
                TargetMethod = targetMethod;
                CacheAttribute = cacheAttribute;
                Parameters = parameters;
            }

            internal MethodInfo TargetMethod { get; }

            internal MemoryCacheAttribute CacheAttribute { get; }

            internal ParameterInfo[] Parameters { get; }
        }
    }
}
