using System;
using System.Threading;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Core.Models;

namespace BeamQualityAnalyzer.Core.Interfaces
{
    /// <summary>
    /// 光束质量分析算法服务接口
    /// 负责执行高斯拟合、双曲线拟合和光束质量参数计算
    /// </summary>
    public interface IAlgorithmService
    {
        /// <summary>
        /// 执行完整的光束质量分析
        /// </summary>
        /// <param name="dataPoints">原始采样数据点数组</param>
        /// <param name="parameters">分析参数配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含所有计算结果的分析结果对象</returns>
        /// <exception cref="ArgumentNullException">当dataPoints或parameters为null时抛出</exception>
        /// <exception cref="ArgumentException">当数据点数量不足时抛出</exception>
        /// <exception cref="InvalidOperationException">当拟合失败时抛出</exception>
        Task<BeamAnalysisResult> AnalyzeAsync(
            RawDataPoint[] dataPoints,
            AnalysisParameters parameters,
            CancellationToken cancellationToken);

        /// <summary>
        /// 执行高斯拟合
        /// </summary>
        /// <param name="positions">探测器位置数组 (mm)</param>
        /// <param name="diameters">光束直径数组 (μm)</param>
        /// <returns>高斯拟合结果</returns>
        /// <exception cref="ArgumentNullException">当positions或diameters为null时抛出</exception>
        /// <exception cref="ArgumentException">当数组长度不匹配或数据不足时抛出</exception>
        GaussianFitResult FitGaussian(double[] positions, double[] diameters);

        /// <summary>
        /// 执行双曲线拟合
        /// </summary>
        /// <param name="positions">探测器位置数组 (mm)</param>
        /// <param name="diameters">光束直径数组 (μm)</param>
        /// <param name="wavelength">波长 (nm)</param>
        /// <returns>双曲线拟合结果</returns>
        /// <exception cref="ArgumentNullException">当positions或diameters为null时抛出</exception>
        /// <exception cref="ArgumentException">当数组长度不匹配或数据不足时抛出</exception>
        HyperbolicFitResult FitHyperbolic(double[] positions, double[] diameters, double wavelength);

        /// <summary>
        /// 计算 M² 因子
        /// </summary>
        /// <param name="fitResult">双曲线拟合结果</param>
        /// <returns>M² 因子值（≥ 1）</returns>
        /// <exception cref="ArgumentNullException">当fitResult为null时抛出</exception>
        double CalculateMSquared(HyperbolicFitResult fitResult);

        /// <summary>
        /// 计算光束腰斑位置和直径
        /// </summary>
        /// <param name="fitResult">双曲线拟合结果</param>
        /// <returns>腰斑位置 (mm) 和腰斑直径 (μm)</returns>
        /// <exception cref="ArgumentNullException">当fitResult为null时抛出</exception>
        (double position, double diameter) CalculateBeamWaist(HyperbolicFitResult fitResult);
    }
}
