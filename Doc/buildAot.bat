set base=..\Bin
set dest=%base%\publish

pushd ..\LuckyClover
dotnet publish -c Release -f net9.0 -r win-x64 --self-contained
popd

mkdir %dest%

copy %base%\net9.0\win-x64\publish\clover.exe %dest%\clover90.exe /y
