using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BeamQualityAnalyzer.Core.Models
{
    /// <summary>
    /// 表示完整的光束质量分析结果
    /// </summary>
    public class BeamAnalysisResult
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 测量时间
        /// </summary>
        public DateTime MeasurementTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 原始数据点集合
        /// </summary>
        public List<RawDataPoint> RawData { get; set; } = new List<RawDataPoint>();

        /// <summary>
        /// X 方向高斯拟合结果
        /// </summary>
        public GaussianFitResult? GaussianFitX { get; set; }

        /// <summary>
        /// Y 方向高斯拟合结果
        /// </summary>
        public GaussianFitResult? GaussianFitY { get; set; }

        /// <summary>
        /// X 方向双曲线拟合结果
        /// </summary>
        public HyperbolicFitResult? HyperbolicFitX { get; set; }

        /// <summary>
        /// Y 方向双曲线拟合结果
        /// </summary>
        public HyperbolicFitResult? HyperbolicFitY { get; set; }

        /// <summary>
        /// X 方向 M² 因子
        /// </summary>
        [Range(1.0, double.MaxValue, ErrorMessage = "M²因子必须大于等于1")]
        public double MSquaredX { get; set; }

        /// <summary>
        /// Y 方向 M² 因子
        /// </summary>
        [Range(1.0, double.MaxValue, ErrorMessage = "M²因子必须大于等于1")]
        public double MSquaredY { get; set; }

        /// <summary>
        /// 全局 M² 因子
        /// </summary>
        [Range(1.0, double.MaxValue, ErrorMessage = "M²因子必须大于等于1")]
        public double MSquaredGlobal { get; set; }

        /// <summary>
        /// X 方向腰斑位置 (mm)
        /// </summary>
        public double BeamWaistPositionX { get; set; }

        /// <summary>
        /// Y 方向腰斑位置 (mm)
        /// </summary>
        public double BeamWaistPositionY { get; set; }

        /// <summary>
        /// X 方向腰斑直径 (μm)
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "腰斑直径必须大于0")]
        public double BeamWaistDiameterX { get; set; }

        /// <summary>
        /// Y 方向腰斑直径 (μm)
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "腰斑直径必须大于0")]
        public double BeamWaistDiameterY { get; set; }

        /// <summary>
        /// X 方向峰值位置 (mm)
        /// </summary>
        public double PeakPositionX { get; set; }

        /// <summary>
        /// Y 方向峰值位置 (mm)
        /// </summary>
        public double PeakPositionY { get; set; }

        /// <summary>
        /// 光斑强度数据（用于 2D 可视化）
        /// </summary>
        public double[,]? SpotIntensityData { get; set; }

        /// <summary>
        /// 3D 能量分布数据
        /// </summary>
        public double[,]? EnergyDistribution3D { get; set; }

        /// <summary>
        /// 验证分析结果的有效性
        /// </summary>
        public bool IsValid()
        {
            return MSquaredX >= 1.0 
                && MSquaredY >= 1.0 
                && MSquaredGlobal >= 1.0
                && BeamWaistDiameterX > 0 
                && BeamWaistDiameterY > 0;
        }
    }
}
