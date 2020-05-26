FROM microsoft/dotnet:2.1-sdk AS build
RUN apt-get update
RUN apt-get install -y git
RUN git clone --recursive https://github.com/mspnp/azure-stream-analytics-data-pipeline.git  &&  cd azure-stream-analytics-data-pipeline && git fetch && git checkout master
WORKDIR azure-stream-analytics-data-pipeline/onprem/DataLoader
RUN dotnet build
RUN dotnet publish -f netcoreapp2.0 -c Release


FROM microsoft/dotnet:2.1-runtime AS runtime
WORKDIR DataLoader
COPY --from=build azure-stream-analytics-data-pipeline/onprem/DataLoader/bin/Release/netcoreapp2.0/publish .
ENTRYPOINT ["dotnet" , "taxi.dll"]
