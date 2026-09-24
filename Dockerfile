FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["dmp.job.invoiceworker/Job.InvoiceWorker.csproj", "dmp.job.invoiceworker/"]
COPY ["DMP.BL/DMP.BL.csproj", "DMP.BL/"]
COPY ["DMP.DataAccess/DMP.DataAccess.csproj", "DMP.DataAccess/"]
RUN dotnet restore "dmp.job.invoiceworker/Job.InvoiceWorker.csproj"
COPY . .
RUN dotnet publish "dmp.job.invoiceworker/Job.InvoiceWorker.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Job.InvoiceWorker.dll"]
