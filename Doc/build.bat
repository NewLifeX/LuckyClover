set base=..\Bin
set dest=%base%\publish

mkdir %dest%

copy %base%\clover\Win32\clover.exe %dest%\clover.exe /y
copy %base%\clover\x64\clover.exe %dest%\clover64.exe /y
copy %base%\net20\clover.exe %dest%\clover20.exe /y
copy %base%\net40\clover.exe %dest%\clover40.exe /y
copy %base%\net45\clover.exe %dest%\clover45.exe /y
copy %base%\publish-windows\clover.exe %dest%\clover80.exe /y
copy %base%\publish-linux64\clover %dest%\clover /y

del %dest%\LuckyClover45.zip /f
del %dest%\LuckyClover.zip /f
del %dest%\LuckyClover2.zip /f

%base%\net8.0\clover.exe zip %dest%\LuckyClover45.zip %base%\Installer45\*.exe %base%\Installer45\*.exe.config %base%\Installer45\*.dll
%base%\net8.0\clover.exe zip %dest%\LuckyClover.zip %base%\Installer4\*.exe %base%\Installer4\*.exe.config %base%\Installer4\*.dll
%base%\net8.0\clover.exe zip %dest%\LuckyClover2.zip %base%\Installer\*.exe %base%\Installer\*.exe.config %base%\Installer\*.dll
