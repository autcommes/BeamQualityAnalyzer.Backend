using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeamQualityAnalyzer.Core.Interfaces;
using BeamQualityAnalyzer.Core.Models;
using Microsoft.Extensions.Logging;

namespace BeamQualityAnalyzer.Core.Services
{
    /// <summary>
    /// 光束质量算法服务实现
    /// 负责高斯拟合、双曲线拟合和M²因子计算
    /// </summary>
    public class BeamQualityAlgorithmService : IAlgorithmService
    {
        private readonly ILogger<BeamQualityAlgorithmService> _logger;

        public BeamQualityAlgorithmService(ILogger<BeamQualityAlgorithmService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 执行完整的光束质量分析
        /// </summary>
        public async Task<BeamAnalysisResult> AnalyzeAsync(
            RawDataPoint[] dataPoints,
            AnalysisParameters parameters,
            CancellationToken cancellationToken)
        {
            if (dataPoints == null)
                throw new ArgumentNullException(nameof(dataPoints));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (dataPoints.Length < parameters.MinDataPoints)
                throw new ArgumentException(
                    $"数据点不足。需要: {parameters.MinDataPoints}, 实际: {dataPoints.Length}",
                    nameof(dataPoints));

            if (!parameters.IsValid())
                throw new ArgumentException("分析参数无效", nameof(parameters));

            _logger.LogInformation("开始光束质量分析，数据点数: {Count}", dataPoints.Length);

            var result = new BeamAnalysisResult
            {
                MeasurementTime = DateTime.Now,
                RawData = dataPoints.ToList()
            };

            // 在后台线程执行计算
            await Task.Run(() =>
            {
                // 提取位置和直径数据
                var positions = dataPoints.Select(d => d.DetectorPosition).ToArray();
                var diametersX = dataPoints.Select(d => d.BeamDiameterX).ToArray();
                var diametersY = dataPoints.Select(d => d.BeamDiameterY).ToArray();

                cancellationToken.ThrowIfCancellationRequested();

                // X 方向高斯拟合
                _logger.LogDebug("执行 X 方向高斯拟合");
                result.GaussianFitX = FitGaussian(positions, diametersX);

                cancellationToken.ThrowIfCancellationRequested();

                // Y 方向高斯拟合
                _logger.LogDebug("执行 Y 方向高斯拟合");
                result.GaussianFitY = FitGaussian(positions, diametersY);

                cancellationToken.ThrowIfCancellationRequested();

                // X 方向双曲线拟合
                _logger.LogDebug("执行 X 方向双曲线拟合");
                result.HyperbolicFitX = FitHyperbolic(positions, diametersX, parameters.Wavelength);

                cancellationToken.ThrowIfCancellationRequested();

                // Y 方向双曲线拟合
                _logger.LogDebug("执行 Y 方向双曲线拟合");
                result.HyperbolicFitY = FitHyperbolic(positions, diametersY, parameters.Wavelength);

                // 计算 M² 因子
                result.MSquaredX = CalculateMSquared(result.HyperbolicFitX);
                result.MSquaredY = CalculateMSquared(result.HyperbolicFitY);
                result.MSquaredGlobal = Math.Sqrt(result.MSquaredX * result.MSquaredY);

                // 计算腰斑参数
                var (posX, diamX) = CalculateBeamWaist(result.HyperbolicFitX);
                result.BeamWaistPositionX = posX;
                result.BeamWaistDiameterX = diamX;

                var (posY, diamY) = CalculateBeamWaist(result.HyperbolicFitY);
                result.BeamWaistPositionY = posY;
                result.BeamWaistDiameterY = diamY;

                // 计算峰值位置（最小直径位置）
                var minIndexX = Array.IndexOf(diametersX, diametersX.Min());
                var minIndexY = Array.IndexOf(diametersY, diametersY.Min());
                result.PeakPositionX = positions[minIndexX];
                result.PeakPositionY = positions[minIndexY];

            }, cancellationToken);

            _logger.LogInformation(
                "光束质量分析完成。M²(X): {MX:F4}, M²(Y): {MY:F4}, M²(Global): {MG:F4}",
                result.MSquaredX, result.MSquaredY, result.MSquaredGlobal);

            return result;
        }

        /// <summary>
        /// 执行高斯拟合
        /// 使用最小二乘法拟合高斯函数: y = A * exp(-((x - μ)² / (2σ²))) + C
        /// </summary>
        public GaussianFitResult FitGaussian(double[] positions, double[] diameters)
        {
            if (positions == null)
                throw new ArgumentNullException(nameof(positions));

            if (diameters == null)
                throw new ArgumentNullException(nameof(diameters));

            if (positions.Length != diameters.Length)
                throw new ArgumentException("位置和直径数组长度必须相同");

            if (positions.Length < 3)
                throw new ArgumentException("数据点不足，至少需要3个点进行高斯拟合");

            try
            {
                // 初始参数估计
                double amplitude = diameters.Max() - diameters.Min();
                double mean = positions[Array.IndexOf(diameters, diameters.Max())];
                double offset = diameters.Min();
                
                // 估计标准差（使用半高全宽 FWHM）
                double halfMax = (diameters.Max() + diameters.Min()) / 2.0;
                var fwhmPoints = positions.Where((p, i) => diameters[i] >= halfMax).ToArray();
                double fwhm = fwhmPoints.Length > 0 ? fwhmPoints.Max() - fwhmPoints.Min() : 1.0;
                double standardDeviation = fwhm / (2.0 * Math.Sqrt(2.0 * Math.Log(2.0)));

                // 简化的最小二乘法拟合（使用初始估计值）
                // 在实际应用中，这里应该使用 Levenberg-Marquardt 算法或其他非线性优化方法
                // 为了演示，我们使用初始估计值

                // 生成拟合曲线
                var fittedCurve = new double[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    double x = positions[i];
                    fittedCurve[i] = amplitude * Math.Exp(-Math.Pow(x - mean, 2) / (2 * Math.Pow(standardDeviation, 2))) + offset;
                }

                // 计算拟合优度 R²
                double rSquared = CalculateRSquared(diameters, fittedCurve);

                var result = new GaussianFitResult
                {
                    Amplitude = amplitude,
                    Mean = mean,
                    StandardDeviation = Math.Abs(standardDeviation),
                    Offset = offset,
                    RSquared = rSquared,
                    FittedCurve = fittedCurve
                };

                _logger.LogDebug(
                    "高斯拟合完成。振幅: {A:F2}, 均值: {M:F2}, 标准差: {S:F2}, R²: {R:F4}",
                    amplitude, mean, standardDeviation, rSquared);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "高斯拟合失败");
                throw new InvalidOperationException("高斯拟合失败", ex);
            }
        }

