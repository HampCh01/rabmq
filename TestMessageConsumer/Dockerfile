# Dockerfile for C# Consumer
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app
COPY out .

ENTRYPOINT ["dotnet", "TestMessageConsumer.dll"]