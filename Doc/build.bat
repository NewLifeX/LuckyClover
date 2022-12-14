set base=..\Bin
set dest=%base%\publish

mkdir %dest%

xcopy %base%\net20\clover.exe %dest%\clover20.exe /y
xcopy %base%\net40\clover.exe %dest%\clover40.exe /y
xcopy %base%\net45\clover.exe %dest%\clover45.exe /y
xcopy %base%\publish-windows\clover.exe %dest%\clover70.exe /y
xcopy %base%\publish-linux64\clover %dest%\clover /y
