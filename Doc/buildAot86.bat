set base=..\Bin
set dest=%base%\publish

pushd ..\LuckyClover
dotnet publish -c Release -f net9.0-windows -r win-x86 --self-contained
popd

mkdir %dest%

copy %base%\net9.0-windows\win-x86\publish\clover.exe %dest%\clover90-x86.exe /y
