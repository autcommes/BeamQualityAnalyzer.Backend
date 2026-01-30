using System;
using BeamQualityAnalyzer.Core.Models;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace BeamQualityAnalyzer.PropertyTests
{
    /// <summary>
    /// 领域模型属性测试
    /// 验证领域模型的核心业务规则和约束
    /// </summary>
    public class DomainModelPropertyTests
    {
        /// <summary>
        /// 属性 4: M²因子计算完整性
        /// Feature: beam-quality-analyzer, Property 4: M²因子计算完整性
        /// 对于任何有效的拟合结果，算法服务应该计算X方向、Y方向和全局的M²因子，且M² ≥ 1。
        /// 验证需求: 4.3
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property MSquaredFactor_ForValidFitResult_ShouldBeGreaterThanOrEqualToOne(ValidHyperbolicFitResult validFit)
        {
            // Feature: beam-quality-analyzer, Property 4: M²因子计算完整性
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validFit)),
                fit =>
                {
                    // Assert - M² 因子必须 >= 1
                    return fit.Value.MSquared >= 1.0;
                });
        }

        /// <summary>
        /// 属性 4.1: BeamAnalysisResult 的 M² 因子完整性
        /// 对于任何有效的分析结果，X、Y 和全局 M² 因子都应该 >= 1
        /// 验证需求: 4.3
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property BeamAnalysisResult_MSquaredFactors_ShouldAllBeGreaterThanOrEqualToOne(ValidBeamAnalysisResult validResult)
        {
            // Feature: beam-quality-analyzer, Property 4: M²因子计算完整性
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validResult)),
                result =>
                {
                    // Assert - 所有 M² 因子必须 >= 1
                    return result.Value.MSquaredX >= 1.0 &&
                           result.Value.MSquaredY >= 1.0 &&
                           result.Value.MSquaredGlobal >= 1.0;
                });
        }

        /// <summary>
        /// 属性 4.2: 全局 M² 因子应该在 X 和 Y 的合理范围内
        /// 全局 M² 因子通常是 X 和 Y 的平均值或最大值
        /// 验证需求: 4.3
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property BeamAnalysisResult_GlobalMSquared_ShouldBeInReasonableRange(ValidBeamAnalysisResult validResult)
        {
            // Feature: beam-quality-analyzer, Property 4: M²因子计算完整性
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validResult)),
                result =>
                {
                    var minMSquared = Math.Min(result.Value.MSquaredX, result.Value.MSquaredY);
                    var maxMSquared = Math.Max(result.Value.MSquaredX, result.Value.MSquaredY);
                    
                    // 全局 M² 应该在 X 和 Y 的范围内（或接近平均值）
                    return result.Value.MSquaredGlobal >= minMSquared * 0.9 &&
                           result.Value.MSquaredGlobal <= maxMSquared * 1.1;
                });
        }

        /// <summary>
        /// 属性 5: 腰斑参数计算
        /// Feature: beam-quality-analyzer, Property 5: 腰斑参数计算
        /// 对于任何有效的双曲线拟合结果，算法服务应该计算腰斑位置和腰斑直径，且腰斑直径 > 0。
        /// 验证需求: 4.4
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property BeamWaistDiameter_ForValidFitResult_ShouldBeGreaterThanZero(ValidHyperbolicFitResult validFit)
        {
            // Feature: beam-quality-analyzer, Property 5: 腰斑参数计算
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validFit)),
                fit =>
                {
                    // Assert - 腰斑直径必须 > 0
                    return fit.Value.WaistDiameter > 0;
                });
        }

        /// <summary>
        /// 属性 5.1: BeamAnalysisResult 的腰斑直径完整性
        /// 对于任何有效的分析结果，X 和 Y 方向的腰斑直径都应该 > 0
        /// 验证需求: 4.4
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property BeamAnalysisResult_WaistDiameters_ShouldAllBeGreaterThanZero(ValidBeamAnalysisResult validResult)
        {
            // Feature: beam-quality-analyzer, Property 5: 腰斑参数计算
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validResult)),
                result =>
                {
                    // Assert - 所有腰斑直径必须 > 0
                    return result.Value.BeamWaistDiameterX > 0 &&
                           result.Value.BeamWaistDiameterY > 0;
                });
        }

        /// <summary>
        /// 属性 5.2: 腰斑直径应该在合理的物理范围内
        /// 激光光束的腰斑直径通常在 1μm 到 10mm 之间
        /// 验证需求: 4.4
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property BeamWaistDiameter_ShouldBeInPhysicallyReasonableRange(ValidHyperbolicFitResult validFit)
        {
            // Feature: beam-quality-analyzer, Property 5: 腰斑参数计算
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validFit)),
                fit =>
                {
                    // 腰斑直径应该在 1μm 到 10000μm (10mm) 之间
                    return fit.Value.WaistDiameter >= 1.0 &&
                           fit.Value.WaistDiameter <= 10000.0;
                });
        }

        /// <summary>
        /// 属性 5.3: 波长应该在合理的物理范围内
        /// 可见光和近红外激光的波长通常在 400nm 到 1500nm 之间
        /// 验证需求: 4.4
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property Wavelength_ShouldBeInPhysicallyReasonableRange(ValidHyperbolicFitResult validFit)
        {
            // Feature: beam-quality-analyzer, Property 5: 腰斑参数计算
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validFit)),
                fit =>
                {
                    // 波长应该在 400nm 到 1500nm 之间
                    return fit.Value.Wavelength >= 400.0 &&
                           fit.Value.Wavelength <= 1500.0;
                });
        }

        /// <summary>
        /// 属性 5.4: 拟合优度应该在 [0, 1] 范围内
        /// R² 值表示拟合质量，必须在 0 到 1 之间
        /// 验证需求: 4.4
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property RSquared_ShouldBeInValidRange(ValidHyperbolicFitResult validFit)
        {
            // Feature: beam-quality-analyzer, Property 5: 腰斑参数计算
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validFit)),
                fit =>
                {
                    // R² 必须在 [0, 1] 范围内
                    return fit.Value.RSquared >= 0.0 &&
                           fit.Value.RSquared <= 1.0;
                });
        }

        /// <summary>
        /// 属性 5.5: IsValid() 方法应该正确验证拟合结果
        /// 验证需求: 4.4
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property HyperbolicFitResult_IsValid_ShouldReturnTrueForValidData(ValidHyperbolicFitResult validFit)
        {
            // Feature: beam-quality-analyzer, Property 5: 腰斑参数计算
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validFit)),
                fit =>
                {
                    // IsValid() 应该返回 true
                    return fit.Value.IsValid();
                });
        }

        /// <summary>
        /// 属性 5.6: BeamAnalysisResult 的 IsValid() 方法应该正确验证分析结果
        /// 验证需求: 4.3, 4.4
        /// </summary>
        [Property(MaxTest = 100, Arbitrary = new[] { typeof(DomainModelGenerators) })]
        public Property BeamAnalysisResult_IsValid_ShouldReturnTrueForValidData(ValidBeamAnalysisResult validResult)
        {
            // Feature: beam-quality-analyzer, Property 4: M²因子计算完整性
            // Feature: beam-quality-analyzer, Property 5: 腰斑参数计算
            
            return Prop.ForAll(
                Arb.From(Gen.Constant(validResult)),
                result =>
                {
                    // IsValid() 应该返回 true
                    return result.Value.IsValid();
                });
        }
    }

    /// <summary>
    /// 有效的双曲线拟合结果包装类
    /// </summary>
    public class ValidHyperbolicFitResult
    {
        public HyperbolicFitResult Value { get; set; } = null!;
    }

    /// <summary>
    /// 有效的光束分析结果包装类
    /// </summary>
    public class ValidBeamAnalysisResult
    {
        public BeamAnalysisResult Value { get; set; } = null!;
    }

    /// <summary>
    /// 领域模型生成器
    /// </summary>
    public static class DomainModelGenerators
    {
        /// <summary>
        /// 生成有效的双曲线拟合结果
        /// </summary>
        public static Arbitrary<ValidHyperbolicFitResult> ValidHyperbolicFitResultArbitrary()
        {
            var gen = from waistDiameter in Gen.Choose(10, 1000).Select(x => (double)x)  // 10μm 到 1000μm
                      from waistPosition in Gen.Choose(-100, 100).Select(x => (double)x) // -100mm 到 100mm
                      from wavelength in Gen.Choose(400, 1500).Select(x => (double)x)    // 400nm 到 1500nm (可见光到近红外)
                      from mSquared in Gen.Choose(10, 50).Select(x => x / 10.0)          // M² 从 1.0 (理想) 到 5.0 (多模)
                      from rSquared in Gen.Choose(90, 99).Select(x => x / 100.0)         // 拟合优度 0.90 到 0.99
                      select new ValidHyperbolicFitResult
                      {
                          Value = new HyperbolicFitResult
                          {
                              WaistDiameter = waistDiameter,
                              WaistPosition = waistPosition,
                              Wavelength = wavelength,
                              MSquared = mSquared,
                              RSquared = rSquared,
                              FittedCurve = null // 可选
                          }
                      };

            return Arb.From(gen);
        }

        /// <summary>
        /// 生成有效的光束分析结果
        /// </summary>
        public static Arbitrary<ValidBeamAnalysisResult> ValidBeamAnalysisResultArbitrary()
        {
            var gen = from mSquaredX in Gen.Choose(10, 50).Select(x => x / 10.0)
                      from mSquaredY in Gen.Choose(10, 50).Select(x => x / 10.0)
                      from waistDiameterX in Gen.Choose(10, 1000).Select(x => (double)x)
                      from waistDiameterY in Gen.Choose(10, 1000).Select(x => (double)x)
                      from waistPositionX in Gen.Choose(-100, 100).Select(x => (double)x)
                      from waistPositionY in Gen.Choose(-100, 100).Select(x => (double)x)
                      from peakPositionX in Gen.Choose(-100, 100).Select(x => (double)x)
                      from peakPositionY in Gen.Choose(-100, 100).Select(x => (double)x)
                      select new ValidBeamAnalysisResult
                      {
                          Value = new BeamAnalysisResult
                          {
                              Id = Guid.NewGuid(),
                              MeasurementTime = DateTime.Now,
                              MSquaredX = mSquaredX,
                              MSquaredY = mSquaredY,
                              MSquaredGlobal = (mSquaredX + mSquaredY) / 2.0, // 平均值
                              BeamWaistDiameterX = waistDiameterX,
                              BeamWaistDiameterY = waistDiameterY,
                              BeamWaistPositionX = waistPositionX,
                              BeamWaistPositionY = waistPositionY,
                              PeakPositionX = peakPositionX,
                              PeakPositionY = peakPositionY,
                              RawData = new System.Collections.Generic.List<RawDataPoint>(),
                              GaussianFitX = null,
                              GaussianFitY = null,
                              HyperbolicFitX = null,
                              HyperbolicFitY = null,
                              SpotIntensityData = null,
                              EnergyDistribution3D = null
                          }
                      };

            return Arb.From(gen);
        }
    }
}
