using BeamQualityAnalyzer.Data.Entities;
using BeamQualityAnalyzer.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace BeamQualityAnalyzer.Data.DbContext
{
    /// <summary>
    /// 光束分析器数据库上下文
    /// </summary>
    public class BeamAnalyzerDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly DatabaseType _databaseType;
        private readonly string _connectionString = string.Empty;

        /// <summary>
        /// 构造函数
        /// </summary>
        public BeamAnalyzerDbContext(DatabaseType databaseType, string connectionString)
        {
            _databaseType = databaseType;
            _connectionString = connectionString;
        }

        /// <summary>
        /// 构造函数（用于依赖注入）
        /// </summary>
        public BeamAnalyzerDbContext(DbContextOptions<BeamAnalyzerDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// 测量记录表
        /// </summary>
        public DbSet<Measurement> Measurements { get; set; } = null!;

        /// <summary>
        /// 原始数据点表
        /// </summary>
        public DbSet<RawDataPointEntity> RawDataPoints { get; set; } = null!;

        /// <summary>
        /// 计算结果表
        /// </summary>
        public DbSet<CalculationResult> CalculationResults { get; set; } = null!;

        /// <summary>
        /// 拟合参数表
        /// </summary>
        public DbSet<FitParameter> FitParameters { get; set; } = null!;

        /// <summary>
        /// 配置数据库连接
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_connectionString))
            {
                switch (_databaseType)
                {
                    case DatabaseType.SQLite:
                        optionsBuilder.UseSqlite(_connectionString);
                        break;

                    case DatabaseType.MySQL:
                        // 需要安装 Pomelo.EntityFrameworkCore.MySql 包
                        // optionsBuilder.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));
                        throw new System.NotImplementedException("MySQL 支持尚未实现");

                    case DatabaseType.SqlServer:
                        // 需要安装 Microsoft.EntityFrameworkCore.SqlServer 包
                        // optionsBuilder.UseSqlServer(_connectionString);
                        throw new System.NotImplementedException("SQL Server 支持尚未实现");

                    default:
                        throw new System.ArgumentException($"不支持的数据库类型: {_databaseType}");
                }
            }
        }

        /// <summary>
        /// 配置实体关系
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置 Measurement 实体
            modelBuilder.Entity<Measurement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MeasurementTime).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();

                // 配置一对多关系：Measurement -> RawDataPoints
                entity.HasMany(e => e.RawDataPoints)
                    .WithOne(e => e.Measurement)
                    .HasForeignKey(e => e.MeasurementId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 配置一对一关系：Measurement -> CalculationResult
                entity.HasOne(e => e.CalculationResult)
                    .WithOne(e => e.Measurement)
                    .HasForeignKey<CalculationResult>(e => e.MeasurementId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置 RawDataPointEntity 实体
            modelBuilder.Entity<RawDataPointEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DetectorPosition).IsRequired();
                entity.Property(e => e.BeamDiameterX).IsRequired();
                entity.Property(e => e.BeamDiameterY).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();

                // 创建索引以提高查询性能
                entity.HasIndex(e => e.MeasurementId);
            });

            // 配置 CalculationResult 实体
            modelBuilder.Entity<CalculationResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MSquaredX).IsRequired();
                entity.Property(e => e.MSquaredY).IsRequired();
                entity.Property(e => e.MSquaredGlobal).IsRequired();

                // 配置一对多关系：CalculationResult -> FitParameters
                entity.HasMany(e => e.FitParameters)
                    .WithOne(e => e.CalculationResult)
                    .HasForeignKey(e => e.CalculationResultId)
                    .OnDelete(DeleteBehavior.Cascade);

                // 创建索引
                entity.HasIndex(e => e.MeasurementId).IsUnique();
            });

            // 配置 FitParameter 实体
            modelBuilder.Entity<FitParameter>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Direction).IsRequired().HasMaxLength(10);
                entity.Property(e => e.FitType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Parameters).IsRequired();
                entity.Property(e => e.RSquared).IsRequired();

                // 创建索引
                entity.HasIndex(e => e.CalculationResultId);
            });
        }
    }
}
