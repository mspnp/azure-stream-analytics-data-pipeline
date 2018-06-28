FROM microsoft/dotnet:2.1-sdk AS build
RUN apt-get update
RUN apt-get install -y git
RUN git clone --recursive https://github.com/mspnp/reference-architectures.git && cd reference-architectures && git checkout asa_ra_develop
WORKDIR reference-architectures/data/streaming_asa/onperm/dotnetcorehelloworld
RUN dotnet build
RUN dotnet publish -f netcoreapp2.1 -c Release

FROM microsoft/dotnet:2.1-runtime AS runtime
WORKDIR dotnetcorehelloworld
COPY --from=build reference-architectures/data/streaming_asa/onperm/dotnetcorehelloworld/bin/Release/netcoreapp2.1/publish .
ENTRYPOINT ["dotnet" ,"dotnetcorehelloworld.dll"]
