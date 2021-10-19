rem start of the script
@echo off

for /d /r . %%d in (bin,obj,packages) do @if exist "%%d" rd /s/q "%%d"
FOR /R %%H IN (*.log) DO del "%%H"
FOR /r %%G IN (*.bak) DO del "%%G"
FOR /R %%J IN (*.suo) DO del "%%J"

IF EXIST .vs rd .vs /s/q
rem end of the script