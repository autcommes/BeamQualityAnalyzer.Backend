using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeamQualityAnalyzer.Data.Entities
{
    /// <summary>
    /// 计算结果实体
    /// </summary>
    [Table("CalculationResults")]
    public class CalculationResult
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
        /// X 方向 M² 因子
        /// </summary>
        [Required]
        public double MSquaredX { get; set; }

        /// <summary>
        /// Y 方向 M² 因子
        /// </summary>
        [Required]
        public double MSquaredY { get; set; }

        /// <summary>
        /// 全局 M² 因子
        /// </summary>
        [Required]
        public double MSquaredGlobal { get; set; }

        /// <summary>
        /// X 方向腰斑位置 (mm)
        /// </summary>
        [Required]
        public double BeamWaistPositionX { get; set; }

        /// <summary>
        /// Y 方向腰斑位置 (mm)
        /// </summary>
        [Required]
        public double BeamWaistPositionY { get; set; }

        /// <summary>
        /// X 方向腰斑直径 (μm)
        /// </summary>
        [Required]
        public double BeamWaistDiameterX { get; set; }

        /// <summary>
        /// Y 方向腰斑直径 (μm)
        /// </summary>
        [Required]
        public double BeamWaistDiameterY { get; set; }

        /// <summary>
        /// X 方向峰值位置 (mm)
        /// </summary>
        [Required]
        public double PeakPositionX { get; set; }

        /// <summary>
        /// Y 方向峰值位置 (mm)
        /// </summary>
        [Required]
        public double PeakPositionY { get; set; }

        /// <summary>
        /// 导航属性 - 所属测量记录
        /// </summary>
        [ForeignKey(nameof(MeasurementId))]
        public virtual Measurement? Measurement { get; set; }

        /// <summary>
        /// 拟合参数集合
        /// </summary>
        public virtual ICollection<FitParameter> FitParameters { get; set; } = new List<FitParameter>();
    }
}
