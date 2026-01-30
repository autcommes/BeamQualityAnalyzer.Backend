# 使用 .NET 9 SDK 作为构建镜像
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制解决方案和项目文件
COPY ["BeamQualityAnalyzer.Backend.sln", "./"]
COPY ["src/BeamQualityAnalyzer.Server/BeamQualityAnalyzer.Server.csproj", "src/BeamQualityAnalyzer.Server/"]
COPY ["src/BeamQualityAnalyzer.Core/BeamQualityAnalyzer.Core.csproj", "src/BeamQualityAnalyzer.Core/"]
COPY ["src/BeamQualityAnalyzer.Data/BeamQualityAnalyzer.Data.csproj", "src/BeamQualityAnalyzer.Data/"]
COPY ["src/BeamQualityAnalyzer.Contracts/BeamQualityAnalyzer.Contracts.csproj", "src/BeamQualityAnalyzer.Contracts/"]

# 还原依赖
RUN dotnet restore "src/BeamQualityAnalyzer.Server/BeamQualityAnalyzer.Server.csproj"

# 复制所有源代码
COPY . .

# 构建项目
WORKDIR "/src/src/BeamQualityAnalyzer.Server"
RUN dotnet build "BeamQualityAnalyzer.Server.csproj" -c Release -o /app/build

# 发布项目
FROM build AS publish
RUN dotnet publish "BeamQualityAnalyzer.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 使用 .NET 9 运行时作为最终镜像
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# 创建日志目录
RUN mkdir -p /app/logs

# 复制发布的文件
COPY --from=publish /app/publish .

# 暴露端口
EXPOSE 5000

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# 启动应用
ENTRYPOINT ["dotnet", "BeamQualityAnalyzer.Server.dll"]