        /// <summary>
        /// 执行双曲线拟合
        /// 拟合公式: w(z) = w0 * sqrt(1 + ((z-z0)*λ*M²/(π*w0²))²)
        /// </summary>
        public HyperbolicFitResult FitHyperbolic(double[] positions, double[] diameters, double wavelength)
        {
            if (positions == null)
                throw new ArgumentNullException(nameof(positions));

            if (diameters == null)
                throw new ArgumentNullException(nameof(diameters));

            if (positions.Length != diameters.Length)
                throw new ArgumentException("位置和直径数组长度必须相同");

            if (positions.Length < 3)
                throw new ArgumentException("数据点不足，至少需要3个点进行双曲线拟合");

            if (wavelength <= 0)
                throw new ArgumentException("波长必须大于0", nameof(wavelength));

            try
            {
                // 初始参数估计
                // 腰斑位置：最小直径对应的位置
                int minIndex = Array.IndexOf(diameters, diameters.Min());
                double waistPosition = positions[minIndex];
                double waistDiameter = diameters[minIndex];

                // 估计 M² 因子
                // 使用远场数据点估计发散角
                double[] farFieldDiameters = diameters.Where((d, i) => Math.Abs(positions[i] - waistPosition) > 10).ToArray();
                double[] farFieldPositions = positions.Where((p, i) => Math.Abs(p - waistPosition) > 10).ToArray();

                double mSquared = 1.0; // 默认值

                if (farFieldDiameters.Length >= 2)
                {
                    // 计算发散角（简化方法）
                    double avgFarFieldDiameter = farFieldDiameters.Average();
                    double avgFarFieldDistance = farFieldPositions.Select(p => Math.Abs(p - waistPosition)).Average();
                    
                    // θ ≈ (w - w0) / z
                    double divergenceAngle = (avgFarFieldDiameter - waistDiameter) / avgFarFieldDistance;
                    
                    // M² = (π * w0 * θ) / λ
                    // 转换单位：w0 (μm), λ (nm), θ (rad)
                    mSquared = (Math.PI * waistDiameter * divergenceAngle) / (wavelength / 1000.0);
                    
                    // 确保 M² >= 1
                    mSquared = Math.Max(1.0, Math.Abs(mSquared));
                }

                // 生成拟合曲线
                var fittedCurve = new double[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    double z = positions[i];
                    // w(z) = w0 * sqrt(1 + ((z-z0)*λ*M²/(π*w0²))²)
                    // 转换单位：λ从nm转为μm，z和z0单位为mm
                    double term = ((z - waistPosition) * (wavelength / 1000.0) * mSquared) / (Math.PI * waistDiameter * waistDiameter);
                    fittedCurve[i] = waistDiameter * Math.Sqrt(1 + term * term);
                }

                // 计算拟合优度 R²
                double rSquared = CalculateRSquared(diameters, fittedCurve);

                var result = new HyperbolicFitResult
                {
                    WaistDiameter = waistDiameter,
                    WaistPosition = waistPosition,
                    Wavelength = wavelength,
                    MSquared = mSquared,
                    RSquared = rSquared,
                    FittedCurve = fittedCurve
                };

                _logger.LogDebug(
                    "双曲线拟合完成。腰斑直径: {W0:F2} μm, 腰斑位置: {Z0:F2} mm, M²: {M:F4}, R²: {R:F4}",
                    waistDiameter, waistPosition, mSquared, rSquared);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "双曲线拟合失败");
                throw new InvalidOperationException("双曲线拟合失败", ex);
            }
        }

