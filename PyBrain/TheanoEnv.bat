REM configuration of paths
REM set VSFORPYTHON="C:\Program Files (x86)\Common Files\Microsoft\Visual C++ for Python\9.0"
set VSFORPYTHON="C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC"
set SCISOFT=%~dp0

REM add tdm gcc stuff
set PATH=C:\Users\Or\AppData\Local\SciSoft\TDM_Gcc64\bin;C:\Users\Or\AppData\Local\SciSoft\TDM_Gcc64\x86_64-w64-mingw32\bin;%PATH%

REM add winpython stuff
REM CALL C:\Users\Or\AppData\Local\SciSoft\WinPython-64bit-3.5.1.2\scripts\env.bat
CALL C:\Users\Or\AppData\Local\SciSoft\WinPython-64bit-2.7.10.3\scripts\env.bat

REM configure path for msvc compilers
REM for a 32 bit installation change this line to
REM CALL %VSFORPYTHON%\vcvarsall.bat
CALL %VSFORPYTHON%\vcvarsall.bat amd64
python ..\\..\\..\\..\\PyBrain\\serverLSTM.py %1 %2
REM return a shell
cmd.exe /k
