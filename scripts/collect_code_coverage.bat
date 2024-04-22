@echo off
setlocal

REM Set the path where the reports will be saved
set REPORT_DIR=../../CoverageReports

REM Execute the tests and collect code coverage
cd ..
cd src/
cd Monkify.Tests/

dotnet test --collect:"XPlat Code Coverage" --results-directory %REPORT_DIR%

REM Generate the HTML report using ReportGenerator
reportgenerator "-reports:%REPORT_DIR%/**/coverage.cobertura.xml" "-targetdir:%REPORT_DIR%/html" "-reporttypes:HTML;"

REM Open the HTML report in the default browser
start %REPORT_DIR%/html/index.htm

endlocal