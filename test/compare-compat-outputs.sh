#!/bin/bash

# Test script to compare UXUnit and XUnit Compat project outputs
# This ensures both projects produce the same test results
#
# Usage: ./compare-compat-outputs.sh [baseline|compare|help]
#
# This script validates that UXUnit compatibility works correctly by comparing
# test outputs with XUnit. Both projects use the same shared test files but
# different test frameworks.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UXUNIT_DIR="$SCRIPT_DIR/Assets/UXUnitCompat"
XUNIT_DIR="$SCRIPT_DIR/Assets/XUnitCompat"
OUTPUT_DIR="$SCRIPT_DIR/../artifacts/.comparison-outputs"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Create output directory (hidden to avoid committing)
mkdir -p "$OUTPUT_DIR"

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to extract test results from dotnet test output
extract_test_results() {
    local input_file="$1"
    local output_file="$2"

    # Extract test names and results, normalize timing information
    grep -E "(Passed|Failed|Skipped)" "$input_file" | \
    sed -E 's/\[[0-9]+ ms\]/[X ms]/g' | \
    sed -E 's/\[< 1 ms\]/[X ms]/g' | \
    sed -E 's/\[[0-9]+ s\]/[X s]/g' | \
    sort > "$output_file"
}

# Function to extract test summary from dotnet test output
extract_test_summary() {
    local input_file="$1"
    local output_file="$2"

    # Extract the summary line and normalize timing
    grep -E "(Total tests:|Test Run|Total time:|duration:)" "$input_file" | \
    sed -E 's/[0-9]+\.[0-9]+[ms]* (Seconds|s)/X.X s/g' | \
    sed -E 's/duration: [0-9]+\.[0-9]+s/duration: X.X s/g' > "$output_file"
}

# Function to run tests and capture output
run_tests() {
    local project_dir="$1"
    local project_name="$2"
    local output_file="$3"

    log_info "Running tests for $project_name..."

    cd "$project_dir"

    # Try to build first
    if ! dotnet build --verbosity quiet > "$OUTPUT_DIR/${project_name}_build.log" 2>&1; then
        log_warning "$project_name failed to build. Check build log for details."
        echo "BUILD_FAILED" > "$output_file"
        return 1
    fi

    # Run tests with detailed output
    if ! dotnet test --no-build --verbosity normal --logger "console;verbosity=detailed" > "$output_file" 2>&1; then
        log_warning "$project_name tests failed to run. Check output for details."
        echo "TEST_RUN_FAILED" >> "$output_file"
        return 1
    fi

    return 0
}

# Main comparison function
compare_outputs() {
    log_info "Starting test output comparison between UXUnit and XUnit compatibility projects..."
    echo "This validates that both frameworks produce identical test results from shared test code."
    echo ""

    # Run XUnit tests (baseline)
    if run_tests "$XUNIT_DIR" "XUnit" "$OUTPUT_DIR/xunit_raw.txt"; then
        extract_test_results "$OUTPUT_DIR/xunit_raw.txt" "$OUTPUT_DIR/xunit_results.txt"
        extract_test_summary "$OUTPUT_DIR/xunit_raw.txt" "$OUTPUT_DIR/xunit_summary.txt"
        log_success "XUnit compatibility tests completed successfully"
    else
        log_error "XUnit compatibility tests failed - this is unexpected!"
        return 1
    fi

    # Run UXUnit tests
    if run_tests "$UXUNIT_DIR" "UXUnit" "$OUTPUT_DIR/uxunit_raw.txt"; then
        extract_test_results "$OUTPUT_DIR/uxunit_raw.txt" "$OUTPUT_DIR/uxunit_results.txt"
        extract_test_summary "$OUTPUT_DIR/uxunit_raw.txt" "$OUTPUT_DIR/uxunit_summary.txt"
        log_success "UXUnit compatibility tests completed successfully"
    else
        log_error "UXUnit compatibility tests failed"

        # Check if it's a build failure (generator not implemented)
        if grep -q "BUILD_FAILED\|CS5001.*Main.*entry point" "$OUTPUT_DIR/uxunit_raw.txt" 2>/dev/null || \
           grep -q "BUILD_FAILED\|CS5001.*Main.*entry point" "$OUTPUT_DIR/UXUnit_build.log" 2>/dev/null; then
            log_warning "UXUnit appears to have generator issues (no Main method)."
            echo -e "\n${YELLOW}This is expected if the UXUnit generator isn't fully implemented yet.${NC}"
            echo ""
            echo "Expected behavior once generator is complete:"
            echo "• UXUnit generator should create test runner code with Main method"
            echo "• Generated code should discover and execute tests marked with UXUnit attributes"
            echo "• Test results should match XUnit output exactly"
            echo ""
            echo "To validate once generator is implemented:"
            echo "  ./compare-compat-outputs.sh compare"
        fi
        return 1
    fi

    # Compare results
    log_info "Comparing test results..."

    if ! cmp -s "$OUTPUT_DIR/xunit_results.txt" "$OUTPUT_DIR/uxunit_results.txt"; then
        log_error "Test results differ between XUnit and UXUnit!"
        echo -e "\n${RED}Differences found:${NC}"
        echo "Expected (XUnit) vs Actual (UXUnit):"
        diff -u "$OUTPUT_DIR/xunit_results.txt" "$OUTPUT_DIR/uxunit_results.txt" || true
        echo ""
        echo "This indicates a compatibility issue that needs to be resolved."
        return 1
    fi

    # Compare summaries (test counts)
    log_info "Comparing test summaries..."

    # Extract just the test counts for comparison
    xunit_count=$(grep -o "Total tests: [0-9]*" "$OUTPUT_DIR/xunit_summary.txt" 2>/dev/null || echo "Total tests: 0")
    uxunit_count=$(grep -o "Total tests: [0-9]*" "$OUTPUT_DIR/uxunit_summary.txt" 2>/dev/null || echo "Total tests: 0")

    if [ "$xunit_count" != "$uxunit_count" ]; then
        log_error "Test counts differ! XUnit: $xunit_count, UXUnit: $uxunit_count"
        return 1
    fi

    log_success "🎉 Compatibility validation successful!"

    echo -e "\n${GREEN}Results Summary:${NC}"
    echo "✅ Test results are identical between XUnit and UXUnit"
    echo "✅ Test counts match: $xunit_count"
    echo "✅ All individual test outcomes are the same"
    echo "✅ Framework compatibility is working correctly"

    echo -e "\n${BLUE}This confirms that:${NC}"
    echo "• UXUnit can execute the same test code as XUnit"
    echo "• Both frameworks produce identical results"
    echo "• The compatibility layer is working as expected"
    echo "• Shared test attributes and assertions work correctly"

    return 0
}

