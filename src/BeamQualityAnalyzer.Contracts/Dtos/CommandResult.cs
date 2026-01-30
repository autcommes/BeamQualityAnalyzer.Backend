namespace BeamQualityAnalyzer.Contracts.Dtos;

/// <summary>
/// 命令执行结果
/// </summary>
public class CommandResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 错误详情（仅在失败时）
    /// </summary>
    public string? ErrorDetails { get; set; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static CommandResult SuccessResult(string message = "操作成功")
    {
        return new CommandResult
        {
            Success = true,
            Message = message
        };
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static CommandResult FailureResult(string message, string? errorDetails = null)
    {
        return new CommandResult
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails
        };
    }
}

/// <summary>
/// 带返回值的命令执行结果
/// </summary>
public class CommandResult<T> : CommandResult
{
    /// <summary>
    /// 返回数据
    /// </summary>
    public T? Data { get; set; }
    
    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static CommandResult<T> SuccessResult(T data, string message = "操作成功")
    {
        return new CommandResult<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    public new static CommandResult<T> FailureResult(string message, string? errorDetails = null)
    {
        return new CommandResult<T>
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails
        };
    }
}
