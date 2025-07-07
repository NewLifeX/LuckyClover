set base=..\Bin
set dest=%base%\publish

mkdir %dest%

pushd ..\LuckyClover
dotnet publish -c Release -f net9.0-windows -r win-x64 --self-contained
popd

copy %base%\net9.0-windows\win-x64\publish\clover.exe %dest%\clover90.exe /y

pushd ..\LuckyAOT
dotnet publish -c Release -f net9.0-windows -r win-x64 --self-contained
popd

copy %base%\net9.0-windows\win-x64\publish\cloverAot.exe %dest%\cloverAot.exe /y
