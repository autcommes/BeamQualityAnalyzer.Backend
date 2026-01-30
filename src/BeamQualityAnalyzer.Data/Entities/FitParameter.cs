using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeamQualityAnalyzer.Data.Entities
{
    /// <summary>
    /// 拟合参数实体
    /// </summary>
    [Table("FitParameters")]
    public class FitParameter
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 外键 - 计算结果ID
        /// </summary>
        [Required]
        public int CalculationResultId { get; set; }

        /// <summary>
        /// 方向（X 或 Y）
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Direction { get; set; } = string.Empty;

        /// <summary>
        /// 拟合类型（Gaussian 或 Hyperbolic）
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string FitType { get; set; } = string.Empty;

        /// <summary>
        /// 拟合参数（JSON 序列化）
        /// </summary>
        [Required]
        public string Parameters { get; set; } = string.Empty;

        /// <summary>
        /// 拟合优度 (R²)
        /// </summary>
        [Required]
        public double RSquared { get; set; }

        /// <summary>
        /// 导航属性 - 所属计算结果
        /// </summary>
        [ForeignKey(nameof(CalculationResultId))]
        public virtual CalculationResult? CalculationResult { get; set; }
    }
}
