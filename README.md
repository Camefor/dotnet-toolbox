# Camefor.Tools

小笨的 .NET 代码工具库 —— 一个实用的 C# 工具集合，涵盖类型转换、集合操作、文件处理、Excel 读写、加密、GPS 坐标转换等常用功能。

[![.NET](https://img.shields.io/badge/.NET-net8.0%20%7C%20netstandard2.0-blue)]()
[![Author](https://img.shields.io/badge/Author-Rhys%20Wang-green)]()

## 快速开始

```csharp
// 引用命名空间
using Camefor.Tools;
```

## 功能模块

### 扩展方法 (Extensions)

**类型转换** — `Extensions.Convert.cs`

```csharp
"123".ToInt();           // string → int
"3.14".ToDouble();       // string → double
"true".ToBool();         // string → bool
"2024-01-01".ToDateTime(); // string → DateTime
```

**日期时间** — `Extensions.DateTime.cs`

```csharp
dateTime.ConvertToUnixTimestamp();       // DateTime → Unix 秒级时间戳
dateTime.ConvertToUnixMillisecondTimestamp(); // DateTime → Unix 毫秒级时间戳
1704067200L.FromSecondsUnixTimestampConvertToDate(); // Unix 时间戳 → DateTime
dateTime.ToChineseDateString();          // "2024年01月01日"
```

**JSON 操作** — `Extensions.Json.cs`

```csharp
object.ToJson();                  // 对象 → JSON 字符串
jsonStr.ToObject<MyClass>();      // JSON → 对象
jsonStr.ToList<MyClass>();        // JSON → List
jsonStr.FindFirstTokenValue<string>("$.name"); // JSONPath 查询
```

**集合操作** — `Extensions.List.cs` / `Extensions.Nullable.cs`

```csharp
list.DistinctBy(x => x.Id);      // 按字段去重
list.FindPage(pageIndex, pageSize); // 内存分页
largeList.SplitList<T>(500);      // 拆分大集合为多个子集合
dataRow.GetValue("column");       // DataRow 安全取值
```

**对象变更检测** — `Extensions.Object.cs`

```csharp
// 比较两个对象的属性差异，结合 XML 文档注释返回变更字段
newObj.GetChangedFieldsWithComment<MyClass>(oldObj);
```

### 分页 (Collections)

```csharp
// IQueryable 分页（适用于 EF Core）
var paged = await queryable.ToPagedList(pageIndex: 0, pageSize: 20);

// IEnumerable 分页
var paged = enumerable.ToPagedList(pageIndex: 0, pageSize: 20);

// 访问分页信息
paged.TotalCount;      // 总记录数
paged.TotalPages;      // 总页数
paged.HasNextPage;     // 是否有下一页
paged.Items;           // 当前页数据
```

### 字符串工具 (String)

```csharp
Str.PinYin("中国");              // 汉字 → 拼音首字母 "ZG"
Str.Splice(list, ",", "'");     // 集合拼接: 'a','b','c'
Str.FirstUpper("hello");        // "Hello"
Str.ToCamel("hello_world");     // "HelloWorld"
Str.ContainsChinese("abc中文");  // true
Str.Truncate("很长的字符串", 4); // "很长的字..."
Str.Unique();                    // 生成 GUID 唯一标识

// 安全密码生成（使用加密随机数）
StringGenerator.GenerateRandomPassword(16);
```

### 文件操作 (File)

```csharp
DirFileHelper.CreateDir(path);           // 创建目录
DirFileHelper.CopyFolder(src, dest);     // 递归复制文件夹
DirFileHelper.GetFileNames(dir, "*.cs", true); // 递归搜索文件
DirFileHelper.GetFileSize(filePath);     // 获取文件大小
DirFileHelper.ToFileSize(bytes);         // 格式化: "1.5 MB"
DirFileHelper.ReadText(path);            // 读取文件内容
DirFileHelper.WriteText(path, content);  // 写入文件内容
```

### Excel 读写 (Excel)

基于 NPOI 实现，支持 XLS/XLSX 格式。

```csharp
// 导出 Excel
var config = new ExcelConfig
{
    FileName = "report",
    Title = "月度报表",
    ColumnEntity = new List<ColumnModel>
    {
        new ColumnModel { Column = "Name", ExcelColumn = "姓名", Width = 20 },
        new ColumnModel { Column = "Amount", ExcelColumn = "金额", Width = 15 }
    }
};
var stream = ExcelHelper.ExportMemoryStream(dataTable, config);

// 导入 Excel
DataTable dt = ExcelHelper.ExcelImport(filePath, sheetName);

// 多 Sheet 导入
var (tables, sheetNames) = ExcelHelper.ExcelImportMultiSheet(filePath);
```

### 加密 (Security)

```csharp
Md5Helper.Encrypt("password", 32);  // MD5 哈希（支持 16/32 位）

DESEncrypt.Encrypt("明文");          // DES 加密
DESEncrypt.Decrypt("密文");          // DES 解密
DESEncrypt.Encrypt("明文", "自定义密钥"); // 使用自定义密钥
```

### HTTP 请求 (Web)

```csharp
// GET 请求
string result = await HttpMethods.GetString(url);

// POST 请求
string result = await HttpMethods.Post(url, jsonBody);

// 文件下载（支持进度回调）
var downloader = new FileDownloadUtil();
downloader.Init(url, savePath, fileName);
downloader.ProgressChanged += (percent) => Console.Write($"\r{percent}%");
downloader.Download();
```

### 时间工具 (Time)

```csharp
Time.Now;                        // 当前时间
Time.ShortDate;                  // "2024/01/01"
Time.GetWeekNameOfDay(date);     // "星期一"
Time.DiffDays(start, end);       // 日期差（天）
Time.GetDaysOfYear(2024);        // 366（闰年）
Time.WeekRange(2024, 1);         // 第1周的起止日期
Time.GetChineseDateTime(date);   // "2024年01月01日 14时30分"
```

### GPS 坐标转换 (Gps)

支持 WGS84（GPS）、GCJ-02（高德/谷歌中国）、BD-09（百度）三种坐标系互转。

```csharp
// WGS84 → GCJ-02（高德坐标）
var (lat, lng) = Coordtransform.Wgs84togcj02(lng, lat);

// GCJ-02 → BD-09（百度坐标）
var (lat, lng) = Coordtransform.Gcj02tobd09(lng, lat);

// BD-09 → WGS84
var (lat, lng) = Coordtransform.Bd09towgs84(lng, lat);
```

### 异步锁 (AsyncLock)

```csharp
private readonly AsyncLock _lock = new AsyncLock();

using (await _lock.LockAsync())
{
    // 线程安全的异步代码
}
```

### 高性能控制台输出 (FastConsole)

线程安全的控制台输出，使用后台线程批量刷新，避免频繁 I/O 阻塞。

```csharp
FastConsole.WriteInfo("信息");         // 绿色
FastConsole.WriteWarning("警告");      // 黄色
FastConsole.WriteError("错误");        // 红色
FastConsole.WriteException(ex);        // 异常详情
```

### 其他工具

| 工具 | 说明 |
|------|------|
| `ObjectCopier.Clone<T>(obj)` | 深拷贝（基于 JSON 序列化） |
| `PerformanceTester` | 代码执行耗时测量 |
| `DailyTrigger` | 每日定时触发器 |
| `CommonHelper.CreateNo()` | 基于时间戳的编号生成 |
| `EventLog` | 文件日志记录 |
| `LinqExtensions` | 表达式树动态组合（And/Or/GreaterThan） |
| `XmlDocumentHelper` | XML 文档注释读取 |
| `EnumAttribute.GetDescription()` | 枚举描述特性读取 |

## 项目结构

```
src/Camefor.Tools/
├── Util/
│   ├── Attributes/       # 特性与枚举扩展
│   ├── Cache/            # 特性内存缓存、启动预热与后台定时刷新
│   ├── Collections/      # 泛型分页集合
│   ├── Common/           # 通用工具（异步锁、深拷贝、性能测试、定时触发）
│   ├── Excel/            # Excel 导入导出（NPOI）
│   ├── Expressions/      # 表达式树与 Lambda 工具
│   ├── Extensions/       # 扩展方法（转换、日期、JSON、集合、验证）
│   ├── File/             # 文件与目录操作
│   ├── Gps/              # GPS 坐标系转换
│   ├── Linq/             # LINQ 表达式动态组合
│   ├── Log/              # 日志与控制台输出
│   ├── Model/            # 分页模型
│   ├── Security/         # MD5 / DES 加密
│   ├── String/           # 字符串处理（拼音、生成、格式化）
│   ├── Time/             # 时间日期工具
│   ├── Web/              # HTTP 请求与文件下载
│   └── XML/              # XML 文档读取
test/TestAppNetCore/      # 测试项目
```

## 依赖

| 包 | 版本 | 用途 |
|----|------|------|
| Newtonsoft.Json | 13.0.3 | JSON 序列化 |
| NPOI | 2.5.4 | Excel 文件读写 |
| Microsoft.AspNetCore.Http.Abstractions | 2.2.0 | ASP.NET Core HTTP 抽象 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.0 | 缓存代理依赖注入注册 |
| Microsoft.Extensions.Hosting.Abstractions | 8.0.0 | 预热与后台定时刷新宿主服务 |
| Microsoft.Extensions.Logging.Abstractions | 8.0.0 | 服务日志抽象 |

### 特性内存缓存 (Cache)

通过接口代理为方法增加进程内缓存，支持同步返回值、`Task<T>` 和 `ValueTask<T>`。缓存键仅保存 SHA-256 摘要，不会在缓存容器中保留方法参数明文。

```csharp
using Camefor.Tools.Cache;

public interface IDataProvider
{
    [MemoryCache(expirationSeconds: 60)]
    Task<string> GetAsync(string id);
}

IDataProvider provider = MemoryCacheProxy.Create<IDataProvider>(new DataProvider());
string value = await provider.GetAsync("sample-id");
```

默认不缓存 `null`、`0`、`false` 等默认值；如需缓存，可设置 `cacheDefaultValues: true`。不参与缓存键计算的参数可标记 `[MemoryCacheKeyIgnore]`。同一缓存键并发未命中时只执行一次真实方法，异常和取消结果不会写入缓存。

> 特性应标在接口方法或实现方法上。当前代理仅支持接口，不支持 `void`、非泛型 `Task` 以及包含 `ref` / `out` 参数的方法。

#### 启动预热与后台定时刷新

为无参缓存方法配置 `Warmup` 与 `RefreshSeconds` 后，即可启用预热和定时刷新：

```csharp
public interface IDataProvider
{
    [MemoryCache(expirationSeconds: 1800, Warmup = true, RefreshSeconds = 900, Priority = 0, Critical = true)]
    Task<Dictionary<string, Item>> GetAllAsync();
}

// ASP.NET Core / Generic Host：一次注册即可，预热/刷新宿主服务会自动装配
builder.Services.AddMemoryCacheProxy<IDataProvider, DataProvider>();
```

`AddMemoryCacheProxy` 会自动调用 `AddMemoryCacheProxyFramework`，注册共享缓存容器、目标注册表、就绪状态，以及代理初始化、启动预热、后台定时刷新三个宿主服务，因此不需要再单独注册。预热按 `Priority` 升序执行，同优先级并发、不同优先级串行；`Critical = true` 的目标预热失败会终止启动。刷新服务为每个目标启动独立循环，启动时随机错峰 0~10 秒，并等待预热完成后再开始。

> 类库无法自动向宿主注入服务，因此宿主必须至少调用一次 `AddMemoryCacheProxy`（或手动启动预热/刷新服务）。除此之外的装配由框架自行完成，避免遗漏。

非宿主环境可以直接手动创建：

```csharp
var store = new MemoryCacheStore();
var registry = new MemoryCacheRegistry();
var readiness = new MemoryCacheReadinessState();
var provider = MemoryCacheProxy.Create<IDataProvider>(new DataProvider(), store, registry);

var warmup = new MemoryCacheWarmupHostedService(registry, readiness);
await warmup.WarmupAsync();

using (var refresh = new MemoryCacheRefreshBackgroundService(registry, readiness))
{
    await refresh.StartAsync(CancellationToken.None);
    // ...
    await refresh.StopAsync(CancellationToken.None);
}
```

约束与行为：

- 预热/刷新仅支持无参方法，有参或泛型方法在创建代理时会抛出 `InvalidOperationException`；有参场景建议采用“外层有参 + 内层无参字典”模式。
- 手动调用 `MemoryCacheProxy.Create(target)` 时，如果方法配置了 `Warmup`/`RefreshSeconds` 却没有传入 `MemoryCacheRegistry`，创建代理会立即抛出 `InvalidOperationException`，避免配置被静默忽略。
- `RefreshSeconds` 应小于 `ExpirationSeconds`，否则会出现缓存到期后的失效窗口，刷新服务启动时会输出警告。
- 刷新采用“先移除旧值，再调用方法回源”；回源失败时缓存保持为空，等待下一次调用懒加载或下一次刷新。
- 不需要自动预热/刷新的方法可标记 `[MemoryCacheIgnore]`。
- DI 注册的缓存代理为单例，实现类型及其依赖不要使用 Scoped 服务。
- `MemoryCacheReadinessState.IsReady` 可用于健康检查，预热完成前返回未就绪。

## License

MIT
