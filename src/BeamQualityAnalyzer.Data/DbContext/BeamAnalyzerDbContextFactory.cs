using BeamQualityAnalyzer.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BeamQualityAnalyzer.Data.DbContext
{
    /// <summary>
    /// 设计时 DbContext 工厂（用于 EF Core 迁移）
    /// </summary>
    public class BeamAnalyzerDbContextFactory : IDesignTimeDbContextFactory<BeamAnalyzerDbContext>
    {
        public BeamAnalyzerDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BeamAnalyzerDbContext>();
            
            // 使用 SQLite 作为默认数据库（用于迁移生成）
            optionsBuilder.UseSqlite("Data Source=beam_analyzer.db");

            return new BeamAnalyzerDbContext(optionsBuilder.Options);
        }
    }
}
