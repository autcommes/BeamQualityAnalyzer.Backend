using System;
using System.ComponentModel.DataAnnotations;

namespace BeamQualityAnalyzer.Core.Models
{
    /// <summary>
    /// 表示单个原始采样点
    /// </summary>
    public class RawDataPoint
    {
        /// <summary>
        /// 探测器位置 (mm)
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "探测器位置必须大于0")]
        public double DetectorPosition { get; set; }

        /// <summary>
        /// X 方向光束直径 (μm)
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "光束直径必须大于0")]
        public double BeamDiameterX { get; set; }

        /// <summary>
        /// Y 方向光束直径 (μm)
        /// </summary>
        [Range(0.0, double.MaxValue, ErrorMessage = "光束直径必须大于0")]
        public double BeamDiameterY { get; set; }

        /// <summary>
        /// 强度矩阵（用于 2D/3D 可视化）
        /// </summary>
        public double[,]? IntensityMatrix { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 验证数据点的有效性
        /// </summary>
        public bool IsValid()
        {
            return DetectorPosition > 0 && BeamDiameterX > 0 && BeamDiameterY > 0;
        }
    }
}
