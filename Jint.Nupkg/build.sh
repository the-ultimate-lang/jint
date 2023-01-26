#! busybox bash
set -uvx
set -e
cwd=`pwd`
ts=`date "+%Y.%m%d.%H%M.%S"`
version="${ts}"

rm -rf $cwd/*.nupkg

sed -i -e "s/<AssemblyName>.*<\/AssemblyName>/<AssemblyName>Ultimate.Language.Jint<\/AssemblyName>/g" ../Jint/Jint.csproj
sed -i -e "s/<PackageId>.*<\/PackageId>/<PackageId>Ultimate.Language.Jint<\/PackageId>/g" ../Jint/Jint.csproj
sed -i -e "s/<Version>.*<\/Version>/<Version>${version}<\/Version>/g" ../Jint/Jint.csproj

rm -rf ../Jint/obj ../Jint/bin
rm -rf obj bin

dotnet build -c Release

cd $cwd/bin/Release/net462
rm -rf nupkg.dll
mkdir tmp
mv Ultimate.Language.Jint.dll tmp/
mkdir -p $cwd/nupkg/lib/net462
ilmerge -internalize -wildcards -out:$cwd/nupkg/lib/net462/Ultimate.Language.Jint.dll tmp/Ultimate.Language.Jint.dll ./*.dll

cd $cwd

sed -i -e "s/<version>.*<\/version>/<version>${version}<\/version>/g" $cwd/nupkg/Ultimate.Language.Jint.nuspec

cd $cwd/nupkg
7z a -tzip -r $cwd/Ultimate.Language.Jint.${version}.nupkg *

cd $cwd
ls -ltr *.nupkg
