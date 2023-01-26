#! busybox bash
cwd=`pwd`
ts=`date "+%Y.%m%d.%H%M.%S"`
version="${ts}"
sed -i -e "s/<AssemblyName>.*<\/AssemblyName>/<AssemblyName>Ultimate.Language.Jint<\/AssemblyName>/g" Jint.csproj
sed -i -e "s/<PackageId>.*<\/PackageId>/<PackageId>Ultimate.Language.Jint<\/PackageId>/g" Jint.csproj
sed -i -e "s/<Version>.*<\/Version>/<Version>${version}<\/Version>/g" Jint.csproj
rm -rf obj bin
dotnet build -c Release
ls -ltr $USERPROFILE/.nuget/packages/ultimate.language.esprima/2023.125.2257.31/lib/net462/Ultimate.Language.Esprima.dll
ls -ltr $USERPROFILE/.nuget/packages/ultimate.language.esprima/2023.125.2257.31/lib/netstandard2.0/Ultimate.Language.Esprima.dll
ls -ltr $USERPROFILE/.nuget/packages/ultimate.language.esprima/2023.125.2257.31/lib/netstandard2.1/Ultimate.Language.Esprima.dll
#dotnet pack -p:Configuration=Release -p:Platform=AnyCPU
#rm -rf *.nupkg
#cp -rpv bin/Release/*.nupkg .
