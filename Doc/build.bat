set base=..\Bin
set dest=%base%\publish

mkdir %dest%

copy %base%\clover\Win32\clover.exe %dest%\clover.exe /y
copy %base%\clover\x64\clover.exe %dest%\clover64.exe /y
copy %base%\net20\clover.exe %dest%\clover20.exe /y
copy %base%\net40\clover.exe %dest%\clover40.exe /y
copy %base%\net45\clover.exe %dest%\clover45.exe /y
copy %base%\publish-windows\clover.exe %dest%\clover70.exe /y
copy %base%\publish-linux64\clover %dest%\clover /y
