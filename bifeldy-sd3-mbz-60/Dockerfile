FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
LABEL maintainer="Bias"
EXPOSE 80
WORKDIR /app
COPY . .
RUN chmod +x /app
CMD ["dotnet", "/app/DataDc.dll"]