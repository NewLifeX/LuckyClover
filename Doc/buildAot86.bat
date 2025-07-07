set base=..\Bin
set dest=%base%\publish

mkdir %dest%

pushd ..\LuckyClover
dotnet publish -c Release -f net9.0-windows -r win-x86 --self-contained
popd

copy %base%\net9.0-windows\win-x86\publish\clover.exe %dest%\clover90-x86.exe /y

pushd ..\LuckyAOT
dotnet publish -c Release -f net9.0-windows -r win-x86 --self-contained
popd

copy %base%\net9.0-windows\win-x86\publish\cloverAot.exe %dest%\cloverAot-x86.exe /y
