# Set the path where the reports will be saved
$REPORT_DIR = "../../CoverageReports"

# Execute the tests and collect code coverage
Set-Location ..\src\Monkify.Tests\

dotnet test --collect:"XPlat Code Coverage" --results-directory $REPORT_DIR

# Generate the HTML report using ReportGenerator
reportgenerator "-reports:$REPORT_DIR/**/coverage.cobertura.xml" "-targetdir:$REPORT_DIR/html" "-reporttypes:HTML;"

# Open the HTML report in the default browser
Start-Process "$REPORT_DIR/html/index.htm"