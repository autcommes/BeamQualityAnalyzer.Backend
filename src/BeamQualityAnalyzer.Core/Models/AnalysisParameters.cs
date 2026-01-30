using System;
using System.ComponentModel.DataAnnotations;

namespace BeamQualityAnalyzer.Core.Models
{
    /// <summary>
    /// 表示分析参数配置
    /// </summary>
    public class AnalysisParameters
    {
        /// <summary>
        /// 倍率
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "倍率必须大于0")]
        public double Magnification { get; set; } = 1.0;

        /// <summary>
        /// 86线结果
        /// </summary>
        public double Line86Result { get; set; }

        /// <summary>
        /// 二阶拟合结果
        /// </summary>
        public double SecondOrderFitResult { get; set; }

        /// <summary>
        /// 波长 (nm)
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "波长必须大于0")]
        public double Wavelength { get; set; } = 632.8; // 默认 He-Ne 激光波长

        /// <summary>
        /// 最小数据点数
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "最小数据点数必须大于0")]
        public int MinDataPoints { get; set; } = 10;

        /// <summary>
        /// 拟合容差
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "拟合容差必须在0到1之间")]
        public double FitTolerance { get; set; } = 0.001;

        /// <summary>
        /// 验证参数的有效性
        /// </summary>
        public bool IsValid()
        {
            return Magnification > 0 
                && Wavelength > 0 
                && MinDataPoints > 0 
                && FitTolerance >= 0 
                && FitTolerance <= 1;
        }
    }
}
