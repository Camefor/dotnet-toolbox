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

## License

MIT
