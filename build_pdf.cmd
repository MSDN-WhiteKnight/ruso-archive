"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe" tools\src\RuSoTools.sln
cd tools\src\ArchiveLoader\bin\Debug\
ArchiveLoader.exe generate
cd ..\..\..\..\..\
%DOCFX_PATH%\docfx.exe .\docfx_pdf.json
pause