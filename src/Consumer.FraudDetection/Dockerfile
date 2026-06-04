FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/Shared/ src/Shared/
COPY src/Consumer.FraudDetection/ src/Consumer.FraudDetection/
RUN dotnet publish src/Consumer.FraudDetection/Consumer.FraudDetection.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "Consumer.FraudDetection.dll"]
