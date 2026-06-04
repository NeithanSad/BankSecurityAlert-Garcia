FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/Shared/ src/Shared/
COPY src/Consumer.AuditLog/ src/Consumer.AuditLog/
RUN dotnet publish src/Consumer.AuditLog/Consumer.AuditLog.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
ENV RABBITMQ_HOST=rabbitmq
ENV DB_PATH=/data/audit.db
EXPOSE 8080
ENTRYPOINT ["dotnet", "Consumer.AuditLog.dll"]
