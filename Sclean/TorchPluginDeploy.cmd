@REM exit 0
REM exit 0

set NET48_DIR=bin\Release\net48\
set TORCH_PLUGIN_DIR=G:\torch-scrapyard\Plugins\Sclean

robocopy.exe %NET48_DIR%\ %TORCH_PLUGIN_DIR%\ "*.*" /mir

echo Done