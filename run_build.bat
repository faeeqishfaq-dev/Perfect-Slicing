@echo off
if exist "Temp\UnityLockfile" del /f /q "Temp\UnityLockfile"
"C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe" -batchmode -projectPath "D:\Perfect Slicing" -executeMethod FoodSlicer.EditorTools.FoodSlicerBuilder.BuildCompleteGame -quit -logFile "D:\Perfect Slicing\build_game.log"
echo Build Finished with code %ERRORLEVEL%
