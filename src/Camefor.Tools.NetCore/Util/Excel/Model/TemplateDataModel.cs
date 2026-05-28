namespace Camefor.Tools.Excel
{
    /// <summary>
    /// 描   述  ： 模板数据模型                          
    /// 版   本  ： V1.0.0                            
    /// 创 建 人 ： rhyswang                                  
    /// 日    期 ：                         
    /// 创 建 人 ：                                   
    /// 创建时间 ：                                  
    /// 修 改 人 ：                                   
    /// 修改时间 ：                                   
    /// 修改描述 ：                                   
    /// </summary> 
    public class TemplateDataModel
    {
        /// <summary>
        /// 行号
        /// </summary>
        public int row { get; set; }
        /// <summary>
        /// 列号
        /// </summary>
        public int cell { get; set; }
        /// <summary>
        /// 数据值
        /// </summary>
        public string value { get; set; }
    }
}