# Function to show current XUnit baseline
show_xunit_baseline() {
    log_info "Showing XUnit baseline results (target for UXUnit compatibility)..."
    echo "This shows what UXUnit should produce when fully implemented."
    echo ""

    if run_tests "$XUNIT_DIR" "XUnit" "$OUTPUT_DIR/xunit_baseline.txt"; then
        echo -e "\n${BLUE}XUnit Compatibility Test Results:${NC}"
        extract_test_results "$OUTPUT_DIR/xunit_baseline.txt" "$OUTPUT_DIR/xunit_baseline_results.txt"
        cat "$OUTPUT_DIR/xunit_baseline_results.txt"

        echo -e "\n${BLUE}XUnit Test Summary:${NC}"
        extract_test_summary "$OUTPUT_DIR/xunit_baseline.txt" "$OUTPUT_DIR/xunit_baseline_summary.txt"
        cat "$OUTPUT_DIR/xunit_baseline_summary.txt"

        echo -e "\n${YELLOW}UXUnit Implementation Requirements:${NC}"
        echo "When the UXUnit generator is fully implemented, it should produce:"
        echo ""
        echo "🎯 Same test discovery:"
        echo "   • Find all methods marked with [UXUnit.Test]"
        echo "   • Handle parameterized tests with [UXUnit.TestData]"
        echo "   • Support async test methods"
        echo ""
        echo "🎯 Same test execution:"
        echo "   • Execute setup methods marked with [UXUnit.Setup]"
        echo "   • Run the actual test method"
        echo "   • Execute cleanup methods marked with [UXUnit.Cleanup]"
        echo "   • Handle exceptions and assertions properly"
        echo ""
        echo "🎯 Same result reporting:"
        echo "   • Report test names in the same format"
        echo "   • Use the same pass/fail status"
        echo "   • Provide similar timing information"
        echo "   • Match the total test count and summary"

        # Count the expected tests by type
        local basic_tests=$(grep -c "BasicTestsCompatibility" "$OUTPUT_DIR/xunit_baseline_results.txt")
        local async_tests=$(grep -c "AsyncTestsCompatibility" "$OUTPUT_DIR/xunit_baseline_results.txt")
        local total_tests=$(wc -l < "$OUTPUT_DIR/xunit_baseline_results.txt")

        echo -e "\n${BLUE}Test Breakdown:${NC}"
        echo "• Basic synchronous tests: $basic_tests"
        echo "• Async tests: $async_tests"
        echo "• Total expected tests: $total_tests"

    else
        log_error "Could not establish XUnit baseline"
        return 1
    fi
}

# Function to show help
show_help() {
    echo "UXUnit/XUnit Compatibility Test Comparison Script"
    echo "================================================"
    echo ""
    echo "This script validates that UXUnit and XUnit produce identical test results"
    echo "when running the same shared compatibility test code."
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  compare (default)  - Compare outputs of both UXUnit and XUnit projects"
    echo "  baseline          - Show XUnit baseline (what UXUnit should match)"
    echo "  help             - Show this help message"
    echo ""
    echo "Project Structure:"
    echo "  UXUnitCompat/     - Project using UXUnit framework"
    echo "  XUnitCompat/      - Project using XUnit framework"
    echo "  shared/           - Shared test code using conditional compilation"
    echo ""
    echo "The shared test code uses preprocessor directives (#if UXUNIT) to"
    echo "use the appropriate test attributes for each framework while keeping"
    echo "the same test logic and assertions."
    echo ""
    echo "Expected workflow:"
    echo "1. Run 'baseline' to see what UXUnit should produce"
    echo "2. Implement UXUnit generator to handle test discovery/execution"
    echo "3. Run 'compare' to validate both frameworks match"
}

# Parse command line arguments
case "${1:-}" in
    "baseline"|"--baseline"|"-b")
        show_xunit_baseline
        ;;
    "compare"|"--compare"|"-c"|"")
        compare_outputs
        ;;
    "help"|"--help"|"-h")
        show_help
        ;;
    *)
        log_error "Unknown command: $1"
        echo "Use '$0 help' for usage information"
        exit 1
        ;;
esac