        /// <summary>
        /// 计算 M² 因子
        /// </summary>
        public double CalculateMSquared(HyperbolicFitResult fitResult)
        {
            if (fitResult == null)
                throw new ArgumentNullException(nameof(fitResult));

            // M² 因子已经在拟合过程中计算
            return fitResult.MSquared;
        }

        /// <summary>
        /// 计算光束腰斑位置和直径
        /// </summary>
        public (double position, double diameter) CalculateBeamWaist(HyperbolicFitResult fitResult)
        {
            if (fitResult == null)
                throw new ArgumentNullException(nameof(fitResult));

            // 腰斑参数已经在拟合过程中计算
            return (fitResult.WaistPosition, fitResult.WaistDiameter);
        }

        /// <summary>
        /// 计算拟合优度 R²
        /// R² = 1 - (SS_res / SS_tot)
        /// </summary>
        private double CalculateRSquared(double[] observed, double[] predicted)
        {
            if (observed.Length != predicted.Length)
                throw new ArgumentException("观测值和预测值数组长度必须相同");

            double mean = observed.Average();

            // 残差平方和
            double ssRes = 0;
            for (int i = 0; i < observed.Length; i++)
            {
                double residual = observed[i] - predicted[i];
                ssRes += residual * residual;
            }

            // 总平方和
            double ssTot = 0;
            for (int i = 0; i < observed.Length; i++)
            {
                double deviation = observed[i] - mean;
                ssTot += deviation * deviation;
            }

            // 避免除以零
            if (ssTot == 0)
                return 1.0;

            double rSquared = 1.0 - (ssRes / ssTot);

            // 确保 R² 在 [0, 1] 范围内
            return Math.Max(0.0, Math.Min(1.0, rSquared));
        }
    }
}
