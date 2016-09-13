@echo off
"C:\Program Files\Autodesk\Autodesk AutoCAD Map 3D 2014\accoreconsole.exe" /i "C:\Test\Plot\Plot01\Scripts\Empty.dwg" /s "C:\Test\Plot\Plot01\Scripts\test.scr" /id 184128H /r 500 /z Est /e "xx,yy,zz" /f "C:\Test\Plot\Plot01\Scripts\dump2.pdf" /isolate
echo >>> errorlevel is %errorlevel%
pause
if errorlevel 1 (
   echo Failure Reason Given is %errorlevel%
   exit /b %errorlevel%
)
