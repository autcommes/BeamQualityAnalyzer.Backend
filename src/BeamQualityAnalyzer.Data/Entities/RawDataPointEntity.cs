using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeamQualityAnalyzer.Data.Entities
{
    /// <summary>
    /// 原始数据点实体
    /// </summary>
    [Table("RawDataPoints")]
    public class RawDataPointEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 外键 - 测量记录ID
        /// </summary>
        [Required]
        public int MeasurementId { get; set; }

        /// <summary>
        /// 探测器位置 (mm)
        /// </summary>
        [Required]
        public double DetectorPosition { get; set; }

        /// <summary>
        /// X 方向光束直径 (μm)
        /// </summary>
        [Required]
        public double BeamDiameterX { get; set; }

        /// <summary>
        /// Y 方向光束直径 (μm)
        /// </summary>
        [Required]
        public double BeamDiameterY { get; set; }

        /// <summary>
        /// 强度矩阵数据（序列化为 BLOB）
        /// </summary>
        public byte[]? IntensityData { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 导航属性 - 所属测量记录
        /// </summary>
        [ForeignKey(nameof(MeasurementId))]
        public virtual Measurement? Measurement { get; set; }
    }
}
