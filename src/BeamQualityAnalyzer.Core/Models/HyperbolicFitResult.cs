using System;
using System.ComponentModel.DataAnnotations;

namespace BeamQualityAnalyzer.Core.Models
{
    /// <summary>
    /// 表示双曲线拟合结果
    /// </summary>
    public class HyperbolicFitResult
    {
        /// <summary>
        /// 腰斑直径 w0 (μm)
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "腰斑直径必须大于0")]
        public double WaistDiameter { get; set; }

        /// <summary>
        /// 腰斑位置 z0 (mm)
        /// </summary>
        public double WaistPosition { get; set; }

        /// <summary>
        /// 波长 λ (nm)
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "波长必须大于0")]
        public double Wavelength { get; set; }

        /// <summary>
        /// M² 因子
        /// </summary>
        [Range(1.0, double.MaxValue, ErrorMessage = "M²因子必须大于等于1")]
        public double MSquared { get; set; }

        /// <summary>
        /// 拟合优度 (R²)
        /// </summary>
        public double RSquared { get; set; }

        /// <summary>
        /// 拟合曲线数据点
        /// </summary>
        public double[]? FittedCurve { get; set; }

        /// <summary>
        /// 验证拟合结果的有效性
        /// </summary>
        public bool IsValid()
        {
            return WaistDiameter > 0 
                && Wavelength > 0 
                && MSquared >= 1.0 
                && RSquared >= 0 
                && RSquared <= 1;
        }
    }
}
