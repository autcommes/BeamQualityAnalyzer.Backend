using System;

namespace BeamQualityAnalyzer.Core.Models
{
    /// <summary>
    /// 表示高斯拟合结果
    /// </summary>
    public class GaussianFitResult
    {
        /// <summary>
        /// 振幅
        /// </summary>
        public double Amplitude { get; set; }

        /// <summary>
        /// 均值（中心位置）
        /// </summary>
        public double Mean { get; set; }

        /// <summary>
        /// 标准差
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// 偏移量
        /// </summary>
        public double Offset { get; set; }

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
            return RSquared >= 0 && RSquared <= 1 && StandardDeviation > 0;
        }
    }
}
