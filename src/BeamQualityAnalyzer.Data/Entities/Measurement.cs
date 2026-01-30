using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeamQualityAnalyzer.Data.Entities
{
    /// <summary>
    /// 测量记录实体
    /// </summary>
    [Table("Measurements")]
    public class Measurement
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 测量时间
        /// </summary>
        [Required]
        public DateTime MeasurementTime { get; set; }

        /// <summary>
        /// 设备信息
        /// </summary>
        [MaxLength(500)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// 状态（Complete, Incomplete, Error）
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Complete";

        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 原始数据点集合
        /// </summary>
        public virtual ICollection<RawDataPointEntity> RawDataPoints { get; set; } = new List<RawDataPointEntity>();

        /// <summary>
        /// 计算结果
        /// </summary>
        public virtual CalculationResult? CalculationResult { get; set; }
    }
}
