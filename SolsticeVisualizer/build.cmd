@echo off
SET OPENTK=D:\Software\OpenTK\1.0\Binaries\OpenTK\Release\OpenTK.dll
mkdir bin\Release\
%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe /out:bin\Release\MainWindow.exe *.cs Properties\*.cs /r:%OPENTK% /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll
copy %OPENTK% bin\Release\
copy "Solstice (U).nes" bin\Release\
copy "Solstice (U).pal" bin\Release\
xcopy /s /y /q Textures bin\Release\Textures\